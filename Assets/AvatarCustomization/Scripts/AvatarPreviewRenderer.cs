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

        private void Start()
        {
            if (appearance == null) appearance = FindFirstObjectByType<AvatarAppearance>();
            root = GetComponent<UIDocument>().rootVisualElement;
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
            var center = bounds.center;
            previewCamera.orthographicSize = Mathf.Max(bounds.extents.y * 1.18f, 0.5f);
            previewCamera.transform.position = center + forward * Mathf.Max(bounds.size.y * 2, 4);
            previewCamera.transform.LookAt(center, Vector3.up);
        }

        private void OnDestroy()
        {
            foreach (var entry in originalLayers) if (entry.Key != null) entry.Key.layer = entry.Value;
            if (previewCamera != null) Destroy(previewCamera.gameObject);
            if (texture != null) { texture.Release(); Destroy(texture); }
        }
    }
}
