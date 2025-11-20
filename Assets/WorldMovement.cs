using UnityEngine;

public class WorldMovement : MonoBehaviour
{
    [SerializeField] private Rigidbody _rigidbody;

    [Header("Movement")]
    [SerializeField] private float _moveSpeed = 5f;
    [SerializeField] private float _rotationSpeed = 360f;

    [Header("Ground Raycast")]
    [SerializeField] private LayerMask _groundMask;
    [SerializeField] private float _rayLength = 1000f;

    // Buffered target position on the floor
    private Vector3 _targetPosition;
    private bool _hasTarget;

    private void Start()
    {
        if (_rigidbody == null)
            _rigidbody = GetComponent<Rigidbody>();

        _targetPosition = transform.position;
        _hasTarget = false;
    }

    private void Update()
    {
        // Sample click on the floor
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
        if (!_hasTarget || _rigidbody == null)
            return;

        Vector3 toTarget = _targetPosition - transform.position;
        toTarget.y = 0f;

        float distSq = toTarget.sqrMagnitude;


        if (distSq < 0.1f)
            return;

        // Rotate towards target
        Quaternion desiredRotation = Quaternion.LookRotation(toTarget.normalized, Vector3.up);
        Quaternion newRotation = Quaternion.RotateTowards(
            _rigidbody.rotation,
            desiredRotation,
            _rotationSpeed * Time.fixedDeltaTime
        );
        _rigidbody.MoveRotation(newRotation);

        // Move forward only if not yet close
        Vector3 newPosition = _rigidbody.position + transform.forward * _moveSpeed * Time.fixedDeltaTime;
        _rigidbody.MovePosition(newPosition);
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