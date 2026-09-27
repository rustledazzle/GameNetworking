using Mirror;
using UnityEngine;

public class HealthSystem : NetworkBehaviour
{
    [SyncVar(hook = nameof(OnHealthChanged))]
    public float health = 100f;

    [SyncVar] public float maxHealth = 100f;

    [Header("Respawn Settings")]
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private float respawnY = 0.3f;
    [SerializeField] private float respawnRange = 8f;
    [SerializeField] private float respawnHeightOffset = 0.5f;
    [SerializeField] private float respawnDelay = 3f;

    private CharacterController characterController;
    private bool isDead = false;

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
        if (isDead) return;

        health = Mathf.Max(0, health - damage);
        Debug.Log($"Health: {health}/{maxHealth}");

        if (health <= 0)
            Die();
    }

    private void Die()
    {
        Debug.Log($"[HealthSystem] {gameObject.name} died");
        isDead = true;

        bool isPlayer = GetComponent<PlayerController>() != null;

        if (isPlayer)
        {
            ShowDeathScreenOnOwnerClientRpc();

            PlayerController pc = GetComponent<PlayerController>();
            if (pc != null) pc.enabled = false;

            Teleport(GetRespawnPosition());

            if (isServer)
                Invoke(nameof(RespawnPlayer), respawnDelay);
        }
        else
        {
            NetworkServer.Destroy(gameObject);
        }
    }
    [TargetRpc]
    private void SyncPositionToOwnerClientRpc(Vector3 newPosition)
    {
        Debug.Log($"[HealthSystem] TargetRpc: SyncPosition received → {newPosition}");

        CharacterController cc = GetComponent<CharacterController>();
        bool hadController = cc != null && cc.enabled;

        if (hadController)
            cc.enabled = false;

        transform.position = newPosition;

        if (hadController)
            cc.enabled = true;
    }
    [TargetRpc]
    private void ShowDeathScreenOnOwnerClientRpc()
    {
        Debug.Log("[HealthSystem] TargetRpc: ShowDeathScreen received");
        if (DeathScreenUI.Instance != null)
            DeathScreenUI.Instance.ShowDeathScreen();
    }

    [TargetRpc]
    private void HideDeathScreenOnOwnerClientRpc()
    {
        Debug.Log("[HealthSystem] TargetRpc: HideDeathScreen received");
        if (DeathScreenUI.Instance != null)
            DeathScreenUI.Instance.HideDeathScreen();
    }

    private void RespawnPlayer()
    {
        if (!isServer) return;

        Vector3 spawnPos = GetRespawnPosition();
        Debug.Log($"[HealthSystem] Respawning {gameObject.name} at {spawnPos}");

        Teleport(spawnPos);

        health = maxHealth;
        isDead = false;

        PlayerController pc = GetComponent<PlayerController>();
        if (pc != null) pc.enabled = true;

        HideDeathScreenOnOwnerClientRpc();

        SyncPositionToOwnerClientRpc(spawnPos);
        // ✅ Force the player's position to sync to clients via the existing
        //    PlayerController position sync (CmdUpdatePosition runs every frame).
        //    No NetworkTransform needed.

        Debug.Log($"[HealthSystem] Respawned at {spawnPos}");
    }

    private Vector3 GetRespawnPosition()
    {
        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            int idx = Random.Range(0, spawnPoints.Length);
            return spawnPoints[idx].position;
        }

        GameObject[] spawns = GameObject.FindGameObjectsWithTag("Respawn");
        if (spawns.Length > 0)
        {
            int idx = Random.Range(0, spawns.Length);
            Debug.Log($"[HealthSystem] Using tagged spawn '{spawns[idx].name}' at {spawns[idx].transform.position}");
            return spawns[idx].transform.position;
        }

        Debug.LogWarning("[HealthSystem] No spawn points found — using SAFE fallback");
        Vector3 origin = new Vector3(0f, 20f, 0f);
        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 50f))
            return new Vector3(0f, hit.point.y + 0.5f, 0f);

        return new Vector3(0f, 5f, 0f);
    }

    private void Teleport(Vector3 position)
    {
        if (characterController == null)
            characterController = GetComponent<CharacterController>();

        bool hadController = characterController != null && characterController.enabled;

        if (hadController)
            characterController.enabled = false;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        transform.position = position;
        transform.rotation = Quaternion.identity;

        if (hadController)
            characterController.enabled = true;
    }

    public bool IsDead() => isDead;
}