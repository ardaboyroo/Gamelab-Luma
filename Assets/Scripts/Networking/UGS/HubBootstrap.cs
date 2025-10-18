// -----------------------------------------------------------------------------
// File: ClientHubBootstrap.cs
// Purpose: Client connecting to local UGS server session post-authentication
// -----------------------------------------------------------------------------

using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Multiplayer;
using System;
using System.Threading.Tasks;
using System.Threading;
using UnityEngine.SceneManagement;

public class ClientHubBootstrap : MonoBehaviour
{
    [SerializeField] private string serverAddress = "127.0.0.1";
    [SerializeField] private ushort serverPort = 8032;

    private bool _connecting = false;

    private async void Start()
    {
        await Connect();
    }

    private async Task Connect()
    {
        if (_connecting)
            return;

        SetConnecting(true);

        var nm = NetworkManager.Singleton;
        var utp = nm.GetComponent<UnityTransport>();
        utp.SetConnectionData(serverAddress, serverPort);

        int retry = 0;
        const int maxRetries = 5;

        while (retry < maxRetries)
        {
            try
            {
                Debug.Log("Querrying Sessions on Server");
                var result = await MultiplayerService.Instance.QuerySessionsAsync(new QuerySessionsOptions());
                if (result.Sessions.Count == 0)
                {
                    Debug.LogWarning("[CLIENT] No sessions found, retrying...");
                    await Task.Delay(1000 * (int)Mathf.Pow(2, retry));
                    retry++;
                    continue;
                }

                Debug.Log("Got Session, Joining.");
                var id = result.Sessions[0].Id;
                await MultiplayerService.Instance.JoinSessionByIdAsync(id);
                Debug.Log("[CLIENT] Successfully connected to session!");
                nm.StartClient();

                SetConnecting(false);
                return;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[CLIENT] Join attempt {retry + 1} failed: {e.Message}");
                await Task.Delay(1000 * (int)Mathf.Pow(2, retry));
                retry++;
            }
        }

        Debug.LogError("[CLIENT] Could not join after multiple attempts.");
        await SceneManager.LoadSceneAsync("Auth");

        SetConnecting(false);
    }

    private void SetConnecting(bool state)
    {
        _connecting = state;
        Debug.Log("Connection Blocker: " + state);
    }

    private void OnApplicationQuit()
    {
        NetworkManager.Singleton?.Shutdown();
    }
}