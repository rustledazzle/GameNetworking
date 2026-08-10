using Unity.Netcode;
using UnityEngine;
using System.Collections;

public class NetworkPositionSync : NetworkBehaviour
{
    [Header("Sync Settings")]
    [SerializeField] private float syncRate = 0.05f; // 20 times per second
    [SerializeField] private float positionThreshold = 0.01f;

    private float timer = 0f;
    private Vector3 lastSentPosition;
    private Vector3 lastReceivedPosition;

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

            float distance = Vector3.Distance(transform.position, lastSentPosition);
            if (distance > positionThreshold)
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

        // Smooth interpolation for other clients
        StopAllCoroutines();
        StartCoroutine(SmoothMove(newPos));
    }

    private IEnumerator SmoothMove(Vector3 targetPos)
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