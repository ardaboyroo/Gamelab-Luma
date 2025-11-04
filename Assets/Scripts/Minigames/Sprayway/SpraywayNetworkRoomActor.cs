using Minigames.Sprayway;
using Unity.Netcode;
using UnityEngine;

public class SpraywayNetworkRoomActor : BaseNetworkRoomActor
{
    private SpraywayGameManager _manager;

    public override void OnClientStart(ulong clientId)
    {
        base.OnClientStart(clientId);

        if (_manager == null)
        {
            _manager = transform.parent.GetComponentInChildren<Minigames.Sprayway.SpraywayGameManager>(true);
            _manager.Room = NetworkRoom.PlayerRoomMap[clientId];
        }
        _manager.SetUIClientRPC(clientId);

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
