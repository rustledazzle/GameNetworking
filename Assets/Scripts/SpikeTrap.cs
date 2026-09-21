using Mirror;
using UnityEngine;

public class SpikeTrap : MonoBehaviour
{
    [Header("Damage Settings")]
    [Tooltip("Damage dealt to the player on hit.")]
    [SerializeField] private float damage = 5f;

    [Tooltip("Seconds deducted from the shared timer.")]
    [SerializeField] private float timePenalty = 5f;

    [Header("Trigger Settings")]
    [Tooltip("How often the trap can trigger (seconds).")]
    [SerializeField] private float cooldown = 2f;

    [Tooltip("Only trigger for objects with this tag.")]
    [SerializeField] private string targetTag = "Player";

    [Header("Debug")]
    [SerializeField] private string trapLabel = "Spikes";

    private float lastTriggerTime = -Mathf.Infinity;
    private NetworkTimer cachedTimer;

    private void OnTriggerEnter(Collider other)
    {
        // ✅ Server-only
        if (!NetworkServer.active) return;

        // ✅ Ignore bullets
        if (other.GetComponent<Projectile>() != null) return;

        // ✅ Only affect players
        if (!other.CompareTag(targetTag)) return;

        // ✅ Respect cooldown
        if (Time.time < lastTriggerTime + cooldown) return;
        lastTriggerTime = Time.time;

        // ✅ Deal damage
        HealthSystem health = other.GetComponentInParent<HealthSystem>();
        if (health != null && health.health > 0)
        {
            health.TakeDamage(damage);
            Debug.Log($"[{trapLabel}] {other.name} took {damage} damage");
        }

        // ✅ Deduct time
        ApplyTimePenalty();
    }

    private void ApplyTimePenalty()
    {
        if (cachedTimer == null)
            cachedTimer = FindObjectOfType<NetworkTimer>();

        if (cachedTimer != null)
        {
            cachedTimer.ApplyPenalty(timePenalty);
            Debug.Log($"[{trapLabel}] Applied -{timePenalty}s penalty");
        }
        else
        {
            Debug.LogWarning($"[{trapLabel}] No NetworkTimer found in scene!");
        }
    }
}