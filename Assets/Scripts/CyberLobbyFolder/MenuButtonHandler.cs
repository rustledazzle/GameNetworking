using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MenuButtonHandler : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private ChatManagerCyber chatManager;

    private void Start()
    {
        // Auto-find if not assigned
        if (statusText == null)
            statusText = FindObjectOfType<TMP_Text>();

        if (chatManager == null)
            chatManager = FindObjectOfType<ChatManagerCyber>();

        if (chatManager == null)
            Debug.LogWarning("ChatManagerCyber not found! Buttons won't send messages.");
    }

    public void OnCustomizeClick()
    {
        AddSystemMessage("[SYSTEM] Customization menu opened.");
    }

    public void OnFontsClick()
    {
        AddSystemMessage("[SYSTEM] Font selection toggled.");
    }

    public void OnColorsClick()
    {
        AddSystemMessage("[SYSTEM] Color palette toggled.");
    }

    public void OnLayoutClick()
    {
        AddSystemMessage("[SYSTEM] Layout settings toggled.");
    }

    public void OnAlertsClick()
    {
        AddSystemMessage("[SYSTEM] Alert preferences toggled.");
    }

    public void OnPlatformsClick()
    {
        AddSystemMessage("[SYSTEM] Platform integrations: Twitch, YouTube, Discord.");
    }

    private void AddSystemMessage(string message)
    {
        if (chatManager != null)
        {
            chatManager.AddSystemMessage(message);
        }
        else
        {
            Debug.Log(message);
        }
    }
}