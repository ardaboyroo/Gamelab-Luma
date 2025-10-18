using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public abstract class SnapshotBasedBehaviour<StateType> : NetworkBehaviour
{
    [Header("Snapshots")]
    [SerializeField] private byte _maxSnapshots = 5;

    protected NetworkVariable<StateType> _onServerState = new
    (
        readPerm: NetworkVariableReadPermission.Everyone,
        writePerm: NetworkVariableWritePermission.Server
    );

    protected Queue<Snapshot<StateType>> _snapshots = new(); 

    protected virtual void Update()
    {
        if (_snapshots.Count >= 2)
            _initInterpolation();
    }

    private void _initInterpolation()
    {
        Snapshot<StateType>[] array = _snapshots.ToArray();
        Snapshot<StateType> from = array[0];
        Snapshot<StateType> to = array[1];

        float duration = to.Timestamp - from.Timestamp;
        float elapsed = Time.time - from.Timestamp;
        float t = Mathf.Clamp01(elapsed / duration);
        
        Interpolate(from, to, t);

        if (t >= 1f) _snapshots.Dequeue();
    }

    protected abstract void Interpolate(Snapshot<StateType> from, Snapshot<StateType> to, float t);

    public override void OnNetworkSpawn()
    {
        if (IsClient)
        {
            _onServerState.OnValueChanged += (oldValue, newValue) =>
            {
                _snapshots.Enqueue(new(newValue, Time.time));

                while (_snapshots.Count > _maxSnapshots)
                {
                    _snapshots.Dequeue();
                }
            };
        }
        base.OnNetworkSpawn();
    }

}
