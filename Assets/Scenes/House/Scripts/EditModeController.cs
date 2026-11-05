using System;
using UnityEngine;

/// Switches between Walk mode and Edit mode.
/// Edit mode: walking is switched off, the camera orbits the room in 90 degree steps,
/// the cells are shown. Leaving edit mode restores the walking camera exactly as it was.
public class EditModeController : MonoBehaviour
{
    public GridManager grid;
    public PlacementController placement;
    public Camera cam;                              // leave empty to use Camera.main

    [Header("Switched off while editing")]
    public Behaviour[] disableInEditMode;           // player controller, camera follow script, ...
    public GameObject[] hideInEditMode;             // e.g. the walking joystick

    [Header("Edit camera")]
    public float pitch = 55f;
    public bool autoDistance = false;
    public float distance = 20f;
    public float turnSmoothTime = 0.25f;

    [Header("Optional: walls that fade out when they block the view")]
    public Renderer[] walls;

    public bool IsEditing { get; private set; }
    public event Action<bool> ModeChanged;

    Vector3 savedPos;
    Quaternion savedRot;
    float yaw, yawTarget, yawVel;

    void Awake() { if (!cam) cam = Camera.main; }

    public void Toggle() => SetEditing(!IsEditing);

    public void SetEditing(bool on)
    {
        if (on == IsEditing) return;
        IsEditing = on;

        if (on)
        {
            savedPos = cam.transform.position;
            savedRot = cam.transform.rotation;
            yaw = yawTarget = Mathf.Round(cam.transform.eulerAngles.y / 90f) * 90f;   // start from the closest side
            yawVel = 0;
            if (autoDistance) distance = Mathf.Max(grid.width, grid.height) * grid.cellSize * 1.3f;
        }
        else
        {
            placement.Cancel();
            cam.transform.SetPositionAndRotation(savedPos, savedRot);
            foreach (var w in walls) if (w) w.enabled = true;
        }

        foreach (var b in disableInEditMode) if (b) b.enabled = !on;
        foreach (var g in hideInEditMode) if (g) g.SetActive(!on);
        grid.SetCellsVisible(on);
        ModeChanged?.Invoke(on);
    }

    // Wired to the < and > buttons. Swap the signs if they turn the wrong way.
    public void TurnLeft() => yawTarget -= 90f;
    public void TurnRight() => yawTarget += 90f;

    void LateUpdate()
    {
        //if (!IsEditing) return;

        yaw = Mathf.SmoothDampAngle(yaw, yawTarget, ref yawVel, turnSmoothTime);
        Vector3 pivot = grid.GridCenter;
        cam.transform.position = pivot + Quaternion.Euler(pitch, yaw, 0) * Vector3.back * distance;
        cam.transform.LookAt(pivot);

        // hide the walls that sit between the camera and the room
        if (walls != null && walls.Length > 0)
        {
            Vector3 toCam = cam.transform.position - pivot; toCam.y = 0; toCam.Normalize();
            foreach (var w in walls)
            {
                if (!w) continue;
                Vector3 d = w.bounds.center - pivot; d.y = 0; d.Normalize();
                w.enabled = Vector3.Dot(d, toCam) < 0.5f;
            }
        }
    }
}