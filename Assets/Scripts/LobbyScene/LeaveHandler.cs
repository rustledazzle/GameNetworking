using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LeaveHandler : MonoBehaviour
{
    [Header("Offline Scene Name")]
    [SerializeField] private string offlineSceneName = "0_OfflineScene";

    // Called by the "Leave Lobby" button
    public void LeaveLobby()
    {
        if (NetworkServer.active && NetworkClient.isConnected)
        {
            // We are the HOST: stop hosting entirely
            Debug.Log("[Leave] Host stopping server");
            NetworkManager.singleton.StopHost();
        }
        else if (NetworkClient.isConnected)
        {
            // We are a CLIENT: disconnect from the host
            Debug.Log("[Leave] Client disconnecting");
            NetworkManager.singleton.StopClient();
        }

        // Return everyone to the offline scene
        // (Mirror's offline scene setting usually handles this automatically,
        //  but we force it in case)
        if (!string.IsNullOrEmpty(offlineSceneName))
            SceneManager.LoadScene(offlineSceneName);
    }

    // Optional: hook this to a "Quit" button
    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}