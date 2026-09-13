using Mirror;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LobbyManagerUI : MonoBehaviour
{
    [SerializeField] private TMP_Text playerListText;
    [SerializeField] private Button readyButton;
    [SerializeField] private Button roleButton;
    [SerializeField] private TMP_Text statusText;

    private LobbyPlayer localPlayer;

    private void Start()
    {
        readyButton.onClick.AddListener(OnReadyClicked);
        roleButton.onClick.AddListener(OnRoleClicked);
    }

    private void Update()
    {
        if (localPlayer == null)
        {
            foreach (var p in FindObjectsByType<LobbyPlayer>(FindObjectsSortMode.None))
            {
                if (p.isOwned)
                {
                    localPlayer = p;
                    break;
                }
            }
        }

        RefreshUI();
    }

    private void RefreshUI()
    {
        var players = FindObjectsByType<LobbyPlayer>(FindObjectsSortMode.None);
        string listText = "";
        int readyCount = 0;

        foreach (var p in players)
        {
            // ✅ Uses GetRoleName() → returns "Officer" or "Soldier"
            string role = p.GetRoleName();
            string ready = p.IsReady ? "✔ READY" : "… waiting";
            listText += $"{p.DisplayName} — {role} — {ready}\n";
            if (p.IsReady) readyCount++;
        }

        playerListText.text = listText;

        if (localPlayer != null)
        {
            readyButton.GetComponentInChildren<TMP_Text>().text =
                localPlayer.IsReady ? "UNREADY" : "READY";

            roleButton.GetComponentInChildren<TMP_Text>().text =
                $"Role: {localPlayer.GetRoleName()}";
        }

        if (players.Length < 2)
            statusText.text = "Waiting for second player...";
        else if (readyCount >= 2)
            statusText.text = "Starting game...";
        else
            statusText.text = $"Ready: {readyCount}/{players.Length}";
    }

    private void OnReadyClicked()
    {
        if (localPlayer != null)
            localPlayer.CmdToggleReady();
    }

    private void OnRoleClicked()
    {
        if (localPlayer != null)
            localPlayer.CmdSwitchRole();
    }
}