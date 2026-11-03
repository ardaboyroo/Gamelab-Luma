using System;
using UnityEngine;
using UnityEngine.UIElements;
using FMODUnity;

public class MenuUI : MonoBehaviour
{
    [SerializeField] private UIDocument _standardUI;
    [SerializeField] private UIDocument _menuUI;
    [SerializeField] private UIDocument _chrEditUI;

    [SerializeField] private GuideDogBehaviour _dog;

    Player _localPlr;

    [SerializeField] private EventReference _click;

    [SerializeField] private EventReference _confirm;

    //bools for what UIDocument is currently active
    private bool _standardActive;
    private bool _menuActive;
    private bool _charEditActive;

    private void Awake()
    {
        PlayerService.OnPlayerRegistered += (Player plr, bool isLocalPlayer) =>
        {
            Debug.Log("[MenuUI] subscribing to player registered");
            if (isLocalPlayer)
            {
                _localPlr = plr;
                plr.StateManager.OnStateChanged += HandleStateChanged;
            }
        };
        
    }

    void Start()
    {
        _standardUI.rootVisualElement.style.display = DisplayStyle.None;
        _menuUI.rootVisualElement.style.display = DisplayStyle.None;
        _chrEditUI.rootVisualElement.style.display = DisplayStyle.None;

        Debug.Log("[MenuUI] executing start");
    }

    private void HandleStateChanged(PlayerStates states)
    {
        var standardRoot = _standardUI.rootVisualElement;
        var menuRoot = _menuUI.rootVisualElement;
        var chrEditRoot = _chrEditUI.rootVisualElement;
        switch (states)
        {
            case PlayerStates.StandardState:
                if (!_standardActive) ShowUI();
                if (_menuActive) MenuHideUI();
                if (_charEditActive) CharacterEditHideUI();
                break;
            case PlayerStates.MenuState:
                if (_standardActive) HideUI();
                if (!_menuActive) MenuShowUI();
                if (_charEditActive) CharacterEditHideUI();
                break;
            case PlayerStates.CharacterEditState:
                if (_standardActive) HideUI();
                if (_menuActive) MenuHideUI();
                if (!_charEditActive) CharacterEditShowUI();
                break;
            case PlayerStates.DialogueState:

                break;
            case PlayerStates.CutsceneState:

                break;
            default:
                break;

        }

        if (states == PlayerStates.MenuState)
        {
            
        }
        else
        {
            
        }
    }

    void Update()
    {
        
    }

    private void ShowUI()
    {
        _standardActive = true;
        if (_standardUI == null) return;
        var root = _standardUI.rootVisualElement;
        root.style.display = DisplayStyle.Flex;

        var menuBTN = root.Q<Button>("menu-btn");
        var skateboardBTN = root.Q<Button>("SkateboardButton");

        menuBTN.clicked += ChangeToMenuState;
        skateboardBTN.clicked += _localPlr.GetComponent<WorldMovement>().ToggleSkateBoard;
    }

    private void HideUI()
    {
        _standardActive = false;
        if (_standardUI == null) return;
        var root = _standardUI.rootVisualElement;
        var skateboardBTN = root.Q<Button>("SkateboardButton");

        root.Q<Button>("menu-btn").clicked -= ChangeToMenuState;
        skateboardBTN.clicked -= _localPlr.GetComponent<WorldMovement>().ToggleSkateBoard;

        root.style.display = DisplayStyle.None;
    }

    private void MenuShowUI()
    {
        _menuActive = true;
        if (_menuUI == null) return;
        var root = _menuUI.rootVisualElement;
        root.style.display = DisplayStyle.Flex;

        var back = root.Q<Button>("back-btn");
        var prof = root.Q<Button>("profile-btn");
        var audio = root.Q<Button>("audio-btn");
        var charedit = root.Q<Button>("chr-btn");

        charedit.clicked += ChangeToCharacterEditState;
        back.clicked += ChangeToStandardState;

        //PlayerStateMachine.Instance.ChangeState(PlayerStateMachine.Instance.GetState<MenuPlayerState>());

        //PlayClick();
    }

    private void MenuHideUI()
    {
        _menuActive = false;
        if (_menuUI == null) return;
        var root = _menuUI.rootVisualElement;

        root.Q<Button>("back-btn").clicked -= ChangeToStandardState;
        //root.Q<Button>("profile-btn").clicked -= ;
        //root.Q<Button>("audio-btn").clicked -= ;
        root.Q<Button>("chr-btn").clicked -= ChangeToCharacterEditState;

        root.style.display = DisplayStyle.None;

        //PlayerStateMachine.Instance.ChangeState(PlayerStateMachine.Instance.GetState<StandardPlayerState>());

        //PlayClick();
    }

    private void CharacterEditShowUI()
    {
        if (_charEditActive)
            return;

        //MenuHideUI();
        _charEditActive = true;

        var root = _chrEditUI.rootVisualElement;
        root.style.display = DisplayStyle.Flex;

        var done = root.Q<Button>("done-btn");

        done.clicked += ChangeToMenuState;

        //PlayerStateMachine.Instance.ChangeState(PlayerStateMachine.Instance.GetState<MenuPlayerState>());

        //PlayClick();

        _dog.enabled = false;
    }

    private void CharacterEditHideUI()
    {
        _charEditActive = false;

        var root = _chrEditUI.rootVisualElement;

        root.Q<Button>("done-btn").clicked -= ChangeToMenuState;

        root.style.display = DisplayStyle.None;

        //PlayConfirm();

        _dog.enabled = true;
    }

    private void ChangeToStandardState()
    {
        ChangePlayerState(PlayerStates.StandardState);
    }

    private void ChangeToMenuState()
    {
        ChangePlayerState(PlayerStates.MenuState);
    }

    private void ChangeToCharacterEditState()
    {
        ChangePlayerState(PlayerStates.CharacterEditState);
    }

    private void ChangePlayerState(PlayerStates newState)
    {
        PlayerService.Instance.LocalPlayer.StateManager.ChangePlayerState(newState);
    }

    private void PlayClick() => RuntimeManager.PlayOneShot(_click);
    private void PlayConfirm() => RuntimeManager.PlayOneShot(_confirm);
}
