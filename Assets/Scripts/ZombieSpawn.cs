using Mirror;
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

    public override void OnStartServer()
    {
        base.OnStartServer();
        Debug.Log("[ZombieSpawner] Server started");
        Invoke(nameof(ForceInitialSpawn), 1f);
    }

    private void ForceInitialSpawn()
    {
        if (!isServer) return;

        for (int i = 0; i < maxZombies; i++)
            SpawnZombie();

        hasSpawnedInitial = true;
    }

    private void Update()
    {
        if (!isServer) return;

        timer += Time.deltaTime;

        if (Input.GetKeyDown(KeyCode.Z))
            SpawnZombie();

        if (timer >= spawnInterval && hasSpawnedInitial)
        {
            timer = 0f;

            int currentCount = CountAliveZombies();
            if (currentCount < maxZombies)
            {
                int toSpawn = maxZombies - currentCount;
                for (int i = 0; i < toSpawn; i++)
                    SpawnZombie();
            }
        }
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
                if (hs == null || hs.health > 0)
                    count++;
            }
        }
        return count;
    }

    private void SpawnZombie()
    {
        if (!isServer) return;
        if (zombiePrefab == null) return;

        Vector3 spawnPos = GetRandomSpawnPosition();
        GameObject zombie = Instantiate(zombiePrefab, spawnPos, Quaternion.identity);
        NetworkServer.Spawn(zombie);
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
                center += player.transform.position;
            center /= players.Length;
        }

        return new Vector3(
            center.x + Mathf.Cos(angle) * distance,
            0,
            center.z + Mathf.Sin(angle) * distance
        );
    }
}