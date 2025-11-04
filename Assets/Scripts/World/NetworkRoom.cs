using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEditor.PackageManager;
using UnityEngine;
using static UnityEditor.ShaderGraph.Internal.KeywordDependentCollection;
using static UnityEngine.UI.Image;

[Serializable]
public class Party
{
    public static Dictionary<ulong, Party> ClientToPartyMap = new();

    private List<ulong> _players = new();

    public ulong Leader => _players.Count > 0 ? _players[0] : 0;

    public Party(ulong leaderClientId)
    {
        AddPlayer(leaderClientId);
        ClientToPartyMap[leaderClientId] = this;
    }

    public ulong[] GetPlayers() => _players.ToArray();
    public void KickPlayer(ulong clientId)
    {
        _players.Remove(clientId);
        ClientToPartyMap[clientId] = null;
    }
    public bool AddPlayer(ulong clientId)
    {
        if (_players.Contains(clientId))
            return false;
        _players.Add(clientId);
        ClientToPartyMap[clientId] = this;
        return true;
    }
    public void Disband()
    {
        foreach (var clientId in _players)
            ClientToPartyMap[clientId] = null;
        _players.Clear();
    }
}

[Serializable]
public class NetworkRoomInstances
{
    private readonly NetworkInstancer _instancer;
    public Dictionary<int, NetworkRoom> Instances;
    public NetworkRoom Original => Instances[0];

    public NetworkRoomInstances(NetworkRoom originalInstance, NetworkInstancer instancer) 
    { 
        Instances = new Dictionary<int, NetworkRoom> { { 0, originalInstance } }; 
        _instancer = instancer; 
    }
    public int GetInstanceID(NetworkRoom instance) 
    {
        foreach (var kvp in Instances)
        {
            if(kvp.Value == instance)
                return kvp.Key;
        }
        return 0;
    }
    public NetworkRoom GetInstanceByID(int id) => Instances[id];
    public NetworkRoom GetOrCreateInstance() 
    {
        if (Original.SingleInstance)
            return Original;

        NetworkRoom result = null;

        foreach (var instance in Instances.Values)
        {
            if (instance == Original || instance.IsFull || instance.IsLocked)
                continue;

            result = instance;
            break;
        }

        if (result == null)
        {
            var freeSpot = GetFreeSpotID();
            var createdInstanceResult = _instancer.CreateInstance(freeSpot);
            result = createdInstanceResult.Room;

            Instances.Add(freeSpot, result);
          
            Debug.Log($"[SERVER] Created new instance of room '{Original.RoomName.ToLower()}' (InstanceID={Instances.Count - 1}, NetworkObjectId={result.NetworkObjectId})");
        }

        return result;
    }

    public int GetFreeSpotID()
    {
        for (int i = 1; i < Instances.Count; i++)
            if (!Instances.ContainsKey(i))
                return i;

        var newID = Instances.Count;
        while (Instances.ContainsKey(newID))
            newID++;

        return newID;
    }

    public void PurgeUnusedInstances()
    {
        List<NetworkRoom> toRemove = new();
        for (int i = 1; i < Instances.Count-1; i++)
        {
            if (Instances[i].IsEmpty)
                toRemove.Add(Instances[i]);
        }

        foreach (var instance in toRemove)
        {
            Instances.Remove(GetInstanceID(instance));
            UnityEngine.Object.Destroy(instance.transform.parent.gameObject);
        }
    }
}

[DisallowMultipleComponent]
public class NetworkRoom : NetworkBehaviour
{
    public static Dictionary<string, NetworkRoomInstances> ExistingRooms = new();
    public static Dictionary<ulong, NetworkRoom> PlayerRoomMap = new();

    [Header("Room Settings")]
    public string RoomName;
    public List<Transform> Entrances = new();
    public GameObject Container;
    [Space(5f)]
    [SerializeField] public bool SingleInstance = true;
    [SerializeField] public bool LockOnStart = false;
    [SerializeField] public bool AllowsSingle = true;
    [SerializeField] public bool AllowsParty = true;
    [SerializeField] public bool AllowsFullParty = true;
    [SerializeField] public int  InstanceCapacity = 512;

    [Space(5f)]
    [SerializeField] private bool _initiallyActive = false;

    [Space(15f)]
    [Header("Room Metadata")]
    [SerializeField][TextArea(2, 5)] public string ActivityDescription = "Default room activity.";
    [SerializeField] public Sprite ActivityIcon;
    
    [Space(15f)]
    [Header("Room Zone Visualization")]
    [SerializeField] private bool _showRoomZone = true;
    [SerializeField] private Color _roomZoneColor = new Color(0.2f, 0.8f, 1f, 0.25f);
    
    [Space(15f)]
    [Header("Room Zone Actor (Optional)")]
    [SerializeField] private BaseNetworkRoomActor _actor;

    //----------------------------------------------------------------------------

    public List<NetworkObject> NetObjects { get; private set; } = new();
    public HashSet<ulong> Members { get; private set; } = new();

    public bool IsEmpty => Members.Count == 0;
    public bool IsFull => Members.Count >= InstanceCapacity;
    public bool IsLocked { get; private set; } = false;

    //----------------------------------------------------------------------------

    private Collider _trigger;
    private static int _nextLayerIndex = 8;
    private int _roomLayer = 0;

    //----------------------------------------------------------------------------

    private void Awake()
    {
        if (!IsServer)
            return;

        _trigger = GetComponent<Collider>();
        _trigger.isTrigger = true;
        gameObject.layer = 0;
    }

    private void Update()
    {
        if (!IsServer)
            return;

        try
        {

            List<ulong> toRemove = new();

            foreach (var clientId in Members)
            {
                if (!NetworkManager.Singleton.ConnectedClientsIds.Contains(clientId))
                {
                    toRemove.Add(clientId);
                    continue;
                }
                _actor?.OnClientUpdate(clientId);
            }

            foreach (var clientId in toRemove)
                RemoveMember(clientId, clientIsOffline: true);
        }
        catch
        {

        }
    }

    public override void OnNetworkSpawn()
    {
        if (IsClient)
        {
            if(!_initiallyActive)
                Container.SetActive(false);
        }
        if (!IsServer)
            return;

        if (!ExistingRooms.ContainsKey(RoomName.ToLower()))
            ExistingRooms[RoomName.ToLower()] = new(this, GetComponentInParent<NetworkInstancer>());

        Debug.Log($"[ROOM] Registered '{RoomName.ToLower()}' on server (NetworkObjectId={NetworkObjectId}).");

        if(!SingleInstance)
            StartCoroutine(InstanceGarbageCollector());

        _roomLayer = GetNextFreeLayer();
        Debug.Log($"[SERVER] Room '{RoomName.ToLower()}' uses layer {_roomLayer}");

        for (int i = 0; i < 32; i++)
        {
            bool ignore = !(i == 0 || i == _roomLayer);
            Physics.IgnoreLayerCollision(_roomLayer, i, ignore);
            Physics.IgnoreLayerCollision(i, _roomLayer, ignore);
        }

        NetObjects.Clear();
        foreach (var no in GetComponentsInChildren<NetworkObject>(true))
        {
            if (no != NetworkObject)
                NetObjects.Add(no);

            foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
                no.NetworkHide(client.ClientId);
        }

        for (int i = 0; i < transform.childCount; i++)
            ApplyLayerRecursively(transform.GetChild(i).gameObject, _roomLayer);
    }
    public override void OnNetworkDespawn()
    {
        if (IsServer && !IsClient)
        {
            if (ExistingRooms.TryGetValue(RoomName.ToLower(), out var refRoom) && refRoom.Original == this)
            {
                ExistingRooms.Remove(RoomName.ToLower());
                Debug.Log($"[ROOM] Unregistered '{RoomName.ToLower()}' (despawn).");
            }
        }
    }

    private IEnumerator InstanceGarbageCollector()
    {
        if (!IsServer)
            yield break;

        while (!this.IsDestroyed())
        {
            yield return new WaitForSeconds(60f);
            ExistingRooms[RoomName.ToLower()].PurgeUnusedInstances();
        }
    }

    // ----------------------- Membership logic ---------------------------------

    public void JoinAsParty(Party party)
    {
        var players = party.GetPlayers();
        foreach (var player in players)
        {
            PlayerRoomMap[player].RemoveMember(player);
            AddMember(player);
        }
    }

    public void AddMember(ulong clientId)
    {
        if (!IsServer || !Members.Add(clientId))
            return;

        _actor?.OnClientEntering(clientId);

        var newPlayer = NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject;
        if (newPlayer != null)
            ApplyLayerRecursively(newPlayer.gameObject, _roomLayer);

        foreach (var no in NetObjects)
            no.NetworkShow(clientId);

        foreach (var existingId in Members)
        {
            if (existingId == clientId) continue;

            var existingPlayer = NetworkManager.Singleton.ConnectedClients[existingId].PlayerObject;
            if (existingPlayer != null && newPlayer != null)
            {
                existingPlayer.NetworkShow(clientId);
                newPlayer.NetworkShow(existingId);
            }
        }
        PlayerRoomMap[clientId] = this;

        SetRoomContainerActive_ClientRpc(true, new ClientRpcParams{ Send = new ClientRpcSendParams { TargetClientIds = new[] { clientId } }});

        Debug.Log($"[SERVER] Player {clientId} joined room '{RoomName.ToLower()}' (layer {_roomLayer})");

        if (_actor != null)
        {
            _actor.OnClientAwake(clientId);
            _actor.OnClientStart(clientId);
        }

        if (LockOnStart)
            IsLocked = true;
    }

    public void RemoveMember(ulong clientId, bool clientIsOffline = false)
    {
        if (!IsServer || !Members.Remove(clientId) || clientIsOffline)
            return;

        _actor?.OnClientExiting(clientId);

        var leavingPlayer = NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject;
        if (leavingPlayer != null)
            ApplyLayerRecursively(leavingPlayer.gameObject, 0);

        foreach (var no in NetObjects)
            no.NetworkHide(clientId);

        foreach (var otherId in Members)
        {
            var other = NetworkManager.Singleton.ConnectedClients[otherId].PlayerObject;
            var leaving = leavingPlayer;
            if (other != null && leaving != null)
            {
                leaving.NetworkHide(otherId);
                other.NetworkHide(clientId);
            }
        }
        PlayerRoomMap[clientId] = null;

        //SetRoomContainerActive_ClientRpc(false, new ClientRpcParams{ Send = new ClientRpcSendParams { TargetClientIds = new[] { clientId } }});

        Debug.Log($"[SERVER] Player {clientId} left room '{RoomName.ToLower()}' and reset to Default layer");
    }

    // ----------------------- Utilities ---------------------------------------

    [ClientRpc]
    private void SetRoomContainerActive_ClientRpc(bool active, ClientRpcParams rpcParams = default)
    {
        if (!IsClient) return;

        Container.SetActive(active);
        Debug.Log($"[CLIENT] Room '{RoomName.ToLower()}' visuals {(active ? "activated" : "deactivated")}");
    }

    private static int GetNextFreeLayer()
    {
        if (_nextLayerIndex >= 31)
        {
            Debug.LogWarning("[SERVER] Out of free layers, wrapping to 8");
            _nextLayerIndex = 8;
        }
        return _nextLayerIndex++;
    }

    private static void ApplyLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
            ApplyLayerRecursively(child.gameObject, layer);
    }

    // ----------------------- Gizmos ------------------------------------------
#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!_showRoomZone)
            return;

        Color oldColor = Gizmos.color;
        Vector3 tpos = transform.position + new Vector3(0, transform.localScale.y * 0.5f, 0);

        // Fill
        Gizmos.color = _roomZoneColor;
        Gizmos.DrawCube(tpos, transform.localScale);

        // Edges
        Gizmos.color = new Color(_roomZoneColor.r, _roomZoneColor.g, _roomZoneColor.b, 1f);
        Vector3 half = transform.localScale * 0.5f;
        Vector3[] c =
        {
            new Vector3(-half.x,-half.y,-half.z),
            new Vector3( half.x,-half.y,-half.z),
            new Vector3( half.x,-half.y, half.z),
            new Vector3(-half.x,-half.y, half.z),
            new Vector3(-half.x, half.y,-half.z),
            new Vector3( half.x, half.y,-half.z),
            new Vector3( half.x, half.y, half.z),
            new Vector3(-half.x, half.y, half.z)
        };
        int[,] e = {
            {0,1},{1,2},{2,3},{3,0},
            {4,5},{5,6},{6,7},{7,4},
            {0,4},{1,5},{2,6},{3,7}
        };
        for (int i = 0; i < e.GetLength(0); i++)
            Gizmos.DrawLine(tpos + c[e[i, 0]], tpos + c[e[i, 1]]);

        Gizmos.color = oldColor;
    }
#endif
}