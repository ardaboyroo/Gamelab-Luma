using UnityEngine;
using Unity.Netcode;
using Unity.Services.Multiplayer;
using System;
using System.Text;
using System.Collections.Generic;
using System.Threading.Tasks;
using Networking.Playfab;
using UnityEditor.PackageManager.Requests;
using Unity.Netcode.Transports.UTP;




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

#if !AUTH_PASS_SECRET_1C92B72I001 // to remove -------------------------------------------------------------------------------------------------------------------------
            var nickname = await ValidatePlayerAsync(unityPlayerId, playfabId); 
            if (nickname == null) 
            { 
                Debug.LogWarning($"[SERVER] Player {playfabId} rejected (not linked / invalid).");
                response.Approved = false; 
                response.Reason = "PlayFab validation failed."; 
                return;
            }
            profile.Nickname = nickname;
#else
            var nickname = $"player_{request.ClientNetworkId}";
            profile.Nickname = nickname; 
#endif
            _cachedProfiles[request.ClientNetworkId] = profile;

            response.Approved = true; 
            response.CreatePlayerObject = true;
            response.Pending = false;
            
            Debug.Log($"[SERVER] Approved {nickname}");
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
    }

    private void OnClientDisconnect(ulong clientId)
    {
        if (_cachedProfiles.Remove(clientId))
            Debug.Log($"[SERVER] {clientId} disconnected and removed from cache.");
    }

    private async Task<string> ValidatePlayerAsync(string unityPlayerId, string playfabId)
    {
        if (string.IsNullOrEmpty(playfabId) || string.IsNullOrEmpty(unityPlayerId))
            return null;

        try
        {
            var userData = await PlayFabWrapperAPI.GetUserDataAsync(new GetUserDataRequest
            {
                PlayFabId = playfabId,
                Keys = new List<string> { "UnityPlayerId", "Nickname" }
            });

            if (userData?.Data == null)
            {
                Debug.LogWarning($"[SERVER] Could not get user data for {playfabId}");
                return null;
            }

            if (!userData.Data.TryGetValue("UnityPlayerId", out var stored) ||
                stored?.Value != unityPlayerId)
            {
                Debug.LogWarning($"[SERVER] UnityPlayerId mismatch for {playfabId}");
                return null;
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

            return nickname;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SERVER] ValidatePlayerAsync failed: {ex.Message}");
            return null;
        }
    }

    private void OnApplicationQuit()
    {
        NetworkManager.Singleton?.Shutdown();
    }

    public static UserModels.Display.PlayerProfile GetProfileForID(ulong id) 
        => _cachedProfiles.TryGetValue(id, out var profile) ? profile : new UserModels.Display.PlayerProfile("Unknown");
}
#endif