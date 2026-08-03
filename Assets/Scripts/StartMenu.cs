using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class StartMenu : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Button hostButton;
    [SerializeField] private Button clientButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private TMP_Text statusText;

    [Header("Scene Settings")]
    [SerializeField] private string gameSceneName = "SampleScene";

    private bool isConnecting = false;

    private void Start()
    {
        if (hostButton != null)
            hostButton.onClick.AddListener(StartHost);

        if (clientButton != null)
            clientButton.onClick.AddListener(StartClient);

        if (quitButton != null)
            quitButton.onClick.AddListener(QuitGame);

        UpdateStatus("Select an option");
    }

    private void StartHost()
    {
        if (isConnecting) return;
        isConnecting = true;

        UpdateStatus("Starting Host...");
        hostButton.interactable = false;
        clientButton.interactable = false;

        // Tell GameScene to start as Host
        PlayerPrefs.SetString("StartupMode", "host");
        PlayerPrefs.Save();

        // Load GameScene
        SceneManager.LoadScene(gameSceneName, LoadSceneMode.Single);
    }

    private void StartClient()
    {
        if (isConnecting) return;
        isConnecting = true;

        UpdateStatus("Connecting to Host...");
        hostButton.interactable = false;
        clientButton.interactable = false;

        // Tell GameScene to start as Client
        PlayerPrefs.SetString("StartupMode", "client");
        PlayerPrefs.Save();

        // Load GameScene
        SceneManager.LoadScene(gameSceneName, LoadSceneMode.Single);
    }

    private void UpdateStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
        Debug.Log($"[StartMenu] {message}");
    }

    private void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
    }
}