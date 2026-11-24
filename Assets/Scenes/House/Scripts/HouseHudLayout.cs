using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/// House-only tweaks to the shared walking HUD (the Standard State UI is also used in the open world,
/// so its UXML stays untouched):
/// - moves the Menu button to the top-left corner (the house button sits bottom-right in HouseUI.uss)
/// - hides the skateboard button, because you can't skate inside
/// - hides the house button and the turn arrows while another screen is open (menu, avatar editor, tutorial)
/// - while the full-screen avatar editor is open, the house camera draws nothing (saves a whole frame of rendering).
/// Put ONE of these in the house scene. Everything is undone when it is disabled.
///
/// Performance: the UI is searched only when something changes (state change, a UI document rebuilt,
/// or at most twice per second to catch new documents), not every frame.
public class HouseHudLayout : MonoBehaviour
{
    [Header("Menu button")]
    public string menuButtonName = "menu-btn";
    public bool moveMenuToTopLeft = true;
    [Tooltip("Position in the Menu button's own panel units.")]
    public float menuLeft = 40f;
    public float menuTop = 82f;

    [Header("Skateboard")]
    public string skateboardButtonName = "SkateboardButton";
    public bool hideSkateboard = true;

    [Header("House buttons")]
    [Tooltip("Only shown while walking or decorating; hidden behind the menu, avatar editor, tutorial...")]
    public string[] houseButtonNames = { "btn-toggle-ui", "turn-left", "turn-right" };
    public PlayerStates[] showHouseButtonsIn = { PlayerStates.StandardState, PlayerStates.HouseEditState };
    [Tooltip("Only shown while walking (not while decorating), e.g. the door to leave the house.")]
    public string[] walkOnlyButtonNames = { "btn-exit-house" };

    [Header("Avatar editor")]
    [Tooltip("While the avatar editor (full screen) is open, the house camera renders nothing. The avatar preview has its own camera.")]
    public bool pauseHouseCameraInAvatarEditor = true;

    [Header("Performance")]
    [Tooltip("How often (seconds) to look for UI documents that appeared later. A state change always triggers a search.")]
    public float rescanInterval = 0.5f;

    readonly List<VisualElement> _menus = new();
    readonly List<VisualElement> _houseButtons = new();
    readonly List<VisualElement> _walkOnlyButtons = new();
    readonly List<VisualElement> _skatePanels = new();

    struct CameraState { public int mask; public CameraClearFlags clear; }
    readonly Dictionary<Camera, CameraState> _pausedCameras = new();

    bool _hasState;
    PlayerStates _lastState;
    float _nextRescan;
    int _reapplyFrames;

    void OnEnable()
    {
        _hasState = false;
        _nextRescan = 0f;
    }

    void LateUpdate()
    {
        bool hasPlayer = PlayerService.Instance != null && PlayerService.Instance.LocalPlayer != null
                         && PlayerService.Instance.LocalPlayer.StateManager != null;
        PlayerStates state = hasPlayer ? PlayerService.Instance.LocalPlayer.StateManager.CurrentState : PlayerStates.StandardState;

        bool stateChanged = !_hasState || state != _lastState;
        _hasState = true;
        _lastState = state;

        // Other scripts show/hide parts of this HUD in the frames right after a state change,
        // so re-apply for a few frames after each change instead of every frame forever.
        if (stateChanged) _reapplyFrames = 3;

        bool rescan = stateChanged || Time.unscaledTime >= _nextRescan || AnyDetached();
        bool found = false;
        if (rescan)
        {
            _nextRescan = Time.unscaledTime + rescanInterval;
            found = Collect();
        }

        if (_reapplyFrames > 0 || found)
        {
            Apply(state, hasPlayer);
            if (_reapplyFrames > 0) _reapplyFrames--;
        }

        UpdateCameras(hasPlayer && state == PlayerStates.CharacterEditState);
    }

    void Apply(PlayerStates state, bool hasPlayer)
    {
        if (moveMenuToTopLeft)
            foreach (var m in _menus)
            {
                m.style.left = menuLeft;
                m.style.top = menuTop;
                m.style.right = StyleKeyword.Auto;
                m.style.bottom = StyleKeyword.Auto;
            }

        if (hideSkateboard)
            foreach (var p in _skatePanels) SetShown(p, false);

        bool walking = !hasPlayer || state == PlayerStates.StandardState;
        foreach (var b in _walkOnlyButtons) SetShown(b, walking);

        bool showHouse = !hasPlayer || showHouseButtonsIn == null || System.Array.IndexOf(showHouseButtonsIn, state) >= 0;
        foreach (var b in _houseButtons) SetShown(b, showHouse);
    }

    static void SetShown(VisualElement e, bool shown)
    {
        if (shown && e.style.display == DisplayStyle.None) e.style.display = StyleKeyword.Null;
        else if (!shown && e.style.display != DisplayStyle.None) e.style.display = DisplayStyle.None;
    }

    // ---------- avatar editor: pause the house camera ----------
    void UpdateCameras(bool pause)
    {
        if (pause && pauseHouseCameraInAvatarEditor)
        {
            foreach (var cam in Camera.allCameras)
            {
                // cameras that draw into a texture (the avatar preview) keep working
                if (cam == null || cam.targetTexture != null || _pausedCameras.ContainsKey(cam)) continue;
                _pausedCameras[cam] = new CameraState { mask = cam.cullingMask, clear = cam.clearFlags };
                cam.cullingMask = 0;                         // draw nothing, only clear the screen
                cam.clearFlags = CameraClearFlags.SolidColor;
            }
        }
        else if (_pausedCameras.Count > 0)
        {
            RestoreCameras();
        }
    }

    void RestoreCameras()
    {
        foreach (var kv in _pausedCameras)
        {
            if (kv.Key == null) continue;
            kv.Key.cullingMask = kv.Value.mask;
            kv.Key.clearFlags = kv.Value.clear;
        }
        _pausedCameras.Clear();
    }

    void OnDisable()
    {
        RestoreCameras();
        foreach (var m in _menus)
        {
            m.style.left = StyleKeyword.Null;
            m.style.top = StyleKeyword.Null;
            m.style.right = StyleKeyword.Null;
            m.style.bottom = StyleKeyword.Null;
        }
        foreach (var p in _skatePanels) p.style.display = StyleKeyword.Null;
        foreach (var b in _houseButtons) b.style.display = StyleKeyword.Null;
        foreach (var b in _walkOnlyButtons) b.style.display = StyleKeyword.Null;
        _walkOnlyButtons.Clear();
        _houseButtons.Clear();
        _menus.Clear();
        _skatePanels.Clear();
    }

    // ---------- finding the elements ----------
    // A UI document that is switched off and on rebuilds its elements; the old ones lose their panel.
    bool AnyDetached()
    {
        foreach (var e in _menus) if (e.panel == null) return true;
        foreach (var e in _houseButtons) if (e.panel == null) return true;
        foreach (var e in _walkOnlyButtons) if (e.panel == null) return true;
        foreach (var e in _skatePanels) if (e.panel == null) return true;
        return false;
    }

    /// Returns true when new elements were found (they need the layout applied).
    bool Collect()
    {
        _menus.RemoveAll(e => e.panel == null);
        _skatePanels.RemoveAll(e => e.panel == null);
        _houseButtons.RemoveAll(e => e.panel == null);
        _walkOnlyButtons.RemoveAll(e => e.panel == null);
        bool found = false;

        foreach (var doc in FindObjectsByType<UIDocument>(FindObjectsSortMode.None))
        {
            var root = doc.rootVisualElement;
            if (root == null) continue;

            if (houseButtonNames != null)
                foreach (var n in houseButtonNames)
                    found |= AddOnce(_houseButtons, string.IsNullOrEmpty(n) ? null : root.Q(n));

            if (walkOnlyButtonNames != null)
                foreach (var n in walkOnlyButtonNames)
                    found |= AddOnce(_walkOnlyButtons, string.IsNullOrEmpty(n) ? null : root.Q(n));

            found |= AddOnce(_menus, root.Q(menuButtonName));

            var skate = root.Q(skateboardButtonName);
            if (skate != null)
            {
                // the button sits in its own decorative corner panel: hide the whole panel
                var target = (skate.parent != null && skate.parent != root && skate.parent.childCount == 1) ? skate.parent : skate;
                found |= AddOnce(_skatePanels, target);
            }
        }
        return found;
    }

    static bool AddOnce(List<VisualElement> list, VisualElement e)
    {
        if (e == null || list.Contains(e)) return false;
        list.Add(e);
        return true;
    }
}
