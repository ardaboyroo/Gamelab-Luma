using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using ETouch = UnityEngine.InputSystem.EnhancedTouch;

[RequireComponent(typeof(RectTransform))]
public class FloatingJoystick : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform _background;
    [SerializeField] private RectTransform _knob;

    [Header("Settings")]
    [SerializeField] private Vector2 _joystickSize = new Vector2(300f, 300f);

    public Vector2 Direction { get; private set; }

    private RectTransform _rectTransform;
    private Canvas _canvas;
    private Finger _movementFinger;

    private float Radius => _joystickSize.x * 0.5f;

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        _canvas = GetComponentInParent<Canvas>();

        _rectTransform.sizeDelta = _joystickSize;
        _background.sizeDelta = _joystickSize;

        _knob.anchoredPosition = Vector2.zero;

        SetVisuals(false);
    }

    private void OnEnable()
    {
        EnhancedTouchSupport.Enable();

        ETouch.Touch.onFingerDown += HandleFingerDown;
        ETouch.Touch.onFingerMove += HandleFingerMove;
        ETouch.Touch.onFingerUp += HandleFingerUp;
    }

    private void OnDisable()
    {
        ETouch.Touch.onFingerDown -= HandleFingerDown;
        ETouch.Touch.onFingerMove -= HandleFingerMove;
        ETouch.Touch.onFingerUp -= HandleFingerUp;

        EnhancedTouchSupport.Disable();

        _movementFinger = null;
        Direction = Vector2.zero;
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

        float halfWidth = canvasRect.rect.width * 0.5f;
        float halfHeight = canvasRect.rect.height * 0.5f;

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
        _background.gameObject.SetActive(visible);
        _knob.gameObject.SetActive(visible);
    }
}