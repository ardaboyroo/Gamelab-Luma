using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

public static class TransportConfigurator
{
    public static void AssignDynamicPort()
    {
        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        if (transport == null) return;

        if (!NetworkManager.Singleton.IsServer)
        {
            ushort randomClientPort = (ushort)UnityEngine.Random.Range(10000, 60000);
            transport.SetConnectionData(
                "127.0.0.1", randomClientPort, "0.0.0.0"
            );
        }
    }
}