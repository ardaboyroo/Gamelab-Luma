using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UIElements;

namespace Luma.AvatarCustomization
{
    [RequireComponent(typeof(UIDocument))]
    [DisallowMultipleComponent]
    public sealed class AvatarPreviewRenderer : MonoBehaviour
    {
        [SerializeField] private AvatarAppearance appearance;
        [SerializeField, Range(0, 31)] private int previewLayer = 30;
        private readonly Dictionary<GameObject, int> originalLayers = new();
        private Camera previewCamera;
        private RenderTexture texture;
        private Image image;
        private VisualElement root;
        private AvatarCustomizationController controller;
        private Vector3 focusCenter;
        private float focusSize;
        private bool hasFocus;

        [Header("Zoom per category (x = focus point from top of body, y = half visible height), fractions of body height")]
        [SerializeField] private Vector2 faceFocus = new(0.25f, 0.27f);
        [SerializeField] private Vector2 hairFocus = new(0.25f, 0.3f);
        [SerializeField] private Vector2 topFocus = new(0.55f, 0.23f);
        [SerializeField] private Vector2 pantsFocus = new(0.74f, 0.21f);
        [SerializeField] private Vector2 shoesFocus = new(0.86f, 0.17f);
        [SerializeField, Tooltip("How fast the camera glides to the new zoom. 0 = instant.")] private float zoomSpeed = 8f;

        private void Start()
        {
            if (appearance == null) appearance = FindFirstObjectByType<AvatarAppearance>();
            root = GetComponent<UIDocument>().rootVisualElement;
            controller = GetComponent<AvatarCustomizationController>();
            image = root.Q<Image>("avatarPreview");
            texture = new RenderTexture(768, 768, 24) { name = "Avatar customization preview" };
            texture.Create(); image.image = texture;
            var cameraObject = new GameObject("Avatar preview camera");
            previewCamera = cameraObject.AddComponent<Camera>();
            previewCamera.orthographic = true; previewCamera.clearFlags = CameraClearFlags.SolidColor;
            previewCamera.backgroundColor = new Color(0, 0, 0, 0);
            previewCamera.cullingMask = 1 << previewLayer; previewCamera.targetTexture = texture;
            previewCamera.nearClipPlane = 0.01f; previewCamera.farClipPlane = 200;
            previewCamera.GetUniversalAdditionalCameraData().renderPostProcessing = false;
        }

        private void LateUpdate()
        {
            if (previewCamera == null || appearance == null || appearance.Rig == null) return;
            previewCamera.enabled = root.resolvedStyle.display != DisplayStyle.None;
            if (!previewCamera.enabled) return;
            foreach (var renderer in appearance.GetComponentsInChildren<Renderer>(true))
            {
                if (!originalLayers.ContainsKey(renderer.gameObject)) originalLayers[renderer.gameObject] = renderer.gameObject.layer;
                renderer.gameObject.layer = previewLayer;
            }
            var bounds = appearance.Rig.BodyRenderer.bounds;
            var forward = appearance.transform.parent.forward;
            var height = bounds.size.y;

            // Zoom in on the part that is being edited (face, hair, clothes); full body for skin.
            GetFocus(controller != null ? controller.SelectedCategory : AvatarCategory.Skin, out var fromTop, out var halfHeight);
            var targetCenter = bounds.center;
            targetCenter.y = bounds.max.y - height * fromTop;
            var targetSize = Mathf.Max(height * halfHeight, 0.25f);

            if (!hasFocus || zoomSpeed <= 0f) { focusCenter = targetCenter; focusSize = targetSize; hasFocus = true; }
            else
            {
                var t = 1f - Mathf.Exp(-zoomSpeed * Time.unscaledDeltaTime);
                focusCenter = Vector3.Lerp(focusCenter, targetCenter, t);
                focusSize = Mathf.Lerp(focusSize, targetSize, t);
            }

            previewCamera.orthographicSize = focusSize;
            previewCamera.transform.position = focusCenter + forward * Mathf.Max(height * 2, 4);
            previewCamera.transform.rotation = Quaternion.LookRotation(-forward, Vector3.up);
        }

        /// fromTop: focus point measured down from the top of the body (0 = top, 1 = feet), as a fraction of body height.
        /// halfHeight: half the visible height, as a fraction of body height (0.59 = whole body with some room).
        private void GetFocus(AvatarCategory category, out float fromTop, out float halfHeight)
        {
            switch (category)
            {
                case AvatarCategory.Eyes:
                case AvatarCategory.Eyebrows:
                case AvatarCategory.Nose:
                case AvatarCategory.Mouth: fromTop = faceFocus.x; halfHeight = faceFocus.y; break;
                case AvatarCategory.Hair: fromTop = hairFocus.x; halfHeight = hairFocus.y; break;
                case AvatarCategory.Top: fromTop = topFocus.x; halfHeight = topFocus.y; break;
                case AvatarCategory.Pants: fromTop = pantsFocus.x; halfHeight = pantsFocus.y; break;
                case AvatarCategory.Shoes: fromTop = shoesFocus.x; halfHeight = shoesFocus.y; break;
                default: fromTop = 0.5f; halfHeight = 0.59f; break;
            }
        }

        private void OnDestroy()
        {
            foreach (var entry in originalLayers) if (entry.Key != null) entry.Key.layer = entry.Value;
            if (previewCamera != null) Destroy(previewCamera.gameObject);
            if (texture != null) { texture.Release(); Destroy(texture); }
        }
    }
}
