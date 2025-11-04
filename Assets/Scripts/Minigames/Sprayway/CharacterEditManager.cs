using Minigames.Sprayway;
using Unity.Netcode;
using UnityEngine;
using UserModels.Display;

namespace Rooms.CharacterEdit
{
    public class CharacterEditManager : NetworkBehaviour
    {
        [Header("Room-scoped")]
        public NetworkRoom Room;


        public void SetMovement(bool state, ulong clientId)
        {
            SetMovementClientRPC(state, clientId);
            SetMovementServerRPC(state, clientId);
        }

        [ClientRpc]
        public void ShowUIClientRpc(ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
                CharacterEditUI.Show();
        }

        [ClientRpc]
        public void SetUIClientRPC(ulong clientId)
        {
            if (NetworkManager.Singleton.LocalClientId == clientId)
                CharacterEditUI.Initialize(this);
        }

        [ClientRpc]
        public void HideUIClientRpc(ulong targetClientId)
        {

            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
            CharacterEditUI.Hide();
        }

        [ClientRpc(RequireOwnership = false)]
        public void SetMovementClientRPC(bool state, ulong clientId)
        {
            Debug.Log("[CLIENT] SetMovementRPC try");

            if (NetworkManager.Singleton.LocalClientId != clientId)
                return;

            Debug.Log("[CLIENT] matched client ID");

            var playerObj = NetworkManager.Singleton.LocalClient?.PlayerObject;
            if (playerObj == null)
                return;

            Debug.Log("[CLIENT] got netObj");

            var globalMovement = playerObj.GetComponent<Player.Global.PlayerMovement>();

            globalMovement.enabled = state;

            Debug.Log("[CLIENT] SetMovementClientRPC: " + state + " for " + clientId);
        }

        [ClientRpc(RequireOwnership = false)]
        public void SetAnimatorModeClientRPC(bool state, ulong clientId)
        {
            Debug.Log("[CLIENT] SetAnimatorModeRPC try");

            if (NetworkManager.Singleton.LocalClientId != clientId)
                return;

            Debug.Log("[CLIENT] matched client ID");

            var playerObj = NetworkManager.Singleton.LocalClient?.PlayerObject;
            if (playerObj == null)
                return;

            Debug.Log("[CLIENT] got netObj");

            var animator = playerObj.GetComponent<Animator>();
            animator.Play(state ? "CharacterCreation" : "PlayerReset", 0, 0f);

            Debug.Log("[CLIENT] SetAnimatorModeClientRPC: " + state + " for " + clientId);
        }

        //-------------------------------------

        [ServerRpc(RequireOwnership = false)]
        public void SetMovementServerRPC(bool state, ulong senderId)
        {
            Debug.Log("[SERVER] SetMovementRPC try");
            if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(senderId, out var client))
                return;

            Debug.Log("[SERVER] found client");
            var playerObj = NetworkManager.Singleton.ConnectedClients[senderId].PlayerObject;
            if (playerObj == null)
                return;

            Debug.Log("[SERVER] got netObj");
            var globalMovement = playerObj.GetComponent<Player.Global.PlayerMovement>();

            globalMovement.enabled = state;

            Debug.Log("[SERVER] SetMovementServerRPC: " + state + " for " + senderId);
        }
    }
}