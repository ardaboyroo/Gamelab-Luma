using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Luma.BubbleChatService
{
    public sealed class PlayerChatBubbles : MonoBehaviour
    {
        [Min(0.1f)] public float lifetimeSeconds = 5f;
        [Min(0.01f)] public float bubbleScale = 1f;
        [Min(0f)] public float headPadding = 0.3f;
        [Min(0f)] public float spacing = 0.15f;
        [Tooltip("Optional head anchor; otherwise the player's renderer bounds are used.")]
        public Transform headAnchor;
        public GameObject bubblePrefab;

        private Renderer[] body;
        private readonly List<Entry> bubbles = new();
        private sealed class Entry
        {
            public GameObject view;
            public float expiresAt;
            public float height;
            public float scale;
            public float bornAt;
        }

        private void Awake() => body = GetComponentsInChildren<Renderer>();

        public bool Show(string message, Sprite emoji)
        {
            var prefab = bubblePrefab != null ? bubblePrefab : Resources.Load<GameObject>("Prefabs/New/Buddy Bark Bubble");
            if (prefab == null) { Debug.LogError("Chat bubble prefab is missing.", this); return false; }
            var view = Instantiate(prefab);
            var bark = view.GetComponent<DogBarkPopup>();
            if (bark != null) { bark.enabled = false; Destroy(bark); }
            var layout = view.GetComponent<BarkBubbleLayout>();
            if (layout == null || layout.text == null || layout.background == null)
            {
                Destroy(view);
                Debug.LogError("Chat bubble requires a BarkBubbleLayout with text and background references.", this);
                return false;
            }
            layout.enabled = false;
            var text = layout.text;
            float width, height;
            if (emoji != null)
            {
                text.gameObject.SetActive(false);
                width = height = 1.4f;
                var icon = new GameObject("Emoji").AddComponent<SpriteRenderer>();
                icon.transform.SetParent(view.transform, false);
                icon.sprite = emoji;
                icon.sortingLayerID = layout.background.sortingLayerID;
                icon.sortingOrder = layout.background.sortingOrder + 2;
                var size = emoji.bounds.size;
                icon.transform.localScale = Vector3.one * (1.4f / Mathf.Max(size.x, size.y, 0.001f));
                icon.transform.localPosition = new Vector3(0, 0.3f + height * 0.5f + layout.padding.y, -0.02f);
            }
            else
            {
                text.text = message;
                text.textWrappingMode = TextWrappingModes.Normal;
                var preferred = text.GetPreferredValues(message, layout.maxTextWidth, 0);
                width = Mathf.Max(0.1f, Mathf.Min(preferred.x, layout.maxTextWidth));
                height = text.GetPreferredValues(message, width, 0).y;
                text.rectTransform.sizeDelta = new Vector2(width + 0.02f, height);
                text.rectTransform.localPosition = new Vector3(0, 0.3f + height * 0.5f + layout.padding.y, -0.01f);
            }
            float boxHeight = height + layout.padding.y * 2;
            layout.background.drawMode = SpriteDrawMode.Sliced;
            layout.background.size = new Vector2(Mathf.Max(layout.minWidth, width + layout.padding.x * 2), boxHeight);
            layout.background.transform.localPosition = new Vector3(0, 0.3f + boxHeight * 0.5f, 0);
            if (layout.tail != null) layout.tail.localPosition = new Vector3(0, 0.3f + layout.tailOverlap, 0.005f);
            bubbles.Add(new Entry { view = view, expiresAt = Time.unscaledTime + Mathf.Max(0.1f, lifetimeSeconds),
                bornAt = Time.unscaledTime, scale = Mathf.Max(0.01f, bubbleScale * Mathf.Abs(transform.lossyScale.y)), height = boxHeight + 0.3f });
            UpdateBubbles();
            return true;
        }

        private void LateUpdate() => UpdateBubbles();

        private void UpdateBubbles()
        {
            for (int i = bubbles.Count - 1; i >= 0; i--)
                if (bubbles[i].view == null || Time.unscaledTime >= bubbles[i].expiresAt)
                { if (bubbles[i].view != null) Destroy(bubbles[i].view); bubbles.RemoveAt(i); }
            Vector3 anchor = transform.position + Vector3.up * 2f;
            if (headAnchor != null) anchor = headAnchor.position;
            else
            {
                bool found = false;
                Bounds bounds = default;
                foreach (var renderer in body)
                    if (renderer != null && renderer.enabled && renderer.gameObject.activeInHierarchy)
                    { if (!found) { bounds = renderer.bounds; found = true; } else bounds.Encapsulate(renderer.bounds); }
                if (found) anchor = new Vector3(bounds.center.x, bounds.max.y, bounds.center.z);
            }
            anchor += Vector3.up * headPadding;
            var camera = Camera.main;
            Vector3 up = camera != null ? camera.transform.up : Vector3.up;
            float offset = 0;
            for (int i = bubbles.Count - 1; i >= 0; i--)
            {
                var entry = bubbles[i];
                entry.view.transform.position = anchor + up * offset;
                if (camera != null) entry.view.transform.rotation = camera.transform.rotation;
                float pop = Mathf.SmoothStep(0.01f, 1f, (Time.unscaledTime - entry.bornAt) / 0.18f);
                entry.view.transform.localScale = Vector3.one * entry.scale * pop;
                offset += entry.height * entry.scale + spacing;
            }
        }

        private void OnDisable()
        {
            foreach (var entry in bubbles) if (entry.view != null) Destroy(entry.view);
            bubbles.Clear();
        }
    }
}
