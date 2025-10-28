using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

namespace Minigames.Sprayway
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(NetworkRigidbody))]
    public class PlayerMovement : SnapshotBasedBehaviour<Vector3>
    {
        [Header("Attributes")]
        [SerializeField] private float _moveSpeed = 5f;
        [SerializeField] private float _flyForce = 15f;
        [SerializeField] private float _gravityScale = 1f;

        private Vector2 _input;
        private float _inputSendInterval = 1f / 30f;
        private float _inputSendTimer;
        private Rigidbody _rb;

        public void OnEnable()
        {
            _rb = GetComponent<Rigidbody>();
            _rb.useGravity = true;
        }

        public void OnDisable()
        {
            _rb = GetComponent<Rigidbody>();
            _rb.useGravity = false;
        }

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
            if (_rb == null)
                _rb = GetComponent<Rigidbody>();

            Vector3 velocity = _rb.linearVelocity;

            velocity.x = _moveSpeed;

            if (input.y > 0)
                velocity.y = Mathf.Lerp(velocity.y, _flyForce, 0.3f);
            else
                velocity.y += Physics.gravity.y * _gravityScale * NetworkManager.Singleton.ServerTime.FixedDeltaTime;

            _rb.linearVelocity = velocity;

            _onServerState.Value = transform.position;
        }
    }
}
