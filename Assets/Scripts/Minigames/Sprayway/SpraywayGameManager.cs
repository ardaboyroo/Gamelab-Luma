using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

namespace Minigames.Sprayway
{
    [DisallowMultipleComponent]
    public class SpraywayGameManager : NetworkBehaviour
    {
        [Header("Room-scoped")]
        public NetworkRoom Room; // Assigned when this instance spawns

        [Header("Scene References")]
        [SerializeField] private Transform startEntrance; // e.g. Entrance[0]

        [SerializeField] private string _roomName;
        [SerializeField] private byte _entranceID = 0;

        [ClientRpc]
        public void SetUIClientRPC(ulong clientId)
        {
            if (NetworkManager.Singleton.LocalClientId == clientId)
                SpraywayGameUI.Initialize(this);
        }

        // Called by Obstacle (server-side only)
        public void OnPlayerCollidedObstacle(ulong clientId, bool isWin)
        {
            if (!IsServer)
                return;

            var playerObj = NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject;
            if (playerObj == null)
                return;

            Debug.Log("Collided for " + clientId);

            SetMovement(false, false, clientId);

            if (isWin)
                ShowGameWinClientRpc(clientId);
            else
                ShowGameOverClientRpc(clientId);
        }

        // ------------------- CLIENT RPCs -------------------


        [ClientRpc]
        private void ShowGameOverClientRpc(ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;

            Debug.Log("GameOver for " + targetClientId);

            SpraywayGameUI.ShowGameOver();
        }

        [ClientRpc]
        private void ShowGameWinClientRpc(ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;

            Debug.Log("GameWin for " + targetClientId);

            SpraywayGameUI.ShowGameWin();
        }

        [ClientRpc]
        private void HideUIClientRpc(ulong targetClientId)
        {
            Debug.Log("hide");

            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
            SpraywayGameUI.Hide();
        }

        // ------------------- SERVER RPCs -------------------

        [ServerRpc(RequireOwnership = false)]
        public void RetryServerRpc(ServerRpcParams rpcParams = default)
        {
            Debug.Log("retry");

            ulong clientId = rpcParams.Receive.SenderClientId;

            var player = NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject;

            var nt = player.GetComponent<NetworkTransform>();
            if (nt != null)
                nt.Teleport(startEntrance.position, startEntrance.rotation, player.transform.localScale);
            else
                player.transform.SetPositionAndRotation(startEntrance.position, startEntrance.rotation);

            SetMovement(true, false, clientId);

            HideUIClientRpc(clientId);
        }

        [ServerRpc(RequireOwnership = false)]
        public void ReturnToHubServerRpc(ServerRpcParams rpcParams = default)
        {
            Debug.Log("return to hub");

            ulong clientId = rpcParams.Receive.SenderClientId;
            if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
                return;

            SetMovement(false, true, clientId);

            NetworkRoomPortal.ForceAssign(clientId, _roomName, _entranceID);

            ResetAnimatorClientRpc(clientId);
            HideUIClientRpc(clientId);
        }

        [ClientRpc(RequireOwnership = false)]
        public void ResetAnimatorClientRpc(ulong clientId)
        {
            if (NetworkManager.Singleton.LocalClientId != clientId)
                return;

            var playerObj = NetworkManager.Singleton.LocalClient?.PlayerObject;
            if (playerObj == null)
                return;


            var animator = playerObj.GetComponent<Animator>();
            animator.Play("PlayerReset", 0, 0f);
        }

        public void SetMovement(bool state1, bool state2, ulong clientId)
        {
            SetMovementClientRPC(state1, state2, clientId);
            SetMovementServerRPC(state1, state2);
        }

        [ServerRpc(RequireOwnership = false)]
        public void SetMovementServerRPC(bool state1, bool state2, ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;
            if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
                return;

            var playerObj = NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject;
            if (playerObj == null)
                return;

            var sprayway = playerObj.GetComponent<Minigames.Sprayway.PlayerMovement>();

            if (sprayway != null) sprayway.enabled = state1;

            var globalMovement = playerObj.GetComponent<Player.Global.PlayerMovement>();

            if (globalMovement != null) globalMovement.enabled = state2;
        }

        [ClientRpc(RequireOwnership = false)]
        public void SetMovementClientRPC(bool state1, bool state2, ulong clientId)
        {
            if (NetworkManager.Singleton.LocalClientId != clientId)
                return;

            var playerObj = NetworkManager.Singleton.LocalClient?.PlayerObject;
            if (playerObj == null)
                return;

            var sprayway = playerObj.GetComponent<Minigames.Sprayway.PlayerMovement>();

            if (sprayway != null) sprayway.enabled = state1;

            var globalMovement = playerObj.GetComponent<Player.Global.PlayerMovement>();

            if (globalMovement != null) globalMovement.enabled = state2;
        }
    }
}
