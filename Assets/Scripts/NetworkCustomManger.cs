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
        Debug.Log($"[NetworkManager] Client {conn.connectionId} connected");

        if (SimpleChat.Instance != null)
            SimpleChat.Instance.HandleServerConnect(conn);
    }

    public override void OnServerDisconnect(NetworkConnectionToClient conn)
    {
        if (SimpleChat.Instance != null)
            SimpleChat.Instance.HandleServerDisconnect(conn);

        base.OnServerDisconnect(conn);
        Debug.Log($"[NetworkManager] Client {conn.connectionId} disconnected");
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

            // ✅ Look for a tagged spawn point in the gameplay scene
            Vector3 spawnPos = gamePlayerSpawnPosition;   // fallback
            GameObject[] spawns = GameObject.FindGameObjectsWithTag("PlayerSpawn");

            if (spawns.Length > 0)
            {
                int idx = (int)(conn.connectionId % (uint)spawns.Length);
                spawnPos = spawns[idx].transform.position;
                Debug.Log($"[CustomNetworkManager] Using spawn '{spawns[idx].name}' at {spawnPos}");
            }
            else
            {
                Debug.LogWarning("[CustomNetworkManager] No PlayerSpawn-tagged objects — using fallback");
            }

            GameObject gamePlayer = Instantiate(playerPrefab, spawnPos, Quaternion.identity);
            NetworkServer.AddPlayerForConnection(conn, gamePlayer);
            Debug.Log($"[CustomNetworkManager] Spawned GamePlayer for {conn.connectionId} at {spawnPos}");
        }
    }

    // ✅ NEW: Client-side disconnect handler — returns to Offline Scene
    public override void OnClientDisconnect()
    {
        base.OnClientDisconnect();

        Debug.Log("[NetworkManager] Disconnected from host — returning to Offline Scene");

        // Store a reason so OfflineMenuUI can show a message
        PlayerPrefs.SetString("DisconnectReason", "Host disconnected");
        PlayerPrefs.Save();

        // Load the offline scene. This is the same scene name you set in the
        // NetworkManager's "Offline Scene" field, but we load it explicitly here
        // so it always runs, even if the automatic path fails.
        string target = string.IsNullOrEmpty(offlineScene) ? "0_OfflineScene" : offlineScene;
        SceneManager.LoadScene(target);
    }
}