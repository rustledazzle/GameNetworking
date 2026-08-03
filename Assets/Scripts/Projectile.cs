using Unity.Netcode;
using UnityEngine;

public class Projectile : NetworkBehaviour
{
    [Header("Combat")]
    [SerializeField] private float damage = 25f;
    [SerializeField] private float lifeTime = 3f;

    [Header("Effects")]
    [SerializeField] private GameObject hitEffectPrefab;

    private Rigidbody rb;
    private Vector3 startVelocity;
    private bool hasStarted = false;
    private bool isServerBullet = false;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        rb = GetComponent<Rigidbody>();
        startVelocity = transform.forward * 30f;

        // Check if this is a server-authoritative bullet
        // If we're on the server AND we're the one who spawned it (or it's a server bullet)
        isServerBullet = IsServer;

        if (IsServer)
        {
            // SERVER BULLET (Host or Server)
            // Destroy after lifetime
            Destroy(gameObject, lifeTime);

            if (rb != null)
            {
                rb.velocity = startVelocity;
                rb.angularVelocity = Vector3.zero;
                rb.useGravity = false;
                rb.isKinematic = false;
                rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            }

            hasStarted = true;
            Debug.Log($"Server bullet spawned at {transform.position}");
        }
        else
        {
            // CLIENT BULLET (Predicted)
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.useGravity = false;
            }

            hasStarted = true;
            Debug.Log($"Client predicted bullet spawned at {transform.position}");
        }
    }

    void Update()
    {
        // CLIENT VISUAL MOVEMENT (only for client-predicted bullets)
        if (!IsServer && hasStarted && !isServerBullet)
        {
            transform.position += startVelocity * Time.deltaTime;
        }
    }

    void FixedUpdate()
    {
        // SERVER PHYSICS MOVEMENT (only for server bullets)
        if (IsServer && rb != null && !rb.isKinematic)
        {
            // Keep the bullet moving on the server
            if (rb.velocity.magnitude < 25f)
            {
                rb.velocity = transform.forward * 30f;
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // ONLY SERVER processes hits
        if (!IsServer) return;

        Debug.Log($"Server bullet hit: {other.gameObject.name}");

        // Check for HealthSystem (zombies, players, etc.)
        HealthSystem health = other.GetComponent<HealthSystem>();
        if (health != null)
        {
            // Apply damage
            health.TakeDamageServerRpc(damage);
            Debug.Log($"Dealt {damage} damage to {other.gameObject.name}");
            DestroyProjectile();
            return;
        }

        // Destroy on any other collision (walls, ground, etc.)
        DestroyProjectile();
    }

    private void DestroyProjectile()
    {
        if (!IsServer) return;

        // Spawn hit effect
        if (hitEffectPrefab != null)
        {
            GameObject effect = Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);
            NetworkObject effectNetObj = effect.GetComponent<NetworkObject>();
            if (effectNetObj != null) effectNetObj.Spawn();
        }

        // Despawn projectile
        NetworkObject projectileNetObj = GetComponent<NetworkObject>();
        if (projectileNetObj != null && projectileNetObj.IsSpawned)
        {
            projectileNetObj.Despawn();
        }
        else
        {
            Destroy(gameObject);
        }
    }
}