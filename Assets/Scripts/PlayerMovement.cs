using Mirror;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : NetworkBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 10f;
    [SerializeField] private float sprintMultiplier = 1.5f;
    [SerializeField] private float gravity = -30f;
    [SerializeField] private float jumpHeight = 1.5f;

    [Header("Client Prediction")]
    [SerializeField] private bool clientPredictionEnabled = true;
    [SerializeField] private float tickRate = 0.033f;
    [SerializeField] private float errorThreshold = 0.5f;

    [Header("Debug")]
    [SerializeField] private KeyCode togglePredictionKey = KeyCode.P;
    [SerializeField] private bool showDebugOverlay = true;

    private CharacterController controller;
    private InputBuffer inputBuffer = new InputBuffer();
    private uint nextSequence = 0;
    private uint lastAcknowledgedSequence = 0;
    private float verticalVelocity = 0f;
    private float tickTimer = 0f;
    private int rollbackCount = 0;

    public bool PredictionEnabled => clientPredictionEnabled;
    public int RollbackCount => rollbackCount;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    private void Update()
    {
        if (!isLocalPlayer) return;

        // Toggle prediction with P key
        if (Input.GetKeyDown(togglePredictionKey))
        {
            clientPredictionEnabled = !clientPredictionEnabled;
            Debug.Log($"[Prediction] {(clientPredictionEnabled ? "ON" : "OFF")}");
            if (!clientPredictionEnabled) inputBuffer.Clear();
        }

        if (SimpleChat.Instance != null && SimpleChat.Instance.IsChatOpen) return;
        if (PauseMenu.IsPauseMenuOpen) return;

        bool jumpFrame = Input.GetKeyDown(KeyCode.Space);

        tickTimer += Time.deltaTime;
        if (tickTimer < tickRate && !jumpFrame) return;
        tickTimer = 0f;

        StepMovement(jumpFrame);
    }

    private void StepMovement(bool jumpPressed)
    {
        // ✅ Build a sequenced InputCommand
        InputCommand cmd = new InputCommand
        {
            sequenceNumber = nextSequence++,
            timestamp = Time.time,
            moveInput = new Vector2(
                Input.GetAxisRaw("Horizontal"),
                Input.GetAxisRaw("Vertical")
            ),
            jump = jumpPressed,
            sprint = Input.GetKey(KeyCode.LeftShift)
        };

        // Local prediction
        if (clientPredictionEnabled)
            ApplyMovement(cmd, tickRate);

        // ✅ Store in the buffer for reconciliation
        inputBuffer.Push(cmd);

        // Send to server
        CmdSendInput(cmd);
    }

    private void ApplyMovement(InputCommand cmd, float dt)
    {
        Vector3 forward = transform.forward; forward.y = 0; forward.Normalize();
        Vector3 right = transform.right; right.y = 0; right.Normalize();
        Vector3 dir = (forward * cmd.moveInput.y + right * cmd.moveInput.x).normalized;

        if (controller.isGrounded && verticalVelocity < 0)
            verticalVelocity = -2f;

        if (controller.isGrounded && cmd.jump)
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);

        verticalVelocity += gravity * dt;

        float speed = cmd.sprint ? moveSpeed * sprintMultiplier : moveSpeed;
        Vector3 motion = dir * speed * dt;
        motion.y = verticalVelocity * dt;

        controller.Move(motion);
    }

    [Command]
    private void CmdSendInput(InputCommand cmd)
    {
        // Server runs the same movement
        ApplyMovement(cmd, tickRate);

        // Remember the last processed sequence
        lastAcknowledgedSequence = cmd.sequenceNumber;

        // Echo the position + sequence back to the owner
        TargetRpcReconcile(connectionToClient, transform.position, lastAcknowledgedSequence);
    }

    [TargetRpc]
    private void TargetRpcReconcile(NetworkConnection target, Vector3 serverPos, uint lastSeq)
    {
        if (!clientPredictionEnabled) return;

        // ✅ Drop acknowledged inputs from the buffer
        inputBuffer.DropOlderThan(lastSeq);

        // Measure horizontal error only
        float error = Vector2.Distance(
            new Vector2(transform.position.x, transform.position.z),
            new Vector2(serverPos.x, serverPos.z)
        );

        if (error > errorThreshold)
        {
            // Snap X/Z to server position (keep Y local so jumps stay smooth)
            Vector3 corrected = new Vector3(serverPos.x, transform.position.y, serverPos.z);

            controller.enabled = false;
            transform.position = corrected;
            controller.enabled = true;

            // ✅ Replay remaining unacknowledged commands
            InputCommand[] pending = inputBuffer.GetAll();
            foreach (var cmd in pending)
                ApplyMovement(cmd, tickRate);

            rollbackCount++;
        }
    }

    // ✅ Called by PlayerController when the game ends
    public void SetDead(bool dead)
    {
        enabled = !dead;
    }

    private void OnGUI()
    {
        if (!showDebugOverlay) return;
        if (!isLocalPlayer) return;

        GUILayout.BeginArea(new Rect(10, 10, 240, 110), GUI.skin.box);
        GUILayout.Label($"Prediction: {(clientPredictionEnabled ? "ON ✅" : "OFF ❌")}");
        GUILayout.Label($"Rollbacks: {rollbackCount}");
        GUILayout.Label($"Buffer: {inputBuffer.Count}");
        GUILayout.Label($"Y-Vel: {verticalVelocity:F2}");
        GUILayout.EndArea();
    }
}