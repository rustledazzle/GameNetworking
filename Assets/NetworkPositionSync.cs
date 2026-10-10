using Mirror;
using UnityEngine;
using System.Collections;

public class NetworkPositionSync : NetworkBehaviour
{
    [Header("Sync Settings")]
    [SerializeField] private float syncRate = 0.05f;
    [SerializeField] private float positionThreshold = 0.01f;
    [SerializeField] private float interpolationDuration = 0.1f;

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
        // ✅ Only the local player sends position updates — ALWAYS
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

    [Command]
    private void CmdUpdatePosition(Vector3 newPos)
    {
        // Anti-cheat: reject large jumps
        if (Vector3.Distance(transform.position, newPos) > 20f)
        {
            Debug.LogWarning($"[NetworkPositionSync] Rejected suspicious jump from {netId}: {Vector3.Distance(transform.position, newPos):F1}m");
            return;
        }

        transform.position = newPos;
        syncedPosition = newPos;
    }

    private void OnSyncedPositionChanged(Vector3 oldValue, Vector3 newValue)
    {
        // Skip on the local player — prediction handles their own view
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