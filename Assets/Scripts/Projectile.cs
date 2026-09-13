using Mirror;
using UnityEngine;

public class Projectile : NetworkBehaviour
{
    [Header("Combat")]
    [SerializeField] private float damage = 25f;
    [SerializeField] private float lifeTime = 3f;
    [SerializeField] private float speed = 30f;

    [SyncVar] private Vector3 velocity;

    private Rigidbody rb;
    private bool hasLaunched;

    public override void OnStartServer()
    {
        base.OnStartServer();

        rb = GetComponent<Rigidbody>();

        // Server decides velocity, then stores it in the SyncVar
        velocity = transform.forward * speed;

        if (rb != null)
        {
            rb.velocity = velocity;
            rb.angularVelocity = Vector3.zero;
        }

        Destroy(gameObject, lifeTime);
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        // Client: enable kinematic so server authority doesn't fight it
        rb = GetComponent<Rigidbody>();
        if (!isServer && rb != null)
        {
            rb.isKinematic = true;
        }
    }

    private void Update()
    {
        // ✅ Client-side visual movement — no physics, no jitter
        if (!isServer && hasLaunched)
        {
            transform.position += velocity * Time.deltaTime;
        }

        // Start moving once we've received the velocity from the server
        if (!isServer && !hasLaunched && velocity.sqrMagnitude > 0.001f)
        {
            hasLaunched = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isServer) return;

        HealthSystem health = other.GetComponentInParent<HealthSystem>();
        if (health != null)
        {
            health.TakeDamage(damage);
            NetworkServer.Destroy(gameObject);
            return;
        }

        if (!other.isTrigger)
        {
            NetworkServer.Destroy(gameObject);
        }
    }
}