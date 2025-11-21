using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(Collider))]
public class ActivityPortal : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private UIDocument _uiDocument;
    [SerializeField] private Activity _activity;

    [Header("Player Tag")]
    [SerializeField] private string _playerTag = "Player";

    [Header("Events")]
    [SerializeField] private CameraEvent _cameraEvent;

    private bool _recentlyInteracted = false;
    private GameObject _player;

    private void Awake()
    {
        if (_uiDocument != null)
            _uiDocument.rootVisualElement.style.display = DisplayStyle.None;
    }

    private void Start()
    {
        PlayerStateMachine.Instance.OnStateChanged += OnStateChanged;
    }

    private void OnDestroy()
    {
        PlayerStateMachine.Instance.OnStateChanged -= OnStateChanged;
    }

    private void OnStateChanged(PlayerState state)
    {
        if(state is not StandardPlayerState)
            GetComponent<Renderer>().enabled = false;
        else 
            GetComponent<Renderer>().enabled = true;

    }

    private void OnTriggerEnter(Collider other)
    {
        if (_activity.IsActive || _recentlyInteracted)
            return;

        if (other.CompareTag(_playerTag))
        {
            _player = other.gameObject;

            ShowUI();

            if(_cameraEvent != null)
                CameraActions.Instance.ApplyCameraEvent(_cameraEvent);

            PlayerStateMachine.Instance.ChangeState(PlayerStateMachine.Instance.GetState<DialoguePlayerState>());
            _recentlyInteracted = true;
        }
    }

    private void ShowUI()
    {
        if (_uiDocument == null) return;
        var root = _uiDocument.rootVisualElement;
        root.style.display = DisplayStyle.Flex;

        var startBtn = root.Q<Button>("start-btn");
        var closeBtn = root.Q<Button>("close-btn");

        startBtn.clicked += OnStartClicked;
        closeBtn.clicked += OnCloseClicked;
    }

    private void HideUI()
    {
        if (_uiDocument == null) return;
        var root = _uiDocument.rootVisualElement;

        root.Q<Button>("start-btn").clicked -= OnStartClicked;
        root.Q<Button>("close-btn").clicked -= OnCloseClicked;

        root.style.display = DisplayStyle.None;
    }

    private void OnStartClicked()
    {
        HideUI();
        _activity.StartActivity();
    }

    private void OnCloseClicked()
    {
        if (_cameraEvent != null)
            CameraActions.Instance.ApplyPreviousEvent();

        PlayerStateMachine.Instance.ChangeState(PlayerStateMachine.Instance.GetState<StandardPlayerState>());

        HideUI();
    }

    private void Update()
    {
        if (_recentlyInteracted)
        {
            if (Vector3.Distance(_player.transform.position, transform.position) > 2.4f)
                _recentlyInteracted = false;
        }
    }
}
