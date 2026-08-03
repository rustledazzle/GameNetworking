using Unity.Netcode;
using UnityEngine;

public class ZombieSpawner_Debug : NetworkBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private GameObject zombiePrefab;
    [SerializeField] private int maxZombies = 5;
    [SerializeField] private float spawnInterval = 2f;
    [SerializeField] private float spawnRadius = 10f;

    private float timer = 0f;
    private bool hasSpawnedInitial = false;
    private int spawnAttempts = 0;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // EASY TO FIND LOGS
        Debug.Log("========================================");
        Debug.Log("🔥 ZOMBIE SPAWNER DEBUG");
        Debug.Log("========================================");
        Debug.Log($"  • IsServer: {IsServer}");
        Debug.Log($"  • IsClient: {IsClient}");
        Debug.Log($"  • IsHost: {IsHost}");
        Debug.Log($"  • Prefab assigned: {(zombiePrefab != null ? "✅ YES" : "❌ NO")}");
        Debug.Log($"  • Max Zombies: {maxZombies}");
        Debug.Log($"  • GameObject active: {gameObject.activeInHierarchy}");
        Debug.Log($"  • Scene: {gameObject.scene.name}");
        Debug.Log("========================================");

        if (IsServer)
        {
            Debug.Log("🟢 SERVER MODE - Zombies will spawn!");
            Invoke(nameof(ForceInitialSpawn), 1f);
        }
        else
        {
            enabled = false;
            Debug.Log("🔵 CLIENT MODE - Spawner disabled (zombies spawn from server)");
        }
    }

    private void ForceInitialSpawn()
    {
        if (!IsServer) return;

        Debug.Log("🚀 FORCE INITIAL SPAWN STARTING...");

        int spawned = 0;
        for (int i = 0; i < maxZombies; i++)
        {
            if (SpawnZombie())
            {
                spawned++;
            }
        }
        hasSpawnedInitial = true;
        Debug.Log($"✅ Initial spawn complete! Spawned {spawned}/{maxZombies} zombies");
    }

    private void Update()
    {
        if (!IsServer) return;

        timer += Time.deltaTime;

        // DEBUG: Press Z key to force spawn
        if (Input.GetKeyDown(KeyCode.Z))
        {
            Debug.Log("🔥 Z KEY PRESSED - Force spawning!");
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
                Debug.Log($"🔄 Spawning {toSpawn} zombie(s) (Current: {currentCount}/{maxZombies})");
                for (int i = 0; i < toSpawn; i++)
                {
                    SpawnZombie();
                }
            }
        }
    }

    private bool SpawnZombie()
    {
        spawnAttempts++;
        Debug.Log($"📍 Spawn attempt #{spawnAttempts}");

        if (!IsServer)
        {
            Debug.LogWarning("❌ Not server - cannot spawn!");
            return false;
        }

        if (zombiePrefab == null)
        {
            Debug.LogError("❌❌❌ ZOMBIE PREFAB IS NULL! Assign it in Inspector!");
            return false;
        }

        Vector3 spawnPos = GetRandomSpawnPosition();
        Debug.Log($"📍 Spawning zombie at: {spawnPos}");

        GameObject zombie = Instantiate(zombiePrefab, spawnPos, Quaternion.identity);
        NetworkObject netObj = zombie.GetComponent<NetworkObject>();

        if (netObj != null)
        {
            netObj.Spawn();
            int total = CountAliveZombies();
            Debug.Log($"✅ Zombie spawned! Total alive: {total}/{maxZombies}");
            return true;
        }
        else
        {
            Debug.LogError("❌ Zombie prefab missing NetworkObject!");
            Destroy(zombie);
            return false;
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
                HealthSystem hs = zombie.GetComponent<HealthSystem>();
                if (hs != null && hs.health.Value > 0)
                {
                    count++;
                }
                else if (hs == null)
                {
                    count++;
                }
            }
        }
        return count;
    }

    private Vector3 GetRandomSpawnPosition()
    {
        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        float distance = Random.Range(3f, spawnRadius);

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
        Debug.Log($"💀 ZombieSpawner destroyed after {spawnAttempts} spawn attempts");
    }
}