using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class GuideDogBehaviour : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    public Transform[] waypoints;

    [Header("Bark Popup (when waiting)")]
    public DogBarkPopup barkPopupPrefab;
    public Camera barkCamera;
    public Vector3 barkLocalOffset = new Vector3(0f, 1.2f, 0f);
    public Vector2 barkIntervalRange = new Vector2(3f, 7f);
    public float barkLifetimeSeconds = 2f;
    public bool barkYawOnly = true;
    public float barkSuppressEndDistance = 1.5f;
    public string[] barkMessages = { "Woof woof!" };

    [Header("Animator")]
    public Animator dogAnimator;
    public string speedParam = "speed";
    public float movingThreshold = 0.05f;

    [Header("Movement")]
    [Min(0.01f)] public float moveSpeed = 3.5f;
    [Min(0.01f)] public float rotationSpeed = 8f;

    [Header("Leading Settings")]
    [Min(0.01f)] public float leadMaxDistance = 7f;
    [Min(0.0f)] public float leadAheadOnPath = 2.5f;
    [Min(0.0f)] public float leadReturnHysteresis = 0.75f;

    [Header("Path Logic")]
    [Min(0.01f)] public float pathSnapRadius = 3f;
    [Min(0.01f)] public float wrongWayBacktrackDistance = 0.5f;
    [Min(0.0f)] public float switchCooldown = 0.35f;

    [Header("Following Settings")]
    [Min(0.0f)] public float followStopDistance = 2f;
    [Min(0.0f)] public float followStopHysteresis = 0.25f;

    [Header("Path Leashing")]
    public bool leashDogToPath = true;
    [Min(0.0f)] public float dogMaxPathDrift = 0.75f;
    [Min(0.01f)] public float leashPullSpeed = 10f;

    [Header("Grounding")]
    public LayerMask groundMask = ~0;
    public float groundProbeHeight = 1.0f;
    public float groundProbeDistance = 10f;
    public float groundOffset = 0.33f;
    public bool stickToGround = true;
    [Min(0.0f)] public float groundSnapSpeed = 25f;

    private enum DogState { Leading, Following }
    [SerializeField] private DogState _state = DogState.Leading;

    // Path polyline (XZ) + cumulative length
    private Vector3[] _pathXZ;
    private float[] _cumLen;
    private float _totalLen;

    // Player/path progress
    private float _prevPlayerS;
    private float _switchLockUntil;
    private bool _returningToPlayer;

    // Runtime
    private float _currentSpeed;
    private Vector3 _debugTargetXZ;
    private float _nextBarkAt;
    private bool _wasWaiting;

    void Awake()
    {
        if (dogAnimator == null)
            dogAnimator = GetComponentInChildren<Animator>();

        RebuildPath();
    }

    void OnValidate()
    {
        RebuildPath();
    }

    void Start()
    {
        if (player != null && _pathXZ != null && _pathXZ.Length >= 2)
            ProjectToPathXZ(player.position, out _prevPlayerS, out _, out _);
    }

    void Update()
    {
        if (PlayerStateMachine.Instance.Current is not StandardPlayerState) return;
        if (player == null) return;
        if (_pathXZ == null || _pathXZ.Length < 2) return;

        float dt = Time.deltaTime;
        if (dt <= 0f) return;

        // --- player projection on path ---
        ProjectToPathXZ(player.position, out float playerS, out Vector3 playerClosest, out float playerDistToPath);

        float deltaS = playerS - _prevPlayerS;
        if (Mathf.Abs(deltaS) < 0.01f) deltaS = 0f;

        bool playerOnPath = playerDistToPath <= pathSnapRadius;
        bool playerBacktracking = playerOnPath && deltaS < -wrongWayBacktrackDistance;
        bool playerOffPath = !playerOnPath;

        // --- transitions with cooldown ---
        if (Time.time >= _switchLockUntil)
        {
            if (_state == DogState.Leading)
            {
                if (playerOffPath || playerBacktracking)
                    EnterFollowing();
            }
            else // Following
            {
                float distDogPlayer = Vector3.Distance(transform.position, player.position);
                if (playerOnPath && !playerBacktracking && distDogPlayer <= leadMaxDistance)
                {
                    EnterLeading();
                    ProjectToPathXZ(player.position, out _prevPlayerS, out _, out _);
                }
            }
        }

        // --- choose target (XZ only) ---
        Vector3 targetXZ = (_state == DogState.Leading)
            ? GetLeadTargetXZ(playerS)
            : GetFollowTargetXZ();

        _debugTargetXZ = targetXZ;

        // --- move XZ ---
        MoveTowardsXZ(targetXZ, moveSpeed, dt);

        // leash to path only when it makes sense
        if (leashDogToPath && (_state == DogState.Leading || playerOnPath))
            PullDogToPath(dt);

        // ground
        StickToGroundSmooth(dt);

        // idle facing
        FacePlayerWhenStopped(dt);

        // animator
        UpdateAnimator();

        ProjectToPathXZ(transform.position, out float dogS, out _, out _);
        HandleWaitingBark(deltaS, playerOffPath, playerBacktracking, dogS);

        _prevPlayerS = playerS;
    }

    // -------------------- states --------------------

    private void EnterFollowing()
    {
        _wasWaiting = false;
        _nextBarkAt = 0f;

        _state = DogState.Following;
        _switchLockUntil = Time.time + switchCooldown;
        _returningToPlayer = false;
    }

    private void EnterLeading()
    {
        _wasWaiting = false;
        _nextBarkAt = 0f;

        _state = DogState.Leading;
        _switchLockUntil = Time.time + switchCooldown;
        _returningToPlayer = false;
    }

    // -------------------- targets --------------------

    private Vector3 GetLeadTargetXZ(float playerS)
    {
        float distDogPlayer = Vector3.Distance(transform.position, player.position);

        if (!_returningToPlayer && distDogPlayer > leadMaxDistance)
            _returningToPlayer = true;
        else if (_returningToPlayer && distDogPlayer < leadMaxDistance * Mathf.Clamp01(leadReturnHysteresis))
            _returningToPlayer = false;

        if (_returningToPlayer)
        {
            return PointAtS(playerS);
        }

        Vector3 playerPosXZ = new Vector3(player.position.x, 0f, player.position.z);
        Vector3 pathDir = GetPathDirectionAtS(playerS);
        Vector3 rightDir = new Vector3(pathDir.z, 0f, -pathDir.x).normalized;

        float sideOffset = 1.5f;
        Vector3 desiredSidePosition = playerPosXZ + (rightDir * sideOffset);

        float currentDistToTarget = Vector3.Distance(new Vector3(transform.position.x, 0f, transform.position.z), desiredSidePosition);
        if (currentDistToTarget < 0.1f && _currentSpeed <= movingThreshold)
        {
            return new Vector3(transform.position.x, 0f, transform.position.z);
        }

        return desiredSidePosition;
    }

    private Vector3 GetPathDirectionAtS(float s)
    {
        float sampleDelta = 0.5f;
        Vector3 p1 = PointAtS(s);
        Vector3 p2 = PointAtS(s + sampleDelta);
        Vector3 dir = (p2 - p1);
        if (dir.sqrMagnitude < 0.0001f) return Vector3.forward;
        return dir.normalized;
    }

    private Vector3 GetFollowTargetXZ()
    {
        Vector3 dogXZ = new Vector3(transform.position.x, 0f, transform.position.z);
        Vector3 plXZ = new Vector3(player.position.x, 0f, player.position.z);

        float distToPlayer = Vector3.Distance(dogXZ, plXZ);
        float stop = Mathf.Max(0f, followStopDistance);

        if (distToPlayer <= stop)
            return dogXZ;

        Vector3 toDog = dogXZ - plXZ;
        if (toDog.sqrMagnitude < 0.0001f)
        {
            toDog = -new Vector3(player.forward.x, 0f, player.forward.z).normalized;
            if (toDog.sqrMagnitude < 0.0001f) toDog = -Vector3.forward;
        }

        return plXZ + toDog.normalized * stop;
    }

    // -------------------- movement & facing --------------------

    private void MoveTowardsXZ(Vector3 targetXZ, float speed, float dt)
    {
        Vector3 pos = transform.position;
        Vector3 currentXZ = new Vector3(pos.x, 0f, pos.z);

        Vector3 nextXZ = Vector3.MoveTowards(currentXZ, targetXZ, speed * dt);
        Vector3 delta = nextXZ - currentXZ;

        pos.x = nextXZ.x;
        pos.z = nextXZ.z;
        transform.position = pos;

        _currentSpeed = delta.magnitude / Mathf.Max(dt, 0.000001f);

        if (delta.sqrMagnitude > 0.000001f)
        {
            Vector3 dir = delta.normalized;
            Quaternion targetRot = Quaternion.LookRotation(dir, Vector3.up);

            float t = 1f - Mathf.Exp(-rotationSpeed * dt);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, t);
        }
    }

    private void FacePlayerWhenStopped(float dt)
    {
        if (_currentSpeed > movingThreshold) return;

        Vector3 toPlayer = player.position - transform.position;
        toPlayer.y = 0f;
        if (toPlayer.sqrMagnitude < 0.0001f) return;

        Quaternion lookRot = Quaternion.LookRotation(toPlayer.normalized, Vector3.up);
        float t = 1f - Mathf.Exp(-rotationSpeed * dt);
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, t);
    }

    private void UpdateAnimator()
    {
        if (dogAnimator == null || string.IsNullOrEmpty(speedParam)) return;
        dogAnimator.SetFloat(speedParam, (_currentSpeed > movingThreshold) ? 1f : 0f);
    }

    private void PullDogToPath(float dt)
    {
        ProjectToPathXZ(transform.position, out _, out Vector3 closest, out float dist);

        if (dist <= dogMaxPathDrift) return;

        Vector3 pos = transform.position;
        Vector3 currentXZ = new Vector3(pos.x, 0f, pos.z);
        Vector3 corrected = Vector3.MoveTowards(currentXZ, closest, leashPullSpeed * dt);

        pos.x = corrected.x;
        pos.z = corrected.z;
        transform.position = pos;
    }

    // -------------------- grounding --------------------

    private void StickToGroundSmooth(float dt)
    {
        if (!stickToGround) return;

        Vector3 pos = transform.position;

        bool hitOk = Physics.Raycast(pos + Vector3.up * groundProbeHeight, Vector3.down, out RaycastHit hit,
            groundProbeHeight + Mathf.Max(groundProbeDistance, 5f), groundMask, QueryTriggerInteraction.Ignore);

        if (!hitOk)
            hitOk = Physics.Raycast(pos + Vector3.up * 50f, Vector3.down, out hit, 200f, groundMask, QueryTriggerInteraction.Ignore);

        if (hitOk)
        {
            float targetY = hit.point.y + groundOffset;
            float t = 1f - Mathf.Exp(-groundSnapSpeed * dt);
            pos.y = Mathf.Lerp(pos.y, targetY, t);
            transform.position = pos;
        }
    }

    // -------------------- path build + projection --------------------

    private void RebuildPath()
    {
        if (waypoints == null || waypoints.Length < 2)
        {
            _pathXZ = null;
            _cumLen = null;
            _totalLen = 0f;
            return;
        }

        List<Vector3> raw = new List<Vector3>(waypoints.Length);
        for (int i = 0; i < waypoints.Length; i++)
        {
            if (waypoints[i] == null) continue;
            Vector3 w = waypoints[i].position;
            Vector3 p = new Vector3(w.x, 0f, w.z);

            if (raw.Count == 0 || (raw[raw.Count - 1] - p).sqrMagnitude > 0.0001f)
                raw.Add(p);
        }

        if (raw.Count < 2)
        {
            _pathXZ = null;
            _cumLen = null;
            _totalLen = 0f;
            return;
        }

        _pathXZ = raw.ToArray();

        _cumLen = new float[_pathXZ.Length];
        _cumLen[0] = 0f;

        for (int i = 1; i < _pathXZ.Length; i++)
            _cumLen[i] = _cumLen[i - 1] + Vector3.Distance(_pathXZ[i - 1], _pathXZ[i]);

        _totalLen = _cumLen[_cumLen.Length - 1];
    }

    private void ProjectToPathXZ(Vector3 worldPos, out float s, out Vector3 closestPoint, out float distance)
    {
        Vector3 p = new Vector3(worldPos.x, 0f, worldPos.z);

        float bestDistSq = float.MaxValue;
        float bestS = 0f;
        Vector3 bestPoint = _pathXZ[0];

        for (int i = 0; i < _pathXZ.Length - 1; i++)
        {
            Vector3 a = _pathXZ[i];
            Vector3 b = _pathXZ[i + 1];
            Vector3 ab = b - a;
            float abLenSq = ab.sqrMagnitude;

            float t = 0f;
            if (abLenSq > 0.000001f)
                t = Mathf.Clamp01(Vector3.Dot(p - a, ab) / abLenSq);

            Vector3 q = a + ab * t;
            float dSq = (p - q).sqrMagnitude;

            if (dSq < bestDistSq)
            {
                bestDistSq = dSq;
                float segLen = Mathf.Sqrt(abLenSq);
                bestS = _cumLen[i] + t * segLen;
                bestPoint = q;
            }
        }

        s = bestS;
        closestPoint = bestPoint;
        distance = Mathf.Sqrt(bestDistSq);
    }

    private Vector3 PointAtS(float s)
    {
        if (_pathXZ == null || _pathXZ.Length == 0) return Vector3.zero;
        if (s <= 0f) return _pathXZ[0];
        if (s >= _totalLen) return _pathXZ[_pathXZ.Length - 1];

        int lo = 0;
        int hi = _cumLen.Length - 1;

        while (lo < hi)
        {
            int mid = (lo + hi) >> 1;
            if (_cumLen[mid] < s) lo = mid + 1;
            else hi = mid;
        }

        int i = Mathf.Clamp(lo - 1, 0, _cumLen.Length - 2);
        float s0 = _cumLen[i];
        float s1 = _cumLen[i + 1];
        float t = (s1 > s0) ? Mathf.Clamp01((s - s0) / (s1 - s0)) : 0f;

        return Vector3.Lerp(_pathXZ[i], _pathXZ[i + 1], t);
    }

    private void HandleWaitingBark(float deltaS, bool playerOffPath, bool playerBacktracking, float dogS)
    {
        if (barkPopupPrefab == null) { _wasWaiting = false; return; }

        bool notAtEnd = dogS < (_totalLen - Mathf.Max(0f, barkSuppressEndDistance));

        bool dogIdle = _currentSpeed <= movingThreshold;
        bool playerNotProgressing = Mathf.Abs(deltaS) < 0.01f;

        bool waiting =
            dogIdle &&
            notAtEnd &&
            (
                (_state == DogState.Following && (playerOffPath || playerBacktracking)) ||
                (_state == DogState.Leading && !_returningToPlayer && playerNotProgressing)
            );

        if (!waiting)
        {
            _wasWaiting = false;
            return;
        }

        if (!_wasWaiting)
        {
            _wasWaiting = true;
            _nextBarkAt = Time.time + Random.Range(barkIntervalRange.x, barkIntervalRange.y);
        }

        if (Time.time >= _nextBarkAt)
        {
            SpawnBarkPopup();
            _nextBarkAt = Time.time + Random.Range(barkIntervalRange.x, barkIntervalRange.y);
        }
    }

    private void SpawnBarkPopup()
    {
        string msg = (barkMessages != null && barkMessages.Length > 0)
            ? barkMessages[Random.Range(0, barkMessages.Length)]
            : "Woof woof!";

        DogBarkPopup popup = Instantiate(barkPopupPrefab, transform);
        popup.transform.localPosition = barkLocalOffset;
        popup.transform.localRotation = Quaternion.identity;

        popup.Init(msg, barkLifetimeSeconds, barkCamera != null ? barkCamera : Camera.main, barkYawOnly);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(new Vector3(_debugTargetXZ.x, transform.position.y + 0.1f, _debugTargetXZ.z), 0.15f);
    }
#endif
}