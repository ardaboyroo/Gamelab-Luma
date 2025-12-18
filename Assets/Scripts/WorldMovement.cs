using UnityEngine;
using FMODUnity;
using System.Collections;

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

    private Animator _animator;

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

        _animator = transform.Find("Model Container").GetComponent<Animator>();
        
        StartCoroutine(IdleActiveAnimation());
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

        if (Input.GetKeyDown(KeyCode.F1))
        {
            _animator.SetTrigger("dance_1");
        }

        if (Input.GetKeyDown(KeyCode.F2))
        {
            _animator.SetTrigger("dance_2");
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

            // Direction we want to face when idle = camera forward on XZ
            Vector3 idleDirection;

            if (Camera.main != null)
            {
                idleDirection = -Camera.main.transform.forward;
                idleDirection.y = 0f;

                if (idleDirection.sqrMagnitude < 0.0001f)
                    idleDirection = -transform.forward; // fallback
                else
                    idleDirection.Normalize();
            }
            else
            {
                idleDirection = -transform.forward; // no camera -> keep current
            }

            Quaternion desiredIdleRotation = Quaternion.LookRotation(idleDirection, Vector3.up);

            Quaternion newIdleRot = Quaternion.RotateTowards(
                _rigidbody.rotation,
                desiredIdleRotation,
                _rotationSpeed * Time.fixedDeltaTime
            );

            _rigidbody.MoveRotation(newIdleRot);
            _animator.SetFloat("speed", 0);
            return;
        }

        _animator.SetFloat("speed", 1);

        // --- MOVING LOGIC ---
        Vector3 normalized = toTarget.normalized;

        Quaternion desiredRotation = Quaternion.LookRotation(normalized, Vector3.up);
        Quaternion newRotation = Quaternion.RotateTowards(
            _rigidbody.rotation,
            desiredRotation,
            _rotationSpeed * Time.fixedDeltaTime
        );

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

    private IEnumerator IdleActiveAnimation()
    {
        while (_animator != null)
        {
            _animator.SetTrigger("idle_active");
            yield return new WaitForSeconds(Random.Range(7, 15));
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