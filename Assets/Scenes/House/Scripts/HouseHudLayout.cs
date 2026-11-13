using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/// House-only tweaks to the shared walking HUD (the Standard State UI is also used in the open world,
/// so its UXML stays untouched):
/// - moves the Menu button to the top-left corner (the house button sits bottom-right in HouseUI.uss)
/// - hides the skateboard button, because you can't skate inside
/// - hides the house button and the turn arrows while another screen is open (menu, avatar editor, tutorial).
/// Put ONE of these in the house scene. Everything is undone when it is disabled.
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

    readonly List<VisualElement> _menus = new();
    readonly List<VisualElement> _houseButtons = new();
    readonly List<VisualElement> _walkOnlyButtons = new();
    readonly List<VisualElement> _skatePanels = new();

    void LateUpdate()
    {
        // Re-applied every frame: other scripts show/hide parts of this HUD when the player state changes.
        Collect();

        if (moveMenuToTopLeft)
            foreach (var m in _menus)
            {
                m.style.left = menuLeft;
                m.style.top = menuTop;
                m.style.right = StyleKeyword.Auto;
                m.style.bottom = StyleKeyword.Auto;
            }

        if (hideSkateboard)
            foreach (var p in _skatePanels)
                if (p.style.display != DisplayStyle.None) p.style.display = DisplayStyle.None;

        bool walking = PlayerService.Instance == null || PlayerService.Instance.LocalPlayer == null
                       || PlayerService.Instance.LocalPlayer.StateManager.CurrentState == PlayerStates.StandardState;
        foreach (var b in _walkOnlyButtons)
        {
            if (walking && b.style.display == DisplayStyle.None) b.style.display = StyleKeyword.Null;
            else if (!walking && b.style.display != DisplayStyle.None) b.style.display = DisplayStyle.None;
        }

        bool showHouse = HouseButtonsAllowed();
        foreach (var b in _houseButtons)
        {
            if (showHouse && b.style.display == DisplayStyle.None) b.style.display = StyleKeyword.Null;
            else if (!showHouse && b.style.display != DisplayStyle.None) b.style.display = DisplayStyle.None;
        }
    }

    bool HouseButtonsAllowed()
    {
        if (PlayerService.Instance == null || PlayerService.Instance.LocalPlayer == null) return true;
        var states = PlayerService.Instance.LocalPlayer.StateManager;
        if (states == null || showHouseButtonsIn == null) return true;
        return System.Array.IndexOf(showHouseButtonsIn, states.CurrentState) >= 0;
    }

    void OnDisable()
    {
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

    void Collect()
    {
        _menus.RemoveAll(e => e.panel == null);
        _skatePanels.RemoveAll(e => e.panel == null);
        _houseButtons.RemoveAll(e => e.panel == null);
        _walkOnlyButtons.RemoveAll(e => e.panel == null);

        foreach (var doc in FindObjectsByType<UIDocument>(FindObjectsSortMode.None))
        {
            var root = doc.rootVisualElement;
            if (root == null) continue;

            if (houseButtonNames != null)
                foreach (var n in houseButtonNames)
                {
                    var hb = string.IsNullOrEmpty(n) ? null : root.Q(n);
                    if (hb != null && !_houseButtons.Contains(hb)) _houseButtons.Add(hb);
                }

            if (walkOnlyButtonNames != null)
                foreach (var n in walkOnlyButtonNames)
                {
                    var wb = string.IsNullOrEmpty(n) ? null : root.Q(n);
                    if (wb != null && !_walkOnlyButtons.Contains(wb)) _walkOnlyButtons.Add(wb);
                }

            var menu = root.Q(menuButtonName);
            if (menu != null && !_menus.Contains(menu)) _menus.Add(menu);

            var skate = root.Q(skateboardButtonName);
            if (skate != null)
            {
                // the button sits in its own decorative corner panel: hide the whole panel
                var target = (skate.parent != null && skate.parent != root && skate.parent.childCount == 1) ? skate.parent : skate;
                if (!_skatePanels.Contains(target)) _skatePanels.Add(target);
            }
        }
    }
}
