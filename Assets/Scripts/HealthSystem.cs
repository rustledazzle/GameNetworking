using Unity.Netcode;
using UnityEngine;

public class HealthSystem : NetworkBehaviour
{
    public NetworkVariable<float> health = new NetworkVariable<float>(100f);
    public NetworkVariable<float> maxHealth = new NetworkVariable<float>(100f);

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

        // Check if this is a player
        if (GetComponent<PlayerController>() != null)
        {
            // Respawn player after delay
            Invoke(nameof(RespawnPlayer), 3f);
        }
        else
        {
            // Zombie or other object - destroy it
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
            transform.position = new Vector3(
                Random.Range(-8f, 8f),
                0,
                Random.Range(-8f, 8f)
            );
            health.Value = maxHealth.Value;
            Debug.Log($"Player respawned at {transform.position}");
        }
    }
}