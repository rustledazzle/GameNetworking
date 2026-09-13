using Mirror;
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

    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();
        SetupCamera();

        if (crosshairPrefab != null)
        {
            crosshairInstance = Instantiate(crosshairPrefab);
            crosshairInstance.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            DontDestroyOnLoad(crosshairInstance);
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        if (!isLocalPlayer)
        {
            Camera[] cams = GetComponentsInChildren<Camera>();
            foreach (Camera c in cams) c.enabled = false;
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
                cameraObj.transform.SetParent(transform);
                cam = cameraObj.AddComponent<Camera>();
                cam.tag = "MainCamera";
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
            if (cam != null) cam.tag = "MainCamera";
        }

        Camera[] allCameras = FindObjectsByType<Camera>(FindObjectsSortMode.None);
        foreach (Camera c in allCameras)
            if (c != cam) c.enabled = false;
    }

    void Update()
    {
        if (!isLocalPlayer) return;
        HandleMouseLook();
        HandleShooting();
        HandleCursorLock();
    }

    private void HandleMouseLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        transform.Rotate(Vector3.up * mouseX);

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        if (playerCamera != null)
            playerCamera.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
    }

    private void HandleShooting()
    {
        if (Input.GetMouseButtonDown(0) && Time.time >= nextFireTime)
        {
            nextFireTime = Time.time + fireRate;
            CmdShoot();
        }
    }

    [Command]
    private void CmdShoot()
    {
        if (shootPoint == null || projectilePrefab == null) return;

        GameObject projectile = Instantiate(projectilePrefab, shootPoint.position, shootPoint.rotation);
        NetworkServer.Spawn(projectile);

        Rigidbody rb = projectile.GetComponent<Rigidbody>();
        if (rb != null)
            rb.velocity = shootPoint.forward * projectileSpeed;
    }

    private void HandleCursorLock()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        if (Input.GetMouseButtonDown(0) && Cursor.lockState == CursorLockMode.None)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    public override void OnStopClient()
    {
        base.OnStopClient();
        if (crosshairInstance != null) Destroy(crosshairInstance);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}