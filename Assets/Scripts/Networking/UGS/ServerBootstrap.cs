using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Multiplayer;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;

public class ServerBootstrap : MonoBehaviour
{
    [SerializeField] private ushort _maxPlayersPerSession = 40;
    [SerializeField] private string _region = "eu-west";
    [SerializeField] private int _listenPort = 8032;

    private readonly Dictionary<string, ISession> _sessions = new();
    private bool _initialized;

    private async void Start()
    {
        var nm = NetworkManager.Singleton;

        NetworkManager.Singleton.StartServer();
        try
        {
            var options = new SessionOptions()
            {
                MaxPlayers = _maxPlayersPerSession,
                IsPrivate = false
            };

            var session = await MultiplayerService.Instance.CreateSessionAsync(options);
            _sessions[session.Name] = session;

            Debug.Log($"[SERVER] Created session '{session.Name}'  Id={session.Id}  Code='{session.Code}'");

            nm.OnClientConnectedCallback += (ulong clientId) =>
            {
                if (clientId == nm.LocalClientId)
                {
                    Debug.Log("[Server] Client connected to server.");
                }
            };
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SERVER] CreateHubSessionAsync failed: {ex.Message}");
        }
    }

    private void OnApplicationQuit()
    {
        NetworkManager.Singleton?.Shutdown();
    }
}