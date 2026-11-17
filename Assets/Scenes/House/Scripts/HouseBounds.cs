using UnityEngine;

/// Keeps the player (and the dog) inside the room of the house.
/// The player moves with Rigidbody.MovePosition (shared open-world script), which is a small teleport
/// every physics step; in a corner the two walls push back at the same time and the player could
/// pop out on the wrong side. This clamps the position back inside the room, so that can't happen.
/// The room size is taken from the GridManager (the same 10x10 grid the furniture uses).
[DefaultExecutionOrder(-50)]
public class HouseBounds : MonoBehaviour
{
    public GridManager grid;
    [Tooltip("How far from the walls the player's centre has to stay (about the player's radius).")]
    public float playerMargin = 0.5f;
    [Tooltip("Same for the dog.")]
    public float dogMargin = 0.3f;
    [Tooltip("Below this height (world Y) something fell through the floor: put it back on the floor.")]
    public float fallLimit = -0.5f;

    Rigidbody _player;
    Transform _dog;
    Vector3 _min, _max;
    float _floorY;

    void Awake()
    {
        if (grid == null) grid = FindFirstObjectByType<GridManager>();
    }

    void FixedUpdate() { Find(); KeepInside(); }   // before the movement script
    void LateUpdate() { KeepInside(); }            // and once more after everything moved

    void Find()
    {
        if (_player == null && PlayerService.Instance != null && PlayerService.Instance.LocalPlayer != null)
            _player = ((Component)PlayerService.Instance.LocalPlayer).GetComponentInChildren<Rigidbody>();
        if (_dog == null)
        {
            var dog = FindFirstObjectByType<DogBuddyFollow>();
            if (dog != null) _dog = dog.transform;
        }
    }

    bool UpdateRoom()
    {
        if (grid == null) return false;
        Vector3 o = grid.transform.position;
        _min = o;
        _max = o + new Vector3(grid.width * grid.cellSize, 0f, grid.height * grid.cellSize);
        _floorY = o.y;
        return true;
    }

    void KeepInside()
    {
        if (!UpdateRoom()) return;

        if (_player != null)
        {
            Vector3 p = _player.position;
            if (Clamp(ref p, playerMargin))
            {
                _player.position = p;
                _player.transform.position = p;
                Vector3 v = _player.linearVelocity;
                _player.linearVelocity = new Vector3(0f, Mathf.Max(v.y, 0f), 0f);
            }
        }

        if (_dog != null)
        {
            Vector3 d = _dog.position;
            if (Clamp(ref d, dogMargin)) _dog.position = d;
        }
    }

    bool Clamp(ref Vector3 pos, float margin)
    {
        Vector3 before = pos;
        pos.x = Mathf.Clamp(pos.x, _min.x + margin, _max.x - margin);
        pos.z = Mathf.Clamp(pos.z, _min.z + margin, _max.z - margin);
        if (pos.y < _floorY + fallLimit) pos.y = _floorY;
        return pos != before;
    }

    void OnDrawGizmosSelected()
    {
        if (!UpdateRoom()) return;
        Gizmos.color = Color.cyan;
        Vector3 size = _max - _min;
        Gizmos.DrawWireCube(_min + size * 0.5f + Vector3.up, new Vector3(size.x - 2 * playerMargin, 2f, size.z - 2 * playerMargin));
    }
}
