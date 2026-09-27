using Mirror;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameOverUI : MonoBehaviour
{
    public static GameOverUI Instance;

    [Header("UI")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text subtitleText;
    [SerializeField] private Button backToMenuButton;

    [Header("Text")]
    [SerializeField] private string title = "MISSION FAILED";
    [SerializeField] private string subtitle = "Time has run out";
    [SerializeField] private Color titleColor = new Color(1f, 0.2f, 0.2f);

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
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        if (backToMenuButton != null)
            backToMenuButton.onClick.AddListener(OnBackToMenuClicked);
    }

    public void Show()
    {
        Debug.Log("[GameOverUI] Show called");

        if (gameOverPanel == null)
        {
            Debug.LogError("[GameOverUI] Game Over Panel is not assigned!");
            return;
        }

        if (titleText != null)
        {
            titleText.text = title;
            titleText.color = titleColor;
        }

        if (subtitleText != null)
            subtitleText.text = subtitle;

        gameOverPanel.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void Hide()
    {
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
    }

    private void OnBackToMenuClicked()
    {
        if (NetworkServer.active && NetworkClient.isConnected)
            NetworkManager.singleton.StopHost();
        else if (NetworkClient.isConnected)
            NetworkManager.singleton.StopClient();
    }
}