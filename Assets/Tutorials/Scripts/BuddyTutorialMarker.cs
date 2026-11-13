using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;

/// Put on the buddy. When a tutorial is queued, a bouncing '!' appears above the buddy's head.
/// Tapping the '!' (or the buddy itself) opens the tutorial in the TutorialDialogue.
/// Queue tutorials from code with Queue(entry), e.g. from HouseTutorialTriggers.
[DefaultExecutionOrder(100)]   // after the camera has moved, so the '!' doesn't trail a frame behind the buddy
public class BuddyTutorialMarker : MonoBehaviour
{
    [Tooltip("Leave empty to use the TutorialDialogue in the scene.")]
    public TutorialDialogue dialogue;
    [Tooltip("Leave empty to use Camera.main.")]
    public Camera cam;

    [Header("Marker")]
    [Tooltip("Extra height above the top of the buddy's model, in world units.")]
    public float heightAboveHead = 0.25f;
    public float bounceHeight = 14f;
    public float bounceSpeed = 4f;
    [Tooltip("Only show the '!' (and allow opening) while the player is in this state.")]
    public PlayerStates showInState = PlayerStates.StandardState;

    [Header("Tapping the buddy itself")]
    [Tooltip("Tapping the buddy opens a waiting tutorial; with no tutorial waiting it pets the buddy.")]
    public bool tapBuddyToOpen = true;
    [Tooltip("A touch that moves further than this (pixels) is a drag, not a tap.")]
    public float maxTapMove = 25f;
    public float maxTapTime = 0.4f;

    [Header("Queued at start (optional)")]
    public TutorialEntry[] startQueued;

    public bool HasPending => _queue.Count > 0;
    public event Action<TutorialEntry> TutorialFinished;

    readonly List<TutorialEntry> _queue = new();
    DogBuddyFollow _follow;
    Renderer[] _renderers;
    Button _marker;
    bool _tracking;
    Vector2 _downPos;
    float _downTime;

    void Awake()
    {
        _renderers = GetComponentsInChildren<Renderer>(true);
        _follow = GetComponent<DogBuddyFollow>();
        if (startQueued != null)
            foreach (var e in startQueued) Queue(e);
    }

    void OnDisable()
    {
        if (_marker != null) _marker.style.display = DisplayStyle.None;
    }

    void OnDestroy()
    {
        if (_marker != null) _marker.RemoveFromHierarchy();
    }

    /// Adds a tutorial; the '!' shows until the player has read it. Already-read ones are skipped.
    public void Queue(TutorialEntry entry)
    {
        if (entry == null || _queue.Contains(entry) || TutorialProgress.HasSeen(entry)) return;
        _queue.Add(entry);
    }

    /// Opens the oldest queued tutorial.
    public void OpenNext()
    {
        if (!HasPending || !CanOpen()) return;
        var entry = _queue[0];
        _tracking = false;
        Dialogue.Show(entry, () =>
        {
            _queue.Remove(entry);
            TutorialFinished?.Invoke(entry);
        });
    }

    TutorialDialogue Dialogue => dialogue != null ? dialogue : TutorialDialogue.Instance;
    Camera Cam => cam != null && cam.isActiveAndEnabled ? cam : Camera.main;

    bool PlayerInShowState()
    {
        if (PlayerService.Instance == null || PlayerService.Instance.LocalPlayer == null) return true;
        var states = PlayerService.Instance.LocalPlayer.StateManager;
        return states == null || states.CurrentState == showInState;
    }

    bool CanOpen() => Dialogue != null && !Dialogue.IsOpen && PlayerInShowState();

    // ---------- marker ----------
    void LateUpdate()
    {
        EnsureMarker();
        if (_marker == null) return;

        Camera c = Cam;
        bool show = HasPending && CanOpen() && c != null;

        Vector3 world = HeadPosition();
        if (show && c.WorldToViewportPoint(world).z <= 0f) show = false;   // behind the camera

        _marker.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
        if (!show || _marker.panel == null) return;

        Vector2 p = RuntimePanelUtils.CameraTransformWorldToPanel(_marker.panel, world, c);
        float bounce = Mathf.Abs(Mathf.Sin(Time.unscaledTime * bounceSpeed)) * bounceHeight;
        float w = _marker.resolvedStyle.width > 0 ? _marker.resolvedStyle.width : 120f;
        float h = _marker.resolvedStyle.height > 0 ? _marker.resolvedStyle.height : 120f;
        _marker.style.left = p.x - w * 0.5f;
        _marker.style.top = p.y - h - bounce;
    }

    void EnsureMarker()
    {
        if (_marker != null && _marker.panel != null) return;
        var d = Dialogue;
        if (d == null || d.MarkerLayer == null) return;

        _marker?.RemoveFromHierarchy();
        _marker = new Button(OpenNext) { name = "buddy-marker", text = "!" };
        _marker.AddToClassList("tutorial-marker");
        _marker.style.display = DisplayStyle.None;
        d.MarkerLayer.Add(_marker);
    }

    Vector3 HeadPosition()
    {
        bool any = false;
        Bounds b = new Bounds(transform.position, Vector3.zero);
        foreach (var r in _renderers)
        {
            if (r == null || !r.enabled || !r.gameObject.activeInHierarchy) continue;
            if (!any) { b = r.bounds; any = true; } else b.Encapsulate(r.bounds);
        }
        return any ? new Vector3(b.center.x, b.max.y + heightAboveHead, b.center.z)
                   : transform.position + Vector3.up * heightAboveHead;
    }

    // ---------- tapping the buddy ----------
    void Update()
    {
        bool dialogueOpen = Dialogue != null && Dialogue.IsOpen;
        if (!tapBuddyToOpen || dialogueOpen || !PlayerInShowState()) { _tracking = false; return; }

        if (Application.isMobilePlatform)
        {
            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch t = Input.GetTouch(i);
                if (t.phase == TouchPhase.Began && !_tracking && !IsOverUI(t.position)) BeginTap(t.position);
                else if (t.phase == TouchPhase.Ended && _tracking) EndTap(t.position);
            }
        }
        else
        {
            if (Input.GetMouseButtonDown(0) && !IsOverUI(Input.mousePosition)) BeginTap(Input.mousePosition);
            else if (Input.GetMouseButtonUp(0) && _tracking) EndTap(Input.mousePosition);
        }
    }

    void BeginTap(Vector2 pos)
    {
        _tracking = true;
        _downPos = pos;
        _downTime = Time.unscaledTime;
    }

    void EndTap(Vector2 pos)
    {
        _tracking = false;
        if (Vector2.Distance(pos, _downPos) > maxTapMove || Time.unscaledTime - _downTime > maxTapTime) return;
        if (!HitsBuddy(pos)) return;

        if (HasPending && CanOpen()) OpenNext();
        else if (_follow != null) _follow.Pet();   // no tutorial waiting: tapping = petting
    }

    bool HitsBuddy(Vector2 screen)
    {
        Camera c = Cam;
        if (c == null) return false;
        Ray ray = c.ScreenPointToRay(screen);
        foreach (var r in _renderers)
        {
            if (r == null || !r.enabled || !r.gameObject.activeInHierarchy) continue;
            Bounds b = r.bounds;
            b.Expand(0.15f * b.size.magnitude);   // a bit forgiving for small fingers
            if (b.IntersectRay(ray)) return true;
        }
        return false;
    }

    static readonly List<RaycastResult> uiHits = new();
    static readonly HashSet<VisualElement> docRoots = new();
    static readonly HashSet<IPanel> checkedPanels = new();

    // Same rule as MirrorInteractor: real UI (buttons, panels) blocks the tap, empty full-screen roots don't.
    static bool IsOverUI(Vector2 screen)
    {
        var es = EventSystem.current;
        if (es != null)
        {
            uiHits.Clear();
            es.RaycastAll(new PointerEventData(es) { position = screen }, uiHits);
            foreach (var h in uiHits)
            {
                if (!(h.module is UnityEngine.UI.GraphicRaycaster)) continue;
                if (h.gameObject.GetComponentInParent<FloatingJoystick>(true) != null) continue;
                return true;
            }
        }

        docRoots.Clear();
        checkedPanels.Clear();
        foreach (var doc in FindObjectsByType<UIDocument>(FindObjectsSortMode.None))
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
}
