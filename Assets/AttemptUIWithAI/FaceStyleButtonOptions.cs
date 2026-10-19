using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Luma.AttemptUIWithAI
{
    [RequireComponent(typeof(UIDocument))]
    public sealed class FaceStyleButtonOptions : MonoBehaviour
    {
        [SerializeField] private List<AvatarTextureClass> eyesOptions = new List<AvatarTextureClass>();
        [SerializeField] private List<AvatarTextureClass> eyebrowsOptions = new List<AvatarTextureClass>();
        [SerializeField] private List<AvatarTextureClass> noseOptions = new List<AvatarTextureClass>();
        [SerializeField] private List<AvatarTextureClass> mouthOptions = new List<AvatarTextureClass>();
        [SerializeField] private List<AvatarObjectClass> hairOptions = new List<AvatarObjectClass>();

        public List<AvatarTextureClass> EyesOptions => eyesOptions;
        public List<AvatarTextureClass> EyebrowsOptions => eyebrowsOptions;
        public List<AvatarTextureClass> NoseOptions => noseOptions;
        public List<AvatarTextureClass> MouthOptions => mouthOptions;
        public List<AvatarObjectClass> HairOptions => hairOptions;

        public event System.Action<AvatarTextureClass> TextureOptionSelected;
        public event System.Action<AvatarObjectClass> ObjectOptionSelected;

        private void OnEnable() => RebuildButtons();

        private void OnDisable()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            if (root == null) return;
            foreach (var name in new[] { "eyesStylePanel", "eyebrowsStylePanel", "noseStylePanel", "mouthStylePanel", "hairStylePanel" })
            {
                var panel = root.Q<ScrollView>(name);
                panel?.Q<VisualElement>("generatedButtonGrid")?.RemoveFromHierarchy();
            }
        }

        public void RebuildButtons()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            if (root == null) return;
            Populate(root, "eyesStylePanel", eyesOptions);
            Populate(root, "eyebrowsStylePanel", eyebrowsOptions);
            Populate(root, "noseStylePanel", noseOptions);
            Populate(root, "mouthStylePanel", mouthOptions);
            Populate(root, "hairStylePanel", hairOptions);
        }

        private void Populate(VisualElement root, string panelName, List<AvatarTextureClass> options)
        {
            var panel = root.Q<ScrollView>(panelName);
            if (panel == null)
            {
                Debug.LogError("Missing style panel: " + panelName, this);
                return;
            }
            DynamicButtonGrid.Populate(panel, options ?? new List<AvatarTextureClass>(), SelectTexture);
        }

        private void Populate(VisualElement root, string panelName, List<AvatarObjectClass> options)
        {
            var panel = root.Q<ScrollView>(panelName);
            if (panel == null)
            {
                Debug.LogError("Missing style panel: " + panelName, this);
                return;
            }
            DynamicButtonGrid.Populate(panel, options ?? new List<AvatarObjectClass>(), SelectObject);
        }

        private void SelectTexture(AvatarTextureClass option)
        {
            Debug.Log($"Selected texture option: {option.avatarOptionName} ({option.avatarOptionTexture})", this);
            TextureOptionSelected?.Invoke(option);
        }

        private void SelectObject(AvatarObjectClass option)
        {
            Debug.Log($"Selected object option: {option.avatarOptionName} ({option.avatarOptionObject})", this);
            ObjectOptionSelected?.Invoke(option);
        }
    }
}
