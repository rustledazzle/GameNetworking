using Unity.Netcode;
using UnityEngine;

public class ZombieSpawner : NetworkBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private GameObject zombiePrefab;
    [SerializeField] private int maxZombies = 5;
    [SerializeField] private float spawnInterval = 2f;
    [SerializeField] private float spawnRadius = 10f;

    private float timer = 0f;
    private bool hasSpawnedInitial = false;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        Debug.Log($"[ZombieSpawner] ===== SPAWNER INITIALIZED =====");
        Debug.Log($"[ZombieSpawner] IsServer: {IsServer}");
        Debug.Log($"[ZombieSpawner] Prefab assigned: {(zombiePrefab != null ? "YES" : "NO")}");
        Debug.Log($"[ZombieSpawner] Max zombies: {maxZombies}");

        if (IsServer)
        {
            // Force spawn initial zombies
            Invoke(nameof(ForceInitialSpawn), 0.5f);
        }
        else
        {
            enabled = false;
            Debug.Log("[ZombieSpawner] Client - disabling");
        }
    }

    private void ForceInitialSpawn()
    {
        if (!IsServer) return;

        Debug.Log("[ZombieSpawner] === FORCE INITIAL SPAWN ===");

        for (int i = 0; i < maxZombies; i++)
        {
            SpawnZombie();
        }
        hasSpawnedInitial = true;
        Debug.Log($"[ZombieSpawner] Initial spawn complete: {CountAliveZombies()}/{maxZombies} zombies");
    }

    private void Update()
    {
        if (!IsServer) return;

        timer += Time.deltaTime;

        // 🔥 DEBUG: Press Z key to force spawn
        if (Input.GetKeyDown(KeyCode.Z))
        {
            Debug.Log("[ZombieSpawner] 🔥 Z key pressed - Force spawning zombie!");
            SpawnZombie();
        }

        // Normal spawning logic
        if (timer >= spawnInterval && hasSpawnedInitial)
        {
            timer = 0f;

            int currentCount = CountAliveZombies();

            if (currentCount < maxZombies)
            {
                int toSpawn = maxZombies - currentCount;
                Debug.Log($"[ZombieSpawner] Spawning {toSpawn} zombie(s)...");
                for (int i = 0; i < toSpawn; i++)
                {
                    SpawnZombie();
                }
            }
        }
    }

    private int CountAliveZombies()
    {
        int count = 0;
        ZombieAI[] zombies = FindObjectsByType<ZombieAI>(FindObjectsSortMode.None);

        foreach (var zombie in zombies)
        {
            if (zombie.IsSpawned && zombie.gameObject.activeInHierarchy)
            {
                // Check if zombie is alive (has health > 0)
                HealthSystem hs = zombie.GetComponent<HealthSystem>();
                if (hs != null && hs.health.Value > 0)
                {
                    count++;
                }
                else if (hs == null)
                {
                    // No HealthSystem - count as alive
                    count++;
                }
            }
        }
        return count;
    }

    private void SpawnZombie()
    {
        if (!IsServer) return;

        if (zombiePrefab == null)
        {
            Debug.LogError("[ZombieSpawner] ❌ Zombie prefab is NULL! Assign it in Inspector!");
            return;
        }

        Vector3 spawnPos = GetRandomSpawnPosition();
        Debug.Log($"[ZombieSpawner] Spawning zombie at: {spawnPos}");

        GameObject zombie = Instantiate(zombiePrefab, spawnPos, Quaternion.identity);
        NetworkObject netObj = zombie.GetComponent<NetworkObject>();

        if (netObj != null)
        {
            netObj.Spawn();
            Debug.Log($"[ZombieSpawner] ✅ Zombie spawned! Total: {CountAliveZombies()}/{maxZombies}");
        }
        else
        {
            Debug.LogError("[ZombieSpawner] ❌ Zombie prefab missing NetworkObject component!");
            Destroy(zombie);
        }
    }

    private Vector3 GetRandomSpawnPosition()
    {
        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        float distance = Random.Range(3f, spawnRadius);

        // Try to spawn near center
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        Vector3 center = Vector3.zero;

        if (players.Length > 0)
        {
            foreach (var player in players)
            {
                center += player.transform.position;
            }
            center /= players.Length;
        }

        return new Vector3(
            center.x + Mathf.Cos(angle) * distance,
            0,
            center.z + Mathf.Sin(angle) * distance
        );
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        Debug.Log("[ZombieSpawner] Destroyed");
    }
}