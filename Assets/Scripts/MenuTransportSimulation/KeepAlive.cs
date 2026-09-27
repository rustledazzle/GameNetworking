using UnityEngine;
public class KeepAlive : MonoBehaviour
{
    void Awake() { DontDestroyOnLoad(gameObject); }
}