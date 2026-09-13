using Unity.Netcode;
using UnityEngine;

public class ZombieAI : NetworkBehaviour
{
    [Header("Movement")]
    [SerializeField] private float speed = 2f;
    [SerializeField] private float attackRange = 1.5f;

    [Header("Combat")]
    [SerializeField] private float damage = 10f;
    [SerializeField] private float attackCooldown = 1f;

    [Header("Health")]
    [SerializeField] private float maxHealth = 50f;

    private Transform target;
    private float lastAttackTime = 0f;
    private HealthSystem healthSystem;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // 🔧 FIX 1: Ensure NetworkObject exists
        if (GetComponent<NetworkObject>() == null)
        {
            Debug.LogError($"ZombieAI: Missing NetworkObject component on {gameObject.name}!");
            enabled = false;
            return;
        }

        // Get or add HealthSystem
        healthSystem = GetComponent<HealthSystem>();
        if (healthSystem == null)
        {
            healthSystem = gameObject.AddComponent<HealthSystem>();
            Debug.Log($"Added HealthSystem to {gameObject.name}");
        }

        // Only enable movement on server
        if (!IsServer)
        {
            enabled = false;
            return;
        }

        // Initial target search
        target = FindNearestPlayer();
    }

    private void Update()
    {
        if (!IsServer) return;

        // Check if zombie is dead
        if (healthSystem != null && healthSystem.health.Value <= 0)
        {
            return; // Dead, don't move or attack
        }

        // 🔧 FIX 2: Check if target is still valid
        if (target == null || !IsTargetValid(target))
        {
            target = FindNearestPlayer();
            if (target == null) return;
        }

        float dist = Vector3.Distance(transform.position, target.position);

        // 🔧 FIX 3: Attack logic - works when DISTANCE IS LESS THAN OR EQUAL TO attackRange
        if (dist <= attackRange)
        {
            // Attack player
            if (Time.time >= lastAttackTime + attackCooldown)
            {
                lastAttackTime = Time.time;
                HealthSystem playerHealth = target.GetComponent<HealthSystem>();
                if (playerHealth != null && playerHealth.health.Value > 0)
                {
                    playerHealth.TakeDamageServerRpc(damage);
                    Debug.Log($"Zombie attacked {target.name} for {damage} damage!");
                }
                else
                {
                    // Target is dead, clear it
                    target = null;
                }
            }
        }
        else
        {
            // Move towards player (only if not attacking)
            Vector3 dir = (target.position - transform.position).normalized;
            transform.position += dir * speed * Time.deltaTime;
            transform.LookAt(target);
        }
    }

    // 🔧 NEW METHOD: Check if target is still alive and has HealthSystem
    private bool IsTargetValid(Transform potentialTarget)
    {
        if (potentialTarget == null) return false;

        HealthSystem hs = potentialTarget.GetComponent<HealthSystem>();
        if (hs == null) return false;

        return hs.health.Value > 0;
    }

    private Transform FindNearestPlayer()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        float minDist = Mathf.Infinity;
        Transform nearest = null;

        foreach (GameObject p in players)
        {
            // Check if player is alive
            HealthSystem hs = p.GetComponent<HealthSystem>();
            if (hs == null || hs.health.Value <= 0) continue;

            float d = Vector3.Distance(transform.position, p.transform.position);
            if (d < minDist)
            {
                minDist = d;
                nearest = p.transform;
            }
        }
        return nearest;
    }

    [ServerRpc(RequireOwnership = false)]
    public void TakeDamageServerRpc(float damageAmount)
    {
        if (!IsServer) return;

        if (healthSystem != null)
        {
            healthSystem.TakeDamageServerRpc(damageAmount);
            Debug.Log($"Zombie took {damageAmount} damage. Health: {healthSystem.health.Value}");
        }
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        Debug.Log($"{gameObject.name} destroyed!");
    }
}