using Minigames.Sprayway;
using Unity.Netcode;
using UnityEngine;

public class CharacterEditRoomActor : BaseNetworkRoomActor
{
    private Rooms.CharacterEdit.CharacterEditManager _manager;

    public override void OnClientStart(ulong clientId)
    {
        base.OnClientStart(clientId);

        Debug.Log("[SERVER] CharacterEditRoomActor OnClientStart for " + clientId);

        if (_manager == null)
        {
            Debug.Log("[SERVER] Finding CharacterEditManager for " + clientId);
            _manager = transform.parent.GetComponentInChildren<Rooms.CharacterEdit.CharacterEditManager>(true);
            _manager.Room = NetworkRoom.PlayerRoomMap[clientId];
            Debug.Log("[SERVER] Found CharacterEditManager: " + _manager + " for " + clientId);
        }
        Debug.Log("[SERVER] Setting UI for " + clientId);

        _manager.SetUIClientRPC(clientId);
        _manager.ShowUIClientRpc(clientId);
       

        _manager.SetMovement(false, clientId);
        _manager.SetAnimatorModeClientRPC(true, clientId);
    }

    public override void OnClientExiting(ulong clientId)
    {
        _manager.SetMovement(true, clientId);
        _manager.SetAnimatorModeClientRPC(false, clientId);
        _manager.HideUIClientRpc(clientId);
        
        base.OnClientExiting(clientId);
    }
}