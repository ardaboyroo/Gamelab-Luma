using UnityEngine;

public class HipHopMusicZone : MonoBehaviour
{
    public int gameState = 0;
    public float intensity = 15f;
    public int gameEndState = 2;

    [Header("Is this the MAIN big zone?")]
    public bool isMainZone = false;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        FmodHipHop.Instance.ColliderSetParameters(gameState, intensity, gameEndState, this);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        FmodHipHop.Instance.ResetHipHopToIdle(this);
    }
}
