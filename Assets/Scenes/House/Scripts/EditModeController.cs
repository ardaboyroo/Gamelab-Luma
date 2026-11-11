using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

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

    [Header("Player while editing")]
    [Tooltip("Leave empty to use the local player from PlayerService.")]
    public Transform player;
    [Tooltip("Hide the character in edit mode. It is always frozen in place and its collider switched off.")]
    public bool hidePlayerInEditMode = true;

    [Header("HUD while editing")]
    [Tooltip("Names of UI Toolkit elements (in any UIDocument) hidden in edit mode. For a button, its surrounding panel is hidden too.")]
    public string[] hideHudElementsInEditMode = { "SkateboardButton" };

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

    // player / HUD state saved while editing
    Transform frozenPlayer;
    Vector3 playerPos;
    Quaternion playerRot;
    Rigidbody playerBody;
    bool playerWasKinematic;
    readonly List<Collider> disabledColliders = new();
    readonly List<Renderer> hiddenRenderers = new();
    readonly List<VisualElement> hiddenHud = new();
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
        SetPlayerEditing(on);
        SetHudHidden(on);
        grid.SetCellsVisible(on);
        ModeChanged?.Invoke(on);
    }

    // ---------- player ----------
    void SetPlayerEditing(bool on)
    {
        if (on)
        {
            Transform p = player;
            if (!p && PlayerService.Instance != null && PlayerService.Instance.LocalPlayer != null)
                p = PlayerService.Instance.LocalPlayer.transform;
            if (!p) return;

            frozenPlayer = p;
            playerPos = p.position;
            playerRot = p.rotation;

            playerBody = p.GetComponent<Rigidbody>();
            if (playerBody)
            {
                playerWasKinematic = playerBody.isKinematic;
                if (!playerBody.isKinematic) { playerBody.linearVelocity = Vector3.zero; playerBody.angularVelocity = Vector3.zero; }
                playerBody.isKinematic = true;          // no falling while the collider is off
            }

            disabledColliders.Clear();
            foreach (var c in p.GetComponentsInChildren<Collider>())
                if (c.enabled) { c.enabled = false; disabledColliders.Add(c); }   // taps go to the furniture, not the player

            hiddenRenderers.Clear();
            if (hidePlayerInEditMode)
                foreach (var r in p.GetComponentsInChildren<Renderer>(true))
                    if (!r.forceRenderingOff) { r.forceRenderingOff = true; hiddenRenderers.Add(r); }
        }
        else
        {
            if (!frozenPlayer) return;

            frozenPlayer.SetPositionAndRotation(playerPos, playerRot);
            if (playerBody)
            {
                playerBody.position = playerPos;
                playerBody.rotation = playerRot;
                playerBody.isKinematic = playerWasKinematic;
            }
            foreach (var c in disabledColliders) if (c) c.enabled = true;
            foreach (var r in hiddenRenderers) if (r) r.forceRenderingOff = false;
            disabledColliders.Clear();
            hiddenRenderers.Clear();
            frozenPlayer = null;
        }
    }

    // Touches used for placing furniture also reach the walking joystick, so keep the player pinned.
    void FixedUpdate()
    {
        if (!IsEditing || !frozenPlayer) return;
        frozenPlayer.SetPositionAndRotation(playerPos, playerRot);
        if (playerBody) { playerBody.position = playerPos; playerBody.rotation = playerRot; }
    }

    // ---------- HUD ----------
    void SetHudHidden(bool hide)
    {
        if (!hide)
        {
            foreach (var e in hiddenHud) e.style.display = StyleKeyword.Null;   // back to whatever the UXML says
            hiddenHud.Clear();
            return;
        }

        hiddenHud.Clear();
        if (hideHudElementsInEditMode == null) return;
        foreach (var doc in FindObjectsByType<UIDocument>(FindObjectsSortMode.None))
        {
            var root = doc.rootVisualElement;
            if (root == null) continue;
            foreach (var name in hideHudElementsInEditMode)
            {
                if (string.IsNullOrEmpty(name)) continue;
                var el = root.Q(name);
                if (el == null) continue;
                // a button sitting in its own decorative panel: hide the whole panel
                var target = (el is Button && el.parent != null && el.parent != root && el.parent.childCount == 1) ? el.parent : el;
                if (target.style.display == DisplayStyle.None) continue;
                target.style.display = DisplayStyle.None;
                hiddenHud.Add(target);
            }
        }
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