using Mirror;
using UnityEngine;

public class ZombieSpawnTrigger : NetworkBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private GameObject zombiePrefab;
    [SerializeField] private float spawnLifetime = 4f;   // seconds zombie stays "spawning"
    [SerializeField] private bool triggerOnce = true;    // only spawn once

    private bool hasSpawned = false;

    private void OnTriggerEnter(Collider other)
    {
        if (!isServer) return;
        if (hasSpawned && triggerOnce) return;

        // Only trigger for players
        if (!other.CompareTag("Player")) return;

        Debug.Log($"[ZombieSpawnTrigger] Player entered area at {transform.position}");
        SpawnZombie();
    }

    private void SpawnZombie()
    {
        if (zombiePrefab == null)
        {
            Debug.LogError("[ZombieSpawnTrigger] Zombie prefab missing!");
            return;
        }

        hasSpawned = true;

        // Spawn slightly above ground so gravity pulls it down
        Vector3 spawnPos = transform.position + Vector3.up * 0.5f;

        GameObject zombie = Instantiate(zombiePrefab, spawnPos, Quaternion.identity);

        // ✅ Tell the zombie to stay still for X seconds before chasing
        ZombieAI ai = zombie.GetComponent<ZombieAI>();
        if (ai != null)
            ai.SetSpawnDelay(spawnLifetime);

        NetworkServer.Spawn(zombie);
    }
}