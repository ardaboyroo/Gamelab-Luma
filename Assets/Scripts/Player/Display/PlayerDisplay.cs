using Networking.Playfab.Login;
using TMPro;
using Unity.Netcode;
using UnityEditor;
using UnityEngine;
using UserModels.Display;

public class PlayerDisplay : NetworkBehaviour
{
    [SerializeField] private TextMeshPro Nameplate;

    // This will automatically replicate to all clients
    private NetworkVariable<PlayerProfile> _playerProfile = new(
        new PlayerProfile("Loading..."),
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private void Start()
    {
        _playerProfile.OnValueChanged += (_, newValue) =>
        {
            Nameplate.text = newValue.Nickname;
        };

        Nameplate.text = _playerProfile.Value.Nickname;
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            _playerProfile.Value = ServerBootstrap.GetProfileForID(OwnerClientId);
        }
    }
}
