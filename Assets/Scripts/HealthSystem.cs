using Mirror;
using UnityEngine;

public class HealthSystem : NetworkBehaviour
{
    [SyncVar(hook = nameof(OnHealthChanged))]
    public float health = 100f;

    [SyncVar] public float maxHealth = 100f;

    [Header("Respawn Settings")]
    [SerializeField] private Transform[] spawnPoints;          // ✅ assign in Inspector
    [SerializeField] private float respawnY = 0.3f;
    [SerializeField] private float respawnRange = 8f;
    [SerializeField] private float respawnHeightOffset = 0.5f;

    private CharacterController characterController;

    public override void OnStartServer()
    {
        base.OnStartServer();
        characterController = GetComponent<CharacterController>();
        health = maxHealth;
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        if (isLocalPlayer)
        {
            PlayerHUD hud = FindObjectOfType<PlayerHUD>();
            if (hud != null)
                hud.UpdateHealthFromNetwork(health, maxHealth);
        }
    }

    private void OnHealthChanged(float oldValue, float newValue)
    {
        if (!isLocalPlayer) return;

        PlayerHUD hud = FindObjectOfType<PlayerHUD>();
        if (hud != null)
            hud.UpdateHealthFromNetwork(newValue, maxHealth);
    }

    public void TakeDamage(float damage)
    {
        if (!isServer) return;

        health = Mathf.Max(0, health - damage);
        Debug.Log($"Health: {health}/{maxHealth}");

        if (health <= 0)
            Die();
    }

    private void Die()
    {
        Debug.Log($"Object died: {gameObject.name}");

        if (GetComponent<PlayerController>() != null)
        {
            // Hide / disable player while dead (optional)
            SetPlayerActive(false);

            Invoke(nameof(RespawnPlayer), 3f);
        }
        else
        {
            NetworkServer.Destroy(gameObject);
        }
    }

    private void RespawnPlayer()
    {
        if (!isServer) return;

        Vector3 spawnPos = GetRespawnPosition();
        Teleport(spawnPos);

        health = maxHealth;
        SetPlayerActive(true);

        Debug.Log($"Player respawned at {spawnPos}");
    }

    private Vector3 GetRespawnPosition()
    {
        // ✅ Look up spawn points by the "Respawn" tag
        GameObject[] spawns = GameObject.FindGameObjectsWithTag("Respawn");

        if (spawns.Length > 0)
        {
            int idx = Random.Range(0, spawns.Length);
            return spawns[idx].transform.position;
        }

        // Fallback: random position near center
        float x = Random.Range(-respawnRange, respawnRange);
        float z = Random.Range(-respawnRange, respawnRange);
        Vector3 origin = new Vector3(x, 10f, z);

        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 20f))
            return new Vector3(x, hit.point.y + respawnHeightOffset, z);

        return new Vector3(x, respawnY, z);
    }
    // ✅ Teleport properly (CharacterController safe)
    private void Teleport(Vector3 position)
    {
        bool hadController = characterController != null;

        // Disable controller so it doesn't overwrite our position
        if (hadController)
            characterController.enabled = false;

        transform.position = position;

        // Re-enable the controller
        if (hadController)
            characterController.enabled = true;

        // ✅ Optionally reset velocity if you have a Rigidbody
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    private void SetPlayerActive(bool active)
    {
        // Optional: disable/enable player visuals / input
        PlayerController pc = GetComponent<PlayerController>();
        if (pc != null) pc.enabled = active;

        // CharacterController affects physics; keep it on even while dead (adjust if needed)
    }
}