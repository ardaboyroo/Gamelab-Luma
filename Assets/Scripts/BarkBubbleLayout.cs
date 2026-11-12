using TMPro;
using UnityEngine;

/// Sizes the buddy's speech bubble to its text and plays a little pop-in.
/// Sits next to DogBarkPopup on the bubble prefab; DogBarkPopup still sets the text, faces the camera and removes it.
public class BarkBubbleLayout : MonoBehaviour
{
    public TMP_Text text;
    [Tooltip("Sliced sprite that grows with the text.")]
    public SpriteRenderer background;
    [Tooltip("The little pointer under the bubble.")]
    public Transform tail;

    [Header("Size (world units at scale 1)")]
    public Vector2 padding = new Vector2(0.38f, 0.24f);
    public float minWidth = 1.1f;
    public float maxTextWidth = 4f;
    [Tooltip("How far the tail overlaps the bubble so their outlines join.")]
    public float tailOverlap = 0.08f;

    [Header("Pop-in")]
    public float popDuration = 0.22f;

    string _laidOutText;
    Vector3 _baseScale;
    float _born;
    bool _started;

    void LateUpdate()
    {
        // Read the scale on the first frame, after whoever spawned the bubble has sized it.
        if (!_started)
        {
            _started = true;
            _baseScale = transform.localScale;
            _born = Time.time;
        }

        if (text != null && text.text != _laidOutText) Layout();

        float t = popDuration > 0f ? Mathf.Clamp01((Time.time - _born) / popDuration) : 1f;
        transform.localScale = _baseScale * Mathf.Max(0.01f, BackOut(t));
    }


    void Layout()
    {
        _laidOutText = text.text;
        text.enableWordWrapping = true;
        Vector2 pref = text.GetPreferredValues(_laidOutText, maxTextWidth, 0f);
        float w = Mathf.Min(pref.x, maxTextWidth);
        float h = text.GetPreferredValues(_laidOutText, w, 0f).y;

        float bw = Mathf.Max(minWidth, w + padding.x * 2f);
        float bh = h + padding.y * 2f;

        // the bubble's bottom sits on the tail; the tail tip is this object's origin
        float tailH = tail != null ? TailHeight() - tailOverlap : 0f;
        Vector3 centre = new Vector3(0f, tailH + bh * 0.5f, 0f);

        if (background != null)
        {
            background.drawMode = SpriteDrawMode.Sliced;
            background.size = new Vector2(bw, bh);
            background.transform.localPosition = centre;
        }

        text.rectTransform.sizeDelta = new Vector2(w + 0.02f, h);
        text.rectTransform.localPosition = centre + new Vector3(0f, 0f, -0.01f);

        if (tail != null)
            tail.localPosition = new Vector3(0f, tailH + tailOverlap, 0.005f);
    }

    float TailHeight()
    {
        var sr = tail.GetComponent<SpriteRenderer>();
        return sr != null && sr.sprite != null ? sr.sprite.bounds.size.y * tail.localScale.y : 0.3f;
    }

    // ease-out with a small overshoot
    static float BackOut(float t)
    {
        const float s = 1.70158f;
        t -= 1f;
        return t * t * ((s + 1f) * t + s) + 1f;
    }
}
