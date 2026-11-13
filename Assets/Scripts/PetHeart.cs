using UnityEngine;

/// A little heart that floats up, wobbles and fades out. Spawned when the dog buddy is petted.
public class PetHeart : MonoBehaviour
{
    public SpriteRenderer sprite;
    [Tooltip("World units it rises at scale 1.")]
    public float riseHeight = 1.2f;
    public float lifetime = 1.1f;
    public float wobble = 0.15f;

    float _start = -1f;
    float _delay;
    float _scale = 1f;
    Vector3 _origin;
    float _phase;

    /// scale: same as the dog's bark scale. delay: wait before appearing (for a little stagger).
    public void Play(float scale, float delay)
    {
        _scale = scale;
        _delay = delay;
        _origin = transform.position;
        _phase = Random.Range(0f, Mathf.PI * 2f);
        _start = Time.time;
        transform.localScale = Vector3.zero;
        if (sprite != null) { var c = sprite.color; c.a = 0f; sprite.color = c; }
    }

    void LateUpdate()
    {
        if (_start < 0f) Play(1f, 0f);

        float t = (Time.time - _start - _delay) / lifetime;
        if (t < 0f) return;
        if (t >= 1f) { Destroy(gameObject); return; }

        // rise with an ease-out, wobble side to side, pop in then fade out
        float rise = 1f - (1f - t) * (1f - t);
        Camera cam = Camera.main;
        Vector3 right = cam != null ? cam.transform.right : Vector3.right;
        transform.position = _origin + Vector3.up * (riseHeight * _scale * rise)
                             + right * (Mathf.Sin(_phase + t * 9f) * wobble * _scale);

        float pop = t < 0.15f ? Mathf.SmoothStep(0f, 1.15f, t / 0.15f) : Mathf.Lerp(1.15f, 0.9f, (t - 0.15f) / 0.85f);
        transform.localScale = Vector3.one * (_scale * pop);

        if (sprite != null)
        {
            var c = sprite.color;
            c.a = t < 0.6f ? 1f : 1f - (t - 0.6f) / 0.4f;
            sprite.color = c;
        }

        if (cam != null) transform.rotation = cam.transform.rotation;   // face the camera
    }
}
