using Mirror;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SpawnManager : NetworkBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private float spawnDistance = 3f;

    private HashSet<int> spawnedPlayers = new HashSet<int>();
    private bool isInitialSpawnComplete = false;

    public override void OnStartServer()
    {
        base.OnStartServer();

        Debug.Log($"[SpawnManager] OnStartServer called");

        // ✅ Subscribe to Mirror's connection events
        NetworkServer.OnConnectedEvent += OnServerConnected;
        NetworkServer.OnDisconnectedEvent += OnServerDisconnected;

        // Start initial spawn after delay
        StartCoroutine(SpawnInitialPlayers());
    }

    private IEnumerator SpawnInitialPlayers()
    {
        // Wait for NetworkManager to fully initialize
        yield return new WaitForSeconds(0.5f);

        if (!isServer) yield break;

        Debug.Log($"[SpawnManager] Spawning initial players. Connected clients: {NetworkServer.connections.Count}");

        // ✅ Spawn all currently connected clients
        foreach (NetworkConnectionToClient conn in NetworkServer.connections.Values)
        {
            if (!spawnedPlayers.Contains(conn.connectionId))
            {
                Vector3 spawnPos = GetSpawnPosition(conn.connectionId);
                SpawnPlayer(conn, spawnPos);
                spawnedPlayers.Add(conn.connectionId);
            }
        }

        isInitialSpawnComplete = true;
    }

    // ✅ Mirror: Called when a client connects (server-only)
    private void OnServerConnected(NetworkConnectionToClient conn)
    {
        if (!isServer) return;

        Debug.Log($"[SpawnManager] Client {conn.connectionId} connected!");

        if (isInitialSpawnComplete)
        {
            StartCoroutine(SpawnClientDelayed(conn));
        }
    }

    private IEnumerator SpawnClientDelayed(NetworkConnectionToClient conn)
    {
        // Wait for client to fully connect
        yield return new WaitForSeconds(0.2f);

        if (!isServer) yield break;
        if (conn == null) yield break;

        // Check if already spawned
        if (spawnedPlayers.Contains(conn.connectionId))
        {
            Debug.Log($"[SpawnManager] Client {conn.connectionId} already spawned");
            yield break;
        }

        Vector3 spawnPos = GetSpawnPosition(conn.connectionId);
        SpawnPlayer(conn, spawnPos);
        spawnedPlayers.Add(conn.connectionId);

        Debug.Log($"[SpawnManager] ✅ Client {conn.connectionId} spawned!");
    }

    private void OnServerDisconnected(NetworkConnectionToClient conn)
    {
        if (!isServer) return;

        if (spawnedPlayers.Contains(conn.connectionId))
            spawnedPlayers.Remove(conn.connectionId);

        Debug.Log($"[SpawnManager] Client {conn.connectionId} disconnected.");
    }

    private Vector3 GetSpawnPosition(int connectionId)
    {
        // First connection (Host, id 0) spawns on left, others on right
        if (connectionId == 0)
            return new Vector3(-spawnDistance, 2, 0);
        else
            return new Vector3(spawnDistance, 2, 0);
    }

    private void SpawnPlayer(NetworkConnectionToClient conn, Vector3 position)
    {
        if (playerPrefab == null)
        {
            Debug.LogError("[SpawnManager] ❌ Player prefab is NULL!");
            return;
        }

        Debug.Log($"[SpawnManager] Spawning player {conn.connectionId} at {position}");

        GameObject player = Instantiate(playerPrefab, position, Quaternion.identity);

        // ✅ Mirror: Check for NetworkIdentity
        NetworkIdentity netIdentity = player.GetComponent<NetworkIdentity>();
        if (netIdentity == null)
        {
            Debug.LogError("❌ Player prefab missing NetworkIdentity!");
            Destroy(player);
            return;
        }

        // ✅ Mirror: Spawn as player object, assign to the specific connection
        NetworkServer.AddPlayerForConnection(conn, player);
        Debug.Log($"✅ Player {conn.connectionId} spawned!");
    }

    private void OnDisable()
    {
        // ✅ Unsubscribe from events
        NetworkServer.OnConnectedEvent -= OnServerConnected;
        NetworkServer.OnDisconnectedEvent -= OnServerDisconnected;
    }
}