using Mirror;
using UnityEngine;
using System.Collections.Generic;

public class DualPressurePlate : NetworkBehaviour
{
    [Header("Plate Settings")]
    [SerializeField] private string targetTag = "Player";
    [SerializeField] private int requiredPlayers = 2;

    [Header("Visual Feedback")]
    [SerializeField] private Renderer plateRenderer;
    [SerializeField] private Color idleColor = new Color(1f, 0.86f, 0.16f);
    [SerializeField] private Color partialColor = new Color(1f, 0.55f, 0.16f);
    [SerializeField] private Color activeColor = new Color(0.24f, 0.86f, 0.31f);

    [Header("Gate")]
    [SerializeField] private GameObject gateObject;
    [SerializeField] private Vector3 openPosition;
    [SerializeField] private Vector3 closedPosition;
    [SerializeField] private float gateMoveSpeed = 3f;

    [Header("Behavior")]
    [SerializeField] private bool oneTimeOpen = false;

    [Header("Audio (optional)")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip openSound;
    [SerializeField] private AudioClip closeSound;

    [Header("Debug")]
    [SerializeField] private string plateLabel = "Dual Plate";

    // ✅ Synced state — 0 = idle, 1 = partial, 2 = active
    [SyncVar(hook = nameof(OnColorStateChanged))]
    private int colorState = 0;

    private readonly HashSet<uint> playersOnPlate = new HashSet<uint>();
    private bool isGateOpen = false;
    private Vector3 targetGatePosition;

    private void Start()
    {
        if (gateObject != null)
        {
            if (openPosition == Vector3.zero && closedPosition == Vector3.zero)
            {
                closedPosition = gateObject.transform.position;
                openPosition = closedPosition + Vector3.up * 5f;
            }
            targetGatePosition = closedPosition;
        }

        // ✅ Apply initial color on both server and client
        ApplyColor(colorState);
    }

    private void Update()
    {
        if (!NetworkServer.active) return;
        if (gateObject == null) return;

        gateObject.transform.position = Vector3.MoveTowards(
            gateObject.transform.position,
            targetGatePosition,
            gateMoveSpeed * Time.deltaTime
        );
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!NetworkServer.active) return;
        if (!other.CompareTag(targetTag)) return;

        NetworkIdentity id = other.GetComponentInParent<NetworkIdentity>();
        if (id == null) return;

        if (playersOnPlate.Add(id.netId))
        {
            Debug.Log($"[{plateLabel}] {other.name} entered ({playersOnPlate.Count}/{requiredPlayers})");
            CheckActivation();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!NetworkServer.active) return;
        if (!other.CompareTag(targetTag)) return;

        NetworkIdentity id = other.GetComponentInParent<NetworkIdentity>();
        if (id == null) return;

        if (playersOnPlate.Remove(id.netId))
        {
            Debug.Log($"[{plateLabel}] {other.name} left ({playersOnPlate.Count}/{requiredPlayers})");
            CheckActivation();
        }
    }

    private void CheckActivation()
    {
        bool shouldOpen = playersOnPlate.Count >= requiredPlayers;

        // ✅ Update the syncvar — this automatically updates all clients
        if (playersOnPlate.Count == 0)
            colorState = 0;
        else if (playersOnPlate.Count < requiredPlayers)
            colorState = 1;
        else
            colorState = 2;

        if (shouldOpen && !isGateOpen)
        {
            OpenGate();
        }
        else if (!shouldOpen && isGateOpen && !oneTimeOpen)
        {
            CloseGate();
        }
    }

    // ✅ Client hook — runs on every client when colorState changes
    private void OnColorStateChanged(int oldValue, int newValue)
    {
        ApplyColor(newValue);
    }

    private void ApplyColor(int state)
    {
        if (plateRenderer == null) return;

        switch (state)
        {
            case 0: plateRenderer.material.color = idleColor; break;
            case 1: plateRenderer.material.color = partialColor; break;
            case 2: plateRenderer.material.color = activeColor; break;
        }
    }

    private void OpenGate()
    {
        isGateOpen = true;
        targetGatePosition = openPosition;
        Debug.Log($"[{plateLabel}] ✅ Gate opening");

        if (audioSource != null && openSound != null)
            audioSource.PlayOneShot(openSound);
    }

    private void CloseGate()
    {
        isGateOpen = false;
        targetGatePosition = closedPosition;
        Debug.Log($"[{plateLabel}] Gate closing");

        if (audioSource != null && closeSound != null)
            audioSource.PlayOneShot(closeSound);
    }
}