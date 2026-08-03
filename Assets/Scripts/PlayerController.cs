using Unity.Netcode;
using UnityEngine;

public class PlayerController : NetworkBehaviour
{
    [Header("Mouse Look")]
    [SerializeField] private float mouseSensitivity = 1.5f;
    [SerializeField] private float maxLookAngle = 80f;

    [Header("Camera")]
    [SerializeField] private Transform playerCamera;
    [SerializeField] private Vector3 cameraOffset = new Vector3(0, 1.7f, 0);

    [Header("Shooting")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform shootPoint;
    [SerializeField] private float fireRate = 0.15f;
    [SerializeField] private float projectileSpeed = 30f;

    [Header("Chat")]
    [SerializeField] private ChatManager chatManager;

    private float xRotation = 0f;
    private float nextFireTime = 0f;
    private Camera cam;
    private bool isCursorLocked = true;
    private bool isInitialized = false;
    private Transform cachedTransform;

    private void Awake()
    {
        cachedTransform = transform;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsOwner)
        {
            SetupCamera();
            LockCursor(true);
            isInitialized = true;
            enabled = true;

            // Find ChatManager if not assigned
            if (chatManager == null)
            {
                chatManager = FindObjectOfType<ChatManager>();
            }

            Debug.Log($"Player {OwnerClientId} initialized as OWNER");
        }
        else
        {
            enabled = false;

            Camera[] cams = GetComponentsInChildren<Camera>();
            foreach (Camera c in cams)
            {
                c.enabled = false;
            }

            Debug.Log($"Player {OwnerClientId} is NOT owner - disabled");
        }
    }

    private void SetupCamera()
    {
        if (playerCamera == null)
        {
            cam = GetComponentInChildren<Camera>();
            if (cam == null)
            {
                GameObject cameraObj = new GameObject("PlayerCamera");
                cameraObj.transform.SetParent(cachedTransform);
                cameraObj.transform.localPosition = cameraOffset;
                cameraObj.transform.localRotation = Quaternion.identity;
                cam = cameraObj.AddComponent<Camera>();

                if (IsOwner)
                {
                    cameraObj.AddComponent<AudioListener>();
                }

                playerCamera = cameraObj.transform;
            }
            else
            {
                playerCamera = cam.transform;
                cam.transform.localPosition = cameraOffset;
                cam.transform.localRotation = Quaternion.identity;
            }
        }

        if (IsOwner)
        {
            cam.tag = "MainCamera";
            cam.enabled = true;

            Camera[] allCameras = FindObjectsByType<Camera>(FindObjectsSortMode.None);
            foreach (Camera c in allCameras)
            {
                if (c != cam)
                {
                    c.enabled = false;
                    AudioListener listener = c.GetComponent<AudioListener>();
                    if (listener != null) Destroy(listener);
                }
            }
        }
        else
        {
            cam.enabled = false;
        }
    }

    void Update()
    {
        if (!IsOwner || !isInitialized) return;

        // Check if chat is active - if so, don't process game input
        if (chatManager != null && chatManager.IsChatActive)
        {
            // Still allow cursor lock/unlock when chat is active
            HandleCursorLock();
            return;
        }

        HandleMouseLook();
        HandleShooting();
        HandleCursorLock();
    }

    private void HandleMouseLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        cachedTransform.Rotate(Vector3.up * mouseX);

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -maxLookAngle, maxLookAngle);

        if (playerCamera != null)
        {
            playerCamera.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        }
    }

    private void HandleShooting()
    {
        if (Input.GetMouseButtonDown(0) && Time.time >= nextFireTime)
        {
            nextFireTime = Time.time + fireRate;

            if (IsOwner)
            {
                // If we're the HOST (Server + Client), spawn directly on server
                if (IsServer)
                {
                    ShootServerDirect();
                }
                else
                {
                    // Client: Predict bullet locally, then tell server
                    ClientPredictShoot();
                    ShootServerRpc(shootPoint.position, shootPoint.rotation);
                }
            }
        }
    }

    // Host only - spawns bullet directly on server
    private void ShootServerDirect()
    {
        if (shootPoint == null || projectilePrefab == null) return;

        GameObject projectile = Instantiate(projectilePrefab, shootPoint.position, shootPoint.rotation);
        NetworkObject netObj = projectile.GetComponent<NetworkObject>();

        if (netObj != null)
        {
            netObj.Spawn();

            Rigidbody rb = projectile.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.velocity = shootPoint.forward * projectileSpeed;
                rb.angularVelocity = Vector3.zero;
            }
        }
    }

    // Client fires immediately (no delay!)
    private void ClientPredictShoot()
    {
        if (shootPoint == null || projectilePrefab == null) return;

        GameObject projectile = Instantiate(projectilePrefab, shootPoint.position, shootPoint.rotation);

        Rigidbody rb = projectile.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = shootPoint.forward * projectileSpeed;
        }

        Destroy(projectile, 3f);
    }

    [ServerRpc]
    private void ShootServerRpc(Vector3 spawnPos, Quaternion spawnRot)
    {
        if (shootPoint == null || projectilePrefab == null) return;

        GameObject projectile = Instantiate(projectilePrefab, spawnPos, spawnRot);
        NetworkObject netObj = projectile.GetComponent<NetworkObject>();

        if (netObj != null)
        {
            netObj.Spawn();

            Rigidbody rb = projectile.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.velocity = spawnRot * Vector3.forward * projectileSpeed;
                rb.angularVelocity = Vector3.zero;
            }
        }
    }

    private void HandleCursorLock()
    {
        // Skip cursor lock if chat is active
        if (chatManager != null && chatManager.IsChatActive) return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            LockCursor(false);
        }

        if (Input.GetMouseButtonDown(0) && !isCursorLocked)
        {
            LockCursor(true);
        }
    }

    private void LockCursor(bool locked)
    {
        isCursorLocked = locked;

        if (locked)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        LockCursor(false);
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!IsOwner) return;

        if (hasFocus && isCursorLocked)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else if (!hasFocus)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}