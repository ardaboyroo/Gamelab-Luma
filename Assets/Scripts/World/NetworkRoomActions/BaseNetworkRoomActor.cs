using UnityEngine;

public class BaseNetworkRoomActor : MonoBehaviour, INetworkRoomActor
{
    public virtual void OnClientEntering(ulong clientId) { Debug.Log($"[SERVER] {clientId} Enter"); }

    public virtual void OnClientExiting(ulong clientId) { Debug.Log($"[SERVER] {clientId} Exit"); }

    public virtual void OnClientAwake(ulong clientId) { Debug.Log($"[SERVER] {clientId} Awake"); }
    public virtual void OnClientStart(ulong clientId) { Debug.Log($"[SERVER] {clientId} Start"); }
    public virtual void OnClientUpdate(ulong clientId) {  }
}
