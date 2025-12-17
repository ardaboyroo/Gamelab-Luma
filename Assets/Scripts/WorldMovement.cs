using UnityEngine;
using FMODUnity;

public class WorldMovement : MonoBehaviour
{
    [SerializeField] private Rigidbody _rigidbody;

    [Header("Movement")]
    [SerializeField] private float _moveSpeed = 5f;
    [SerializeField] private float _rotationSpeed = 360f;

    [Header("Ground Raycast")]
    [SerializeField] private LayerMask _groundMask;
    [SerializeField] private float _rayLength = 1000f;

    [Header("Audio Settings")]
    [SerializeField] private EventReference _footstepEvent;
    [SerializeField] private float _stepDistance = 1.8f;

    // Internal tracker for audio
    private float _currentStepTracker = 0f;

    // Buffered target position on the floor
    private Vector3 _targetPosition;
    private bool _hasTarget;

    private void Start()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _rigidbody.freezeRotation = true;

        _targetPosition = transform.position;
        _hasTarget = false;
    }

    public void NullifyTarget() => _targetPosition = transform.position;

    private void Update()
    {
        if (Input.GetMouseButton(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out RaycastHit hit, _rayLength, _groundMask))
            {
                _targetPosition = hit.point;
                _hasTarget = true;
            }
        }
    }

    private void FixedUpdate()
    {
        if (_rigidbody == null)
            return;

        Vector3 toTarget = _targetPosition - transform.position;
        toTarget.y = 0f;

        float distSq = toTarget.sqrMagnitude;
        bool closeEnough = distSq < 0.1f;

        // --- IDLE LOGIC ---
        if (closeEnough)
        {
            _currentStepTracker = 0f;

            Vector3 idleDirection = -Vector3.forward;
            Quaternion desiredIdleRotation = Quaternion.LookRotation(idleDirection, Vector3.up);

            // We define 'newIdleRot' here. It only exists inside these brackets { }.
            Quaternion newIdleRot = Quaternion.RotateTowards(
                _rigidbody.rotation,
                desiredIdleRotation,
                _rotationSpeed * Time.fixedDeltaTime
            );

            _rigidbody.MoveRotation(newIdleRot);
            return;
        }

        // --- MOVING LOGIC ---
        Vector3 normalized = toTarget.normalized;

        Quaternion desiredRotation = Quaternion.LookRotation(normalized, Vector3.up);
        Quaternion newRotation = Quaternion.RotateTowards(
            _rigidbody.rotation,
            desiredRotation,
            _rotationSpeed * Time.fixedDeltaTime
        );

        // ERROR WAS HERE: You were trying to use 'newIdleRot' (which is dead/out of scope).
        // FIX: Use 'newRotation' which you just calculated above.
        _rigidbody.MoveRotation(newRotation);

        Vector3 move = transform.forward * _moveSpeed * Time.fixedDeltaTime;
        _rigidbody.MovePosition(_rigidbody.position + move);

        PlayFootstepIfMoved(move.magnitude);
    }

    private void PlayFootstepIfMoved(float distanceMoved)
    {
        _currentStepTracker += distanceMoved;

        if (_currentStepTracker >= _stepDistance)
        {
            if (!_footstepEvent.IsNull)
            {
                RuntimeManager.PlayOneShot(_footstepEvent, transform.position);
            }
            _currentStepTracker = 0f;
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (_hasTarget)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(_targetPosition + Vector3.up * 0.05f, 0.15f);
        }
    }
#endif
}