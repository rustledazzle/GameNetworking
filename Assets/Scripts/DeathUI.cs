using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class DeathScreenUI : MonoBehaviour
{
    public static DeathScreenUI Instance;

    [Header("UI")]
    [SerializeField] private GameObject deathPanel;      // ← the whole panel
    [SerializeField] private TMP_Text deathText;

    [Header("Settings")]
    [SerializeField] private string deathMessage = "DECEASED";
    [SerializeField] private float holdDuration = 2f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        // ✅ Start hidden
        if (deathPanel != null)
            deathPanel.SetActive(false);
    }

    public void ShowDeathScreen()
    {
        Debug.Log("[DeathScreenUI] ShowDeathScreen called");

        if (deathPanel == null)
        {
            Debug.LogError("[DeathScreenUI] Death Panel is not assigned!");
            return;
        }

        if (deathText != null)
            deathText.text = deathMessage;

        deathPanel.SetActive(true);

        StopAllCoroutines();
        StartCoroutine(HideAfterDelay());
    }

    public void HideDeathScreen()
    {
        if (deathPanel != null)
            deathPanel.SetActive(false);

        StopAllCoroutines();
        Debug.Log("[DeathScreenUI] Hidden");
    }

    private IEnumerator HideAfterDelay()
    {
        yield return new WaitForSeconds(holdDuration);
        HideDeathScreen();
    }
}