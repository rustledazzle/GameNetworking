using Mirror;
using UnityEngine;
using System.Collections.Generic;

public class SuccessZone : MonoBehaviour
{
    [Header("Zone Settings")]
    [Tooltip("Only players with this tag count toward completion.")]
    [SerializeField] private string targetTag = "Player";

    [Tooltip("How many players must be inside the zone to complete.")]
    [SerializeField] private int requiredPlayers = 2;

    [Tooltip("Label used in Console logs.")]
    [SerializeField] private string zoneLabel = "Extraction Zone";

    [Header("Feedback")]
    [SerializeField] private bool debugLogs = true;

    // Server-side: which players are inside (we use netId, not connectionId)
    private readonly HashSet<uint> playersInside = new HashSet<uint>();

    private void OnTriggerEnter(Collider other)
    {
        if (!NetworkServer.active) return;
        if (!other.CompareTag(targetTag)) return;

        // ✅ Look for the NetworkIdentity up the hierarchy
        NetworkIdentity id = other.GetComponentInParent<NetworkIdentity>();
        if (id == null) return;

        // ✅ Use netId — Mirror's equivalent of an object ID
        uint idKey = id.netId;

        if (playersInside.Add(idKey))
        {
            if (debugLogs)
                Debug.Log($"[{zoneLabel}] Player {idKey} entered ({playersInside.Count}/{requiredPlayers})");

            CheckCompletion();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!NetworkServer.active) return;
        if (!other.CompareTag(targetTag)) return;

        NetworkIdentity id = other.GetComponentInParent<NetworkIdentity>();
        if (id == null) return;

        uint idKey = id.netId;

        if (playersInside.Remove(idKey))
        {
            if (debugLogs)
                Debug.Log($"[{zoneLabel}] Player {idKey} left ({playersInside.Count}/{requiredPlayers})");
        }
    }

    private void CheckCompletion()
    {
        if (playersInside.Count < requiredPlayers) return;

        Debug.Log($"[{zoneLabel}] ✅ All {requiredPlayers} players extracted! Mission complete.");

        // ✅ Notify all clients that the mission is complete
        if (GameStateManager.Instance != null)
            GameStateManager.Instance.CompleteMission();
        else
            Debug.LogWarning($"[{zoneLabel}] GameStateManager.Instance is null!");
    }
}