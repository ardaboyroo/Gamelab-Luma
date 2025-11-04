using Unity.Netcode;
using UnityEngine;

namespace Minigames.Sprayway
{

    [RequireComponent(typeof(Collider))]
    public class SpraywayTrigger : NetworkBehaviour
    {
        [SerializeField] private bool _isWin = false;
        [SerializeField] private SpraywayGameManager _localManager;

        private void OnTriggerEnter(Collider other)
        {
            if (!IsServer) return;

            var netObj = other.GetComponentInParent<NetworkObject>();
            if (netObj == null || !netObj.IsPlayerObject)
                return;

            var playerMove = netObj.GetComponent<PlayerMovement>();
            if (playerMove != null)
                playerMove.enabled = false;

            _localManager?.OnPlayerCollidedObstacle(netObj.OwnerClientId, _isWin);
        }
    }
}