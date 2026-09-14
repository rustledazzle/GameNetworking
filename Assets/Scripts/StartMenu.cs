using Mirror;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class OfflineMenuUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_InputField serverInput;
    [SerializeField] private TMP_InputField usernameInput;
    [SerializeField] private Button hostButton;
    [SerializeField] private Button clientButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private TMP_Text statusText;

    [Header("Defaults")]
    [SerializeField] private string defaultServer = "localhost";
    [SerializeField] private int defaultPort = 7777;

    private bool isConnecting = false;

    private void Start()
    {
        // Load saved values or use defaults
        serverInput.text = PlayerPrefs.GetString("server", defaultServer);
        usernameInput.text = PlayerPrefs.GetString(
            "username",
            $"Player{Random.Range(100, 999)}"
        );

        if (hostButton != null)
            hostButton.onClick.AddListener(StartHost);

        if (clientButton != null)
            clientButton.onClick.AddListener(StartClient);

        if (quitButton != null)
            quitButton.onClick.AddListener(QuitGame);

        // ✅ Reset the connect flag whenever this scene loads
        isConnecting = false;
        SetButtonsInteractable(true);

        // ✅ Show disconnect reason if the client just came back from a session
        string reason = PlayerPrefs.GetString("DisconnectReason", "");
        if (!string.IsNullOrEmpty(reason))
        {
            UpdateStatus($"⚠️ {reason}");
            PlayerPrefs.DeleteKey("DisconnectReason");
            PlayerPrefs.Save();
        }
        else
        {
            UpdateStatus("Enter a username, then choose Host or Client.");
        }
    }

    // ----- Host -----
    private void StartHost()
    {
        if (isConnecting) return;

        if (!SavePrefsAndValidate())
            return;

        isConnecting = true;
        SetButtonsInteractable(false);
        UpdateStatus("Starting Host...");

        if (NetworkManager.singleton == null)
        {
            UpdateStatus("❌ NetworkManager not found.");
            isConnecting = false;
            SetButtonsInteractable(true);
            return;
        }

        NetworkManager.singleton.StartHost();
        UpdateStatus("Host started. Waiting for client...");
    }

    // ----- Client -----
    private void StartClient()
    {
        if (isConnecting) return;

        if (!SavePrefsAndValidate())
            return;

        isConnecting = true;
        SetButtonsInteractable(false);
        UpdateStatus($"Connecting to {serverInput.text}...");

        if (NetworkManager.singleton == null)
        {
            UpdateStatus("❌ NetworkManager not found.");
            isConnecting = false;
            SetButtonsInteractable(true);
            return;
        }

        NetworkManager.singleton.networkAddress = serverInput.text.Trim();
        NetworkManager.singleton.StartClient();
    }

    // ----- Helpers -----
    private bool SavePrefsAndValidate()
    {
        string username = usernameInput.text.Trim();

        if (string.IsNullOrEmpty(username))
        {
            UpdateStatus("❌ Please enter a username.");
            return false;
        }

        string server = serverInput.text.Trim();
        if (string.IsNullOrEmpty(server))
            server = defaultServer;

        PlayerPrefs.SetString("server", server);
        PlayerPrefs.SetString("username", username);
        PlayerPrefs.SetString("localPlayerName", username);
        PlayerPrefs.Save();

        return true;
    }

    private void SetButtonsInteractable(bool state)
    {
        if (hostButton != null) hostButton.interactable = state;
        if (clientButton != null) clientButton.interactable = state;
    }

    private void UpdateStatus(string message)
    {
        if (statusText != null)
            statusText.text = message;

        Debug.Log($"[OfflineMenu] {message}");
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