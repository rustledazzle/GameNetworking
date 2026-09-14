using Mirror;
using UnityEngine;

public class ZombieSpawnTrigger : MonoBehaviour
{
    [Header("Spawn Settings")]
    [Tooltip("The zombie prefab to spawn.")]
    [SerializeField] private GameObject zombiePrefab;

    [Tooltip("How many zombies to spawn when the player enters.")]
    [SerializeField] private int zombiesToSpawn = 2;

    [Tooltip("Seconds each zombie waits before chasing.")]
    [SerializeField] private float spawnLifetime = 4f;

    [Tooltip("Radius around the trigger where zombies spawn (spread them out).")]
    [SerializeField] private float spawnRadius = 2f;

    [Tooltip("Only fire once, or allow repeat triggers.")]
    [SerializeField] private bool triggerOnce = true;

    [Tooltip("Delay between repeated waves (only used if triggerOnce is false).")]
    [SerializeField] private float repeatCooldown = 15f;

    [Tooltip("Label for reading Console logs more easily.")]
    [SerializeField] private string groupLabel = "Wave 1";

    private bool hasSpawned = false;
    private float lastTriggerTime = -Mathf.Infinity;

    private void OnTriggerEnter(Collider other)
    {
        // ✅ Server-only spawn logic
        if (!NetworkServer.active) return;

        // ✅ Ignore bullets and other non-player triggers
        if (other.GetComponent<Projectile>() != null) return;

        // ✅ Only react to players
        if (!other.CompareTag("Player")) return;

        // ✅ Respect "once" or "cooldown" logic
        if (triggerOnce && hasSpawned) return;
        if (!triggerOnce && Time.time < lastTriggerTime + repeatCooldown) return;

        Debug.Log($"[Trigger:{groupLabel}] Player entered — spawning {zombiesToSpawn} zombie(s)");
        SpawnZombies();
    }

    private void SpawnZombies()
    {
        if (zombiePrefab == null)
        {
            Debug.LogError($"[Trigger:{groupLabel}] Zombie prefab missing!");
            return;
        }

        hasSpawned = true;
        lastTriggerTime = Time.time;

        for (int i = 0; i < zombiesToSpawn; i++)
        {
            // Spread zombies within the trigger radius
            Vector2 offset = Random.insideUnitCircle * spawnRadius;
            Vector3 spawnPos = transform.position + new Vector3(offset.x, 0.5f, offset.y);

            GameObject zombie = Instantiate(zombiePrefab, spawnPos, Quaternion.identity);

            // Delay chase behavior
            ZombieAI ai = zombie.GetComponent<ZombieAI>();
            if (ai != null)
                ai.SetSpawnDelay(spawnLifetime);

            NetworkServer.Spawn(zombie);
        }
    }
}