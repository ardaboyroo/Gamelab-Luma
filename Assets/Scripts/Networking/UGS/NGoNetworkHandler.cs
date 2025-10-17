using System.Threading.Tasks;
using System;
using Unity.Netcode;
using Unity.Services.Multiplayer;

public class NGoNetworkHandler : INetworkHandler
{
    public Task StartAsync(NetworkConfiguration configuration)
    {
        if (NetworkManager.Singleton == null)
            throw new InvalidOperationException("NetworkManager missing.");

        if (configuration.Role == NetworkRole.Server)
            NetworkManager.Singleton.StartServer();
        else if (configuration.Role == NetworkRole.Host)
            NetworkManager.Singleton.StartHost();
        else
            NetworkManager.Singleton.StartClient();

        return Task.CompletedTask;
    }

    public Task StopAsync()
    {
        if (NetworkManager.Singleton == null) return Task.CompletedTask;

        if (NetworkManager.Singleton.IsHost)
            NetworkManager.Singleton.Shutdown();

        return Task.CompletedTask;
    }
}