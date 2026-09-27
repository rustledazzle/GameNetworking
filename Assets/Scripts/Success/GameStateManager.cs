using Mirror;
using UnityEngine;

public class GameStateManager : NetworkBehaviour
{
    public static GameStateManager Instance;

    [SyncVar(hook = nameof(OnMissionCompleteChanged))]
    private bool missionComplete = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        Debug.Log("[GameState] Awake — Instance set");
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        missionComplete = false;
    }

    [Server]
    public void CompleteMission()
    {
        if (missionComplete) return;

        missionComplete = true;
        Debug.Log("[GameState] Mission complete!");
    }

    private void OnMissionCompleteChanged(bool oldValue, bool newValue)
    {
        if (!newValue) return;

        Debug.Log("[GameState] Hook fired — showing UI");

        if (MissionCompleteUI.Instance != null)
            MissionCompleteUI.Instance.Show();
        else
            Debug.LogWarning("[GameState] MissionCompleteUI.Instance is null!");
    }
}