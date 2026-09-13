using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CustomNetworkManager : NetworkManager
{
    [Header("Lobby")]
    [SerializeField] private GameObject lobbyPlayerPrefab;

    [Header("Gameplay Spawn")]
    [Tooltip("Where the GamePlayer spawns in the gameplay scene.")]
    [SerializeField] private Vector3 gamePlayerSpawnPosition = new Vector3(0f, 2f, 0f);

    public override void OnServerConnect(NetworkConnectionToClient conn)
    {
        base.OnServerConnect(conn);
        Debug.Log($"Client {conn.connectionId} connected");

        if (SimpleChat.Instance != null)
            SimpleChat.Instance.HandleServerConnect(conn);
    }

    public override void OnServerDisconnect(NetworkConnectionToClient conn)
    {
        if (SimpleChat.Instance != null)
            SimpleChat.Instance.HandleServerDisconnect(conn);

        base.OnServerDisconnect(conn);
        Debug.Log($"Client {conn.connectionId} disconnected");
    }

    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        // ---- Lobby Scene: spawn LobbyPlayerPrefab ----
        if (SceneManager.GetActiveScene().name == "1_LobbyScene")
        {
            if (lobbyPlayerPrefab == null)
            {
                Debug.LogError("[CustomNetworkManager] Lobby Player Prefab is NOT assigned!");
                return;
            }

            GameObject lobbyPlayer = Instantiate(lobbyPlayerPrefab);
            NetworkServer.AddPlayerForConnection(conn, lobbyPlayer);
            Debug.Log($"[CustomNetworkManager] Spawned LobbyPlayer for {conn.connectionId}");
        }
        // ---- Gameplay Scene: spawn the real Player at the fixed position ----
        else
        {
            if (playerPrefab == null)
            {
                Debug.LogError("[CustomNetworkManager] Player Prefab is NOT assigned!");
                return;
            }

            GameObject gamePlayer = Instantiate(
                playerPrefab,
                gamePlayerSpawnPosition,
                Quaternion.identity
            );

            NetworkServer.AddPlayerForConnection(conn, gamePlayer);
            Debug.Log($"[CustomNetworkManager] Spawned GamePlayer for {conn.connectionId} at {gamePlayerSpawnPosition}");
        }
    }
}