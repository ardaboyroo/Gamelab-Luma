using UnityEngine;
using UnityEngine.UIElements;

namespace Luma.AttemptUIWithAI
{
    [RequireComponent(typeof(UIDocument))]
    public sealed class AvatarCreationPanelTabs : MonoBehaviour
    {
        private VisualElement stylePanel, colorPanel;
        private Button styleTab, colorTab;
        private bool showingStyles;

        private void OnEnable()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            stylePanel = root.Q<VisualElement>("stylePanel");
            colorPanel = root.Q<VisualElement>("colorPanel");
            styleTab = root.Q<Button>("styleTab");
            colorTab = root.Q<Button>("colorTab");
            if (stylePanel == null || colorPanel == null || styleTab == null || colorTab == null)
            {
                Debug.LogError("Avatar panel tabs require the named elements in AvatarCreation.uxml.", this);
                return;
            }
            styleTab.clicked += ShowStyles;
            colorTab.clicked += ShowColors;
            ApplySelection();
        }

        private void OnDisable()
        {
            if (styleTab != null) styleTab.clicked -= ShowStyles;
            if (colorTab != null) colorTab.clicked -= ShowColors;
        }

        public void ShowStyles()
        {
            showingStyles = true;
            ApplySelection();
        }

        public void ShowColors()
        {
            showingStyles = false;
            ApplySelection();
        }

        private void ApplySelection()
        {
            if (stylePanel == null || colorPanel == null || styleTab == null || colorTab == null) return;
            stylePanel.EnableInClassList("is-hidden", !showingStyles);
            colorPanel.EnableInClassList("is-hidden", showingStyles);
            styleTab.EnableInClassList("selected-option", showingStyles);
            colorTab.EnableInClassList("selected-option", !showingStyles);
        }
    }
}
