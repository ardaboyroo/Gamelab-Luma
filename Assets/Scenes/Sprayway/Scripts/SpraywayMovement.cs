using UnityEngine;
using UnityEngine.UI;

public class SpraywayMovement : MonoBehaviour
{
    [Header("Wall Orientation")]
    [SerializeField] private Transform _wall;          // defines wall "up" and "forward"
    [Tooltip("Heavy gravity so you drop instantly when letting go.")]
    [SerializeField] private float _gravityStrength = 45f;

    [Header("Movement (Jetpack / Flappy Style)")]
    [Tooltip("Constant forward speed along the wall.")]
    [SerializeField] private float _forwardSpeed = 10f;
    [Tooltip("Instant upward flight speed when holding the mouse button.")]
    [SerializeField] private float _flySpeed = 18f;
    [Tooltip("Minimum height along the wall (bottom limit).")]
    [SerializeField] private float _minHeight = 0f;
    [Tooltip("Maximum height along the wall (top limit).")]
    [SerializeField] private float _maxHeight = 10f;

    [Header("Auto Descend")]
    [SerializeField] private float _descendAmount = 0.25f;

    [Header("Spray FX")]
    [SerializeField] private ParticleSystem _sprayParticles;
    [Tooltip("UI Slider used as progress bar (0–1).")]
    [SerializeField] private Slider _progressBar;
    [Tooltip("How fast the progress fills per second while spraying.")]
    [SerializeField] private float _fillPerSecond = 0.3f;

    [SerializeField] private FMODUnity.EventReference sprayEvent;
    private FMOD.Studio.EventInstance sprayInstance;

    private Rigidbody _rb;
    private Animator _animator;
    private SprayWay _sprayWayActivity;

    private bool _wantsThrust;
    private bool _isSpraying;
    private bool _stopped;
    private bool _isPlayingAudioSpray;

    private float _prevHeight;

    private void Awake()
    {
        _stopped = false;
        _rb = GetComponent<Rigidbody>();
        _rb.useGravity = false;
        _rb.constraints = RigidbodyConstraints.FreezeRotation;

        _animator = transform.Find("Model Container").GetComponent<Animator>();

        _sprayParticles = transform.Find("Model Container").Find("BaseMesh").Find("Spraycan").Find("SprayWayParticleSystem").GetComponent<ParticleSystem>();
        _sprayParticles.transform.parent.GetComponent<Renderer>().enabled = true;
    }

    public void SetSprayWay(SprayWay sprayWay) => _sprayWayActivity = sprayWay;

    public void SetWall(Transform wall) => _wall = wall;

    private void Start()
    {
        sprayInstance = FMODUnity.RuntimeManager.CreateInstance(sprayEvent);
    }

    private void Update()
    {
        if (_stopped)
        {
            _animator.SetBool("mid_air", false);
            _animator.SetFloat("speed", 0);
            return;
        }

        _animator.SetFloat("speed", 2f);

        // Raw input
        _wantsThrust = Input.GetMouseButton(0);

        if (_progressBar == null && _sprayWayActivity != null)
        {
            _progressBar = _sprayWayActivity.transform.Find("Root").Find("Canvas").Find("Progress").GetComponent<Slider>();
        }

        if (_progressBar != null && _sprayWayActivity != null)
        {
            if (_isSpraying)
            {
                _animator.SetBool("mid_air", true);
                _sprayWayActivity.AddProgress(_fillPerSecond * Time.deltaTime);
            }

            _progressBar.value = Mathf.Clamp01(_sprayWayActivity.Progress);
        }
    }

    private void FixedUpdate()
    {
        if (_stopped)
        {
            _rb.linearVelocity = Vector3.zero;
            return;
        }

        if (_wall == null || _rb == null)
            return;

        if (Mathf.Abs(transform.position.y - _prevHeight) <= 0.01f)
            _animator.SetBool("mid_air", false);

        Vector3 wallUp = _wall.up;
        Vector3 wallForward = _wall.right;

        Vector3 vel = _rb.linearVelocity;
        float verticalVel = Vector3.Dot(vel, wallUp);
        float forwardVel = _forwardSpeed;

        float currentHeight = GetHeightAlongWall(transform.position, _wall.position, wallUp);
        bool canGoUp = currentHeight < _maxHeight - 0.01f;

        // INSTANT SNAP MOVEMENT (No Lerp/Smoothing lag)
        if (_wantsThrust && canGoUp)
        {
            // Jetpack Joyride style: Instantly lock to high upward speed
            verticalVel = _flySpeed;
        }
        else
        {
            // Flappy Bird style: Heavy, immediate gravity pull down
            verticalVel -= _gravityStrength * Time.fixedDeltaTime;
        }

        // Clamp movement at boundaries
        if (currentHeight >= _maxHeight && verticalVel > 0f)
        {
            verticalVel = 0f;
        }
        if (currentHeight <= _minHeight && verticalVel < 0f)
        {
            verticalVel = 0f;
        }

        // Apply velocity instantly
        Vector3 newVel = (wallUp * verticalVel) + (wallForward * forwardVel);
        _rb.linearVelocity = newVel;

        _isSpraying = _wantsThrust && verticalVel > 0.01f;
        _prevHeight = transform.position.y;

        if (_sprayParticles == null)
        {
            _sprayParticles = transform.Find("Model Container").Find("BaseMesh").Find("Spraycan").Find("SprayWayParticleSystem").GetComponent<ParticleSystem>();
        }
        else
        {
            var emission = _sprayParticles.emission;
            emission.enabled = _isSpraying;

            if (_isSpraying && !_isPlayingAudioSpray)
            {
                StartSprayAudio();
            }
            else if (!_isSpraying && _isPlayingAudioSpray)
            {
                StopSprayAudio();
            }
        }
    }

    public void AutoDescend()
    {
        Vector3 wallUp = _wall.forward;
        Vector3 descendDir = -wallUp * _descendAmount;
        transform.position += descendDir;

        if (_sprayWayActivity != null)
        {
            _sprayWayActivity.NextLayer();
        }
    }

    private float GetHeightAlongWall(Vector3 worldPos, Vector3 wallOrigin, Vector3 wallUp)
    {
        return Vector3.Dot(worldPos - wallOrigin, wallUp);
    }

    private void OnDestroy()
    {
        if (_sprayParticles != null && _sprayParticles.transform.parent != null)
        {
            _sprayParticles.transform.parent.GetComponent<Renderer>().enabled = false;
        }
        sprayInstance.release();
    }

    public void Stop()
    {
        _isSpraying = false;
        _stopped = true;

        if (_sprayParticles != null)
        {
            var emission = _sprayParticles.emission;
            emission.enabled = false;
        }
        StopSprayAudio();
    }

    public void Ressurect() => _stopped = false;

    private void StartSprayAudio()
    {
        _isPlayingAudioSpray = true;
        sprayInstance.setParameterByName("Spray", 1f);
        sprayInstance.start();
    }

    private void StopSprayAudio()
    {
        _isPlayingAudioSpray = false;
        sprayInstance.setParameterByName("Spray", 0f);
    }
}