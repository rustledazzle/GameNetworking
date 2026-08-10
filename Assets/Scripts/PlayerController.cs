using Unity.Netcode;
using UnityEngine;

public class PlayerController : NetworkBehaviour
{
    [Header("Mouse Look")]
    [SerializeField] private float mouseSensitivity = 1.5f;
    [SerializeField] private float maxLookAngle = 80f;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float sprintMultiplier = 1.5f;

    [Header("Camera")]
    [SerializeField] private Transform playerCamera;
    [SerializeField] private Vector3 cameraOffset = new Vector3(0, 1.7f, 0);

    [Header("Shooting")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private float fireRate = 0.15f;
    [SerializeField] private float projectileSpeed = 30f;
    [SerializeField] private float bulletSpawnOffset = 0.5f; // How far from camera the bullet spawns

    [Header("Chat")]
    [SerializeField] private ChatManager chatManager;

    private float xRotation = 0f;
    private float nextFireTime = 0f;
    private Camera cam;
    private bool isCursorLocked = true;
    private bool isInitialized = false;
    private Transform cachedTransform;

    private Vector3 moveDirection;
    private float currentSpeed;

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

            if (chatManager == null)
            {
                chatManager = FindObjectOfType<ChatManager>();
                if (chatManager != null)
                {
                    Debug.Log($"[PlayerController] ChatManager found automatically!");
                }
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

        if (chatManager != null && chatManager.IsChatActive)
        {
            HandleCursorLock();
            return;
        }

        HandleMovement();
        HandleMouseLook();
        HandleShooting();
        HandleCursorLock();
    }

    private void HandleMovement()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        Vector3 forward = cachedTransform.forward;
        Vector3 right = cachedTransform.right;

        forward.y = 0;
        right.y = 0;
        forward.Normalize();
        right.Normalize();

        moveDirection = (forward * vertical + right * horizontal).normalized;

        bool isSprinting = Input.GetKey(KeyCode.LeftShift);
        currentSpeed = isSprinting ? moveSpeed * sprintMultiplier : moveSpeed;

        if (moveDirection.magnitude > 0.1f)
        {
            cachedTransform.position += moveDirection * currentSpeed * Time.deltaTime;

            if (IsOwner)
            {
                UpdatePositionServerRpc(cachedTransform.position);
            }
        }
    }

    [ServerRpc]
    private void UpdatePositionServerRpc(Vector3 newPosition)
    {
        cachedTransform.position = newPosition;
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
                // Get direction from camera
                Vector3 shootDir = playerCamera.forward;
                // Spawn bullet in front of camera (NOT inside the player!)
                Vector3 spawnPos = playerCamera.position + shootDir * bulletSpawnOffset;

                if (IsServer)
                {
                    ShootServerDirect(spawnPos, shootDir);
                }
                else
                {
                    ClientPredictShoot(spawnPos, shootDir);
                    ShootServerRpc(spawnPos, shootDir);
                }
            }
        }
    }

    private void ShootServerDirect(Vector3 spawnPos, Vector3 direction)
    {
        if (projectilePrefab == null) return;

        Quaternion rotation = Quaternion.LookRotation(direction);
        GameObject projectile = Instantiate(projectilePrefab, spawnPos, rotation);
        NetworkObject netObj = projectile.GetComponent<NetworkObject>();

        if (netObj != null)
        {
            netObj.Spawn();

            Rigidbody rb = projectile.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.velocity = direction * projectileSpeed;
                rb.angularVelocity = Vector3.zero;
            }
        }
    }

    private void ClientPredictShoot(Vector3 spawnPos, Vector3 direction)
    {
        if (projectilePrefab == null) return;

        Quaternion rotation = Quaternion.LookRotation(direction);
        GameObject projectile = Instantiate(projectilePrefab, spawnPos, rotation);

        Rigidbody rb = projectile.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = direction * projectileSpeed;
        }

        Destroy(projectile, 3f);
    }

    [ServerRpc]
    private void ShootServerRpc(Vector3 spawnPos, Vector3 direction)
    {
        if (projectilePrefab == null) return;

        Quaternion rotation = Quaternion.LookRotation(direction);
        GameObject projectile = Instantiate(projectilePrefab, spawnPos, rotation);
        NetworkObject netObj = projectile.GetComponent<NetworkObject>();

        if (netObj != null)
        {
            netObj.Spawn();

            Rigidbody rb = projectile.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.velocity = direction * projectileSpeed;
                rb.angularVelocity = Vector3.zero;
            }
        }
    }

    private void HandleCursorLock()
    {
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