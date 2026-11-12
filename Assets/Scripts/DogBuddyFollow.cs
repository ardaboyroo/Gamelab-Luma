using UnityEngine;

/// <summary>
/// Dog buddy that follows the local player around the open world.
/// It walks to a spot near the player, runs to catch up when it falls behind, teleports
/// back if it gets lost, stays on the ground, faces the player when standing, and can bark.
/// Unlike GuideDogBehaviour it needs no waypoints.
/// </summary>
public class DogBuddyFollow : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Leave empty to use the local player from PlayerService (or the object tagged Player).")]
    public Transform player;
    [Tooltip("Leave empty to use the Animator on the dog model.")]
    public Animator dogAnimator;

    [Header("Following")]
    [Tooltip("The dog stops when it is this close to the player.")]
    [Min(0f)] public float followDistance = 7f;
    [Tooltip("The dog starts walking again once the player is this much further away.")]
    [Min(0f)] public float resumeMargin = 2f;
    [Tooltip("Walking speed. The player walks at 20.")]
    [Min(0.01f)] public float walkSpeed = 22f;
    [Tooltip("Speed when it has fallen behind. The player skates at 35.")]
    [Min(0.01f)] public float runSpeed = 40f;
    [Tooltip("Start running when further away than this.")]
    [Min(0f)] public float runDistance = 25f;
    [Tooltip("Teleport next to the player when further away than this (e.g. after a cutscene or fast travel).")]
    [Min(1f)] public float teleportDistance = 90f;
    [Min(0.01f)] public float acceleration = 60f;
    [Min(0.01f)] public float turnSpeed = 8f;

    [Header("Only follow in these states")]
    public PlayerStates activeState = PlayerStates.StandardState;

    [Header("Grounding")]
    public LayerMask groundMask = ~0;
    [Tooltip("How high above the dog the ground ray starts, so it also works going uphill.")]
    [Min(0f)] public float groundProbeHeight = 20f;
    [Min(0f)] public float groundProbeDistance = 200f;
    public float groundOffset = 0f;
    [Min(0f)] public float groundSnapSpeed = 20f;

    [Header("Animation")]
    [Tooltip("Float parameter set to 1 while moving and 0 while standing (if the controller has it).")]
    public string speedParam = "speed";
    [Tooltip("Walk animation playback speed per unit of movement speed, so the paws match the ground.")]
    [Min(0f)] public float animationSpeedPerUnit = 0.06f;
    [Min(0f)] public float movingThreshold = 0.5f;

    [Header("Bark popup (optional)")]
    public DogBarkPopup barkPopupPrefab;
    public Vector3 barkLocalOffset = new Vector3(0f, 4.5f, 0f);
    [Min(0.01f)] public float barkScale = 1f;
    public Vector2 barkIntervalRange = new Vector2(8f, 15f);
    [Min(0.05f)] public float barkLifetimeSeconds = 2f;
    public string[] barkMessages = { "Woef woef!", "Waf!", "WOEF!", "Waf waf!", "*niest*", "*kwispelt*" };
    [Tooltip("On: the bubble always faces the camera (best with a top-down camera). Off: it only turns left/right.")]
    public bool barkFacesCameraFully = true;

    private float _currentSpeed;   // speed the dog is trying to move at
    private float _actualSpeed;    // speed it really moved this frame (drives the animation)
    private bool _isFollowing;
    private float _nextBarkAt;
    private int _speedParamHash;
    private bool _hasSpeedParam;

    private void Awake()
    {
        if (dogAnimator == null)
            dogAnimator = GetComponentInChildren<Animator>();

        if (dogAnimator != null)
        {
            dogAnimator.applyRootMotion = false;

            if (!string.IsNullOrEmpty(speedParam))
            {
                _speedParamHash = Animator.StringToHash(speedParam);
                foreach (var p in dogAnimator.parameters)
                    if (p.nameHash == _speedParamHash && p.type == AnimatorControllerParameterType.Float)
                        _hasSpeedParam = true;
            }
        }

        ScheduleNextBark();
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f)
            return;

        if (player == null)
            player = FindPlayer();

        Vector3 startPosition = transform.position;

        if (player == null || !PlayerIsInActiveState())
        {
            Brake(dt);
            _actualSpeed = 0f;
            StickToGround(dt);
            UpdateAnimator();
            return;
        }

        Vector3 toPlayer = Flat(player.position - transform.position);
        float distance = toPlayer.magnitude;

        if (distance > teleportDistance)
        {
            TeleportNearPlayer();
            return;
        }

        // Hysteresis so the dog doesn't stutter at the edge of followDistance.
        if (!_isFollowing && distance > followDistance + resumeMargin)
            _isFollowing = true;
        else if (_isFollowing && distance <= followDistance + StopTolerance)
            _isFollowing = false;

        if (_isFollowing)
        {
            float targetSpeed = distance > runDistance ? runSpeed : walkSpeed;
            _currentSpeed = Mathf.MoveTowards(_currentSpeed, targetSpeed, acceleration * dt);

            // Never step past the stop point (don't run into the player).
            float step = Mathf.Min(_currentSpeed * dt, Mathf.Max(0f, distance - followDistance));
            Vector3 dir = toPlayer / Mathf.Max(distance, 0.0001f);
            transform.position += dir * step;

            // Arrived: stop right away so the walk cycle doesn't keep playing in place.
            if (distance - step <= followDistance + StopTolerance)
            {
                _isFollowing = false;
                _currentSpeed = 0f;
            }

            TurnTowards(dir, dt);
        }
        else
        {
            Brake(dt);

            // Standing still: look at the player.
            if (distance > 0.01f)
                TurnTowards(toPlayer / distance, dt);

            TryBark();
        }

        StickToGround(dt);
        _actualSpeed = Flat(transform.position - startPosition).magnitude / dt;
        UpdateAnimator();
    }

    // -------------------- helpers --------------------

    private const float StopTolerance = 0.05f;

    private static Vector3 Flat(Vector3 v)
    {
        v.y = 0f;
        return v;
    }

    private Transform FindPlayer()
    {
        if (PlayerService.Instance != null && PlayerService.Instance.LocalPlayer != null)
            return PlayerService.Instance.LocalPlayer.transform;

        GameObject tagged = GameObject.FindWithTag("Player");
        return tagged != null ? tagged.transform : null;
    }

    private bool PlayerIsInActiveState()
    {
        // No PlayerService / state manager: just keep following.
        if (PlayerService.Instance == null || PlayerService.Instance.LocalPlayer == null)
            return true;

        PlayerStateManager states = PlayerService.Instance.LocalPlayer.StateManager;
        return states == null || states.CurrentState == activeState;
    }

    private void Brake(float dt)
    {
        _currentSpeed = Mathf.MoveTowards(_currentSpeed, 0f, acceleration * 2f * dt);
        _isFollowing = _isFollowing && _currentSpeed > 0f;
    }

    private void TurnTowards(Vector3 direction, float dt)
    {
        if (direction.sqrMagnitude < 0.0001f)
            return;

        Quaternion target = Quaternion.LookRotation(direction, Vector3.up);
        float t = 1f - Mathf.Exp(-turnSpeed * dt);
        transform.rotation = Quaternion.Slerp(transform.rotation, target, t);
    }

    private void TeleportNearPlayer()
    {
        Vector3 behind = player.position - Flat(player.forward).normalized * followDistance;
        transform.position = behind;
        _currentSpeed = 0f;
        _actualSpeed = 0f;
        _isFollowing = false;

        Vector3 look = Flat(player.position - behind);
        if (look.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(look.normalized, Vector3.up);

        StickToGround(0f, snapInstantly: true);
    }

    private void StickToGround(float dt, bool snapInstantly = false)
    {
        Vector3 origin = transform.position + Vector3.up * groundProbeHeight;
        RaycastHit[] hits = Physics.RaycastAll(origin, Vector3.down, groundProbeHeight + groundProbeDistance,
            groundMask, QueryTriggerInteraction.Ignore);

        float bestDistance = float.MaxValue;
        float groundY = 0f;
        bool found = false;

        foreach (RaycastHit hit in hits)
        {
            Transform t = hit.collider.transform;

            // Ignore the dog itself and anything that belongs to the player (capsule, camera sphere).
            if (t.IsChildOf(transform) || (player != null && t.IsChildOf(player)))
                continue;

            if (hit.distance < bestDistance)
            {
                bestDistance = hit.distance;
                groundY = hit.point.y;
                found = true;
            }
        }

        if (!found)
            return;

        Vector3 pos = transform.position;
        float targetY = groundY + groundOffset;
        pos.y = snapInstantly ? targetY : Mathf.Lerp(pos.y, targetY, 1f - Mathf.Exp(-groundSnapSpeed * dt));
        transform.position = pos;
    }

    private void UpdateAnimator()
    {
        if (dogAnimator == null)
            return;

        bool moving = _actualSpeed > movingThreshold;

        if (_hasSpeedParam)
            dogAnimator.SetFloat(_speedParamHash, moving ? 1f : 0f);

        // The dog only has a walk cycle, so standing still = pausing that cycle.
        dogAnimator.speed = moving ? Mathf.Max(0.5f, _actualSpeed * animationSpeedPerUnit) : 0f;
    }

    private void ScheduleNextBark()
    {
        float min = Mathf.Min(barkIntervalRange.x, barkIntervalRange.y);
        float max = Mathf.Max(barkIntervalRange.x, barkIntervalRange.y);
        _nextBarkAt = Time.time + Random.Range(min, max);
    }

    private void TryBark()
    {
        if (barkPopupPrefab == null || Time.time < _nextBarkAt)
            return;

        // Don't bark over a tutorial '!' - the '!' already means the buddy wants your attention.
        var marker = GetComponent<BuddyTutorialMarker>();
        if (marker != null && marker.HasPending)
        {
            ScheduleNextBark();
            return;
        }

        ScheduleNextBark();

        string msg = (barkMessages != null && barkMessages.Length > 0)
            ? barkMessages[Random.Range(0, barkMessages.Length)]
            : "Woef woef!";

        DogBarkPopup popup = Instantiate(barkPopupPrefab, transform);
        popup.transform.localPosition = barkLocalOffset;
        popup.transform.localRotation = Quaternion.identity;
        popup.transform.localScale *= barkScale;
        popup.Init(msg, barkLifetimeSeconds, Camera.main, !barkFacesCameraFully);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 0.5f);

        if (player != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(player.position, followDistance);
        }
    }
#endif
}
