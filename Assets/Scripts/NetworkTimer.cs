using Mirror;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class NetworkTimer : NetworkBehaviour
{
    [Header("Timer Settings")]
    [SerializeField] private float startingTime = 300f;
    [SerializeField] private float penaltySeconds = 30f;

    [Header("UI")]
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private TimerPenaltyFlash penaltyFlash;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color warningColor = Color.red;
    [SerializeField] private float warningThreshold = 30f;

    [Header("Timer Flash (optional)")]
    [SerializeField] private bool flashTimerOnPenalty = true;
    [SerializeField] private float flashDuration = 0.15f;

    [SyncVar(hook = nameof(OnTimerChanged))]
    private float timeRemaining;

    [SyncVar(hook = nameof(OnGameOverChanged))]
    private bool isGameOver = false;

    private bool isRunning = false;
    private Color lastKnownColor;

    public override void OnStartServer()
    {
        timeRemaining = startingTime;
        isRunning = true;
        isGameOver = false;
        Debug.Log($"[Timer] Started: {startingTime}s");
    }

    public override void OnStartClient()
    {
        UpdateTimerUI(timeRemaining);
    }

    private void Update()
    {
        if (!isServer) return;
        if (!isRunning) return;
        if (isGameOver) return;

        timeRemaining -= Time.deltaTime;

        if (timeRemaining <= 0f)
        {
            timeRemaining = 0f;
            isRunning = false;
            OnTimerExpired();
        }
    }

    [Server]
    public void ApplyPenalty()
    {
        ApplyPenalty(penaltySeconds);
    }

    [Server]
    public void ApplyPenalty(float amount)
    {
        if (!isServer) return;
        if (isGameOver) return;

        timeRemaining = Mathf.Max(0f, timeRemaining - amount);
        Debug.Log($"[Timer] Penalty: -{amount}s → {timeRemaining:F1}s remaining");

        RpcShowPenalty(amount);
    }

    [Server]
    public void ResetTimer()
    {
        timeRemaining = startingTime;
        isRunning = true;
        isGameOver = false;
    }

    private void OnTimerChanged(float oldValue, float newValue)
    {
        UpdateTimerUI(newValue);
    }

    private void UpdateTimerUI(float seconds)
    {
        if (timerText == null) return;

        int m = Mathf.FloorToInt(seconds / 60);
        int s = Mathf.FloorToInt(seconds % 60);
        timerText.text = $"{m:00}:{s:00}";

        lastKnownColor = seconds <= warningThreshold ? warningColor : normalColor;
        timerText.color = lastKnownColor;
    }

    [ClientRpc]
    private void RpcShowPenalty(float amount)
    {
        if (penaltyFlash != null)
            penaltyFlash.FlashPenalty(amount);

        if (flashTimerOnPenalty && timerText != null)
            StartCoroutine(FlashTimerRoutine());
    }

    private System.Collections.IEnumerator FlashTimerRoutine()
    {
        Color originalColor = timerText.color;
        timerText.color = Color.red;
        yield return new WaitForSeconds(flashDuration);
        timerText.color = originalColor;
    }

    // -------- Timer Expiry → Game Over --------

    [Server]
    private void OnTimerExpired()
    {
        if (isGameOver) return;

        isGameOver = true;
        Debug.Log("[Timer] TIME'S UP — Game Over");

        // ✅ Tell all clients to freeze player controls
        RpcDisablePlayerControls();
    }

    private void OnGameOverChanged(bool oldValue, bool newValue)
    {
        if (!newValue) return;

        Debug.Log("[Timer] Game Over — showing failure UI");

        if (GameOverUI.Instance != null)
            GameOverUI.Instance.Show();
        else
            Debug.LogWarning("[Timer] GameOverUI.Instance is null!");
    }

    [ClientRpc]
    private void RpcDisablePlayerControls()
    {
        Debug.Log("[Timer] RpcDisablePlayerControls received");

        // ✅ Disable the local player's controller
        foreach (var pc in FindObjectsByType<PlayerController>(FindObjectsSortMode.None))
        {
            if (pc.isLocalPlayer)
            {
                pc.DisableControls();
                break;
            }
        }

        // ✅ Ensure cursor is free for the UI button
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public float GetTimeRemaining() => timeRemaining;
    public bool IsGameOver() => isGameOver;
}