using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;

/// Switches the local player into the avatar customization state when they tap a Mirror in walk mode.
/// Put ONE of these on any GameObject in the scene (not on the mirror prefab), because the mirror
/// is spawned by the furniture system and prefabs can't hold references to scene objects.
///
/// The player, PlayerService and MenuUI (which shows the Avatar Creation UI for CharacterEditState)
/// live in the Main scene, so test this with Main loaded together with the house.
public class MirrorInteractor : MonoBehaviour
{
    [Tooltip("The state the player goes into when they tap the mirror.")]
    public PlayerStates customizationState = PlayerStates.CharacterEditState;
    [Tooltip("The state ReturnFromMirror() goes back to.")]
    public PlayerStates returnState = PlayerStates.StandardState;

    public EditModeController editMode;             // taps are ignored while editing the room
    [Tooltip("Leave empty to use Camera.main (looked up on each tap, so it also works when the camera comes from another scene).")]
    public Camera cam;

    [Header("Player")]
    [Tooltip("Leave empty to use the local player from PlayerService automatically.")]
    public Transform player;
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

    static readonly List<RaycastResult> uiHits = new();
    static readonly HashSet<VisualElement> docRoots = new();
    static readonly HashSet<IPanel> checkedPanels = new();

    // ---------- input ----------
    void Update()
    {
        if (editMode && editMode.IsEditing) { trackedFinger = -1; return; }

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
                    if (trackedFinger == -1 && !IsOverUI(t.position))
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
        if (Input.GetMouseButtonDown(0) && !IsOverUI(Input.mousePosition))
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

    // True when the finger is on a real piece of UI (a button, a panel...), for both uGUI and UI Toolkit.
    // Empty full-screen UI Toolkit document roots are ignored, otherwise every tap would count as 'on UI'.
    static bool IsOverUI(Vector2 screen)
    {
        var es = EventSystem.current;
        if (es != null)
        {
            uiHits.Clear();
            es.RaycastAll(new PointerEventData(es) { position = screen }, uiHits);
            foreach (var h in uiHits)
                if (h.module is UnityEngine.UI.GraphicRaycaster) return true;
        }

        var docs = FindObjectsByType<UIDocument>(FindObjectsSortMode.None);
        docRoots.Clear();
        checkedPanels.Clear();
        foreach (var doc in docs)
            if (doc.isActiveAndEnabled && doc.rootVisualElement != null) docRoots.Add(doc.rootVisualElement);

        foreach (var root in docRoots)
        {
            var panel = root.panel;
            if (panel == null || !checkedPanels.Add(panel)) continue;

            Vector2 p = RuntimePanelUtils.ScreenToPanel(panel, new Vector2(screen.x, Screen.height - screen.y));
            VisualElement picked = panel.Pick(p);
            if (picked == null || picked == panel.visualTree || docRoots.Contains(picked)) continue;
            return true;
        }
        return false;
    }

    // ---------- using the mirror ----------
    static Player LocalPlayer() => PlayerService.Instance != null ? PlayerService.Instance.LocalPlayer : null;

    void TryUseAt(Vector2 screen)
    {
        Player local = LocalPlayer();
        Transform playerT = player ? player : (local ? local.transform : null);

        Mirror mirror = Pick(screen, playerT);
        if (mirror == null) return;

        if (local == null)
        {
            Debug.LogWarning("[MirrorInteractor] Mirror tapped, but there is no local player. " +
                             "Play with the Main scene loaded (it has the PLAYER, PlayerService and MenuUI).", this);
            return;
        }

        if (local.StateManager.CurrentState == customizationState) return;   // already customizing

        if (playerT && maxDistance > 0f)
        {
            Vector3 d = mirror.transform.position - playerT.position; d.y = 0;
            if (d.magnitude > maxDistance) { TooFar?.Invoke(); return; }
        }

        trackedFinger = -1;
        local.StateManager.ChangePlayerState(customizationState);
        MirrorUsed?.Invoke();
    }

    /// Optional: wire a button to this if nothing else takes the player out of the customization state.
    public void ReturnFromMirror()
    {
        Player local = LocalPlayer();
        if (local != null) local.StateManager.ChangePlayerState(returnState);
    }

    // First thing along the ray that isn't the player. If that's a mirror, return it;
    // if it's a wall or other object, the mirror is blocked and nothing happens.
    Mirror Pick(Vector2 screen, Transform playerT)
    {
        Camera c = cam && cam.isActiveAndEnabled ? cam : Camera.main;
        if (c == null) return null;

        Ray ray = c.ScreenPointToRay(screen);
        var hits = Physics.RaycastAll(ray, 200f, ~0, QueryTriggerInteraction.Ignore);
        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        foreach (var h in hits)
        {
            if (playerT && h.transform.IsChildOf(playerT)) continue;
            return h.collider.GetComponentInParent<Mirror>();
        }
        return null;
    }
}
