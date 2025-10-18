using System.Threading.Tasks;
using System;
using Unity.Netcode;
using Unity.Services.Multiplayer;

public class NGoNetworkHandler : INetworkHandler
{
    public Task StartAsync(NetworkConfiguration configuration)
    {
        if (configuration.Role == NetworkRole.Server)
            NetworkManager.Singleton.StartServer();
        else
            NetworkManager.Singleton.StartClient();

        return Task.CompletedTask;
    }

    public Task StopAsync()
    {
        if (NetworkManager.Singleton == null) return Task.CompletedTask;

        if (NetworkManager.Singleton.IsServer)
            NetworkManager.Singleton.Shutdown();

        return Task.CompletedTask;
    }
}
