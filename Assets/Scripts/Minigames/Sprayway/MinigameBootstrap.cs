using Unity.Netcode;
using UnityEngine;

namespace Minigames.Sprayway
{
    public class MinigameBootstrap : NetworkBehaviour
    {
        [SerializeField] private string introState = "Intro";
        [SerializeField] private float duration = 6f;

        // --------------------------------------------------------------------

        [ClientRpc]
        public void PlayIntroClientRpc(ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId)
                return;

            var playerObj = NetworkManager.Singleton.LocalClient?.PlayerObject;
            if (playerObj == null)
                return;

            var global = playerObj.GetComponent<Player.Global.PlayerMovement>();
            var sprayway = playerObj.GetComponent<Minigames.Sprayway.PlayerMovement>();
            if (global != null) global.enabled = false;
            if (sprayway != null) sprayway.enabled = false;

            var animator = playerObj.GetComponent<Animator>();
            var introCtrl = playerObj.gameObject.AddComponent<IntroAnimatorController>();
            introCtrl.Initialize(animator.runtimeAnimatorController, introState, duration);

            NotifyServerEnableSpraywayServerRpc();
        }

        // --------------------------------------------------------------------

        [ServerRpc(RequireOwnership = false)]
        private void NotifyServerEnableSpraywayServerRpc(ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;

            if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
                return;

            var player = client.PlayerObject;
            if (player == null)
                return;

            // Server switches to sprayway movement
            var global = player.GetComponent<Player.Global.PlayerMovement>();
            var sprayway = player.GetComponent<Minigames.Sprayway.PlayerMovement>();
            if (global != null) global.enabled = false;
            if (sprayway != null) sprayway.enabled = true;
        }

        // --------------------------------------------------------------------

        [ClientRpc]
        public void ReturnControlsClientRpc(ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId)
                return;

            var playerObj = NetworkManager.Singleton.LocalClient.PlayerObject;
            if (playerObj == null)
                return;

            var global = playerObj.GetComponent<Player.Global.PlayerMovement>();
            var sprayway = playerObj.GetComponent<Minigames.Sprayway.PlayerMovement>();
            if (sprayway != null) sprayway.enabled = false;
            if (global != null) global.enabled = true;

            NotifyServerEnableGlobalServerRpc();
        }

        [ServerRpc(RequireOwnership = false)]
        private void NotifyServerEnableGlobalServerRpc(ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;

            if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
                return;

            var player = client.PlayerObject;
            if (player == null)
                return;

            var global = player.GetComponent<Player.Global.PlayerMovement>();
            var sprayway = player.GetComponent<Minigames.Sprayway.PlayerMovement>();
            if (sprayway != null) sprayway.enabled = false;
            if (global != null) global.enabled = true;
        }
    }
}