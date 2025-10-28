using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;


namespace Player.Global
{

    public class PlayerMovement : SnapshotBasedBehaviour<Vector3>
    {
        [Header("Attributes")]
        [SerializeField] private float _moveSpeed = 5f;

        private Vector2 _input;
        private float _inputSendInterval = 1 / 30f;
        private float _inputSendTimer;


        protected override void Update()
        {
            if (IsOwner)
            {
                _inputSendTimer += Time.deltaTime;
                if (_inputSendTimer >= _inputSendInterval)
                {
                    _input = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
                    SendInputServerRPC(_input);
                    _inputSendTimer = 0f;
                }
            }
            base.Update();
        }

        protected override void Interpolate(Snapshot<Vector3> from, Snapshot<Vector3> to, float t)
        {
            transform.position = Vector3.Lerp(from.State, to.State, t);
        }

        [ServerRpc]
        private void SendInputServerRPC(Vector2 input)
        {
            float deltaTime = NetworkManager.Singleton.ServerTime.FixedDeltaTime;

            Vector3 movement = new Vector3(input.x, 0, input.y) * _moveSpeed * deltaTime;
            Vector3 targetPosition = transform.position + movement;

            if (movement.magnitude <= 0.5f)
            {
                transform.position = targetPosition;
                _onServerState.Value = transform.position;
            }
        }

    }
}