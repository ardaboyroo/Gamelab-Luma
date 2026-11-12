using System;
using UnityEngine;
using UnityEngine.UIElements;

/// The big tutorial pop-up: buddy portrait, name plate and a text box that types itself out.
/// Tap while typing = show the whole page, tap again = next page, tap on the last page = close.
/// While it is open the player is put in the dialogue state, so walking and the joystick pause.
[RequireComponent(typeof(UIDocument))]
public class TutorialDialogue : MonoBehaviour
{
    public static TutorialDialogue Instance { get; private set; }

    [Header("Text")]
    [Min(1f)] public float charactersPerSecond = 45f;

    [Header("Player")]
    [Tooltip("Put the player in this state while the tutorial is open (pauses walking and the joystick).")]
    public bool pausePlayer = true;
    public PlayerStates dialogueState = PlayerStates.DialogueState;

    public bool IsOpen { get; private set; }

    /// Layer behind the dialogue where the buddy's '!' marker lives.
    public VisualElement MarkerLayer { get { EnsureBound(); return _markerLayer; } }

    public event Action<TutorialEntry> Opened;
    public event Action<TutorialEntry> Closed;

    UIDocument _doc;
    VisualElement _dialogue, _portrait, _nameplate, _next, _markerLayer;
    Label _text, _name;

    TutorialEntry _entry;
    Action _onClosed;
    int _page;
    string _fullText = "";
    float _shownChars;
    bool _pageDone;
    bool _restoreState;
    PlayerStates _stateBefore;

    void Awake()
    {
        if (Instance != null && Instance != this)
            Debug.LogWarning("[TutorialDialogue] There is more than one TutorialDialogue in the scene; using the newest.", this);
        Instance = this;
        _doc = GetComponent<UIDocument>();
    }

    void OnEnable() => EnsureBound();

    void OnDisable()
    {
        if (IsOpen) Close();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void EnsureBound()
    {
        if (_doc == null) _doc = GetComponent<UIDocument>();
        var root = _doc != null ? _doc.rootVisualElement : null;
        if (root == null) return;
        if (_dialogue != null && _dialogue.panel != null && _dialogue.panel == root.panel) return;

        _dialogue = root.Q<VisualElement>("dialogue");
        _portrait = root.Q<VisualElement>("portrait");
        _nameplate = root.Q<VisualElement>("nameplate");
        _next = root.Q<VisualElement>("next");
        _markerLayer = root.Q<VisualElement>("marker-layer");
        _text = root.Q<Label>("text");
        _name = root.Q<Label>("name");

        if (_dialogue == null)
        {
            Debug.LogError("[TutorialDialogue] The UIDocument needs the TutorialDialogue.uxml layout.", this);
            return;
        }

        _text.enableRichText = false;   // the typewriter cuts text mid-word, so tags would break
        _dialogue.RegisterCallback<PointerDownEvent>(OnTap);
        SetVisible(false);
    }

    /// Opens a tutorial. onClosed runs after the player taps through the last page.
    public bool Show(TutorialEntry entry, Action onClosed = null)
    {
        if (entry == null || entry.pages == null || entry.pages.Length == 0) return false;
        EnsureBound();
        if (_dialogue == null) return false;
        if (IsOpen) Close();

        _entry = entry;
        _onClosed = onClosed;
        _page = 0;

        bool hasName = !string.IsNullOrWhiteSpace(entry.speakerName);
        _name.text = hasName ? entry.speakerName : "";
        _nameplate.style.display = hasName ? DisplayStyle.Flex : DisplayStyle.None;

        _portrait.style.backgroundImage = entry.portrait != null ? new StyleBackground(entry.portrait) : new StyleBackground(StyleKeyword.None);
        _portrait.style.display = entry.portrait != null ? DisplayStyle.Flex : DisplayStyle.None;

        IsOpen = true;
        SetVisible(true);
        PausePlayer();
        StartPage(0);
        Opened?.Invoke(entry);
        return true;
    }

    public void Close()
    {
        if (!IsOpen) return;
        IsOpen = false;
        SetVisible(false);
        RestorePlayer();

        var entry = _entry;
        var callback = _onClosed;
        _entry = null;
        _onClosed = null;

        TutorialProgress.MarkSeen(entry);
        callback?.Invoke();
        Closed?.Invoke(entry);
    }

    void Update()
    {
        if (!IsOpen) return;

        if (!_pageDone)
        {
            _shownChars += charactersPerSecond * Time.unscaledDeltaTime;
            int n = Mathf.Min(_fullText.Length, Mathf.FloorToInt(_shownChars));
            _text.text = _fullText.Substring(0, n);
            if (n >= _fullText.Length) FinishPage();
        }
        else if (_next != null)
        {
            // gentle blink on the 'next' arrow
            _next.style.opacity = 0.35f + 0.65f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 3f));
        }

        // PC testing: Space / Enter also advance.
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
            Advance();
    }

    void OnTap(PointerDownEvent evt)
    {
        evt.StopPropagation();
        Advance();
    }

    void Advance()
    {
        if (!IsOpen) return;
        if (!_pageDone) { FinishPage(); return; }

        if (_page + 1 < _entry.pages.Length) StartPage(_page + 1);
        else Close();
    }

    void StartPage(int index)
    {
        _page = index;
        _fullText = _entry.pages[index] ?? "";
        _shownChars = 0f;
        _pageDone = false;
        _text.text = "";
        if (_next != null) _next.style.visibility = Visibility.Hidden;
    }

    void FinishPage()
    {
        _pageDone = true;
        _text.text = _fullText;
        if (_next != null) _next.style.visibility = Visibility.Visible;
    }

    void SetVisible(bool visible)
    {
        if (_dialogue == null) return;
        if (visible)
        {
            _dialogue.style.display = DisplayStyle.Flex;
            // add the class a frame later so the fade/slide-in transition plays
            _dialogue.schedule.Execute(() => { if (IsOpen) _dialogue.AddToClassList("dialogue--open"); });
        }
        else
        {
            _dialogue.RemoveFromClassList("dialogue--open");
            _dialogue.style.display = DisplayStyle.None;
        }
    }

    // ---------- player state ----------
    static PlayerStateManager LocalStates() =>
        PlayerService.Instance != null && PlayerService.Instance.LocalPlayer != null
            ? PlayerService.Instance.LocalPlayer.StateManager
            : null;

    void PausePlayer()
    {
        _restoreState = false;
        if (!pausePlayer) return;
        var states = LocalStates();
        if (states == null || states.CurrentState == dialogueState) return;

        _stateBefore = states.CurrentState;
        _restoreState = true;
        states.ChangePlayerState(dialogueState);
    }

    void RestorePlayer()
    {
        if (!_restoreState) return;
        _restoreState = false;
        var states = LocalStates();
        if (states != null && states.CurrentState == dialogueState)
            states.ChangePlayerState(_stateBefore);
    }
}
