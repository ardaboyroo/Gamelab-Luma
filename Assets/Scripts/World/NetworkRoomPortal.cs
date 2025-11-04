using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEditor.PackageManager;
using UnityEngine;

public class PendingParty
{
    public Party Party;
    public string TargetRoom;
    public int TargetSize;

    public PendingParty(Party party, string targetRoom, int targetSize)
    {
        Party = party;
        TargetRoom = targetRoom;
        TargetSize = targetSize;
    }

    public bool CombineParties(Party party)
    {
        if (Party.GetPlayers().Length + party.GetPlayers().Length > TargetSize)
            return false;

        party.GetPlayers().ToList().ForEach(p => Party.AddPlayer(p));
        party.Disband();
        return true;
    }
}

public class NetworkRoomPortal : NetworkBehaviour
{
    public static NetworkRoomPortal _randomInstance;

    public enum EntryMode : byte { Single, Party, Full }

    [SerializeField] private string _roomName;
    [SerializeField] private byte _entranceID = 0;

    private List<PendingParty> _pendingParties;
    private bool _allowsSingle => NetworkRoom.ExistingRooms[_roomName.ToLower()].Instances[0].AllowsSingle;
    private bool _allowsParty => NetworkRoom.ExistingRooms[_roomName.ToLower()].Instances[0].AllowsParty;
    private bool _allowsFullParty => NetworkRoom.ExistingRooms[_roomName.ToLower()].Instances[0].AllowsFullParty;
    private bool _singleInstance => NetworkRoom.ExistingRooms[_roomName.ToLower()].Instances[0].SingleInstance;

    private void Awake()
    {
        _randomInstance = this;

        if (!IsServer)
            return;

        if (_allowsFullParty && !_singleInstance)
            _pendingParties = new();

        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer)
            return;

        var netObj = other.GetComponentInParent<NetworkObject>();
        if (netObj == null || !netObj.IsPlayerObject)
            return;

        if (_singleInstance)
        {
            NormalEnter();
        }
        else
        {
            // send info to the triggering client
            var clientId = netObj.OwnerClientId;
            if (NetworkRoom.ExistingRooms.TryGetValue(_roomName.ToLower(), out var refRoom))
            {
                var instance = refRoom.Original;
                ShowRoomDialogueClientRpc(
                    _roomName,
                    instance.ActivityDescription,
                    instance.AllowsSingle,
                    instance.AllowsParty,
                    instance.AllowsFullParty,
                    new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = new[] { clientId } } }
                );
            }
        }
        void NormalEnter()
        {
            TeleportSingle(netObj);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestEnterServerRpc(byte mode, ServerRpcParams rpc = default)
    {
        var senderId = rpc.Receive.SenderClientId;
        var netObj = NetworkManager.Singleton.ConnectedClients[senderId].PlayerObject;

        switch ((EntryMode)mode)
        {
            case EntryMode.Single: SingleEnter(); break;
            case EntryMode.Party: PartyEnter(); break;
            case EntryMode.Full: FullPartyEnter(); break;
        }

        //- LOCALS -----------------------------------------------

        void SingleEnter()
        {
            if (Party.ClientToPartyMap.TryGetValue(netObj.NetworkObjectId, out var party))
                Party.ClientToPartyMap[netObj.NetworkObjectId].KickPlayer(netObj.NetworkObjectId);

            TeleportSingle(netObj);
        }
        void PartyEnter()
        {
            if (!Party.ClientToPartyMap.TryGetValue(netObj.NetworkObjectId, out var party))
                party = new Party(netObj.NetworkObjectId);

            TeleportParty(party);
        }
        void FullPartyEnter()
        {
            TryJoinPendingParty(netObj);
            ValidatePendingParties();
        }
    }

    [ClientRpc]
    private void ShowRoomDialogueClientRpc(string roomName, string description, bool allowSingle, bool allowParty, bool allowFull, ClientRpcParams rpc = default)
    {
        if (!IsOwner && !IsClient) return;
        PortalClientUI.Show(this, roomName, description, allowSingle, allowParty, allowFull);
    }

    public static void ForceAssign(ulong clientId, string roomName, byte entranceID)
    {
        if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
            return;

        var netObj = client.PlayerObject;

        if (NetworkRoom.ExistingRooms.TryGetValue(roomName.ToLower(), out var roomInstances))
        {
            var nextRoom = roomInstances.GetOrCreateInstance();

            var target = nextRoom.Entrances[entranceID];

            if (netObj.TryGetComponent<NetworkTransform>(out var netTransform))
                netTransform.Teleport(target.position, target.rotation, netObj.transform.localScale);
            else
                netObj.transform.SetPositionAndRotation(target.position, target.rotation);

            _randomInstance.ForceClientTeleportClientRpc(target.position, target.rotation, netObj.OwnerClientId);

            if (NetworkRoom.PlayerRoomMap.TryGetValue(netObj.OwnerClientId, out NetworkRoom previousRoom))
            {
                previousRoom.RemoveMember(netObj.OwnerClientId);
                nextRoom.AddMember(netObj.OwnerClientId);
            }
            else // In this case player was never register to any of the rooms, so security breach is possible. -> Kick just in case.
            {
                nextRoom.AddMember(netObj.OwnerClientId);
                //NetworkManager.Singleton.DisconnectClient(netObj.OwnerClientId);
                Debug.LogError($"[SERVER] Player {netObj.OwnerClientId} was kicked due to possible security breach (Teleport attempt with no previous room registration)");
            }
        }
        else
            Debug.LogError($"[SERVER] Room {roomName} does not exist!");
    }

    private void ValidatePendingParties()
    {
        List<PendingParty> toRemove = new();
        foreach (var pendingParty  in _pendingParties)
        {
            if(pendingParty.TargetSize == pendingParty.Party.GetPlayers().Length)
                toRemove.Add(pendingParty);
        }

        foreach (var party in toRemove)
        {
            _pendingParties.Remove(party);
            TeleportParty(party.Party);
        }
    }

    private void TeleportSingle(NetworkObject netObj)
    {
        if (NetworkRoom.ExistingRooms.TryGetValue(_roomName.ToLower(), out var roomInstances))
        {
            var nextRoom = roomInstances.GetOrCreateInstance();

            var target = nextRoom.Entrances[_entranceID];

            if (netObj.TryGetComponent<NetworkTransform>(out var netTransform))
                netTransform.Teleport(target.position, target.rotation, netObj.transform.localScale);
            else
                netObj.transform.SetPositionAndRotation(target.position, target.rotation);

            //ForceClientTeleportClientRpc(target.position, target.rotation, netObj.OwnerClientId);

            if (NetworkRoom.PlayerRoomMap.TryGetValue(netObj.OwnerClientId, out NetworkRoom previousRoom))
            {
                previousRoom.RemoveMember(netObj.OwnerClientId);
                nextRoom.AddMember(netObj.OwnerClientId);
            }
            else // In this case player was never register to any of the rooms, so security breach is possible. -> Kick just in case.
            {
                nextRoom.AddMember(netObj.OwnerClientId);
                //NetworkManager.Singleton.DisconnectClient(netObj.OwnerClientId);
                Debug.LogError($"[SERVER] Player {netObj.OwnerClientId} was kicked due to possible security breach (Teleport attempt with no previous room registration)");
            }
        }
        else
            Debug.LogError($"[SERVER] Room {_roomName} does not exist!");
    }

    private PendingParty TryJoinPendingParty(NetworkObject netObj)
    {
        if(!Party.ClientToPartyMap.TryGetValue(netObj.OwnerClientId, out var party))
            party = new Party(netObj.OwnerClientId);

        foreach (var pendingParty in _pendingParties)
        {
            //if(pendingParty..)

            if (pendingParty.CombineParties(party))
                return pendingParty;
        }

        var newPendingParty = new PendingParty(party, _roomName, NetworkRoom.ExistingRooms[_roomName.ToLower()].Instances[0].InstanceCapacity);
        _pendingParties.Add(newPendingParty);
        return newPendingParty;
    }

    private void TeleportParty(Party party)
    {

    }

    [ClientRpc]
    private void ForceClientTeleportClientRpc(Vector3 pos, Quaternion rot, ulong targetClientId)
    {
        if (NetworkManager.Singleton.LocalClientId != targetClientId)
            return;

        var player = NetworkManager.Singleton.LocalClient?.PlayerObject;
        if (player != null)
            player.transform.SetPositionAndRotation(pos, rot);
    }
}
