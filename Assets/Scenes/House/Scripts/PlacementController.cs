using System;
using UnityEngine;

/// Holds the "ghost" while the player is positioning furniture.
/// Driven by the UI: Begin(item), MoveToScreenPoint(), Rotate(), Confirm(), Cancel().
public class PlacementController : MonoBehaviour
{
    public GridManager grid;
    public Camera cam;                         // leave empty to use Camera.main

    public event Action<bool> PlacementChanged;   // true = a ghost is active
    public event Action<bool> ValidityChanged;    // true = ghost is on free cells

    public bool IsPlacing => current != null;

    FurnitureData current;
    GameObject ghost;
    Vector2Int origin;
    int rotation;
    bool valid;

    Vector2Int Size => GridManager.RotatedSize(current.size, rotation);

    void Awake() { if (!cam) cam = Camera.main; }

    public void Begin(FurnitureData data)
    {
        if (data == null || data.prefab == null) return;
        End();

        current = data;
        rotation = 0;
        ghost = Instantiate(data.prefab);
        foreach (var c in ghost.GetComponentsInChildren<Collider>()) c.enabled = false;

        var s = Size;
        origin = new Vector2Int((grid.width - s.x) / 2, (grid.height - s.y) / 2);   // start in the middle
        Refresh();
        PlacementChanged?.Invoke(true);
    }

    /// Call with a screen position in pixels (origin bottom-left, like Input.mousePosition).
    public void MoveToScreenPoint(Vector2 screen)
    {
        if (!IsPlacing) return;
        var plane = new Plane(Vector3.up, grid.transform.position);
        Ray ray = cam.ScreenPointToRay(screen);
        if (!plane.Raycast(ray, out float dist)) return;

        Vector2Int cell = grid.WorldToCell(ray.GetPoint(dist));
        var s = Size;
        origin = ClampOrigin(cell - new Vector2Int(s.x / 2, s.y / 2));   // footprint centered on the finger
        Refresh();
    }

    public void Rotate()
    {
        if (!IsPlacing) return;
        rotation = (rotation + 1) % 4;
        origin = ClampOrigin(origin);
        Refresh();
    }

    public bool Confirm()
    {
        if (!IsPlacing || !valid) return false;
        grid.Place(current, origin, rotation);
        End();
        return true;
    }

    public void Cancel() => End();

    Vector2Int ClampOrigin(Vector2Int o)
    {
        var s = Size;
        return new Vector2Int(Mathf.Clamp(o.x, 0, grid.width - s.x), Mathf.Clamp(o.y, 0, grid.height - s.y));
    }

    void Refresh()
    {
        var s = Size;
        valid = grid.CanPlace(origin, s);
        ghost.transform.SetPositionAndRotation(grid.FootprintCenter(origin, s), Quaternion.Euler(0, rotation * 90f, 0));
        grid.ShowPreview(origin, s, valid);
        ValidityChanged?.Invoke(valid);
    }

    void End()
    {
        if (current == null) return;
        if (ghost) Destroy(ghost);
        grid.ClearPreview();
        current = null;
        ghost = null;
        PlacementChanged?.Invoke(false);
    }
}