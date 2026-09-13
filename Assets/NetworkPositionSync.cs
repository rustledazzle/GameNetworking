using Mirror;
using UnityEngine;
using System.Collections;

public class NetworkPositionSync : NetworkBehaviour
{
    [Header("Sync Settings")]
    [SerializeField] private float syncRate = 0.05f;
    [SerializeField] private float positionThreshold = 0.01f;
    [SerializeField] private float interpolationDuration = 0.1f;

    // ✅ SyncVar with hook
    [SyncVar(hook = nameof(OnSyncedPositionChanged))]
    private Vector3 syncedPosition;

    private float timer = 0f;
    private Vector3 lastSentPosition;

    public override void OnStartServer()
    {
        base.OnStartServer();
        syncedPosition = transform.position;
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        lastSentPosition = transform.position;
    }

    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();
        lastSentPosition = transform.position;
    }

    void Update()
    {
        // Only the local player sends position updates
        if (!isLocalPlayer) return;

        timer += Time.deltaTime;
        if (timer >= syncRate)
        {
            timer = 0f;

            float distance = Vector3.Distance(transform.position, lastSentPosition);
            if (distance > positionThreshold)
            {
                CmdUpdatePosition(transform.position);
                lastSentPosition = transform.position;
            }
        }
    }

    // Mirror Command: Client → Server
    [Command]
    private void CmdUpdatePosition(Vector3 newPos)
    {
        // Anti-cheat: reject large jumps
        if (Vector3.Distance(transform.position, newPos) > 10f)
        {
            Debug.LogWarning($"Rejected suspicious position update from {netId}");
            return;
        }

        transform.position = newPos;
        syncedPosition = newPos;
    }

    // SyncVar hook: called on clients when syncedPosition changes
    private void OnSyncedPositionChanged(Vector3 oldValue, Vector3 newValue)
    {
        // Skip if this is the local player (we already moved ourselves)
        if (isLocalPlayer) return;

        StopAllCoroutines();
        StartCoroutine(SmoothMove(newValue));
    }

    private IEnumerator SmoothMove(Vector3 targetPos)
    {
        float elapsed = 0f;
        Vector3 startPos = transform.position;

        while (elapsed < interpolationDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / interpolationDuration;
            transform.position = Vector3.Lerp(startPos, targetPos, t);
            yield return null;
        }

        transform.position = targetPos;
    }
}