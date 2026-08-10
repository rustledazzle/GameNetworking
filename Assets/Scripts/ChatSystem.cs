using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class ChatManager : NetworkBehaviour
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

    private List<string> messageHistory = new List<string>();
    private float lastMessageTime = 0f;
    private bool isChatActive = false;
    private string playerName = "Player";

    public bool IsChatActive => isChatActive;

    private void Start()
    {
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

        if (chatPanel != null)
        {
            chatPanel.SetActive(false);
        }

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        }
    }

    private void Update()
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsConnectedClient)
            return;

        // ✅ FIX: Only toggle chat with T key when chat is CLOSED
        // This prevents "t" from closing chat when typing
        if (!isChatActive && Input.GetKeyDown(toggleChatKey))
        {
            ToggleChat();
        }

        // Send message with Enter key
        if (isChatActive && Input.GetKeyDown(chatKey))
        {
            SendMessage();
        }

        // Escape to close chat
        if (isChatActive && Input.GetKeyDown(KeyCode.Escape))
        {
            CloseChat();
        }
    }

    private void ToggleChat()
    {
        isChatActive = !isChatActive;

        if (isChatActive)
        {
            OpenChat();
        }
        else
        {
            CloseChat();
        }
    }

    private void OpenChat()
    {
        if (chatInput != null)
        {
            chatInput.gameObject.SetActive(true);
            chatInput.Select();
            chatInput.text = "";
        }

        if (chatPanel != null)
        {
            chatPanel.SetActive(true);
        }

        if (sendButton != null)
        {
            sendButton.gameObject.SetActive(true);
        }

        isChatActive = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void CloseChat()
    {
        if (chatInput != null)
        {
            chatInput.gameObject.SetActive(false);
        }

        if (chatPanel != null)
        {
            chatPanel.SetActive(false);
        }

        if (sendButton != null)
        {
            sendButton.gameObject.SetActive(false);
        }

        isChatActive = false;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void OnSendMessage(string message)
    {
        SendMessage();
    }

    private void OnSendButtonClick()
    {
        SendMessage();
    }

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

    [ClientRpc]
    private void BroadcastMessageClientRpc(string message)
    {
        AddMessageToUI(message, Color.white);
    }

    private void AddMessageToUI(string message, Color color)
    {
        if (chatMessages == null) return;

        messageHistory.Add(message);
        if (messageHistory.Count > maxMessages)
            messageHistory.RemoveAt(0);

        string fullText = "";
        foreach (string msg in messageHistory)
            fullText += msg + "\n";

        chatMessages.text = fullText;

        if (chatScrollRect != null)
        {
            Canvas.ForceUpdateCanvases();
            chatScrollRect.verticalNormalizedPosition = 0f;
        }
    }

    private string GetPlayerName(ulong clientId)
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.ConnectedClients.ContainsKey(clientId))
            return $"Player {clientId}";
        return $"Player {clientId}";
    }

    private void OnClientConnected(ulong clientId)
    {
        if (IsServer)
            BroadcastMessageClientRpc($"[SYSTEM] Player {clientId} joined the game!");
    }

    private void OnClientDisconnected(ulong clientId)
    {
        if (IsServer)
            BroadcastMessageClientRpc($"[SYSTEM] Player {clientId} left the game.");
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