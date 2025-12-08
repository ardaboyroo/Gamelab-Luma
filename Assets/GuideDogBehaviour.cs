using UnityEngine;

[RequireComponent(typeof(Collider))]
public class GuideDogBehaviour : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    public Transform[] waypoints;   // ordered along the desired path

    [Header("Movement")]
    public float moveSpeed = 3.5f;
    public float rotationSpeed = 8f;

    [Header("Distances")]
    public float leadMaxDistance = 7f;        // how far ahead of the player the dog may be
    public float followBehindDistance = 1.5f; // how far behind the player the dog tries to stay
    public float waypointReachRadius = 0.25f; // for "reached waypoint" check
    public float pathSnapRadius = 3f;         // how close the player must be to path to be “on it”

    [Header("Direction check")]
    public int backwardTolerance = 1;         // how many waypoints player can step back without counting as wrong way

    [Header("Grounding")]
    public LayerMask groundMask = ~0;         // which layers count as ground
    public float groundProbeHeight = 1.0f;    // ray start height above dog
    public float groundProbeDistance = 3.0f;  // how far down we cast
    public float groundOffset = 0.0f;         // offset if pivot is not at paws
    public bool stickToGround = true;

    private enum DogState { Leading, FollowingPlayer }
    private DogState _state = DogState.Leading;

    private int _currentWaypointIndex;
    private int _lastPlayerWaypointIndex;
    private Vector3 _moveTarget;
    private float _currentSpeed;

    void Start()
    {
        if (waypoints != null && waypoints.Length > 0)
        {
            _currentWaypointIndex = 0;
            _moveTarget = waypoints[_currentWaypointIndex].position;
        }

        if (player != null && waypoints != null && waypoints.Length > 0)
            _lastPlayerWaypointIndex = GetClosestWaypointIndex(player.position);
    }

    void Update()
    {
        if (PlayerStateMachine.Instance.Current is not StandardPlayerState) return;
        if (player == null) return;
        if (waypoints == null || waypoints.Length == 0) return;

        // 1. Player relative to the path
        bool playerGoingRightWay = UpdatePlayerProgress(out int playerWaypointIndex);
        float distDogPlayer = Vector3.Distance(transform.position, player.position);

        // 2. State machine
        switch (_state)
        {
            case DogState.Leading:
                if (distDogPlayer > leadMaxDistance || !playerGoingRightWay)
                {
                    _state = DogState.FollowingPlayer;
                }
                else
                {
                    LeadBehaviour();
                }
                break;

            case DogState.FollowingPlayer:
                FollowBehaviour(distDogPlayer);

                if (distDogPlayer < leadMaxDistance * 0.7f && playerGoingRightWay)
                {
                    int nearDog = GetClosestWaypointIndex(transform.position);
                    int next = Mathf.Max(nearDog, playerWaypointIndex);
                    _currentWaypointIndex = Mathf.Clamp(next, 0, waypoints.Length - 1);

                    _state = DogState.Leading;
                    LeadBehaviour();
                }
                break;
        }

        // 3. Move & orient dog (horizontal only)
        MoveDog();

        // 4. Snap to ground
        StickToGround();

        // 5. If standing still, look at the player
        FacePlayerWhenStopped();
    }

    // ----------------- behaviours -----------------

    private void LeadBehaviour()
    {
        Vector3 wp = waypoints[_currentWaypointIndex].position;
        float sqrDist = (transform.position - wp).sqrMagnitude;
        if (sqrDist <= waypointReachRadius * waypointReachRadius &&
            _currentWaypointIndex < waypoints.Length - 1)
        {
            _currentWaypointIndex++;
            wp = waypoints[_currentWaypointIndex].position;
        }

        _moveTarget = wp;
    }

    private void FollowBehaviour(float distDogPlayer)
    {
        Vector3 behind = player.position - player.forward * followBehindDistance;

        if (distDogPlayer > followBehindDistance + 0.1f)
            _moveTarget = behind;
        else
            _moveTarget = transform.position;   // close enough, stand and look
    }

    // ----------------- movement & rotation -----------------

    private void MoveDog()
    {
        Vector3 toTarget = _moveTarget - transform.position;
        toTarget.y = 0f;

        if (toTarget.sqrMagnitude > 0.0001f)
        {
            Vector3 dir = toTarget.normalized;
            Vector3 step = dir * moveSpeed * Time.deltaTime;

            transform.position += step;

            // face movement direction while moving
            Quaternion targetRot = Quaternion.LookRotation(dir, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);

            _currentSpeed = step.magnitude / Time.deltaTime;
        }
        else
        {
            _currentSpeed = 0f;
        }
    }

    private void StickToGround()
    {
        if (!stickToGround) return;

        Vector3 origin = transform.position + Vector3.up * groundProbeHeight;

        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit,
                            groundProbeHeight + groundProbeDistance,
                            groundMask, QueryTriggerInteraction.Ignore))
        {
            Vector3 pos = transform.position;
            pos.y = hit.point.y + groundOffset;
            transform.position = pos;
        }
    }

    private void FacePlayerWhenStopped()
    {
        if (_currentSpeed > 0.05f) return;

        Vector3 toPlayer = player.position - transform.position;
        toPlayer.y = 0f;
        if (toPlayer.sqrMagnitude < 0.0001f) return;

        Quaternion lookRot = Quaternion.LookRotation(toPlayer.normalized, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, rotationSpeed * Time.deltaTime);
    }

    // ----------------- path / direction helpers -----------------

    private bool UpdatePlayerProgress(out int closestIndex)
    {
        closestIndex = GetClosestWaypointIndex(player.position, out float sqrDist);
        bool closeToPath = sqrDist <= pathSnapRadius * pathSnapRadius;

        bool goingRightWay;

        if (!closeToPath)
        {
            // Player is far from the path: treat as being at first waypoint
            closestIndex = 0;
            goingRightWay = false;
        }
        else
        {
            if (closestIndex + backwardTolerance < _lastPlayerWaypointIndex)
                goingRightWay = false;
            else
                goingRightWay = true;
        }

        _lastPlayerWaypointIndex = closestIndex;
        return goingRightWay;
    }

    private int GetClosestWaypointIndex(Vector3 pos)
    {
        return GetClosestWaypointIndex(pos, out _);
    }

    private int GetClosestWaypointIndex(Vector3 pos, out float sqrDist)
    {
        int closest = 0;
        sqrDist = float.MaxValue;

        for (int i = 0; i < waypoints.Length; ++i)
        {
            float d = (waypoints[i].position - pos).sqrMagnitude;
            if (d < sqrDist)
            {
                sqrDist = d;
                closest = i;
            }
        }

        return closest;
    }
}
