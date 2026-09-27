using Mirror;
using UnityEngine;

public class PlayerController : NetworkBehaviour
{
    [Header("Mouse Look")]
    [SerializeField] private float mouseSensitivity = 1.5f;
    [SerializeField] private float maxLookAngle = 80f;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float sprintMultiplier = 1.5f;
    [SerializeField] private float gravity = -9.81f;
    [SerializeField] private float jumpHeight = 1.5f;
    [SerializeField] private KeyCode jumpKey = KeyCode.Space;

    [Header("Camera")]
    [SerializeField] private Transform playerCamera;
    [SerializeField] private Vector3 cameraOffset = new Vector3(0, 1.7f, 0);

    [Header("Shooting")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private float fireRate = 0.15f;
    [SerializeField] private float projectileSpeed = 30f;
    [SerializeField] private float bulletSpawnOffset = 0.5f;
    [SerializeField] private int ammoPerShot = 1;

    [Header("HUD")]
    [SerializeField] private PlayerHUD playerHUD;

    [SyncVar(hook = nameof(OnRotationChanged))]
    private float syncedYRotation;

    private float xRotation = 0f;
    private float nextFireTime = 0f;
    private Camera cam;
    private bool isCursorLocked = true;
    private bool isInitialized = false;
    private Transform cachedTransform;
    private HealthSystem healthSystem;
    private CharacterController characterController;
    private float verticalVelocity = 0f;

    private Vector3 moveDirection;
    private float currentSpeed;

    private void Awake()
    {
        cachedTransform = transform;
        characterController = GetComponent<CharacterController>();
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        syncedYRotation = transform.eulerAngles.y;
    }

    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();

        SetupCamera();
        LockCursor(true);
        isInitialized = true;

        if (playerHUD == null)
            playerHUD = FindObjectOfType<PlayerHUD>();

        healthSystem = GetComponent<HealthSystem>();
        if (healthSystem != null && playerHUD != null)
            playerHUD.UpdateHealthFromNetwork(healthSystem.health);

        Debug.Log($"Player {netId} initialized as LOCAL PLAYER");
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        if (!isLocalPlayer)
        {
            Camera[] cams = GetComponentsInChildren<Camera>(true);
            foreach (Camera c in cams)
            {
                c.enabled = false;
                AudioListener al = c.GetComponent<AudioListener>();
                if (al != null) al.enabled = false;
            }

            if (playerHUD != null)
                playerHUD.gameObject.SetActive(false);
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

                if (isLocalPlayer)
                    cameraObj.AddComponent<AudioListener>();

                playerCamera = cameraObj.transform;
            }
            else
            {
                playerCamera = cam.transform;
                cam.transform.localPosition = cameraOffset;
                cam.transform.localRotation = Quaternion.identity;

                AudioListener existing = cam.GetComponent<AudioListener>();
                if (existing == null && isLocalPlayer)
                    cam.gameObject.AddComponent<AudioListener>();
                else if (existing != null && !isLocalPlayer)
                    existing.enabled = false;
            }
        }

        cam.tag = "MainCamera";
        cam.enabled = true;

        Camera[] allCameras = FindObjectsByType<Camera>(FindObjectsSortMode.None);
        foreach (Camera c in allCameras)
        {
            if (c != cam)
            {
                c.enabled = false;
                AudioListener listener = c.GetComponent<AudioListener>();
                if (listener != null) listener.enabled = false;
            }
        }
    }

    void Update()
    {
        if (!isLocalPlayer || !isInitialized) return;

        if (SimpleChat.Instance != null && SimpleChat.Instance.IsChatOpen)
            return;

        if (PauseMenu.IsPauseMenuOpen)
            return;

        HandleMovement();
        HandleMouseLook();
        HandleShooting();
        HandleReload();
        HandleCursorLock();

        CmdUpdateRotation(cachedTransform.eulerAngles.y);
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

        if (characterController != null)
        {
            if (characterController.isGrounded && verticalVelocity < 0)
                verticalVelocity = -2f;

            if (characterController.isGrounded && Input.GetKeyDown(jumpKey))
            {
                verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }

            verticalVelocity += gravity * Time.deltaTime;

            Vector3 move = moveDirection * currentSpeed;
            move.y = verticalVelocity;

            characterController.Move(move * Time.deltaTime);
            CmdUpdatePosition(cachedTransform.position);
        }
        else
        {
            if (moveDirection.magnitude > 0.1f)
            {
                cachedTransform.position += moveDirection * currentSpeed * Time.deltaTime;
                CmdUpdatePosition(cachedTransform.position);
            }
        }
    }

    [Command]
    private void CmdUpdatePosition(Vector3 newPosition)
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
            playerCamera.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
    }

    private void HandleShooting()
    {
        if (Input.GetMouseButtonDown(0) && Time.time >= nextFireTime)
        {
            if (playerHUD != null && !playerHUD.CanShoot())
            {
                Debug.Log("No ammo! Press R to reload.");
                return;
            }

            nextFireTime = Time.time + fireRate;

            if (playerHUD != null)
                playerHUD.UseAmmo(ammoPerShot);

            Vector3 shootDir = playerCamera.forward;
            Vector3 spawnPos = playerCamera.position + shootDir * bulletSpawnOffset;

            CmdShoot(spawnPos, shootDir);
        }
    }

    private void HandleReload()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            if (playerHUD != null)
                playerHUD.Reload();
        }
    }

    [Command]
    private void CmdShoot(Vector3 spawnPos, Vector3 direction)
    {
        if (projectilePrefab == null) return;

        Quaternion rotation = Quaternion.LookRotation(direction);
        GameObject projectile = Instantiate(projectilePrefab, spawnPos, rotation);
        NetworkServer.Spawn(projectile);

        Rigidbody rb = projectile.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = direction * projectileSpeed;
            rb.angularVelocity = Vector3.zero;
        }
    }

    private void HandleCursorLock()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            LockCursor(false);

        if (Input.GetMouseButtonDown(0) && !isCursorLocked)
            LockCursor(true);
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

    public void TakeDamage(float damage)
    {
        if (!isLocalPlayer) return;

        if (healthSystem != null)
            CmdTakeDamage(damage);
    }

    [Command]
    private void CmdTakeDamage(float damage)
    {
        if (healthSystem != null)
            healthSystem.TakeDamage(damage);
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!isLocalPlayer) return;
        if (!enabled) return;    // ✅ skip when controls are disabled (game over)

        if (SimpleChat.Instance != null && SimpleChat.Instance.IsChatOpen) return;
        if (PauseMenu.IsPauseMenuOpen) return;

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

    [Command]
    private void CmdUpdateRotation(float y)
    {
        syncedYRotation = y;
    }

    private void OnRotationChanged(float oldValue, float newValue)
    {
        if (isLocalPlayer) return;

        cachedTransform.rotation = Quaternion.Euler(0f, newValue, 0f);
    }

    // ✅ Called by NetworkTimer / MissionCompleteUI when the game ends
    public void DisableControls()
    {
        enabled = false;

        isCursorLocked = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Debug.Log($"[PlayerController] Controls disabled for player {netId}");
    }
}