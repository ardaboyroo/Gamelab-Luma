using Networking.Playfab.Login;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UserModels.Display;

public class PlayerDisplay : NetworkBehaviour
{
    [SerializeField] private List<GameObject> _hairVariants;
    [SerializeField] private List<GameObject> _noseVariants;
    [SerializeField] private TextMeshPro Nameplate;

    private readonly NetworkVariable<PlayerProfile> _playerProfile = new(
        new PlayerProfile("Loading...", default),
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private void Start()
    {
        _playerProfile.OnValueChanged += (_, newValue) => OnDataChanged(newValue);
        OnDataChanged(_playerProfile.Value);
    }

    private void OnDataChanged(PlayerProfile newData)
    {
        Nameplate.text = newData.Nickname;
        Preview(newData.Avatar);
    }

    public static AvatarData GetAvatar()
    {
        return NetworkManager.Singleton.LocalClient.PlayerObject
            .GetComponent<PlayerDisplay>()
            ._playerProfile.Value.Avatar;
    }

    public void RequestApplyAvatar(AvatarData avatar)
    {
        ApplyAvatarServerRpc(avatar);
    }

    [ServerRpc(RequireOwnership = false)]
    private void ApplyAvatarServerRpc(AvatarData avatar, ServerRpcParams rpcParams = default)
    {
        // Update authoritative state on server
        var profile = _playerProfile.Value;
        profile.Avatar = avatar;
        _playerProfile.Value = profile;

#if ENABLE_PLAYFABSERVER_API
        ServerBootstrap.UpdateProfileAvatar(OwnerClientId, avatar);
#endif
    }

    public void Preview(AvatarData avatar)
    {
        // simple local visual switcher
        for (int i = 0; i < _hairVariants.Count; i++)
            _hairVariants[i].SetActive(i == avatar.HairID);

        for (int i = 0; i < _noseVariants.Count; i++)
            _noseVariants[i].SetActive(i == avatar.NoseID);
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
#if ENABLE_PLAYFABSERVER_API
            _playerProfile.Value = ServerBootstrap.GetProfileForID(OwnerClientId);
#endif
        }
    }
}