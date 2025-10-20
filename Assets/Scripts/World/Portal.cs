using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

public class Portal : NetworkBehaviour
{
    [SerializeField] private NetworkRoom _room;
    [SerializeField] private byte _entranceID = 0;

    private void Awake()
    {
        if (!IsServer)
            return;

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

        var target = _room.Entrances[_entranceID];

        var netTransform = netObj.GetComponent<NetworkTransform>();
        if (netTransform)
            netTransform.Teleport(target.position, target.rotation, netObj.transform.localScale);
        else
            netObj.transform.SetPositionAndRotation(target.position, target.rotation);
    }
}