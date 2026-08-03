using Unity.Netcode;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SpawnManager : NetworkBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private float spawnDistance = 3f;

    private HashSet<ulong> spawnedPlayers = new HashSet<ulong>();
    private bool isInitialSpawnComplete = false;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        Debug.Log($"[SpawnManager] OnNetworkSpawn called. IsServer: {IsServer}");

        if (IsServer)
        {
            // Subscribe to client connection events
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;

            // Start initial spawn after delay
            StartCoroutine(SpawnInitialPlayers());
        }
    }

    private IEnumerator SpawnInitialPlayers()
    {
        // Wait for NetworkManager to fully initialize
        yield return new WaitForSeconds(0.5f);

        if (!IsServer) yield break;

        Debug.Log($"[SpawnManager] Spawning initial players. Connected clients: {NetworkManager.Singleton.ConnectedClients.Count}");

        // Spawn all currently connected clients
        foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            if (!spawnedPlayers.Contains(clientId))
            {
                Vector3 spawnPos = GetSpawnPosition(clientId);
                SpawnPlayer(clientId, spawnPos);
                spawnedPlayers.Add(clientId);
            }
        }

        isInitialSpawnComplete = true;
    }

    private void OnClientConnected(ulong clientId)
    {
        if (!IsServer) return;

        Debug.Log($"[SpawnManager] Client {clientId} connected!");

        // If initial spawn is complete, spawn this client immediately
        if (isInitialSpawnComplete)
        {
            // Wait a moment for client to fully connect
            StartCoroutine(SpawnClientDelayed(clientId));
        }
    }

    private IEnumerator SpawnClientDelayed(ulong clientId)
    {
        // Wait for client to fully connect
        yield return new WaitForSeconds(0.2f);

        if (!IsServer) yield break;

        // Check if already spawned
        if (spawnedPlayers.Contains(clientId))
        {
            Debug.Log($"[SpawnManager] Client {clientId} already spawned");
            yield break;
        }

        Vector3 spawnPos = GetSpawnPosition(clientId);
        SpawnPlayer(clientId, spawnPos);
        spawnedPlayers.Add(clientId);

        Debug.Log($"[SpawnManager] ✅ Client {clientId} spawned!");
    }

    private Vector3 GetSpawnPosition(ulong clientId)
    {
        // Host (0) spawns on left, others on right
        if (clientId == 0)
            return new Vector3(-spawnDistance, 2, 0);
        else
            return new Vector3(spawnDistance, 2, 0);
    }

    private void SpawnPlayer(ulong clientId, Vector3 position)
    {
        if (playerPrefab == null)
        {
            Debug.LogError("[SpawnManager] ❌ Player prefab is NULL!");
            return;
        }

        Debug.Log($"[SpawnManager] Spawning player {clientId} at {position}");

        GameObject player = Instantiate(playerPrefab, position, Quaternion.identity);
        NetworkObject netObj = player.GetComponent<NetworkObject>();

        if (netObj != null)
        {
            netObj.SpawnAsPlayerObject(clientId);
            Debug.Log($"✅ Player {clientId} spawned!");
        }
        else
        {
            Debug.LogError("❌ Player prefab missing NetworkObject!");
            Destroy(player);
        }
    }

    private void OnDisable()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        }
    }
}