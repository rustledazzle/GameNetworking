using Unity.Netcode;
using UnityEngine;

public class NetworkPositionSync : NetworkBehaviour
{
    [Header("Sync Settings")]
    [SerializeField] private float syncRate = 0.05f; // 20 times per second

    private float timer = 0f;
    private Vector3 lastSentPosition;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsOwner)
        {
            lastSentPosition = transform.position;
        }
    }

    void Update()
    {
        if (!IsOwner) return;

        timer += Time.deltaTime;
        if (timer >= syncRate)
        {
            timer = 0f;

            if (Vector3.Distance(transform.position, lastSentPosition) > 0.01f)
            {
                UpdatePositionServerRpc(transform.position);
                lastSentPosition = transform.position;
            }
        }
    }

    [ServerRpc]
    private void UpdatePositionServerRpc(Vector3 newPos)
    {
        transform.position = newPos;
        UpdatePositionClientRpc(newPos);
    }

    [ClientRpc]
    private void UpdatePositionClientRpc(Vector3 newPos)
    {
        if (IsOwner) return;

        StopAllCoroutines();
        StartCoroutine(SmoothMove(newPos));
    }

    private System.Collections.IEnumerator SmoothMove(Vector3 targetPos)
    {
        float duration = 0.1f;
        float elapsed = 0f;
        Vector3 startPos = transform.position;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            transform.position = Vector3.Lerp(startPos, targetPos, t);
            yield return null;
        }

        transform.position = targetPos;
    }
}