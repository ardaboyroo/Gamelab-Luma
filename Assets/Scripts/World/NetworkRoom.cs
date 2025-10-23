using System.Collections.Generic;
using Unity.Netcode;
using UnityEditor.PackageManager;
using UnityEngine;

[DisallowMultipleComponent]
public class NetworkRoom : NetworkBehaviour
{
    public static Dictionary<string, NetworkRoom> ExistingRooms = new();

    [Header("Room Settings")]
    public string RoomName;
    public List<Transform> Entrances = new();
    public GameObject Container;
    [Space(5f)]
    [SerializeField] private bool _initiallyActive = false;

    [Header("Room Zone Visualization")]
    [SerializeField] private bool _showRoomZone = true;
    [SerializeField] private Color _roomZoneColor = new Color(0.2f, 0.8f, 1f, 0.25f);

    public List<NetworkObject> NetObjects { get; private set; } = new();
    public HashSet<ulong> Members { get; private set; } = new();

    private Collider _trigger;
    private static int _nextLayerIndex = 8;
    private int _roomLayer = 0;

    private void Awake()
    {
        if (!IsServer)
            return;

        _trigger = GetComponent<Collider>();
        _trigger.isTrigger = true;
        gameObject.layer = 0;
    }

    public override void OnNetworkSpawn()
    {
        if (IsClient)
        {
            if(!_initiallyActive)
                Container.SetActive(false);
            return;
        }

        ExistingRooms[RoomName] = this;
        Debug.Log($"[ROOM] Registered '{RoomName}' on server (NetworkObjectId={NetworkObjectId}).");

        // Assign a unique layer for this room
        _roomLayer = GetNextFreeLayer();
        Debug.Log($"[SERVER] Room '{RoomName}' uses layer {_roomLayer}");

        // Physics: only collide with itself and Default (layer 0)
        for (int i = 0; i < 32; i++)
        {
            bool ignore = !(i == 0 || i == _roomLayer);
            Physics.IgnoreLayerCollision(_roomLayer, i, ignore);
            Physics.IgnoreLayerCollision(i, _roomLayer, ignore);
        }

        // Register & assign layer to objects
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
        if (IsServer)
        {
            if (ExistingRooms.TryGetValue(RoomName, out var refRoom) && refRoom == this)
            {
                ExistingRooms.Remove(RoomName);
                Debug.Log($"[ROOM] Unregistered '{RoomName}' (despawn).");
            }
        }
    }

    // ----------------------- Collision-based membership -----------------------

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer)
            return;

        var netObj = other.GetComponentInParent<NetworkObject>();
        if (netObj == null || !netObj.IsPlayerObject)
            return;

        ulong clientId = netObj.OwnerClientId;
        if (Members.Contains(clientId))
            return;

        // Remove from any previous room
        foreach (var r in FindObjectsOfType<NetworkRoom>())
        {
            if (r != this && r.Members.Contains(clientId))
            {
                r.RemoveMember(clientId);
                break;
            }
        }

        AddMember(clientId);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsServer)
            return;

        var netObj = other.GetComponentInParent<NetworkObject>();
        if (netObj == null || !netObj.IsPlayerObject)
            return;

        ulong clientId = netObj.OwnerClientId;
        if (Members.Contains(clientId))
            RemoveMember(clientId);
    }

    // ----------------------- Membership logic ---------------------------------

    public void AddMember(ulong newClientId)
    {
        if (!IsServer || !Members.Add(newClientId))
            return;

        var newPlayer = NetworkManager.Singleton.ConnectedClients[newClientId].PlayerObject;
        if (newPlayer != null)
            ApplyLayerRecursively(newPlayer.gameObject, _roomLayer);

        // Show this room’s objects to that player
        foreach (var no in NetObjects)
            no.NetworkShow(newClientId);

        // Mutual visibility between all players in this room
        foreach (var existingId in Members)
        {
            if (existingId == newClientId) continue;

            var existingPlayer = NetworkManager.Singleton.ConnectedClients[existingId].PlayerObject;
            if (existingPlayer != null && newPlayer != null)
            {
                existingPlayer.NetworkShow(newClientId);
                newPlayer.NetworkShow(existingId);
            }
        }

        SetRoomContainerActive_ClientRpc(true, new ClientRpcParams
        {
            Send = new ClientRpcSendParams { TargetClientIds = new[] { newClientId } }
        });

        Debug.Log($"[SERVER] Player {newClientId} joined room '{RoomName}' (layer {_roomLayer})");
    }

    public void RemoveMember(ulong clientId)
    {
        if (!IsServer || !Members.Remove(clientId))
            return;

        // Reset player back to Default layer
        var leavingPlayer = NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject;
        if (leavingPlayer != null)
            ApplyLayerRecursively(leavingPlayer.gameObject, 0);

        // Hide this room’s objects from that player
        foreach (var no in NetObjects)
            no.NetworkHide(clientId);

        // Remove mutual visibility
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

        SetRoomContainerActive_ClientRpc(false, new ClientRpcParams
        {
            Send = new ClientRpcSendParams { TargetClientIds = new[] { clientId } }
        });

        Debug.Log($"[SERVER] Player {clientId} left room '{RoomName}' and reset to Default layer");
    }

    // ----------------------- Utilities ---------------------------------------

    [ClientRpc]
    private void SetRoomContainerActive_ClientRpc(bool active, ClientRpcParams rpcParams = default)
    {
        if (!IsClient) return;

        Container.SetActive(active);
        Debug.Log($"[CLIENT] Room '{RoomName}' visuals {(active ? "activated" : "deactivated")}");
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