using System;
using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using ETouch = UnityEngine.InputSystem.EnhancedTouch;

[RequireComponent(typeof(RectTransform))]
public class FloatingJoystick : MonoBehaviour
{
    [SerializeField] private PlayerStates _workingState = PlayerStates.StandardState;

    [Header("References")]
    [SerializeField] private RectTransform _background;
    [SerializeField] private RectTransform _knob;

    [Header("Settings")]
    [SerializeField] private Vector2 _joystickSize = new Vector2(300f, 300f);

    public Vector2 Direction { get; private set; }

    private RectTransform _rectTransform;
    private Canvas _canvas;
    private Finger _movementFinger;

    // The state manager we are subscribed to. Kept so we can unsubscribe from exactly that one
    // when this joystick is destroyed (e.g. when its scene is unloaded).
    private PlayerStateManager _stateManager;
    private bool _touchSubscribed;

    private float Radius => _joystickSize.x * 0.5f;

    private void Awake()
    {
        // Named method instead of a lambda: PlayerService.OnPlayerRegistered is static and outlives
        // this scene, so we must be able to remove ourselves from it in OnDestroy.
        PlayerService.OnPlayerRegistered += HandlePlayerRegistered;

        _rectTransform = GetComponent<RectTransform>();
        _canvas = GetComponentInParent<Canvas>();

        _rectTransform.sizeDelta = _joystickSize;
        _background.sizeDelta = _joystickSize;

        _knob.anchoredPosition = Vector2.zero;

        SetVisuals(false);
    }

    private void OnEnable()
    {
        // EnhancedTouchSupport is reference counted: exactly one Enable here, one Disable in OnDisable.
        EnhancedTouchSupport.Enable();

        // If we were re-enabled while the player is in our state, start listening to touches again.
        if (_stateManager != null && _stateManager.CurrentState == _workingState)
            EnableJoystick();
    }

    private void OnDisable()
    {
        DisableJoystick();
        EnhancedTouchSupport.Disable();
    }

    private void OnDestroy()
    {
        PlayerService.OnPlayerRegistered -= HandlePlayerRegistered;
        UnbindStateManager();
    }

    private void HandlePlayerRegistered(Player plr, bool isLocalPlayer)
    {
        if (!isLocalPlayer || plr == null)
            return;

        UnbindStateManager();
        _stateManager = plr.StateManager;
        _stateManager.OnStateChanged += HandleStateChanged;
    }

    private void UnbindStateManager()
    {
        // ReferenceEquals: still unsubscribe even if the state manager's GameObject was destroyed first.
        if (!ReferenceEquals(_stateManager, null))
            _stateManager.OnStateChanged -= HandleStateChanged;

        _stateManager = null;
    }

    private void HandleStateChanged(PlayerStates states)
    {
        if (states == _workingState)
        {
            EnableJoystick();
        }
        else
        {
            DisableJoystick();
        }
    }

    private void EnableJoystick()
    {
        // Don't listen to touches while this component is off, and never subscribe twice.
        if (_touchSubscribed || !isActiveAndEnabled)
            return;

        ETouch.Touch.onFingerDown += HandleFingerDown;
        ETouch.Touch.onFingerMove += HandleFingerMove;
        ETouch.Touch.onFingerUp += HandleFingerUp;
        _touchSubscribed = true;
    }

    private void DisableJoystick()
    {
        ETouch.Touch.onFingerDown -= HandleFingerDown;
        ETouch.Touch.onFingerMove -= HandleFingerMove;
        ETouch.Touch.onFingerUp -= HandleFingerUp;
        _touchSubscribed = false;

        _movementFinger = null;
        Direction = Vector2.zero;

        if (_knob != null)
            _knob.anchoredPosition = Vector2.zero;

        SetVisuals(false);
    }

    private void HandleFingerDown(Finger finger)
    {
        if (_movementFinger != null)
            return;

        Vector2 screenPosition = finger.screenPosition;

        _movementFinger = finger;

        SetJoystickPosition(screenPosition);

        Direction = Vector2.zero;
        _knob.anchoredPosition = Vector2.zero;

        SetVisuals(true);
    }

    private void HandleFingerMove(Finger finger)
    {
        if (finger != _movementFinger)
            return;

        UpdateDirection(finger.currentTouch.screenPosition);
    }

    private void HandleFingerUp(Finger finger)
    {
        if (finger != _movementFinger)
            return;

        _movementFinger = null;

        Direction = Vector2.zero;
        _knob.anchoredPosition = Vector2.zero;

        SetVisuals(false);
    }

    private void SetJoystickPosition(Vector2 screenPosition)
    {
        RectTransform parentRect = _rectTransform.parent as RectTransform;

        Camera camera = _canvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : _canvas.worldCamera;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentRect,
            screenPosition,
            camera,
            out Vector2 localPosition
        );

        localPosition = ClampToCanvas(localPosition);

        _rectTransform.anchoredPosition = localPosition;
    }

    private void UpdateDirection(Vector2 screenPosition)
    {
        Camera camera = _canvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : _canvas.worldCamera;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _rectTransform,
            screenPosition,
            camera,
            out Vector2 localPosition
        );

        Direction = Vector2.ClampMagnitude(
            localPosition / Radius,
            1f
        );

        _knob.anchoredPosition = Direction * Radius;
    }

    private Vector2 ClampToCanvas(Vector2 position)
    {
        RectTransform canvasRect = _canvas.transform as RectTransform;

        position.x = Mathf.Clamp(
            position.x,
            canvasRect.rect.xMin + Radius,
            canvasRect.rect.xMax - Radius
        );

        position.y = Mathf.Clamp(
            position.y,
            canvasRect.rect.yMin + Radius,
            canvasRect.rect.yMax - Radius
        );

        return position;
    }

    private void SetVisuals(bool visible)
    {
        // During scene unload the child objects can already be gone.
        if (_background != null)
            _background.gameObject.SetActive(visible);

        if (_knob != null)
            _knob.gameObject.SetActive(visible);
    }
}
