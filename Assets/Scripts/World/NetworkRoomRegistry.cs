using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

[DefaultExecutionOrder(-200)]
public class NetworkRoomRegistry : MonoBehaviour
{
    [SerializeField] private SerializableDictionary<string, NetworkObject> _prefabsEditor; 

    private static Dictionary<string, NetworkObject> _prefabs;
    public static Dictionary<string, NetworkObject> Prefabs => _prefabs;

    private void Awake()
    {
        _prefabs = new Dictionary<string, NetworkObject>();
        foreach (var prefab in _prefabsEditor)
            _prefabs.Add(prefab.Key.ToLower(), prefab.Value);
    }
}
