using UnityEngine;

public class CameraActions : MonoBehaviour
{
    public static CameraActions Instance;

    [SerializeField] private bool _rotationYLock = true;
    [SerializeField] private Transform _player;

    [Header("Lerp Speeds")]
    [SerializeField] private float _positionLerpSpeed = 5f;
    [SerializeField] private float _rotationLerpSpeed = 5f;
    [SerializeField] private float _fovLerpSpeed = 5f;

    private Camera _cam;

    // default follow setup
    private Vector3 _defaultOffset;
    private Quaternion _defaultRotation;
    private float _defaultFov;

    // current targets
    private Vector3 _targetOffset;
    private Quaternion _targetRotation;
    private float _targetFov;

    // smoothed values
    private Vector3 _currentOffset;
    private Quaternion _currentRotation;
    private float _currentFov;

    private bool _isActive;

    private CameraEvent _previous;
    private CameraEvent _current;

    private Transform _cameraEventVolume;

    private void Awake()
    {
        Instance = this;
        _cam = transform.GetChild(0).GetComponent<Camera>();
    }

    private void Start()
    {
        if (_player == null)
            return;

        _defaultOffset = _cam.transform.position - _player.position;
        _defaultRotation = _cam.transform.rotation;
        _defaultFov = _cam != null ? _cam.fieldOfView : 60f;

        _targetOffset = _currentOffset = _defaultOffset;
        _targetRotation = _currentRotation = _defaultRotation;
        _targetFov = _currentFov = _defaultFov;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.transform == _cameraEventVolume)
            return;

        // trigger volumes must be tagged "CameraAction"
        if (!other.CompareTag("CameraAction"))
            return;

        var evt = other.GetComponent<CameraEvent>();
        if (evt == null)
            return;

        _cameraEventVolume = other.transform;

        ApplyCameraEvent(evt);
    }

    private void LateUpdate()
    {
        if (_rotationYLock && _player == null)
        {
            Vector3 local = transform.localEulerAngles; 
            float playerY = _player.eulerAngles.y; 
            local.y = -playerY; 
            transform.localEulerAngles = local;
        }

        _currentOffset = Vector3.Lerp(_currentOffset, _targetOffset, _positionLerpSpeed * Time.deltaTime);
        _currentRotation = Quaternion.Slerp(_currentRotation, _targetRotation, _rotationLerpSpeed * Time.deltaTime);
        _currentFov = Mathf.Lerp(_currentFov, _targetFov, _fovLerpSpeed * Time.deltaTime);

        _cam.transform.position = _player.position + _currentOffset;
        _cam.transform.rotation = _currentRotation; 

        if (_cam != null)
            _cam.fieldOfView = _currentFov;
    }

    public void SetActive(bool state)
    {
        _isActive = state;
    }

    public void ApplyCameraEvent(CameraEvent evt)
    {
        _previous = _current;
        _current = evt;

        _targetOffset = evt.Offset == Vector3.zero ? _defaultOffset : evt.Offset;
        _targetRotation = Quaternion.Euler(evt.Rotation);
        _targetFov = evt.FOV > 0f ? evt.FOV : _defaultFov;
    }

    public void ApplyPreviousEvent() => ApplyCameraEvent(_previous);
}