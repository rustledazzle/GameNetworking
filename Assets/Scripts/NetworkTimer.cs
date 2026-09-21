using Mirror;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class NetworkTimer : NetworkBehaviour
{
    [Header("Timer Settings")]
    [SerializeField] private float startingTime = 300f;      // 5 minutes
    [SerializeField] private float penaltySeconds = 30f;     // default per hit

    [Header("UI")]
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private TimerPenaltyFlash penaltyFlash;   // ✅ NEW
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color warningColor = Color.red;
    [SerializeField] private float warningThreshold = 30f;

    [Header("Timer Flash (optional)")]
    [Tooltip("Briefly flash the timer red when a penalty is applied.")]
    [SerializeField] private bool flashTimerOnPenalty = true;
    [SerializeField] private float flashDuration = 0.15f;

    [SyncVar(hook = nameof(OnTimerChanged))]
    private float timeRemaining;

    private bool isRunning = false;
    private Color lastKnownColor;

    public override void OnStartServer()
    {
        timeRemaining = startingTime;
        isRunning = true;
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

        timeRemaining -= Time.deltaTime;

        if (timeRemaining <= 0f)
        {
            timeRemaining = 0f;
            isRunning = false;
            OnTimerExpired();
        }
    }

    // -------- Penalty API --------

    /// <summary>
    /// Applies the default penalty (used by zombies).
    /// </summary>
    [Server]
    public void ApplyPenalty()
    {
        ApplyPenalty(penaltySeconds);
    }

    /// <summary>
    /// Applies a custom penalty amount (used by traps).
    /// </summary>
    [Server]
    public void ApplyPenalty(float amount)
    {
        if (!isServer) return;

        timeRemaining = Mathf.Max(0f, timeRemaining - amount);
        Debug.Log($"[Timer] Penalty: -{amount}s → {timeRemaining:F1}s remaining");

        // ✅ Tell all clients to show the "-30s" / "-5s" text
        RpcShowPenalty(amount);
    }

    // -------- Reset --------

    [Server]
    public void ResetTimer()
    {
        timeRemaining = startingTime;
        isRunning = true;
    }

    // -------- UI Updates --------

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

    // -------- Penalty Flash on Clients --------

    [ClientRpc]
    private void RpcShowPenalty(float amount)
    {
        // ✅ Show the floating "-30s" text
        if (penaltyFlash != null)
            penaltyFlash.FlashPenalty(amount);

        // ✅ Optional: briefly flash the timer red
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

    // -------- Timer Expiry --------

    [Server]
    private void OnTimerExpired()
    {
        Debug.Log("[Timer] TIME'S UP!");
        // Trigger game over scene, disable player input, etc.
    }

    public float GetTimeRemaining() => timeRemaining;
}