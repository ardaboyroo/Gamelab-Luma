using Minigames.Sprayway;
using Unity.Netcode;
using UnityEngine;

public class SpraywayNetworkRoomActor : BaseNetworkRoomActor
{
    public override void OnClientStart(ulong clientId)
    {
        base.OnClientStart(clientId);

        NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject.transform.SetParent(GetComponent<NetworkRoom>().Container.transform);

        MinigameBootstrap intro = FindActiveIntro();
        if (intro != null)
            intro.PlayIntroClientRpc(clientId);
    }

    public override void OnClientExiting(ulong clientId)
    {
        base.OnClientExiting(clientId);

        MinigameBootstrap intro = FindActiveIntro();
        if (intro != null)
            intro.ReturnControlsClientRpc(clientId);

        NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject.transform.SetParent(gameObject.transform.root);
    }

    private MinigameBootstrap FindActiveIntro()
    {
        foreach (var instance in FindObjectsByType<MinigameBootstrap>(FindObjectsSortMode.None))
        {
            if (instance.gameObject.activeSelf && instance.isActiveAndEnabled)
                return instance;
        }
        return null;
    }
}