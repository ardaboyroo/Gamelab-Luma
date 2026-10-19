using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Luma.AttemptUIWithAI
{
    [RequireComponent(typeof(UIDocument))]
    public sealed class FaceStyleButtonOptions : MonoBehaviour
    {
        [SerializeField] private List<int> eyesValues = new List<int> { 0, 1, 2, 3, 4, 5, 6 };
        [SerializeField] private List<int> eyebrowsValues = new List<int> { 0, 1, 2, 3 };
        [SerializeField] private List<int> noseValues = new List<int> { 0, 1, 2, 3 };
        [SerializeField] private List<int> mouthValues = new List<int> { 0, 1, 2, 3 };
        [SerializeField] private List<int> hairValues = new List<int> { 0, 1, 2, 3 };

        [SerializeField] private List<AvatarTextureClass> eyesOptions = new List<AvatarTextureClass>();
        [SerializeField] private List<AvatarTextureClass> eyebrowsOptions = new List<AvatarTextureClass>();
        [SerializeField] private List<AvatarTextureClass> noseOptions = new List<AvatarTextureClass>();
        [SerializeField] private List<AvatarTextureClass> mouthOptions = new List<AvatarTextureClass>();
        [SerializeField] private List<AvatarObjectClass> hairOptions = new List<AvatarObjectClass>();

        public List<int> EyesValues => eyesValues;
        public List<int> EyebrowsValues => eyebrowsValues;
        public List<int> NoseValues => noseValues;
        public List<int> MouthValues => mouthValues;
        public List<int> HairValues => hairValues;

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
            Populate(root, "eyesStylePanel", eyesValues);
            Populate(root, "eyebrowsStylePanel", eyebrowsValues);
            Populate(root, "noseStylePanel", noseValues);
            Populate(root, "mouthStylePanel", mouthValues);
            Populate(root, "hairStylePanel", hairValues);
        }

        private void Populate(VisualElement root, string panelName, List<int> values)
        {
            var panel = root.Q<ScrollView>(panelName);
            if (panel == null)
            {
                Debug.LogError("Missing style panel: " + panelName, this);
                return;
            }
            DynamicButtonGrid.Populate(panel, values ?? new List<int>(), LogValue);
        }

        private void LogValue(int value) => Debug.Log(value, this);
    }
}
