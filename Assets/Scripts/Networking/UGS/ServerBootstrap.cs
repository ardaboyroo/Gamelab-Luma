using UnityEngine;
using Unity.Netcode;
using Unity.Services.Multiplayer;
using System;
using System.Text;
using System.Collections.Generic;
using System.Threading.Tasks;
using Networking.Playfab;
using Unity.Netcode.Transports.UTP;
using UnityEngine.Assertions.Must;
using Networking.Playfab.Database;






#if ENABLE_PLAYFABSERVER_API
using PlayFab.ServerModels;

public class ServerBootstrap : MonoBehaviour
{
    [SerializeField] private ushort _maxPlayersPerSession = 512;
    private static readonly Dictionary<string, ISession> _sessions = new();
    private static readonly Dictionary<ulong, UserModels.Display.PlayerProfile> _cachedProfiles = new();

    private async void Start()
    {
        var nm = NetworkManager.Singleton;
        var transport = nm.GetComponent<UnityTransport>();
        nm.ConnectionApprovalCallback += OnConnectionApproval;
        nm.OnClientDisconnectCallback += OnClientDisconnect;
        nm.OnClientConnectedCallback += OnClientConnected;

        bool success = false;
        transport.SetConnectionData(transport.ConnectionData.Address, transport.ConnectionData.Port--);
        while (!success) 
        {
            transport.SetConnectionData(transport.ConnectionData.Address, transport.ConnectionData.Port++);
            success = nm.StartServer();
        }
        try
        {

            var options = new SessionOptions()
            {
                MaxPlayers = _maxPlayersPerSession,
                IsPrivate = false
            };

            var session = await MultiplayerService.Instance.CreateSessionAsync(options);
            _sessions[session.Name] = session;

            Debug.Log($"[SERVER] Created session '{session.Name}' Id={session.Id} Code='{session.Code}'");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SERVER] CreateHubSessionAsync failed: {ex.Message}");
        }
    }

    private async void OnConnectionApproval(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
    {
        try
        {
            UserModels.Display.PlayerProfile profile = new();
            response.Pending = true;
            string payload = Encoding.UTF8.GetString(request.Payload); 

            // Format: "<UnityPlayerId>|<PlayFabId>"
            var parts = payload.Split('|'); 

            string unityPlayerId = parts.Length > 0 ? parts[0] : string.Empty;
            string playfabId = parts.Length > 1 ? parts[1] : string.Empty; 

            Debug.Log($"[SERVER] Connection request: UnityID={unityPlayerId} PlayFabID={playfabId}");

            profile = await ValidatePlayerAsync(unityPlayerId, playfabId);

            if (profile.Nickname == "") 
            { 
                Debug.LogWarning($"[SERVER] Player {playfabId} rejected (not linked / invalid).");
                response.Approved = false; 
                response.Reason = "PlayFab validation failed."; 
                return;
            }

            _cachedProfiles[request.ClientNetworkId] = profile;

            response.Approved = true; 
            response.CreatePlayerObject = true;
            response.Pending = false;
            
            Debug.Log($"[SERVER] Approved {profile.Nickname}");
        } 
        catch (Exception ex) 
        { 
            Debug.LogError($"[SERVER] ConnectionApproval error: {ex}"); 
        } 
    }

    private void OnClientConnected(ulong clientId)
    {
        Debug.Log($"[SERVER] Client {clientId} Connected");

        NetworkRoom.ExistingRooms["hub"].GetOrCreateInstance().AddMember(clientId);

        if (!_cachedProfiles[clientId].Avatar.Initialized)
        {
            NetworkRoomPortal.ForceAssign(clientId, "character_edit", 0);
        }
        else
        {
            NetworkRoomPortal.ForceAssign(clientId, "hub", 0);
        }
    }

    private void OnClientDisconnect(ulong clientId)
    {
        if (_cachedProfiles.Remove(clientId))
            Debug.Log($"[SERVER] {clientId} disconnected and removed from cache.");
    }

    private async Task<UserModels.Display.PlayerProfile> ValidatePlayerAsync(string unityPlayerId, string playfabId)
    {
        var failed = new UserModels.Display.PlayerProfile("", default);
        var result = new UserModels.Display.PlayerProfile("", default);

        if (string.IsNullOrEmpty(playfabId) || string.IsNullOrEmpty(unityPlayerId))
            return failed;

        try
        {
            var userData = await PlayFabWrapperAPI.GetUserDataAsync(new GetUserDataRequest
            {
                PlayFabId = playfabId,
                Keys = new List<string> { "UnityPlayerId", "Nickname", "AvatarData" }
            });

            if (userData?.Data == null)
            {
                Debug.LogWarning($"[SERVER] Could not get user data for {playfabId}");
                return failed;
            }

            if (!userData.Data.TryGetValue("UnityPlayerId", out var stored) ||
                stored?.Value != unityPlayerId)
            {
                Debug.LogWarning($"[SERVER] UnityPlayerId mismatch for {playfabId}");
                return failed;
            }

            string nickname;
            if (userData.Data.TryGetValue("Nickname", out var n) && !string.IsNullOrEmpty(n?.Value))
            {
                nickname = n.Value;
            }
            else
            {
                var acc = await PlayFabWrapperAPI.GetAccountInfoAsync(new GetUserAccountInfoRequest { PlayFabId = playfabId });
                nickname = acc?.UserInfo?.Username ?? $"Player_{playfabId.Substring(0, 5)}";

                await PlayFabWrapperAPI.UpdateUserDataAsync(new UpdateUserDataRequest
                {
                    PlayFabId = playfabId,
                    Data = new Dictionary<string, string> { { "Nickname", nickname } }
                });
            }

            UserModels.Display.AvatarData avatar = default;
            if (userData.Data.TryGetValue("AvatarData", out var avatarData) && !string.IsNullOrEmpty(n?.Value))
            {
                avatar = JsonUtility.FromJson<UserModels.Display.AvatarData>(avatarData.Value);
            }

            result.Nickname = nickname;
            result.Avatar = avatar;

            return result;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SERVER] ValidatePlayerAsync failed: {ex.Message}");
            return failed;
        }
    }

    private void OnApplicationQuit()
    {
        NetworkManager.Singleton?.Shutdown();
    }

    public static UserModels.Display.PlayerProfile GetProfileForID(ulong id) 
        => _cachedProfiles.TryGetValue(id, out var profile) ? profile : new UserModels.Display.PlayerProfile("Unknown", default);
}
#endif