using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class TimerPenaltyFlash : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text penaltyText;

    [Header("Animation")]
    [Tooltip("How long the text stays visible before fading.")]
    [SerializeField] private float visibleDuration = 1.2f;

    [Tooltip("How long the fade-out takes.")]
    [SerializeField] private float fadeDuration = 0.8f;

    [Tooltip("How far up the text floats during its lifetime.")]
    [SerializeField] private float floatDistance = 30f;

    [Tooltip("Starting color of the text.")]
    [SerializeField] private Color baseColor = new Color(1f, 0.4f, 0.1f, 1f);   // orange

    private RectTransform rect;
    private Vector2 originalPosition;
    private Coroutine activeFlash;

    private void Awake()
    {
        if (penaltyText == null)
            penaltyText = GetComponent<TMP_Text>();

        rect = penaltyText.rectTransform;
        originalPosition = rect.anchoredPosition;

        // Hide at start
        penaltyText.text = "";
        penaltyText.color = new Color(baseColor.r, baseColor.g, baseColor.b, 0f);
    }

    /// <summary>
    /// Show "-30s" style text that fades out.
    /// </summary>
    public void FlashPenalty(float amount)
    {
        // Cancel any previous flash
        if (activeFlash != null)
            StopCoroutine(activeFlash);

        activeFlash = StartCoroutine(FlashRoutine(amount));
    }

    private IEnumerator FlashRoutine(float amount)
    {
        // Set the text: -30s or -5s
        penaltyText.text = $"-{Mathf.RoundToInt(amount)}s";

        // Reset position and alpha
        rect.anchoredPosition = originalPosition;
        penaltyText.color = new Color(baseColor.r, baseColor.g, baseColor.b, 1f);

        // Wait before starting to fade
        float elapsed = 0f;
        while (elapsed < visibleDuration)
        {
            elapsed += Time.deltaTime;

            // Float upward while visible
            float t = elapsed / visibleDuration;
            rect.anchoredPosition = originalPosition + new Vector2(0f, floatDistance * t);

            yield return null;
        }

        // Fade out
        elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = 1f - (elapsed / fadeDuration);

            penaltyText.color = new Color(baseColor.r, baseColor.g, baseColor.b, t);
            yield return null;
        }

        // Hide completely
        penaltyText.text = "";
        rect.anchoredPosition = originalPosition;
        activeFlash = null;
    }
}