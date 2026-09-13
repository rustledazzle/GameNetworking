using Mirror;
using UnityEngine;

public class LobbyPlayer : NetworkBehaviour
{
    [SyncVar] public string DisplayName = "Player";
    [SyncVar] public int Role = 0;           // 0 = Officer, 1 = Soldier
    [SyncVar] public bool IsReady = false;

    // ✅ Each client sends its own username to the server when it spawns
    public override void OnStartLocalPlayer()
    {
        string name = PlayerPrefs.GetString(
            "localPlayerName",
            $"Player{Random.Range(100, 999)}"
        );

        Debug.Log($"[LobbyPlayer] Sending username '{name}' to server");
        CmdSetUsername(name);
    }

    [Command]
    private void CmdSetUsername(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return;

        DisplayName = name;
        Debug.Log($"[LobbyPlayer] netId={netId} name set to '{DisplayName}'");
    }

    public override void OnStartServer()
    {
        // Assign roles based on connection order
        // First connected player → Officer
        // Second connected player → Soldier
        int existing = 0;
        foreach (var conn in NetworkServer.connections.Values)
            if (conn.identity != null) existing++;

        Role = existing <= 1 ? 0 : 1;

        Debug.Log($"[LobbyPlayer] {netId} spawned with role {(Role == 0 ? "Officer" : "Soldier")}");
    }

    public string GetRoleName() => Role == 0 ? "Officer" : "Soldier";

    [Command]
    public void CmdToggleReady()
    {
        IsReady = !IsReady;
        Debug.Log($"[Lobby] {DisplayName} ready = {IsReady}");
        CheckAllReady();
    }

    [Command]
    public void CmdSwitchRole()
    {
        Role = (Role + 1) % 2;
    }

    [Server]
    private void CheckAllReady()
    {
        var players = FindObjectsByType<LobbyPlayer>(FindObjectsSortMode.None);
        if (players.Length < 2) return;

        foreach (var p in players)
            if (!p.IsReady) return;

        Debug.Log("[Lobby] All ready — loading gameplay");

        // ✅ Switch scene — must match the exact scene file name
        NetworkManager.singleton.ServerChangeScene("2_GameplayScene");
    }
}