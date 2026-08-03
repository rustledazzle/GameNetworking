using UnityEngine;
using Unity.Netcode;

public class NetworkManagerGlobal : MonoBehaviour
{
    private static NetworkManagerGlobal instance;

    private void Awake()
    {
        // Singleton pattern - only one instance
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Ensure NetworkManager persists
        if (NetworkManager.Singleton != null)
        {
            DontDestroyOnLoad(NetworkManager.Singleton.gameObject);
        }
    }

    private void Start()
    {
        Debug.Log("NetworkManagerGlobal initialized!");
    }
}