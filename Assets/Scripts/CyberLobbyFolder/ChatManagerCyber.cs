using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class ChatManagerCyber : NetworkBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_InputField chatInput;
    [SerializeField] private TextMeshProUGUI chatMessages;
    [SerializeField] private ScrollRect chatScrollRect;
    [SerializeField] private Button sendButton;
    [SerializeField] private GameObject chatPanel;

    [Header("Settings")]
    [SerializeField] private int maxMessages = 50;
    [SerializeField] private float messageCooldown = 0.5f;
    [SerializeField] private int maxMessageLength = 200;
    [SerializeField] private KeyCode chatKey = KeyCode.Return;
    [SerializeField] private KeyCode toggleChatKey = KeyCode.T;
    [SerializeField] private float scrollSpeed = 10f;

    // Chat state
    private List<string> messageHistory = new List<string>();
    private float lastMessageTime = 0f;
    private bool isChatActive = false;
    private string localPlayerName = "";
    private bool isScrolling = false;

    // Server-side name storage
    private Dictionary<ulong, string> clientNames = new Dictionary<ulong, string>();

    // CanvasGroup for showing/hiding without disabling
    private CanvasGroup chatCanvasGroup;

    public bool IsChatActive => isChatActive;

    private void Start()
    {
        if (IsOwner)
        {
            localPlayerName = $"User_{Random.Range(100, 999)}";
        }

        if (chatPanel != null)
        {
            chatCanvasGroup = chatPanel.GetComponent<CanvasGroup>();
            if (chatCanvasGroup == null)
                chatCanvasGroup = chatPanel.AddComponent<CanvasGroup>();

            chatCanvasGroup.alpha = 0;
            chatCanvasGroup.interactable = false;
            chatCanvasGroup.blocksRaycasts = false;
        }

        if (chatInput != null)
        {
            chatInput.onSubmit.AddListener(OnSendMessage);
            chatInput.gameObject.SetActive(false);
        }

        if (sendButton != null)
        {
            sendButton.onClick.AddListener(OnSendButtonClick);
            sendButton.gameObject.SetActive(false);
        }

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        }

        ForceScrollbarSettings();

        if (IsOwner)
        {
            AddMessageToUI("[SYSTEM] ⚡ CyberLobby Terminal Initialized ⚡", Color.cyan);
            AddMessageToUI($"[SYSTEM] Welcome {localPlayerName}!", Color.cyan);
            AddMessageToUI("[SYSTEM] Commands: /nick <name> | /clear | /ping", new Color(0f, 1f, 0.4f));
            AddMessageToUI("[SYSTEM] Page Up/Down to scroll chat", Color.yellow);
        }
    }

    private void Update()
    {
        // TEMPORARY: Force content height to a large value
        if (isChatActive && Input.GetKeyDown(KeyCode.F1))
        {
            if (chatScrollRect != null && chatScrollRect.content != null)
            {
                chatScrollRect.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 500f);
                Debug.Log($"Content height forced to 500");
                RefreshLayout();
            }
        }
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsConnectedClient)
            return;

        if (!isChatActive && Input.GetKeyDown(toggleChatKey))
        {
            ToggleChat();
        }

        if (isChatActive && Input.GetKeyDown(chatKey))
        {
            SendMessage();
        }

        if (isChatActive && Input.GetKeyDown(KeyCode.Escape))
        {
            CloseChat();
        }

        // SCROLLING CONTROLS
        if (isChatActive && chatScrollRect != null)
        {
            // Mouse Wheel
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll != 0)
            {
                float newPosition = chatScrollRect.verticalNormalizedPosition + (scroll * scrollSpeed * Time.deltaTime);
                chatScrollRect.verticalNormalizedPosition = Mathf.Clamp01(newPosition);

                if (scroll > 0)
                    isScrolling = true;
                else if (scroll < 0 && chatScrollRect.verticalNormalizedPosition <= 0.01f)
                    isScrolling = false;
            }

            // ✅ PAGE UP - Scroll up (older messages)
            if (Input.GetKeyDown(KeyCode.PageUp))
            {
                float newPos = chatScrollRect.verticalNormalizedPosition + 0.2f;
                chatScrollRect.verticalNormalizedPosition = Mathf.Clamp01(newPos);
                isScrolling = true;
                Debug.Log($"Page Up - Position: {chatScrollRect.verticalNormalizedPosition:F2}");
            }

            // ✅ PAGE DOWN - Scroll down (newer messages)
            if (Input.GetKeyDown(KeyCode.PageDown))
            {
                float newPos = chatScrollRect.verticalNormalizedPosition - 0.2f;
                chatScrollRect.verticalNormalizedPosition = Mathf.Clamp01(newPos);

                if (chatScrollRect.verticalNormalizedPosition <= 0.01f)
                    isScrolling = false;

                Debug.Log($"Page Down - Position: {chatScrollRect.verticalNormalizedPosition:F2}");
            }

            // Detect scrollbar drag
            if (chatScrollRect.verticalScrollbar != null && chatScrollRect.verticalScrollbar.IsActive())
            {
                if (chatScrollRect.verticalNormalizedPosition > 0.05f)
                    isScrolling = true;
            }
        }
    }

    private void ToggleChat()
    {
        isChatActive = !isChatActive;
        if (isChatActive) OpenChat();
        else CloseChat();
    }

    private void OpenChat()
    {
        if (chatCanvasGroup != null)
        {
            chatCanvasGroup.alpha = 1;
            chatCanvasGroup.interactable = true;
            chatCanvasGroup.blocksRaycasts = true;
        }

        ForceScrollbarSettings();
        isScrolling = false;

        if (chatInput != null)
        {
            chatInput.gameObject.SetActive(true);
            chatInput.Select();
            chatInput.text = "";
        }

        if (sendButton != null)
            sendButton.gameObject.SetActive(true);

        isChatActive = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        RefreshLayout();
    }

    private void CloseChat()
    {
        if (chatCanvasGroup != null)
        {
            chatCanvasGroup.alpha = 0;
            chatCanvasGroup.interactable = false;
            chatCanvasGroup.blocksRaycasts = false;
        }

        if (chatInput != null)
            chatInput.gameObject.SetActive(false);

        if (sendButton != null)
            sendButton.gameObject.SetActive(false);

        isChatActive = false;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void OnSendMessage(string message) => SendMessage();
    private void OnSendButtonClick() => SendMessage();

    private void SendMessage()
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsConnectedClient)
            return;

        if (chatInput == null) return;

        string message = chatInput.text.Trim();

        if (string.IsNullOrEmpty(message))
        {
            chatInput.text = "";
            return;
        }

        if (message.StartsWith("/"))
        {
            ProcessCommand(message);
            chatInput.text = "";
            return;
        }

        if (message.Length > maxMessageLength)
        {
            AddMessageToUI("[SYSTEM] Message too long!", Color.red);
            chatInput.text = "";
            return;
        }

        if (Time.time - lastMessageTime < messageCooldown)
        {
            AddMessageToUI("[SYSTEM] Please wait before sending another message.", Color.yellow);
            chatInput.text = "";
            return;
        }

        SubmitMessageServerRpc(message);
        chatInput.text = "";
        lastMessageTime = Time.time;
    }

    private void ProcessCommand(string command)
    {
        string cmd = command.ToLower();

        if (cmd.StartsWith("/nick "))
        {
            string newName = command.Substring(6).Trim();
            if (!string.IsNullOrEmpty(newName))
            {
                string oldName = localPlayerName;
                localPlayerName = newName;
                AddMessageToUI($"[SYSTEM] {oldName} → {newName}", new Color(0f, 1f, 0.4f));
                UpdateNameServerRpc(newName);
            }
            else
            {
                AddMessageToUI("[SYSTEM] Usage: /nick <new_name>", Color.yellow);
            }
        }
        else if (cmd == "/clear")
        {
            messageHistory.Clear();
            chatMessages.text = "";
            AddMessageToUI("[SYSTEM] Chat cleared.", Color.yellow);
        }
        else if (cmd == "/ping")
        {
            float ping = (float)NetworkManager.Singleton.ServerTime.Time;
            AddMessageToUI($"[SYSTEM] Ping: {ping:F0}ms", new Color(0f, 1f, 0.4f));
        }
        else
        {
            AddMessageToUI($"[SYSTEM] Unknown command: {command}", Color.red);
            AddMessageToUI("[SYSTEM] Available: /nick, /clear, /ping", new Color(0f, 1f, 0.4f));
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void SubmitMessageServerRpc(string message, ServerRpcParams rpcParams = default)
    {
        if (string.IsNullOrEmpty(message) || message.Length > maxMessageLength)
            return;

        ulong clientId = rpcParams.Receive.SenderClientId;
        string playerName = GetPlayerName(clientId);
        string finalMessage = $"[{playerName}]: {message}";

        BroadcastMessageClientRpc(finalMessage);
    }

    [ServerRpc(RequireOwnership = false)]
    private void UpdateNameServerRpc(string newName, ServerRpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;

        if (clientNames.ContainsKey(clientId))
        {
            clientNames[clientId] = newName;
        }
        else
        {
            clientNames.Add(clientId, newName);
        }

        UpdateNameClientRpc(clientId, newName);
    }

    [ClientRpc]
    private void UpdateNameClientRpc(ulong clientId, string newName)
    {
        AddMessageToUI($"[SYSTEM] User_{clientId} is now known as {newName}", new Color(0f, 1f, 0.4f));
    }

    [ClientRpc]
    private void BroadcastMessageClientRpc(string message)
    {
        AddMessageToUI(message, Color.white);
    }

    private void AddMessageToUI(string message, Color color)
    {
        if (chatMessages == null) return;

        string coloredMessage = $"<color=#{ColorUtility.ToHtmlStringRGB(color)}>{message}</color>";
        messageHistory.Add(coloredMessage);

        if (messageHistory.Count > maxMessages)
            messageHistory.RemoveAt(0);

        string fullText = "";
        foreach (string msg in messageHistory)
            fullText += msg + "\n";

        chatMessages.text = fullText;

        ForceContentHeight();

        if (!isScrolling)
            RefreshLayout();
    }

    private void ForceContentHeight()
    {
        if (chatScrollRect == null || chatScrollRect.content == null) return;

        float textHeight = chatMessages.preferredHeight;
        float currentContentHeight = chatScrollRect.content.rect.height;
        float minContentHeight = textHeight + 20f;

        if (currentContentHeight < minContentHeight || currentContentHeight < 50f)
        {
            chatScrollRect.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, minContentHeight);
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(chatScrollRect.content);
        Canvas.ForceUpdateCanvases();
    }

    private void RefreshLayout()
    {
        if (chatScrollRect != null)
        {
            Canvas.ForceUpdateCanvases();
            chatScrollRect.verticalNormalizedPosition = 0f;
        }
    }

    private void ForceScrollbarSettings()
    {
        if (chatScrollRect == null) return;

        Scrollbar scrollbar = chatScrollRect.verticalScrollbar;
        if (scrollbar == null) return;

        scrollbar.interactable = true;
        scrollbar.gameObject.SetActive(true);
        scrollbar.size = 0.4f;
        scrollbar.numberOfSteps = 0;
        scrollbar.value = 0;
    }

    private string GetPlayerName(ulong clientId)
    {
        if (clientNames.ContainsKey(clientId))
        {
            return clientNames[clientId];
        }
        return $"User_{clientId}";
    }

    private void OnClientConnected(ulong clientId)
    {
        if (IsServer)
        {
            string defaultName = $"User_{clientId}";
            clientNames[clientId] = defaultName;
            BroadcastMessageClientRpc($"[SYSTEM] {defaultName} joined the lobby!");
        }
    }

    private void OnClientDisconnected(ulong clientId)
    {
        if (IsServer)
        {
            if (clientNames.ContainsKey(clientId))
                clientNames.Remove(clientId);

            BroadcastMessageClientRpc($"[SYSTEM] User_{clientId} left the lobby.");
        }
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        }
    }
}