using TMPro;
using UnityEngine;

public sealed class DogBarkPopup : MonoBehaviour
{
    [SerializeField] private TextMeshPro _text;
    [SerializeField] private bool _yawOnly = true;

    [Header("Billboard Fix")]
    [SerializeField] private bool _flip180Yaw = true; // <- important for SpriteRenderer

    private Camera _cam;
    private float _dieAt;

    public void Init(string message, float lifetimeSeconds, Camera cam, bool yawOnly)
    {
        if (_text != null) _text.text = message;

        _cam = cam != null ? cam : Camera.main;
        _yawOnly = yawOnly;
        _dieAt = Time.time + Mathf.Max(0.05f, lifetimeSeconds);
    }

    private void LateUpdate()
    {
        if (_cam == null) _cam = Camera.main;

        if (_cam != null)
        {
            // Use camera rotation (stable) instead of camera position (can cause odd tilt depending on offsets)
            Quaternion rot;

            if (_yawOnly)
            {
                Vector3 f = _cam.transform.forward;
                f.y = 0f;
                if (f.sqrMagnitude < 0.0001f) f = Vector3.forward;
                rot = Quaternion.LookRotation(f.normalized, Vector3.up);
            }
            else
            {
                rot = _cam.transform.rotation;
            }

            // SpriteRenderer usually needs this so the "front" faces the camera.
            if (_flip180Yaw)
                rot *= Quaternion.Euler(0f, 180f, 0f);

            transform.rotation = rot;
        }

        if (Time.time >= _dieAt)
            Destroy(gameObject);
    }
}
