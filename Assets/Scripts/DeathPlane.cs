using Mirror;
using UnityEngine;

public class DeathPlane : MonoBehaviour
{
    [Header("Damage Settings")]
    [Tooltip("Damage dealt to the player. Use 999 to instantly kill.")]
    [SerializeField] private float damage = 999f;

    [Header("Filter")]
    [Tooltip("Only trigger for objects with this tag.")]
    [SerializeField] private string targetTag = "Player";

    [Header("Debug")]
    [SerializeField] private string planeLabel = "Death Plane";

    private void OnTriggerEnter(Collider other)
    {
        // ✅ Server-only
        if (!NetworkServer.active) return;

        // ✅ Only process if the incoming object is tagged Player
        if (!other.CompareTag(targetTag)) return;

        // ✅ Only handle the FIRST collider that has the HealthSystem
        HealthSystem health = other.GetComponentInParent<HealthSystem>();

        if (health == null)
        {
            // Not a player — silently ignore (bullets, props, etc.)
            return;
        }

        if (health.health <= 0)
        {
            // Already dead, no need to trigger again
            return;
        }

        // ✅ Apply lethal damage → triggers the respawn flow
        health.TakeDamage(damage);
        Debug.Log($"[{planeLabel}] {other.name} fell and was respawned.");
    }
}