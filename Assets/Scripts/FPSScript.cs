using Unity.Netcode;
using UnityEngine;

public class FPSPlayerController : NetworkBehaviour
{
    [Header("Camera Settings")]
    [SerializeField] private float mouseSensitivity = 100f;
    [SerializeField] private Transform playerCamera;
    [SerializeField] private float cameraDistance = 5f;

    [Header("Shooting")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private float fireRate = 0.15f;
    [SerializeField] private float projectileSpeed = 30f;
    [SerializeField] private Transform shootPoint;

    [Header("Crosshair")]
    [SerializeField] private GameObject crosshairPrefab;

    private float xRotation = 0f;
    private float nextFireTime = 0f;
    private Camera cam;
    private GameObject crosshairInstance;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsOwner)
        {
            // Setup camera
            SetupCamera();

            // Setup crosshair
            if (crosshairPrefab != null)
            {
                crosshairInstance = Instantiate(crosshairPrefab);
                crosshairInstance.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
                DontDestroyOnLoad(crosshairInstance);
            }

            // Lock cursor
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void SetupCamera()
    {
        // If no camera assigned, find or create one
        if (playerCamera == null)
        {
            // Try to find existing camera
            cam = GetComponentInChildren<Camera>();
            if (cam == null)
            {
                // Create new camera
                GameObject cameraObj = new GameObject("PlayerCamera");
                cameraObj.transform.SetParent(transform);
                cam = cameraObj.AddComponent<Camera>();
                cam.tag = "MainCamera";

                // Position camera above player (FPS view)
                cameraObj.transform.localPosition = new Vector3(0, 2f, 0);
                cameraObj.transform.localRotation = Quaternion.identity;
            }
            else
            {
                cam = GetComponentInChildren<Camera>();
            }

            playerCamera = cam.transform;
        }
        else
        {
            cam = playerCamera.GetComponent<Camera>();
            if (cam != null)
            {
                cam.tag = "MainCamera";
            }
        }

        // Disable other cameras
        Camera[] allCameras = FindObjectsByType<Camera>(FindObjectsSortMode.None);
        foreach (Camera c in allCameras)
        {
            if (c != cam)
            {
                c.enabled = false;
            }
        }
    }

    void Update()
    {
        if (!IsOwner) return;

        HandleMouseLook();
        HandleShooting();
        HandleCursorLock();
    }

    private void HandleMouseLook()
    {
        // Get mouse input
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        // Rotate player horizontally (Y axis)
        transform.Rotate(Vector3.up * mouseX);

        // Rotate camera vertically (X axis) - limited to prevent flipping
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

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
            ShootServerRpc();
        }
    }

    [ServerRpc]
    private void ShootServerRpc()
    {
        if (shootPoint == null) return;

        // Spawn projectile on server
        GameObject projectile = Instantiate(
            projectilePrefab,
            shootPoint.position,
            shootPoint.rotation
        );

        NetworkObject netObj = projectile.GetComponent<NetworkObject>();
        netObj.Spawn();

        Rigidbody rb = projectile.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = shootPoint.forward * projectileSpeed;
        }
    }

    private void HandleCursorLock()
    {
        // Press Escape to unlock cursor
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        // Click to lock cursor again
        if (Input.GetMouseButtonDown(0) && Cursor.lockState == CursorLockMode.None)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        // Clean up crosshair
        if (crosshairInstance != null)
        {
            Destroy(crosshairInstance);
        }

        // Restore cursor
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}