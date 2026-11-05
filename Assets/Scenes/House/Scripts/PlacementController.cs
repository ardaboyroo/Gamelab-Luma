using System;
using UnityEngine;

/// Holds the "ghost" while the player is positioning furniture, and the current selection
/// when they tap a piece that is already placed.
/// Driven by the UI: Begin(item), MoveToScreenPoint(), Rotate(), Confirm(), Cancel(),
/// TapSelect(), MoveSelected(), RemoveSelected(), Deselect().
public class PlacementController : MonoBehaviour
{
    public GridManager grid;
    public Camera cam;                         // leave empty to use Camera.main

    public event Action<bool> PlacementChanged;   // true = a ghost is active
    public event Action<bool> ValidityChanged;    // true = ghost is on free cells
    public event Action<bool> SelectionChanged;   // true = a placed piece is selected

    public bool IsPlacing => current != null;
    public bool HasSelection => selected != null;

    FurnitureData current;
    GameObject ghost;
    Vector2Int origin;
    int rotation;
    bool valid;

    PlacedFurniture selected;

    // When an existing piece is being moved we remember where it came from,
    // so Cancel can put it back exactly as it was.
    bool moving;
    FurnitureData moveData;
    Vector2Int moveOrigin;
    int moveRotation;

    Vector2Int Size => GridManager.RotatedSize(current.size, rotation);

    void Awake() { if (!cam) cam = Camera.main; }

    // ---------- placing ----------
    public void Begin(FurnitureData data)
    {
        if (data == null || data.prefab == null) return;
        Cancel();       // drop any ghost (and put a half-moved piece back)
        Deselect();
        StartGhost(data, 0, null);
    }

    void StartGhost(FurnitureData data, int startRotation, Vector2Int? startOrigin)
    {
        current = data;
        rotation = startRotation;
        ghost = Instantiate(data.prefab);
        foreach (var c in ghost.GetComponentsInChildren<Collider>()) c.enabled = false;

        var s = Size;
        origin = startOrigin ?? new Vector2Int((grid.width - s.x) / 2, (grid.height - s.y) / 2);   // new items start in the middle
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
        moving = false;
        End();
        return true;
    }

    public void Cancel()
    {
        if (!IsPlacing) return;
        if (moving)
        {
            grid.Place(moveData, moveOrigin, moveRotation);   // back where it was
            moving = false;
        }
        End();
    }

    // ---------- selecting placed furniture ----------
    /// Call with a screen position in pixels. Tapping a piece selects it, tapping empty space deselects.
    public void TapSelect(Vector2 screen)
    {
        if (IsPlacing) return;
        var hit = Pick(screen);
        if (hit == null) { Deselect(); return; }
        if (hit == selected) return;

        selected = hit;
        grid.ShowHighlight(hit);
        SelectionChanged?.Invoke(true);
    }

    public void Deselect()
    {
        if (selected == null) return;
        selected = null;
        grid.ClearPreview();
        SelectionChanged?.Invoke(false);
    }

    /// Picks the selected piece up and turns it into the ghost, starting where it stood.
    public void MoveSelected()
    {
        if (selected == null) return;
        var p = selected;
        Deselect();

        moveData = p.data;
        moveOrigin = p.origin;
        moveRotation = p.rotation;
        moving = true;

        grid.Remove(p);
        StartGhost(p.data, p.rotation, p.origin);
    }

    public void RemoveSelected()
    {
        if (selected == null) return;
        var p = selected;
        Deselect();
        grid.Remove(p);
    }

    PlacedFurniture Pick(Vector2 screen)
    {
        Ray ray = cam.ScreenPointToRay(screen);

        // 1) colliders: first piece of furniture along the ray (walls etc. are skipped)
        var hits = Physics.RaycastAll(ray, 200f);
        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        foreach (var h in hits)
        {
            var handle = h.collider.GetComponentInParent<FurnitureHandle>();
            if (handle != null) return handle.placed;
        }

        // 2) fallback for prefabs without colliders: whatever stands on the tapped floor cell
        var plane = new Plane(Vector3.up, grid.transform.position);
        if (plane.Raycast(ray, out float d)) return grid.GetAt(grid.WorldToCell(ray.GetPoint(d)));
        return null;
    }

    // ---------- internals ----------
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
