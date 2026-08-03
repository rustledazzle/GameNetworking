using Unity.Netcode;
using UnityEngine;

public class NetworkStarter : MonoBehaviour
{
    void Update()
    {
        // Press H to start as Host (Server + Client)
        if (Input.GetKeyDown(KeyCode.H))
        {
            NetworkManager.Singleton.StartHost();
            Debug.Log("Starting as HOST");
        }
        // Press C to start as Client
        if (Input.GetKeyDown(KeyCode.C))
        {
            NetworkManager.Singleton.StartClient();
            Debug.Log("Starting as CLIENT");
        }
    }
}