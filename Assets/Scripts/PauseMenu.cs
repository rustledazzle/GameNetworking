using Mirror;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class PauseMenu : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button backToMenuButton;
    [SerializeField] private Button exitButton;
    [SerializeField] private TMP_Text titleText;

    [Header("Settings")]
    [SerializeField] private KeyCode toggleKey = KeyCode.F1;
    [SerializeField] private string offlineSceneName = "0_OfflineScene";

    public static PauseMenu Instance;
    public bool IsOpen { get; private set; } = false;

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
        if (pausePanel != null) pausePanel.SetActive(false);

        if (resumeButton != null)
            resumeButton.onClick.AddListener(Resume);

        if (backToMenuButton != null)
            backToMenuButton.onClick.AddListener(BackToMenu);

        if (exitButton != null)
            exitButton.onClick.AddListener(ExitGame);

        if (titleText != null)
            titleText.text = "PAUSED";
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            if (IsOpen) Resume();
            else Open();
        }

        if (IsOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            Resume();
        }
    }

    public void Open()
    {
        IsOpen = true;
        if (pausePanel != null) pausePanel.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void Resume()
    {
        IsOpen = false;
        if (pausePanel != null) pausePanel.SetActive(false);

        if (SimpleChat.Instance == null || !SimpleChat.Instance.IsChatOpen)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    // ✅ Updated: only stop the network — CustomNetworkManager.OnClientDisconnect
    // will handle loading the offline scene (avoids double-load)
    public void BackToMenu()
    {
        IsOpen = false;

        if (NetworkServer.active && NetworkClient.isConnected)
        {
            Debug.Log("[PauseMenu] Host stopping server");
            NetworkManager.singleton.StopHost();
        }
        else if (NetworkClient.isConnected)
        {
            Debug.Log("[PauseMenu] Client disconnecting");
            NetworkManager.singleton.StopClient();
        }
        else
        {
            // Not connected to anything — just load the menu directly
            if (!string.IsNullOrEmpty(offlineSceneName))
                SceneManager.LoadScene(offlineSceneName);
        }
    }

    public void ExitGame()
    {
        Debug.Log("[PauseMenu] Exiting game...");

        if (NetworkServer.active && NetworkClient.isConnected)
            NetworkManager.singleton.StopHost();
        else if (NetworkClient.isConnected)
            NetworkManager.singleton.StopClient();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public static bool IsPauseMenuOpen =>
        Instance != null && Instance.IsOpen;
}