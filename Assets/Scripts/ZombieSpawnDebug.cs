using Mirror;
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

    public override void OnStartServer()
    {
        base.OnStartServer();

        // EASY TO FIND LOGS
        Debug.Log("========================================");
        Debug.Log("🔥 ZOMBIE SPAWNER DEBUG");
        Debug.Log("========================================");
        Debug.Log($"  • isServer: {isServer}");
        Debug.Log($"  • isClient: {isClient}");
        Debug.Log($"  • Prefab assigned: {(zombiePrefab != null ? "✅ YES" : "❌ NO")}");
        Debug.Log($"  • Max Zombies: {maxZombies}");
        Debug.Log($"  • GameObject active: {gameObject.activeInHierarchy}");
        Debug.Log($"  • Scene: {gameObject.scene.name}");
        Debug.Log("========================================");

        Debug.Log("🟢 SERVER MODE - Zombies will spawn!");
        Invoke(nameof(ForceInitialSpawn), 1f);
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        if (!isServer)
        {
            enabled = false;
            Debug.Log("🔵 CLIENT MODE - Spawner disabled (zombies spawn from server)");
        }
    }

    private void ForceInitialSpawn()
    {
        if (!isServer) return;

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
        if (!isServer) return;

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

        if (!isServer)
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

        // ✅ Mirror: Check for NetworkIdentity
        NetworkIdentity netIdentity = zombie.GetComponent<NetworkIdentity>();
        if (netIdentity == null)
        {
            Debug.LogError("❌ Zombie prefab missing NetworkIdentity! Add it to the prefab.");
            Destroy(zombie);
            return false;
        }

        // ✅ Mirror: Spawn on server
        NetworkServer.Spawn(zombie);

        int total = CountAliveZombies();
        Debug.Log($"✅ Zombie spawned! Total alive: {total}/{maxZombies}");
        return true;
    }

    private int CountAliveZombies()
    {
        int count = 0;
        ZombieAI[] zombies = FindObjectsByType<ZombieAI>(FindObjectsSortMode.None);

        foreach (var zombie in zombies)
        {
            if (zombie.isActiveAndEnabled)
            {
                HealthSystem hs = zombie.GetComponent<HealthSystem>();
                if (hs != null && hs.health > 0)
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

    public override void OnStopServer()
    {
        base.OnStopServer();
        Debug.Log($"💀 ZombieSpawner destroyed after {spawnAttempts} spawn attempts");
    }
}