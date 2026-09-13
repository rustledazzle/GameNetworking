using Mirror;
using UnityEngine;

public class QuickStart : MonoBehaviour
{
    void Start()
    {
        Debug.Log("=== CYBERLOBBY QUICK START ===");
        Debug.Log("Press H to Start as HOST");
        Debug.Log("Press C to Start as CLIENT");
        Debug.Log("Press T to Open Chat");
    }

    void Update()
    {
        // Press H to start as Host
        if (Input.GetKeyDown(KeyCode.H))
        {
            if (NetworkManager.singleton != null)
            {
                NetworkManager.singleton.StartHost();
                Debug.Log("✅ Started as HOST!");
            }
            else
            {
                Debug.LogError("❌ NetworkManager not found!");
            }
        }

        // Press C to start as Client
        if (Input.GetKeyDown(KeyCode.C))
        {
            if (NetworkManager.singleton != null)
            {
                NetworkManager.singleton.StartClient();
                Debug.Log("✅ Started as CLIENT!");
            }
            else
            {
                Debug.LogError("❌ NetworkManager not found!");
            }
        }
    }
}