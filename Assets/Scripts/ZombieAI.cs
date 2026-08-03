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

        // Get or add HealthSystem
        healthSystem = GetComponent<HealthSystem>();
        if (healthSystem == null)
        {
            // Add HealthSystem if it doesn't exist
            healthSystem = gameObject.AddComponent<HealthSystem>();
            Debug.Log("Added HealthSystem to zombie");
        }

        // Only enable movement on server
        if (!IsServer)
        {
            enabled = false;
        }
    }

    private void Update()
    {
        if (!IsServer) return;

        // Check if zombie is dead
        if (healthSystem != null && healthSystem.health.Value <= 0)
        {
            // Zombie is dead, don't move
            return;
        }

        if (target == null)
            target = FindNearestPlayer();

        if (target != null)
        {
            float dist = Vector3.Distance(transform.position, target.position);
            if (dist > attackRange)
            {
                Vector3 dir = (target.position - transform.position).normalized;
                transform.position += dir * speed * Time.deltaTime;
                transform.LookAt(target);
            }
            else
            {
                if (Time.time >= lastAttackTime + attackCooldown)
                {
                    lastAttackTime = Time.time;
                    HealthSystem playerHealth = target.GetComponent<HealthSystem>();
                    if (playerHealth != null)
                    {
                        playerHealth.TakeDamageServerRpc(damage);
                        Debug.Log($"Zombie attacked player for {damage} damage");
                    }
                }
            }
        }
    }

    private Transform FindNearestPlayer()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        float minDist = Mathf.Infinity;
        Transform nearest = null;

        foreach (GameObject p in players)
        {
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
        Debug.Log("Zombie destroyed!");
    }
}