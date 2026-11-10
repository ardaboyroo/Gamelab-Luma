using System;
using UnityEngine;
using UnityEngine.EventSystems;

/// Switches the local player into the avatar customization state when they tap a Mirror in walk mode.
/// Put ONE of these on any GameObject in the scene (not on the mirror prefab), because the mirror
/// is spawned by the furniture system and prefabs can't hold references to scene objects.
public class MirrorInteractor : MonoBehaviour
{
    [Tooltip("The state the player goes into when they tap the mirror. Pick it in the Inspector.")]
    public PlayerStates customizationState;
    [Tooltip("The state ReturnFromMirror() goes back to. Only needed if something calls ReturnFromMirror().")]
    public PlayerStates returnState;

    public EditModeController editMode;             // taps are ignored while editing the room
    public Camera cam;                              // leave empty to use Camera.main (only used to aim the tap)

    [Header("Player")]
    public Transform player;                        // used for the distance check
    [Tooltip("How close the player must be to use the mirror. 0 = any distance.")]
    public float maxDistance = 3f;

    [Header("Tap detection")]
    [Tooltip("A touch that moves further than this (pixels) is a drag, not a tap.")]
    public float maxTapMove = 25f;
    public float maxTapTime = 0.4f;

    public event Action MirrorUsed;                 // the player was switched to the customization state
    public event Action TooFar;                     // mirror tapped but the player is too far away

    int trackedFinger = -1;
    Vector2 downPos;
    float downTime;

    void Awake() { if (!cam) cam = Camera.main; }

    // ---------- input ----------
    void Update()
    {
        if (editMode && editMode.IsEditing) return;

        if (Application.isMobilePlatform) HandleTouches();
        else HandleMouse();
    }

    // Tracks every finger, so a tap still works while another finger holds the joystick.
    void HandleTouches()
    {
        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch t = Input.GetTouch(i);
            switch (t.phase)
            {
                case TouchPhase.Began:
                    if (trackedFinger == -1 && !IsOverUI(t.fingerId))
                    {
                        trackedFinger = t.fingerId;
                        downPos = t.position;
                        downTime = Time.unscaledTime;
                    }
                    break;

                case TouchPhase.Ended:
                    if (t.fingerId == trackedFinger)
                    {
                        trackedFinger = -1;
                        if (IsTap(t.position)) TryUseAt(t.position);
                    }
                    break;

                case TouchPhase.Canceled:
                    if (t.fingerId == trackedFinger) trackedFinger = -1;
                    break;
            }
        }
    }

    // Editor / PC testing.
    void HandleMouse()
    {
        if (Input.GetMouseButtonDown(0) && !IsOverUI(-1))
        {
            trackedFinger = 0;
            downPos = Input.mousePosition;
            downTime = Time.unscaledTime;
        }
        else if (Input.GetMouseButtonUp(0) && trackedFinger == 0)
        {
            trackedFinger = -1;
            if (IsTap(Input.mousePosition)) TryUseAt(Input.mousePosition);
        }
    }

    bool IsTap(Vector2 upPos) =>
        Vector2.Distance(upPos, downPos) <= maxTapMove && Time.unscaledTime - downTime <= maxTapTime;

    static bool IsOverUI(int pointerId) =>
        EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(pointerId);

    // ---------- using the mirror ----------
    void TryUseAt(Vector2 screen)
    {
        Mirror mirror = Pick(screen);
        if (mirror == null) return;

        if (player && maxDistance > 0f)
        {
            Vector3 d = mirror.transform.position - player.position; d.y = 0;
            if (d.magnitude > maxDistance) { TooFar?.Invoke(); return; }
        }

        trackedFinger = -1;
        ChangePlayerState(customizationState);
        MirrorUsed?.Invoke();
    }

    /// Optional: wire a button to this if nothing else takes the player out of the customization state.
    public void ReturnFromMirror() => ChangePlayerState(returnState);

    private void ChangePlayerState(PlayerStates newState)
    {
        var local = PlayerService.Instance != null ? PlayerService.Instance.LocalPlayer : null;
        if (local == null) return;   // player not spawned yet
        local.StateManager.ChangePlayerState(newState);
    }

    // First thing along the ray that isn't the player. If that's a mirror, return it;
    // if it's a wall or other object, the mirror is blocked and nothing happens.
    Mirror Pick(Vector2 screen)
    {
        Ray ray = cam.ScreenPointToRay(screen);
        var hits = Physics.RaycastAll(ray, 200f, ~0, QueryTriggerInteraction.Ignore);
        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        foreach (var h in hits)
        {
            if (player && h.transform.IsChildOf(player)) continue;
            return h.collider.GetComponentInParent<Mirror>();
        }
        return null;
    }
}