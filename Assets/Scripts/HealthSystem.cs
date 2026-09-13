using Unity.Netcode;
using UnityEngine;

public class HealthSystem : NetworkBehaviour
{
    public NetworkVariable<float> health = new NetworkVariable<float>(100f);
    public NetworkVariable<float> maxHealth = new NetworkVariable<float>(100f);

    [Header("Respawn Settings")]
    [SerializeField] private float respawnY = 0.3f;  // ← Adjust this in Inspector!
    [SerializeField] private float respawnRange = 8f;
    [SerializeField] private float respawnHeightOffset = 0.5f;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsServer)
        {
            health.Value = maxHealth.Value;
            Debug.Log($"HealthSystem initialized with {health.Value} HP");
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void TakeDamageServerRpc(float damage)
    {
        if (!IsServer) return;

        health.Value = Mathf.Max(0, health.Value - damage);
        Debug.Log($"Health: {health.Value}/{maxHealth.Value}");

        if (health.Value <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        Debug.Log($"Object died: {gameObject.name}");

        if (GetComponent<PlayerController>() != null)
        {
            Invoke(nameof(RespawnPlayer), 3f);
        }
        else
        {
            NetworkObject netObj = GetComponent<NetworkObject>();
            if (netObj != null && netObj.IsSpawned)
            {
                netObj.Despawn();
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }

    private void RespawnPlayer()
    {
        if (IsServer)
        {
            Vector3 spawnPos = GetGroundPosition();
            transform.position = spawnPos;
            health.Value = maxHealth.Value;
            Debug.Log($"Player respawned at {transform.position}");
        }
    }

    private Vector3 GetGroundPosition()
    {
        // Random X and Z position
        float x = Random.Range(-respawnRange, respawnRange);
        float z = Random.Range(-respawnRange, respawnRange);

        Vector3 spawnPos = new Vector3(x, 10f, z);

        // Raycast to find ground
        RaycastHit hit;
        if (Physics.Raycast(spawnPos, Vector3.down, out hit, 20f))
        {
            // Place player on ground with offset
            float y = hit.point.y + respawnHeightOffset;
            return new Vector3(x, y, z);
        }

        // Fallback: use Inspector value
        return new Vector3(x, respawnY, z);
    }
}