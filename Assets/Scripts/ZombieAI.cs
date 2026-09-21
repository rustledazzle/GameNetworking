using Mirror;
using UnityEngine;

public class ZombieAI : NetworkBehaviour
{
    [Header("Movement")]
    [SerializeField] private float speed = 2f;
    [SerializeField] private float attackRange = 1.5f;
    [SerializeField] private float gravity = -9.81f;
    [SerializeField] private float rotationSpeed = 8f;

    [Header("Combat")]
    [SerializeField] private float damage = 10f;
    [SerializeField] private float attackCooldown = 1f;
    [Tooltip("Seconds removed from the shared timer each hit.")]
    [SerializeField] private float timePenalty = 30f;

    [Header("Spawn Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private string spawnTriggerName = "Spawn";

    private Transform target;
    private float lastAttackTime = 0f;
    private HealthSystem healthSystem;
    private CharacterController controller;
    private float verticalVelocity = 0f;

    // Cached reference to the timer (found lazily to avoid per-hit lookups)
    private NetworkTimer cachedTimer;

    // Spawn delay
    private float spawnDelayRemaining = 0f;
    private bool hasSpawned = false;

    public override void OnStartServer()
    {
        base.OnStartServer();

        healthSystem = GetComponent<HealthSystem>();
        if (healthSystem == null)
            healthSystem = gameObject.AddComponent<HealthSystem>();

        controller = GetComponent<CharacterController>();
        animator = animator != null ? animator : GetComponentInChildren<Animator>();
    }

    // Called by ZombieSpawnTrigger
    public void SetSpawnDelay(float seconds)
    {
        spawnDelayRemaining = seconds;
        hasSpawned = true;

        if (animator != null && !string.IsNullOrEmpty(spawnTriggerName))
            animator.SetTrigger(spawnTriggerName);

        Debug.Log($"[ZombieAI] Spawn delay set to {seconds}s");
    }

    private void Update()
    {
        if (!isServer) return;

        if (healthSystem != null && healthSystem.health <= 0)
            return;

        // Handle spawn delay
        if (spawnDelayRemaining > 0f)
        {
            spawnDelayRemaining -= Time.deltaTime;

            if (controller != null)
            {
                if (controller.isGrounded && verticalVelocity < 0)
                    verticalVelocity = -2f;

                verticalVelocity += gravity * Time.deltaTime;
                controller.Move(new Vector3(0, verticalVelocity, 0) * Time.deltaTime);
            }
            return;
        }

        // Find target
        if (target == null || !IsTargetValid(target))
        {
            target = FindNearestPlayer();
            if (target == null) return;
        }

        // Gravity
        if (controller != null)
        {
            if (controller.isGrounded && verticalVelocity < 0)
                verticalVelocity = -2f;

            verticalVelocity += gravity * Time.deltaTime;
        }

        float dist = Vector3.Distance(transform.position, target.position);

        if (dist <= attackRange)
        {
            if (Time.time >= lastAttackTime + attackCooldown)
            {
                lastAttackTime = Time.time;

                HealthSystem playerHealth = target.GetComponent<HealthSystem>();
                if (playerHealth != null && playerHealth.health > 0)
                {
                    playerHealth.TakeDamage(damage);
                    Debug.Log($"Zombie attacked {target.name} for {damage} damage!");

                    // ✅ NEW: Deduct time from the shared timer
                    ApplyTimePenalty();
                }
                else target = null;
            }

            // Face target
            Vector3 lookDir = target.position - transform.position;
            lookDir.y = 0f;
            if (lookDir.sqrMagnitude > 0.001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(lookDir);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
            }

            if (controller != null)
                controller.Move(new Vector3(0, verticalVelocity, 0) * Time.deltaTime);
        }
        else
        {
            // Chase
            Vector3 dir = target.position - transform.position;
            dir.y = 0f;
            dir.Normalize();

            Vector3 move = dir * speed;
            move.y = verticalVelocity;

            if (controller != null)
                controller.Move(move * Time.deltaTime);
            else
                transform.position += move * Time.deltaTime;

            if (dir.sqrMagnitude > 0.001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(dir);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
            }
        }
    }

    // ✅ NEW: Deducts time from the shared NetworkTimer
    private void ApplyTimePenalty()
    {
        if (!isServer) return;

        // Cache the timer to avoid per-hit FindObjectOfType calls
        if (cachedTimer == null)
            cachedTimer = FindObjectOfType<NetworkTimer>();

        if (cachedTimer != null)
        {
            cachedTimer.ApplyPenalty();
            Debug.Log($"[ZombieAI] Applied -{timePenalty}s penalty");
        }
        else
        {
            Debug.LogWarning("[ZombieAI] No NetworkTimer found in scene!");
        }
    }

    private bool IsTargetValid(Transform potentialTarget)
    {
        if (potentialTarget == null) return false;
        HealthSystem hs = potentialTarget.GetComponent<HealthSystem>();
        return hs != null && hs.health > 0;
    }

    private Transform FindNearestPlayer()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        float minDist = Mathf.Infinity;
        Transform nearest = null;

        foreach (GameObject p in players)
        {
            HealthSystem hs = p.GetComponent<HealthSystem>();
            if (hs == null || hs.health <= 0) continue;

            float d = Vector3.Distance(transform.position, p.transform.position);
            if (d < minDist)
            {
                minDist = d;
                nearest = p.transform;
            }
        }
        return nearest;
    }
}