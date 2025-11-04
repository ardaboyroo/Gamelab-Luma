using Networking.Playfab.Login;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEditor;
using UnityEngine;
using UserModels.Display;

public class PlayerDisplay : NetworkBehaviour
{
    private static PlayerDisplay _instance;

    [SerializeField] private List<GameObject> _hairVariants;
    [SerializeField] private List<GameObject> _noseVariants;

    [SerializeField] private TextMeshPro Nameplate;

    private NetworkVariable<PlayerProfile> _playerProfile = new(
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
        ApplyAvatar(_playerProfile.Value.Avatar);
    }

    public static void ApplyAvatar(AvatarData avatar, bool update = false)
    {
        _instance.ApplyAvatarLocal(avatar, NetworkManager.Singleton.LocalClientId);
        _instance.ApplyAvatarServerRpc(avatar, NetworkManager.Singleton.LocalClientId);
        if(update)
            _instance.UpdateProfileServerRpc();
    }

    private void ApplyAvatarLocal(AvatarData avatar, ulong clientId)
    {
        if (clientId != NetworkManager.Singleton.LocalClientId)
            return;

        _instance._hairVariants.ForEach(h => h.SetActive(false));
        _instance._hairVariants[avatar.HairID].SetActive(true);

        _instance._noseVariants.ForEach(h => h.SetActive(false));
        _instance._noseVariants[avatar.NoseID].SetActive(true);
    }

    [ServerRpc]
    public void ApplyAvatarServerRpc(AvatarData avatar, ulong clientId)
    {
        NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject.GetComponent<PlayerDisplay>().ApplyAvatarLocal(avatar, clientId);
    }

    public static AvatarData GetAvatar()
    {
        return NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<PlayerDisplay>()._playerProfile.Value.Avatar;
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

    [ServerRpc]
    public void UpdateProfileServerRpc()
    {
        if (IsServer)
        {
#if ENABLE_PLAYFABSERVER_API
            _playerProfile.Value = ServerBootstrap.GetProfileForID(OwnerClientId);
#endif
        }
    }

    private void Awake()
    {
        if (_instance == null)
            _instance = this;
    }
}
