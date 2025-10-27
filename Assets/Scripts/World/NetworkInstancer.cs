using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class NetworkInstancer : MonoBehaviour
{
    public struct InstanceRefs
    {
        public List<Transform> Entrances;
        public Transform Container;
        public NetworkRoom Room;
        public NetworkObject NetworkObject;
    }

    public struct InstanceResult
    {
        public GameObject Parent;
        public GameObject Container;
        public NetworkRoom Room;
    }

    /// <summary>
    /// Creates and spawns a new networked instance of the registered prefab.
    /// </summary>
    public InstanceResult CreateInstance(int instanceID)
    {
        var roomName = GetComponentInChildren<NetworkRoom>().RoomName.ToLower();
        var prefab = NetworkRoomRegistry.Prefabs[roomName];

        if (prefab == null)
        {
            Debug.LogError("[NetworkInstancer] Prefab reference missing!");
            return default;
        }

        var obj = Instantiate(prefab, transform.parent);
        obj.name = $"{prefab.name}_Instance{instanceID}";

        var refs = GetRefs(obj.gameObject);

        obj.transform.position += new Vector3(0, instanceID * (refs.Room.transform.localScale.y + 100f), 0);

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
        {
            if (!obj.IsSpawned)
            {
                obj.Spawn(true);
                Debug.Log($"[SERVER] Spawned network instance '{refs.Room?.RoomName}' (NetworkObjectId={obj.NetworkObjectId})");
            }
        }

        return new InstanceResult
        {
            Parent = obj.gameObject,
            Container = refs.Container?.gameObject,
            Room = refs.Room
        };
    }

    /// <summary>
    /// Collects all sub-references inside a freshly instantiated prefab.
    /// </summary>
    private InstanceRefs GetRefs(GameObject obj)
    {
        var room = obj.GetComponentInChildren<NetworkRoom>(true);
        var networkObject = obj.GetComponent<NetworkObject>();

        Transform entrancesContainer = null;
        Transform container = null;
        var entrances = new List<Transform>();

        for (int i = 0; i < obj.transform.childCount; i++)
        {
            var child = obj.transform.GetChild(i);
            if (child.CompareTag("Entrances"))
                entrancesContainer = child;
            else if (child.CompareTag("Container"))
                container = child;
        }

        if (entrancesContainer != null)
        {
            for (int i = 0; i < entrancesContainer.childCount; i++)
                entrances.Add(entrancesContainer.GetChild(i));
        }

        return new InstanceRefs
        {
            NetworkObject = networkObject,
            Entrances = entrances,
            Container = container,
            Room = room
        };
    }
}