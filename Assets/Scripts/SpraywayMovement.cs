using UnityEngine;
using UnityEngine.UI;

public class SpraywayMovement : MonoBehaviour
{
    [Header("Wall Orientation")]
    [SerializeField] private Transform _wall;          // defines wall "up" and "forward"
    [Tooltip("Down-the-wall gravity strength (like 9.81).")]
    [SerializeField] private float _gravityStrength = 9.81f;

    [Header("Movement")]
    [Tooltip("Constant forward speed along the wall.")]
    [SerializeField] private float _forwardSpeed = 5f;
    [Tooltip("Acceleration upward along the wall while spraying.")]
    [SerializeField] private float _thrustAcceleration = 25f;
    [Tooltip("Minimum height along the wall (bottom limit).")]
    [SerializeField] private float _minHeight = 0f;
    [Tooltip("Maximum height along the wall (top limit).")]
    [SerializeField] private float _maxHeight = 10f;

    [Header("Spray FX")]
    [SerializeField] private ParticleSystem _sprayParticles;
    [Tooltip("UI Slider used as progress bar (0�1).")]
    [SerializeField] private Slider _progressBar;
    [Tooltip("How fast the progress fills per second while spraying.")]
    [SerializeField] private float _fillPerSecond = 0.2f;

    [Header("Turning")]
    [SerializeField] private float _turnSpeed = 180f;   // degrees per second

    [Header("Auto Descend")]
    [SerializeField] private float _descendAmount = 0.25f; // how much to drop per turn

    private bool _isTurning;
    private Quaternion _targetRotation;

    private Rigidbody _rb;

    private SprayWay _sprayWayActivity;

    // input buffer from Update ? used in FixedUpdate
    private bool _wantsThrust;
    private bool _isSpraying;
    private bool _stopped;
    private bool _isPlayingAudioSpray;

    private void Awake()
    {
        _stopped = false;
        _rb = GetComponent<Rigidbody>();
        _rb.useGravity = false;                      // we do custom gravity
        _rb.constraints = RigidbodyConstraints.FreezeRotation;  // no spinning

        _sprayParticles = transform.Find("Model Container").Find("BaseMesh").Find("Spraycan").Find("SprayWayParticleSystem").GetComponent<ParticleSystem>();
        _sprayParticles.transform.parent.GetComponent<Renderer>().enabled = true;
    }

    public void SetSprayWay(SprayWay sprayWay) => _sprayWayActivity = sprayWay;

    public void SetWall(Transform wall) => _wall = wall;
    

    private void Update()
    {
        if (_stopped)
            return;

        // raw input (replace with your input system if needed)
        _wantsThrust = Input.GetMouseButton(0);

        // FX & progress bar are easier to handle here
        if (_progressBar == null) 
        {
            _progressBar = _sprayWayActivity.transform.Find("Root").Find("Canvas").Find("Progress").GetComponent<Slider>();
        }
        if (_progressBar != null && _isSpraying)
        {
            _sprayWayActivity.AddProgress(_fillPerSecond * Time.deltaTime);
        }

        _progressBar.value = Mathf.Clamp01(
            _sprayWayActivity.Progress
        );
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("SprayWayTurn"))
        {
            StartTurn();
        }
    }

    private void StartTurn()
    {
        _isTurning = true;

        // 180° flip relative to current orientation
        _targetRotation = Quaternion.Euler(transform.eulerAngles.x, transform.eulerAngles.y + 180f, transform.eulerAngles.z + 180);
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

        if (_isTurning)
        {
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                _targetRotation,
                _turnSpeed * Time.fixedDeltaTime
            );

            // stop when we reach target
            if (Quaternion.Angle(transform.rotation, _targetRotation) < 0.5f)
            {
                _isTurning = false;
                AutoDescend();   // <<< NEW
            }
        }

        // Define wall axes
        Vector3 wallUp = _wall.up;      // along the wall: "up" (spray direction)
        Vector3 wallForward = Vector3.ProjectOnPlane(transform.forward, _wall.forward).normalized;   // along the wall: run direction
        Vector3 gravityDir = -wallUp;       // gravity goes down the wall

        Vector3 vel = _rb.linearVelocity;

        // Decompose current velocity into vertical + forward components in wall space
        float verticalVel = Vector3.Dot(vel, wallUp);
        float forwardVel = Vector3.Dot(vel, wallForward);

        // Base forward movement (constant auto-run)
        forwardVel = _forwardSpeed;

        // Apply custom gravity along the wall
        verticalVel += -_gravityStrength * Time.fixedDeltaTime;

        // Thrust when holding input, but only if we�re not past max height
        float currentHeight = GetHeightAlongWall(transform.position, _wall.position, wallUp);

        bool canGoUp = currentHeight < _maxHeight - 0.01f;
        bool onGround = currentHeight <= _minHeight + 0.01f;

        if (_wantsThrust && canGoUp)
        {
            verticalVel += _thrustAcceleration * Time.fixedDeltaTime;
        }

        // Clamp movement at top/bottom boundaries
        if (currentHeight >= _maxHeight && verticalVel > 0f)
        {
            verticalVel = 0f;
        }
        if (currentHeight <= _minHeight && verticalVel < 0f)
        {
            verticalVel = 0f;
        }

        // Build new velocity back in world space
        Vector3 newVel = wallUp * verticalVel + wallForward * forwardVel;
        _rb.linearVelocity = newVel;

        // Determine when we are actually "flying up" vs falling/walking
        _isSpraying = _wantsThrust && verticalVel > 0.01f;

        // Toggle spray particles
        if (_sprayParticles == null)
        {
            _sprayParticles = transform.Find("Model Container").Find("BaseMesh").Find("Spraycan").Find("SprayWayParticleSystem").GetComponent<ParticleSystem>();
        }
        else
        {
            var emission = _sprayParticles.emission;
            emission.enabled = _isSpraying;

            if(_isSpraying == true && _isPlayingAudioSpray == false)
            {
                StartSprayAudio();
            }
            else if (_isSpraying == false && _isPlayingAudioSpray == true)
            {
                StopSprayAudio();
            }
        }
    }

    private void AutoDescend()
    {
        // move down along the wall: use -wallUp
        Vector3 wallUp = _wall.forward;
        Vector3 descendDir = -wallUp * _descendAmount;

        // apply movement without physics impulses
        transform.position += descendDir;

        _sprayWayActivity.NextLayer();
    }

    private float GetHeightAlongWall(Vector3 worldPos, Vector3 wallOrigin, Vector3 wallUp)
    {
        return Vector3.Dot(worldPos - wallOrigin, wallUp);
    }

    private void OnDestroy()
    {
        _sprayParticles.transform.parent.GetComponent<Renderer>().enabled = false;
    }

    public void Stop()
    {
        _isSpraying = false;
        _stopped = true;
        FixedUpdate();
    }
    public void Ressurect() => _stopped = false;

    private void StartSprayAudio()
    {
        _isPlayingAudioSpray = true;
    }

    private void StopSprayAudio()
    {
        _isPlayingAudioSpray = false;
    }

}