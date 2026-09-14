using Mirror;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class SimpleChat : NetworkBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private TextMeshProUGUI chatText;
    [SerializeField] private GameObject chatPanel;
    [SerializeField] private KeyCode toggleKey = KeyCode.T;
    [SerializeField] private int maxMessages = 50;

    private bool isOpen = false;
    private List<string> history = new List<string>();
    private readonly Dictionary<uint, string> names = new Dictionary<uint, string>();

    public static SimpleChat Instance;
    public bool IsChatOpen => isOpen;

    private void Awake() => Instance = this;

    private void Start()
    {
        if (inputField != null)
            inputField.onSubmit.AddListener(OnChatSubmit);

        if (chatPanel != null)
            chatPanel.SetActive(false);
    }

    private void Update()
    {
        // ✅ Don't allow chat while the pause menu is open
        if (PauseMenu.IsPauseMenuOpen) return;

        // ✅ T only OPENS the chat — never closes it
        if (!isOpen && Input.GetKeyDown(toggleKey))
        {
            OpenChat();
        }

        // ✅ Escape closes chat
        if (isOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            CloseChat();
        }
    }

    private void OpenChat()
    {
        isOpen = true;
        if (chatPanel != null) chatPanel.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (inputField != null)
            StartCoroutine(FocusInputNextFrame());
    }

    private void CloseChat()
    {
        isOpen = false;
        if (chatPanel != null) chatPanel.SetActive(false);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (inputField != null)
            inputField.text = "";
    }

    private IEnumerator FocusInputNextFrame()
    {
        yield return null;
        yield return null;
        if (inputField != null)
        {
            inputField.ActivateInputField();
            inputField.Select();
        }
    }

    private void OnChatSubmit(string msg)
    {
        if (string.IsNullOrWhiteSpace(msg)) return;
        msg = msg.Trim();

        if (msg.StartsWith("/nick "))
        {
            string newName = msg.Substring(6).Trim();
            if (!string.IsNullOrEmpty(newName))
            {
                AddLine($"[SYSTEM] You are now {newName}", Color.cyan);
                CmdUpdateName(newName);
            }
            ClearInput();
            return;
        }

        if (msg == "/clear")
        {
            history.Clear();
            if (chatText != null) chatText.text = "";
            ClearInput();
            return;
        }

        if (msg == "/ping")
        {
            AddLine($"[SYSTEM] Ping: {Time.time * 1000f:F0}ms", Color.green);
            ClearInput();
            return;
        }

        CmdSend(msg);
        ClearInput();
    }

    private void ClearInput()
    {
        if (inputField == null) return;
        inputField.text = "";
        inputField.ActivateInputField();
        inputField.Select();
    }

    [Command(requiresAuthority = false)]
    private void CmdSend(string msg, NetworkConnectionToClient sender = null)
    {
        uint id = sender != null ? (uint)sender.connectionId : 0u;
        string name = GetName(id);
        RpcReceive($"[{name}]: {msg}");
    }

    [Command(requiresAuthority = false)]
    private void CmdUpdateName(string newName, NetworkConnectionToClient sender = null)
    {
        uint id = sender != null ? (uint)sender.connectionId : 0u;
        names[id] = newName;
        RpcReceive($"[SYSTEM] User_{id} is now known as {newName}");
    }

    [ClientRpc]
    private void RpcReceive(string msg)
    {
        AddLine(msg, Color.white);
    }

    private void AddLine(string msg, Color color)
    {
        string line = $"<color=#{ColorUtility.ToHtmlStringRGB(color)}>{msg}</color>";
        history.Add(line);
        if (history.Count > maxMessages) history.RemoveAt(0);
        if (chatText != null)
            chatText.text = string.Join("\n", history);
    }

    private string GetName(uint id)
    {
        return names.ContainsKey(id) ? names[id] : $"User_{id}";
    }

    public void HandleServerConnect(NetworkConnectionToClient conn)
    {
        if (!isServer) return;
        uint id = (uint)conn.connectionId;
        names[id] = $"User_{id}";
        RpcReceive($"[SYSTEM] User_{id} joined.");
    }

    public void HandleServerDisconnect(NetworkConnectionToClient conn)
    {
        if (!isServer) return;
        uint id = (uint)conn.connectionId;
        names.Remove(id);
        RpcReceive($"[SYSTEM] User_{id} left.");
    }

    // ✅ Cleanup so a stale instance doesn't linger across sessions
    public override void OnStopClient()
    {
        base.OnStopClient();
        history.Clear();
        names.Clear();
        if (chatText != null) chatText.text = "";
    }
}