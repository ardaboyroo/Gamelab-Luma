using System;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;


namespace Player.Global
{
    [DisallowMultipleComponent]
    public class CameraMovement : NetworkBehaviour
    {
        [Header("Smoothing Parameters")]
        [SerializeField, Range(0.01f, 20f)]
        private float _lerpSpeed = 5f;

        [Tooltip("How close in world units before snapping to target")]
        [SerializeField, Range(0f, 0.5f)]
        private float _snapThreshold = 0.01f;

        [Header("Runtime State (Debug)")]
        [SerializeField, ReadOnly] private bool _active = true;
        [SerializeField, ReadOnly] private Vector3 _worldPosition;

        private Transform _parent;

        public override void OnNetworkSpawn()
        {
            if (!IsOwner || !IsClient)
                Destroy(gameObject);

            _parent = transform.parent;
            if (_parent == null)
            {
                Debug.LogError($"{nameof(CameraMovement)} must be a child of a parent object!");
                enabled = false;
                return;
            }

            // Initialize world position
            _worldPosition = transform.position;

            base.OnNetworkSpawn();
        }

        private void LateUpdate()
        {
            if (!_active) return;

            Vector3 targetWorldPos = _parent.position;

            // Lerp world position toward target
            _worldPosition = Vector3.Lerp(
                _worldPosition,
                targetWorldPos,
                Time.deltaTime * _lerpSpeed
            );

            // Snap if close enough
            if (Vector3.Distance(_worldPosition, targetWorldPos) < _snapThreshold)
                _worldPosition = targetWorldPos;

            // Apply world position to transform
            transform.position = _worldPosition;
        }

        /// <summary>
        /// Activates smooth following. Syncs the world position to current camera transform to prevent jumps.
        /// </summary>
        public void Activate()
        {
            if (_active) return;

            // Resync so no jump
            _worldPosition = transform.position;
            _active = true;
        }

        /// <summary>
        /// Deactivates smooth following and sets transform to final world position one last time.
        /// </summary>
        public void Deactivate()
        {
            if (!_active) return;

            transform.position = _worldPosition;
            _active = false;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (_parent != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(_worldPosition, _parent.position);
                Gizmos.DrawSphere(_parent.position, 0.05f);
            }
        }
#endif
    }
}