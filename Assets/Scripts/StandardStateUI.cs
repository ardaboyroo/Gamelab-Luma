using FMODUnity;
using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

public class StandardStateUI : MonoBehaviour
{
    [SerializeField] private UIDocument _uiDocument;
    [SerializeField] private UIDocument _menu;
    [SerializeField] private UIDocument _chrEdit;

    [SerializeField] private GuideDogBehaviour _dog;

    private bool _isShown;

    private Vector3 _bufferedPosition;
    [SerializeField] private Vector3 _characterEdit;

    [SerializeField] private EventReference click;

    [SerializeField] private EventReference confirm;

    private bool _charedit;

    private void Start()
    {
        _isShown = false;
        _uiDocument.rootVisualElement.style.display = DisplayStyle.None;
        _menu.rootVisualElement.style.display = DisplayStyle.None;
        _chrEdit.rootVisualElement.style.display = DisplayStyle.None;

        PlayerStateMachine.Instance.OnStateChanged += OnStateChanged;
    }

    private void OnStateChanged(PlayerState obj)
    {
        if (obj is not StandardPlayerState)
        {
            HideUI();
        }
        else
        {
            ShowUI();
        }
    }

    private void ShowUI()
    {
        _isShown = true;
        if (_uiDocument == null) return;
        var root = _uiDocument.rootVisualElement;
        root.style.display = DisplayStyle.Flex;

        var menuBTN = root.Q<Button>("menu-btn");

        menuBTN.clicked += MenuShowUI;
    }

    private void HideUI()
    {
        _isShown = false;

        if (_uiDocument == null) return;
        var root = _uiDocument.rootVisualElement;

        root.Q<Button>("menu-btn").clicked -= MenuShowUI;

        root.style.display = DisplayStyle.None;
    }

    private void MenuShowUI()
    {
        if (_menu == null) return;
        var root = _menu.rootVisualElement;
        root.style.display = DisplayStyle.Flex;

        var back = root.Q<Button>("back-btn");
        var prof = root.Q<Button>("profile-btn");
        var audio = root.Q<Button>("audio-btn");
        var charedit = root.Q<Button>("chr-btn");

        charedit.clicked += CharacterEditShowUI;
        back.clicked += MenuHideUI;

        PlayerStateMachine.Instance.ChangeState(PlayerStateMachine.Instance.GetState<MenuPlayerState>());

        PlayClick();
    }

    private void MenuHideUI()
    {
        if (_menu == null) return;
        var root = _menu.rootVisualElement;

        root.Q<Button>("back-btn").clicked -= MenuHideUI;
        //root.Q<Button>("profile-btn").clicked -= ;
        //root.Q<Button>("audio-btn").clicked -= ;
        root.Q<Button>("chr-btn").clicked -= CharacterEditShowUI;

        root.style.display = DisplayStyle.None;

        PlayerStateMachine.Instance.ChangeState(PlayerStateMachine.Instance.GetState<StandardPlayerState>());

        PlayClick();
    }

    private void CharacterEditShowUI()
    {
        if (_charedit)
            return;

        MenuHideUI();
        _charedit = true;
        _bufferedPosition = PlayerStateMachine.Instance.transform.position;
        PlayerStateMachine.Instance.transform.position = _characterEdit;

        var root = _chrEdit.rootVisualElement;
        root.style.display = DisplayStyle.Flex;

        var done = root.Q<Button>("done-btn");

        done.clicked += CharacterEditHideUI;

        PlayerStateMachine.Instance.ChangeState(PlayerStateMachine.Instance.GetState<MenuPlayerState>());

        PlayClick();

        _dog.enabled = false;
        Debug.Log("Buffered position set to: " + _bufferedPosition);
    }

    private void CharacterEditHideUI()
    {
        _charedit = false;
        PlayerStateMachine.Instance.transform.position = _bufferedPosition;

        var root = _chrEdit.rootVisualElement;

        root.Q<Button>("done-btn").clicked -= CharacterEditHideUI;

        root.style.display = DisplayStyle.None;

        PlayConfirm();

        StartCoroutine(TeleportBack(_bufferedPosition));
        _dog.enabled = true;
        Debug.Log("Buffered position set derived from: " + _bufferedPosition);
    }

    private IEnumerator TeleportBack(Vector3 pos)
    {
        float elapsedTime = 0f;
        while (elapsedTime < 0.4f)
        {
            PlayerStateMachine.Instance.transform.position = _bufferedPosition;
            yield return null;
            elapsedTime += Time.deltaTime;
        }
    }

    private void PlayClick() => RuntimeManager.PlayOneShot(click);
    private void PlayConfirm() => RuntimeManager.PlayOneShot(confirm);
}