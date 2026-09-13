using Mirror;
using UnityEngine;

public class GameSceneManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SpawnManager spawnManager;

    private void Start()
    {
        Debug.Log("=== GAME SCENE LOADED ===");

        // Read startup mode from PlayerPrefs
        string mode = PlayerPrefs.GetString("StartupMode", "");
        Debug.Log($"Startup mode from PlayerPrefs: '{mode}'");

        if (string.IsNullOrEmpty(mode))
        {
            Debug.LogError("❌ No startup mode found! StartMenu must set PlayerPrefs before loading.");
            return;
        }

        if (mode == "host")
        {
            Debug.Log("🟢 Starting as HOST...");
            if (NetworkManager.singleton != null)
            {
                NetworkManager.singleton.StartHost();
                Debug.Log("✅ Host started!");
            }
            else
            {
                Debug.LogError("❌ NetworkManager not found!");
            }
        }
        else if (mode == "client")
        {
            Debug.Log("🔵 Starting as CLIENT...");
            if (NetworkManager.singleton != null)
            {
                NetworkManager.singleton.StartClient();
                Debug.Log("✅ Client started!");
            }
            else
            {
                Debug.LogError("❌ NetworkManager not found!");
            }
        }

        // Clear PlayerPrefs so it doesn't persist
        PlayerPrefs.DeleteKey("StartupMode");
        PlayerPrefs.Save();
    }
}