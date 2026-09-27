using Mirror;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MissionCompleteUI : MonoBehaviour
{
    public static MissionCompleteUI Instance;

    [Header("UI")]
    [SerializeField] private GameObject missionCompletePanel;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text subtitleText;
    [SerializeField] private Button backToMenuButton;

    [Header("Text")]
    [SerializeField] private string title = "MISSION COMPLETE";
    [SerializeField] private string subtitle = "Both operatives extracted";

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
        if (missionCompletePanel != null)
            missionCompletePanel.SetActive(false);

        if (backToMenuButton != null)
            backToMenuButton.onClick.AddListener(OnBackToMenuClicked);
    }

    public void Show()
    {
        Debug.Log("[MissionCompleteUI] Show called");

        if (missionCompletePanel == null)
        {
            Debug.LogError("[MissionCompleteUI] Mission Complete Panel is not assigned!");
            return;
        }

        if (titleText != null) titleText.text = title;
        if (subtitleText != null) subtitleText.text = subtitle;

        missionCompletePanel.SetActive(true);

        // ✅ Freeze local player controls
        foreach (var pc in FindObjectsByType<PlayerController>(FindObjectsSortMode.None))
        {
            if (pc.isLocalPlayer)
            {
                pc.DisableControls();
                break;
            }
        }

        // ✅ Unlock cursor for the UI button
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void Hide()
    {
        if (missionCompletePanel != null)
            missionCompletePanel.SetActive(false);
    }

    private void OnBackToMenuClicked()
    {
        if (NetworkServer.active && NetworkClient.isConnected)
            NetworkManager.singleton.StopHost();
        else if (NetworkClient.isConnected)
            NetworkManager.singleton.StopClient();
    }
}