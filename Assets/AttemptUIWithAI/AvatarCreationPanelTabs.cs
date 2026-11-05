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
        private static readonly string[] Categories = { "eyes", "eyebrows", "nose", "mouth", "hair" };
        private readonly Button[] categoryButtons = new Button[5];
        private readonly ScrollView[] categoryPanels = new ScrollView[5];
        private readonly System.Action[] categoryCallbacks = new System.Action[5];
        private int selectedCategory;

        public string SelectedFaceCategory => Categories[selectedCategory];

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
            for (int i = 0; i < Categories.Length; i++)
            {
                categoryButtons[i] = root.Q<Button>(Categories[i] + "Button");
                categoryPanels[i] = root.Q<ScrollView>(Categories[i] + "StylePanel");
                if (categoryButtons[i] == null || categoryPanels[i] == null)
                {
                    Debug.LogError("Missing face category button or style panel: " + Categories[i], this);
                    return;
                }
            }
            for (int i = 0; i < Categories.Length; i++)
            {
                int index = i;
                categoryCallbacks[i] = () => SelectCategory(index);
                categoryButtons[i].clicked += categoryCallbacks[i];
            }
            styleTab.clicked += ShowStyles;
            colorTab.clicked += ShowColors;
            ApplySelection();
        }

        private void OnDisable()
        {
            if (styleTab != null) styleTab.clicked -= ShowStyles;
            if (colorTab != null) colorTab.clicked -= ShowColors;
            for (int i = 0; i < categoryButtons.Length; i++)
            {
                if (categoryButtons[i] != null && categoryCallbacks[i] != null)
                    categoryButtons[i].clicked -= categoryCallbacks[i];
                categoryCallbacks[i] = null;
            }
        }

        private void SelectCategory(int index)
        {
            selectedCategory = index;
            ApplySelection();
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
            for (int i = 0; i < Categories.Length; i++)
            {
                categoryPanels[i]?.EnableInClassList("is-hidden", i != selectedCategory);
                categoryButtons[i]?.EnableInClassList("selected-option", i == selectedCategory);
            }
        }
    }
}
