using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Dog buddy that follows the local player.
/// - Follows with smooth "arrive" movement: it eases into a steady trailing distance instead of stop-start.
/// - Optional wandering when the player stands still (used in the house), staying inside the room and away from furniture.
/// - Walks to a DogBed (if there is one) every now and then and rests there.
/// - Can be petted (see Pet(); BuddyTutorialMarker calls it when the buddy is tapped).
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
    [Tooltip("The distance the dog likes to keep from the player.")]
    [Min(0f)] public float followDistance = 7f;
    [Tooltip("Within this extra distance the dog eases off its speed, so it glides into place instead of braking hard.")]
    [Min(0.01f)] public float slowDownDistance = 5f;
    [Tooltip("Normal top speed. The player walks at 20 in the open world.")]
    [Min(0.01f)] public float walkSpeed = 22f;
    [Tooltip("Top speed when it has fallen far behind. The player skates at 35.")]
    [Min(0.01f)] public float runSpeed = 40f;
    [Tooltip("Run when further behind than this.")]
    [Min(0f)] public float runDistance = 25f;
    [Tooltip("Teleport next to the player when further away than this (e.g. after a cutscene).")]
    [Min(1f)] public float teleportDistance = 90f;
    [Tooltip("How quickly the dog changes speed (units per second per second). Higher = snappier, lower = floatier.")]
    [Min(0.01f)] public float acceleration = 60f;
    [Min(0.01f)] public float turnSpeed = 8f;

    [Header("Only active in these states")]
    public PlayerStates activeState = PlayerStates.StandardState;

    [Header("Wandering (when the player stands still)")]
    public bool wanderWhenIdle = false;
    [Tooltip("Seconds the player has to stand still before the dog starts sniffing around.")]
    [Min(0f)] public float idleBeforeWander = 3f;
    [Min(0.01f)] public float wanderSpeed = 3f;
    [Tooltip("How far from the player the dog may wander (when there is no room grid to stay inside).")]
    [Min(0f)] public float wanderRadius = 3f;
    [Tooltip("Seconds the dog waits at each spot.")]
    public Vector2 wanderPause = new Vector2(1.5f, 4f);
    [Tooltip("Keep this far away from walls and room edges.")]
    [Min(0f)] public float wanderEdgeMargin = 0.6f;
    [Tooltip("The player counts as standing still below this speed.")]
    [Min(0f)] public float playerMovingSpeed = 0.5f;

    [Header("Dog bed")]
    [Tooltip("Chance (0-1) that the dog goes to lie in its bed instead of sniffing around, each time it picks something to do.")]
    [Range(0f, 1f)] public float bedChance = 0.35f;
    [Tooltip("How long it stays in bed (seconds), unless petted or the player walks far away.")]
    public Vector2 restDuration = new Vector2(10f, 25f);
    [Tooltip("Gets out of bed when the player is further away than this.")]
    [Min(0f)] public float wakeDistance = 12f;

    [Header("Petting")]
    [Tooltip("How long a petting moment lasts (seconds).")]
    [Min(0.1f)] public float petDuration = 1.6f;
    [Tooltip("0 = can always be petted. Otherwise the player has to be this close.")]
    [Min(0f)] public float petMaxDistance = 0f;
    public PetHeart heartPrefab;
    [Min(0)] public int heartsPerPet = 3;

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
    [Tooltip("Bool parameter set while lying in the bed (if the controller has it).")]
    public string lyingParam = "lying";
    [Tooltip("Walk animation playback speed per unit of movement speed, so the paws match the ground.")]
    [Min(0f)] public float animationSpeedPerUnit = 0.06f;
    [Tooltip("Below this speed the walk cycle eases to a stop.")]
    [Min(0f)] public float movingThreshold = 0.5f;
    [Tooltip("How quickly the walk cycle speeds up / slows down.")]
    [Min(0.01f)] public float animationSmoothing = 10f;

    [Header("Bark popup (optional)")]
    public DogBarkPopup barkPopupPrefab;
    public Vector3 barkLocalOffset = new Vector3(0f, 4.5f, 0f);
    [Tooltip("Size of the bark bubble and the petting hearts.")]
    [Min(0.01f)] public float barkScale = 1f;
    public Vector2 barkIntervalRange = new Vector2(8f, 15f);
    [Min(0.05f)] public float barkLifetimeSeconds = 2f;
    public string[] barkMessages = { "Woef woef!", "Waf!", "WOEF!", "Waf waf!", "*niest*", "*kwispelt*" };
    [Tooltip("On: the bubble always faces the camera (best with a top-down camera). Off: it only turns left/right.")]
    public bool barkFacesCameraFully = true;

    enum Mode { Follow, Wander, GoToBed, Resting, Petted }

    Mode _mode = Mode.Follow;
    Vector3 _velocity;              // horizontal velocity
    float _animSpeed;
    bool _walkCycleOn;
    float _nextBarkAt;
    int _speedParamHash, _lyingParamHash;
    bool _hasSpeedParam, _hasLyingParam;

    // player tracking
    Vector3 _lastPlayerPos;
    bool _hasLastPlayerPos;
    float _playerStillSince;

    // wander / bed
    Vector3 _wanderTarget;
    bool _hasWanderTarget;
    float _waitUntil;
    DogBed _bed;
    float _restUntil;

    // petting
    float _petUntil;
    Mode _modeBeforePet;
    Transform _model;
    Vector3 _modelBasePos, _modelBaseScale;
    float _bodyHeight = 1f, _bodyRadius = 0.5f;

    readonly RaycastHit[] _hits = new RaycastHit[16];
    readonly Collider[] _overlaps = new Collider[16];
    GridManager _grid;

    public bool IsResting => _mode == Mode.Resting;

    void Awake()
    {
        if (dogAnimator == null)
            dogAnimator = GetComponentInChildren<Animator>();

        if (dogAnimator != null)
        {
            dogAnimator.applyRootMotion = false;
            _speedParamHash = Animator.StringToHash(speedParam ?? "");
            _lyingParamHash = Animator.StringToHash(lyingParam ?? "");
            foreach (var p in dogAnimator.parameters)
            {
                if (p.nameHash == _speedParamHash && p.type == AnimatorControllerParameterType.Float) _hasSpeedParam = true;
                if (p.nameHash == _lyingParamHash && p.type == AnimatorControllerParameterType.Bool) _hasLyingParam = true;
            }
            _model = dogAnimator.transform;
        }
        else if (transform.childCount > 0)
        {
            _model = transform.GetChild(0);
        }

        if (_model != null)
        {
            _modelBasePos = _model.localPosition;
            _modelBaseScale = _model.localScale;
        }

        MeasureBody();
        ScheduleNextBark();
        _grid = FindFirstObjectByType<GridManager>();
    }

    void MeasureBody()
    {
        bool any = false;
        Bounds b = default;
        foreach (var r in GetComponentsInChildren<Renderer>())
        {
            if (r is ParticleSystemRenderer) continue;
            if (!any) { b = r.bounds; any = true; } else b.Encapsulate(r.bounds);
        }
        if (!any) return;
        _bodyHeight = Mathf.Max(0.05f, b.size.y);
        _bodyRadius = Mathf.Max(0.05f, Mathf.Max(b.extents.x, b.extents.z) * 0.6f);
    }

    void Update()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f) return;

        if (player == null)
            player = FindPlayer();

        TrackPlayer(dt);

        bool active = player != null && PlayerIsInActiveState();
        if (!active && _mode != Mode.Resting && _mode != Mode.Petted)
        {
            // menus, edit mode, dialogue...: stand still (a resting dog keeps resting)
            Steer(Vector3.zero, dt);
            Finish(dt);
            return;
        }

        float distance = player != null ? Flat(player.position - transform.position).magnitude : 0f;

        if (player != null && distance > teleportDistance && _mode != Mode.Resting)
        {
            TeleportNearPlayer();
            Finish(dt);
            return;
        }

        switch (_mode)
        {
            case Mode.Follow: UpdateFollow(distance, dt); break;
            case Mode.Wander: UpdateWander(dt); break;
            case Mode.GoToBed: UpdateGoToBed(dt); break;
            case Mode.Resting: UpdateResting(distance, dt); break;
            case Mode.Petted: UpdatePetted(dt); break;
        }

        Finish(dt);
    }

    void Finish(float dt)
    {
        transform.position += _velocity * dt;
        StickToGround(dt);
        UpdateAnimator(dt);
        UpdatePetBounce();
    }

    // -------------------- modes --------------------

    void UpdateFollow(float distance, float dt)
    {
        Vector3 toPlayer = Flat(player.position - transform.position);
        float gap = distance - followDistance;

        // Arrive: full speed when far, easing down to zero right at followDistance.
        float top = gap > runDistance ? runSpeed : walkSpeed;
        float desired = gap <= 0f ? 0f : top * Mathf.Clamp01(gap / slowDownDistance);

        Vector3 dir = distance > 0.0001f ? toPlayer / distance : transform.forward;
        Steer(dir * desired, dt);

        if (_velocity.sqrMagnitude < 0.01f && distance > 0.01f)
            TurnTowards(dir, dt);   // standing: look at the player
        else
            TurnTowards(_velocity, dt);

        if (wanderWhenIdle && PlayerStillFor() >= idleBeforeWander && gap <= slowDownDistance * 0.5f)
            StartIdleActivity();
        else
            TryBark();
    }

    void StartIdleActivity()
    {
        // sometimes go lie down in the bed, otherwise sniff around
        if (Random.value < bedChance && TryFindBed(out var bed))
        {
            _bed = bed;
            _mode = Mode.GoToBed;
            return;
        }

        _mode = Mode.Wander;
        _hasWanderTarget = false;
        _waitUntil = Time.time + Random.Range(0.2f, 1f);
    }

    void UpdateWander(float dt)
    {
        if (PlayerIsMoving())
        {
            _mode = Mode.Follow;
            return;
        }

        if (!_hasWanderTarget)
        {
            Steer(Vector3.zero, dt);
            if (_velocity.sqrMagnitude < 0.01f) TryBark();
            if (Time.time < _waitUntil) return;

            if (Random.value < bedChance * 0.5f && TryFindBed(out var bed))
            {
                _bed = bed;
                _mode = Mode.GoToBed;
                return;
            }

            _hasWanderTarget = TryPickWanderTarget(out _wanderTarget);
            if (!_hasWanderTarget) _waitUntil = Time.time + 1f;   // try again in a moment
            return;
        }

        if (MoveTowardsPoint(_wanderTarget, wanderSpeed, dt))
        {
            _hasWanderTarget = false;
            _waitUntil = Time.time + Random.Range(Mathf.Min(wanderPause.x, wanderPause.y), Mathf.Max(wanderPause.x, wanderPause.y));
        }
    }

    void UpdateGoToBed(float dt)
    {
        if (_bed == null || !_bed.IsAvailable || PlayerIsMoving())
        {
            _bed = null;
            _mode = Mode.Follow;
            return;
        }

        if (MoveTowardsPoint(_bed.RestPosition, wanderSpeed, dt))
        {
            _mode = Mode.Resting;
            _velocity = Vector3.zero;
            _restUntil = Time.time + Random.Range(Mathf.Min(restDuration.x, restDuration.y), Mathf.Max(restDuration.x, restDuration.y));
        }
    }

    void UpdateResting(float distance, float dt)
    {
        _velocity = Vector3.zero;

        if (_bed == null || !_bed.IsAvailable || Time.time >= _restUntil || (player != null && distance > wakeDistance))
        {
            _bed = null;
            _mode = Mode.Follow;
            return;
        }

        // snuggle into the bed's spot and direction
        Vector3 p = transform.position;
        Vector3 target = _bed.RestPosition;
        float t = 1f - Mathf.Exp(-6f * dt);
        p.x = Mathf.Lerp(p.x, target.x, t);
        p.z = Mathf.Lerp(p.z, target.z, t);
        transform.position = p;
        TurnTowards(_bed.RestForward, dt);
    }

    void UpdatePetted(float dt)
    {
        Steer(Vector3.zero, dt * 3f);
        if (player != null)
        {
            Vector3 toPlayer = Flat(player.position - transform.position);
            if (_modeBeforePet != Mode.Resting && toPlayer.sqrMagnitude > 0.0001f)
                TurnTowards(toPlayer, dt * 1.5f);
        }

        if (Time.time >= _petUntil)
        {
            _mode = _modeBeforePet == Mode.Resting && _bed != null ? Mode.Resting : Mode.Follow;
            if (_mode == Mode.Follow) _playerStillSince = Time.time;   // don't wander off straight away
        }
    }

    // -------------------- petting --------------------

    public bool CanBePetted()
    {
        if (player == null || !PlayerIsInActiveState()) return false;
        if (_mode == Mode.Petted) return false;
        if (petMaxDistance > 0f && Flat(player.position - transform.position).magnitude > petMaxDistance) return false;
        return true;
    }

    /// Pet the dog: it stops, looks at you, does a happy hop and hearts float up.
    public void Pet()
    {
        if (!CanBePetted()) return;

        _modeBeforePet = _mode;
        _mode = Mode.Petted;
        _petUntil = Time.time + petDuration;

        // no speech bubble while petting; also push the next random bark back a bit
        foreach (var bubble in GetComponentsInChildren<DogBarkPopup>())
            Destroy(bubble.gameObject);
        ScheduleNextBark();

        if (heartPrefab != null)
        {
            Vector3 top = transform.position + Vector3.up * (_bodyHeight * 0.9f);
            for (int i = 0; i < heartsPerPet; i++)
            {
                Vector3 jitter = new Vector3(Random.Range(-1f, 1f), Random.Range(0f, 0.3f), Random.Range(-1f, 1f)) * (_bodyRadius);
                var h = Instantiate(heartPrefab, top + jitter, Quaternion.identity);
                h.Play(barkScale, i * 0.12f);
            }
        }
    }

    void UpdatePetBounce()
    {
        if (_model == null) return;

        if (_mode != Mode.Petted)
        {
            _model.localPosition = _modelBasePos;
            _model.localScale = _modelBaseScale;
            return;
        }

        // two little happy hops with a bit of squash and stretch
        float elapsed = petDuration - (_petUntil - Time.time);
        float hopTime = Mathf.Clamp01(elapsed / (petDuration * 0.6f));
        float hop = Mathf.Abs(Mathf.Sin(hopTime * Mathf.PI * 2f)) * (1f - hopTime * 0.4f);
        float lift = hop * 0.12f;
        float squash = 1f + (hop - 0.5f) * 0.1f;

        Vector3 up = _model.parent != null ? _model.parent.InverseTransformVector(Vector3.up * _bodyHeight * lift) : Vector3.up * lift;
        _model.localPosition = _modelBasePos + up;
        _model.localScale = new Vector3(_modelBaseScale.x / Mathf.Sqrt(squash), _modelBaseScale.y * squash, _modelBaseScale.z / Mathf.Sqrt(squash));
    }

    // -------------------- movement helpers --------------------

    /// Smoothly changes the velocity towards the wanted velocity (no instant starts or stops).
    void Steer(Vector3 desiredVelocity, float dt)
    {
        _velocity = Vector3.MoveTowards(_velocity, Flat(desiredVelocity), acceleration * dt);
    }

    /// Walks to a point with a gentle arrival. Returns true when it has arrived.
    bool MoveTowardsPoint(Vector3 target, float speed, float dt)
    {
        Vector3 to = Flat(target - transform.position);
        float d = to.magnitude;
        float arriveRadius = Mathf.Max(0.05f, _bodyRadius * 0.3f);
        if (d <= arriveRadius && _velocity.magnitude < speed * 0.25f)
        {
            Steer(Vector3.zero, dt);
            return true;
        }

        float slow = Mathf.Max(arriveRadius * 2f, speed * 0.6f);
        float desired = speed * Mathf.Clamp01(d / slow);
        Vector3 dir = d > 0.0001f ? to / d : transform.forward;
        Steer(dir * desired, dt);
        TurnTowards(_velocity.sqrMagnitude > 0.0001f ? _velocity : dir, dt);
        return false;
    }

    void TurnTowards(Vector3 direction, float dt)
    {
        direction = Flat(direction);
        if (direction.sqrMagnitude < 0.0001f) return;
        Quaternion target = Quaternion.LookRotation(direction.normalized, Vector3.up);
        float t = 1f - Mathf.Exp(-turnSpeed * dt);
        transform.rotation = Quaternion.Slerp(transform.rotation, target, t);
    }

    void TeleportNearPlayer()
    {
        Vector3 behind = player.position - Flat(player.forward).normalized * followDistance;
        transform.position = behind;
        _velocity = Vector3.zero;
        _mode = Mode.Follow;

        Vector3 look = Flat(player.position - behind);
        if (look.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(look.normalized, Vector3.up);

        StickToGround(0f, snapInstantly: true);
    }

    // -------------------- player tracking --------------------

    void TrackPlayer(float dt)
    {
        if (player == null) { _hasLastPlayerPos = false; return; }

        Vector3 p = player.position;
        if (!_hasLastPlayerPos)
        {
            _lastPlayerPos = p;
            _hasLastPlayerPos = true;
            _playerStillSince = Time.time;
            return;
        }

        float speed = Flat(p - _lastPlayerPos).magnitude / dt;
        _lastPlayerPos = p;
        if (speed > playerMovingSpeed) _playerStillSince = Time.time;
    }

    bool PlayerIsMoving() => Time.time - _playerStillSince < 0.15f;
    float PlayerStillFor() => Time.time - _playerStillSince;

    Transform FindPlayer()
    {
        if (PlayerService.Instance != null && PlayerService.Instance.LocalPlayer != null)
            return PlayerService.Instance.LocalPlayer.transform;

        GameObject tagged = GameObject.FindWithTag("Player");
        return tagged != null ? tagged.transform : null;
    }

    bool PlayerIsInActiveState()
    {
        if (PlayerService.Instance == null || PlayerService.Instance.LocalPlayer == null)
            return true;

        PlayerStateManager states = PlayerService.Instance.LocalPlayer.StateManager;
        return states == null || states.CurrentState == activeState;
    }

    // -------------------- wander targets --------------------

    bool TryPickWanderTarget(out Vector3 target)
    {
        for (int attempt = 0; attempt < 12; attempt++)
        {
            Vector3 candidate;
            if (_grid != null)
            {
                float m = wanderEdgeMargin;
                Vector3 o = _grid.transform.position;
                float w = _grid.width * _grid.cellSize, h = _grid.height * _grid.cellSize;
                candidate = new Vector3(Random.Range(o.x + m, o.x + w - m), transform.position.y, Random.Range(o.z + m, o.z + h - m));
                if (wanderRadius > 0f && player != null && Flat(candidate - player.position).magnitude > wanderRadius * 1.5f) continue;
            }
            else
            {
                Vector3 centre = player != null ? player.position : transform.position;
                Vector2 r = Random.insideUnitCircle * wanderRadius;
                candidate = new Vector3(centre.x + r.x, transform.position.y, centre.z + r.y);
            }

            if (Flat(candidate - transform.position).magnitude < _bodyRadius * 2f) continue;
            if (player != null && Flat(candidate - player.position).magnitude < _bodyRadius * 2f) continue;
            if (!SpotIsFree(candidate) || PathIsBlocked(transform.position, candidate)) continue;

            target = candidate;
            return true;
        }

        target = default;
        return false;
    }

    bool SpotIsFree(Vector3 spot)
    {
        Vector3 centre = spot + Vector3.up * (_bodyHeight * 0.5f);
        int n = Physics.OverlapSphereNonAlloc(centre, _bodyRadius, _overlaps, ~0, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < n; i++)
            if (!IsIgnored(_overlaps[i].transform)) return false;
        return true;
    }

    bool PathIsBlocked(Vector3 from, Vector3 to)
    {
        Vector3 a = from + Vector3.up * (_bodyHeight * 0.5f);
        Vector3 b = to + Vector3.up * (_bodyHeight * 0.5f);
        Vector3 d = b - a;
        float len = d.magnitude;
        if (len < 0.001f) return false;
        int n = Physics.SphereCastNonAlloc(a, _bodyRadius * 0.6f, d / len, _hits, len, ~0, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < n; i++)
            if (!IsIgnored(_hits[i].transform)) return true;
        return false;
    }

    bool IsIgnored(Transform t)
    {
        if (t.IsChildOf(transform)) return true;
        if (player != null && t.IsChildOf(player)) return true;
        if (_bed != null && t.IsChildOf(_bed.transform)) return true;
        return false;
    }

    bool TryFindBed(out DogBed bed)
    {
        bed = null;
        float best = float.MaxValue;
        foreach (var b in FindObjectsByType<DogBed>(FindObjectsSortMode.None))
        {
            if (!b.IsAvailable) continue;
            float d = Flat(b.RestPosition - transform.position).sqrMagnitude;
            if (d < best) { best = d; bed = b; }
        }
        return bed != null;
    }

    // -------------------- grounding --------------------

    static Vector3 Flat(Vector3 v)
    {
        v.y = 0f;
        return v;
    }

    void StickToGround(float dt, bool snapInstantly = false)
    {
        Vector3 origin = transform.position + Vector3.up * groundProbeHeight;
        int n = Physics.RaycastNonAlloc(origin, Vector3.down, _hits, groundProbeHeight + groundProbeDistance,
            groundMask, QueryTriggerInteraction.Ignore);

        float bestDistance = float.MaxValue;
        float groundY = 0f;
        bool found = false;

        for (int i = 0; i < n; i++)
        {
            Transform t = _hits[i].collider.transform;

            // Ignore the dog itself and anything that belongs to the player (capsule, camera sphere).
            // A bed counts as ground only while the dog is lying in it.
            if (t.IsChildOf(transform) || (player != null && t.IsChildOf(player))) continue;
            if (_bed != null && _mode != Mode.Resting && t.IsChildOf(_bed.transform)) continue;

            if (_hits[i].distance < bestDistance)
            {
                bestDistance = _hits[i].distance;
                groundY = _hits[i].point.y;
                found = true;
            }
        }

        if (_mode == Mode.Resting && _bed != null)
        {
            groundY = Mathf.Max(found ? groundY : _bed.RestPosition.y, _bed.RestPosition.y);
            found = true;
        }

        if (!found)
            return;

        Vector3 pos = transform.position;
        float targetY = groundY + groundOffset;
        pos.y = snapInstantly ? targetY : Mathf.Lerp(pos.y, targetY, 1f - Mathf.Exp(-groundSnapSpeed * dt));
        transform.position = pos;
    }

    // -------------------- animation --------------------

    void UpdateAnimator(float dt)
    {
        if (dogAnimator == null)
            return;

        float speed = _velocity.magnitude;

        // hysteresis so the walk cycle doesn't flicker on/off around the threshold
        if (_walkCycleOn && speed < movingThreshold * 0.5f) _walkCycleOn = false;
        else if (!_walkCycleOn && speed > movingThreshold) _walkCycleOn = true;

        float target = _walkCycleOn ? Mathf.Max(0.35f, speed * animationSpeedPerUnit) : 0f;
        _animSpeed = Mathf.Lerp(_animSpeed, target, 1f - Mathf.Exp(-animationSmoothing * dt));
        if (!_walkCycleOn && _animSpeed < 0.02f) _animSpeed = 0f;
        dogAnimator.speed = _animSpeed;

        if (_hasSpeedParam)
            dogAnimator.SetFloat(_speedParamHash, _walkCycleOn ? 1f : 0f);
        if (_hasLyingParam)
            dogAnimator.SetBool(_lyingParamHash, _mode == Mode.Resting);
    }

    // -------------------- barking --------------------

    void ScheduleNextBark()
    {
        float min = Mathf.Min(barkIntervalRange.x, barkIntervalRange.y);
        float max = Mathf.Max(barkIntervalRange.x, barkIntervalRange.y);
        _nextBarkAt = Time.time + Random.Range(min, max);
    }

    void TryBark()
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
        SpawnBark(msg);
    }

    void SpawnBark(string msg)
    {
        if (barkPopupPrefab == null) return;

        // only one bubble at a time
        foreach (var old in GetComponentsInChildren<DogBarkPopup>())
            Destroy(old.gameObject);

        DogBarkPopup popup = Instantiate(barkPopupPrefab, transform);
        popup.transform.localPosition = barkLocalOffset;
        popup.transform.localRotation = Quaternion.identity;
        popup.transform.localScale *= barkScale;
        popup.Init(msg, barkLifetimeSeconds, Camera.main, !barkFacesCameraFully);
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 0.5f);

        if (player != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(player.position, followDistance);
        }

        if (_hasWanderTarget)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, _wanderTarget);
        }
    }
#endif
}
