using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Multiplayer;
using UnityEngine;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using Unity.Netcode;

namespace Networking.UGS
{
    namespace Networking.UGS
    {
        public class HubBootstrap : MonoBehaviour
        {
            [SerializeField] private NetworkManager networkManagerPrefab;
            private ISession _currentSession;

            private async void Start()
            {
                EnsureNetworkManager();

                if (!UnityServices.State.Equals(ServicesInitializationState.Initialized))
                    await UnityServices.InitializeAsync();

                if (!AuthenticationService.Instance.IsSignedIn)
                    await AuthenticationService.Instance.SignInAnonymouslyAsync();

                Debug.Log("UGS + Auth ready, starting session...");
                await CreateOrJoinSession();
            }

            private void EnsureNetworkManager()
            {
                if (NetworkManager.Singleton == null)
                {
                    Instantiate(networkManagerPrefab);
                    Debug.Log("NetworkManager instantiated.");
                }
            }

            private async Task CreateOrJoinSession()
            {
                try
                {
                    var sessionOptions = new SessionOptions()
                    {
                        MaxPlayers = 40,
                        SessionProperties = new Dictionary<string, SessionProperty>
                        {
                            { "GameMode", new ("Hub") },
                            { "Map", new("Central") }
                        },
                        IsPrivate = false
                    }
                        .WithRelayNetwork() 
                        .WithNetworkHandler(new NGoNetworkHandler()); 

                    _currentSession = await MultiplayerService.Instance.CreateOrJoinSessionAsync("HubSession", sessionOptions);

                    Debug.Log($"Session ready. ID: {_currentSession.Id}, Code: {_currentSession.Code}");
                }
                catch (Exception e)
                {
                    Debug.LogError($"Failed to create or join session: {e.Message}");
                }
            }
        }
    }
}