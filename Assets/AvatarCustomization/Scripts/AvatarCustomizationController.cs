using System;
using System.Collections;
using System.Collections.Generic;
using Luma.AttemptUIWithAI;
using UnityEngine;
using UnityEngine.UIElements;

namespace Luma.AvatarCustomization
{
    [RequireComponent(typeof(UIDocument), typeof(AvatarCreationColorPicker))]
    [DisallowMultipleComponent]
    public sealed class AvatarCustomizationController : MonoBehaviour
    {
        [SerializeField] private AvatarAppearance appearance;
        private UIDocument document;
        private AvatarCreationColorPicker picker;
        private VisualElement root;
        private AvatarCategory category = AvatarCategory.Eyes;
        private readonly Dictionary<Button, Action> callbacks = new();
        private bool syncing, showingStyles = true, dirty;
        private float saveAt;
        public AvatarCategory SelectedCategory => category;

        private IEnumerator Start()
        {
            if (appearance == null) appearance = FindFirstObjectByType<AvatarAppearance>();
            while (appearance != null && appearance.State == null) yield return null;
            if (appearance == null) { Debug.LogError("Avatar appearance is not assigned.", this); yield break; }
            Bind();
        }

        private void Bind()
        {
            document = GetComponent<UIDocument>(); picker = GetComponent<AvatarCreationColorPicker>();
            root = document.rootVisualElement;
            BindButton("faceTab", () => ShowGroup("face", AvatarCategory.Eyes));
            BindButton("bodyTab", () => ShowGroup("body", AvatarCategory.Skin));
            BindButton("clothesTab", () => ShowGroup("clothes", AvatarCategory.Top));
            foreach (AvatarCategory value in Enum.GetValues(typeof(AvatarCategory)))
            {
                var captured = value;
                BindButton(Key(value) + "Button", () => SelectCategory(captured));
            }
            BindButton("styleTab", () => { showingStyles = true; Refresh(); });
            BindButton("colorTab", () => { showingStyles = false; Refresh(); });
            BindButton("done-btn", () => { appearance.Save(); dirty = false; });
            picker.ColorChanged += OnColorChanged;
            ShowGroup("face", AvatarCategory.Eyes);
        }

        private void BindButton(string name, Action action)
        {
            var button = root.Q<Button>(name);
            if (button == null) return;
            button.clicked += action; callbacks[button] = action;
        }

        public void ShowGroup(string group, AvatarCategory initial)
        {
            foreach (var name in new[] { "face", "body", "clothes" })
            {
                root.Q<VisualElement>(name + "Categories")?.EnableInClassList("is-hidden", name != group);
                root.Q<Button>(name + "Tab")?.EnableInClassList("selected-tab", name == group);
            }
            SelectCategory(initial);
        }

        public void SelectCategory(AvatarCategory value)
        {
            category = value;
            if (category == AvatarCategory.Nose) showingStyles = true;
            if (category == AvatarCategory.Skin) showingStyles = false;
            Refresh();
        }

        public void Choose(AvatarOption option)
        {
            if (option == null || option.Category != category) return;
            appearance.Select(option); Refresh();
        }

        private void OnColorChanged(Color color)
        {
            if (syncing || category == AvatarCategory.Nose || appearance.State == null) return;
            appearance.SetColor(category, color);
            dirty = true; saveAt = Time.unscaledTime + 0.35f;
        }

        private void Refresh()
        {
            var nose = category == AvatarCategory.Nose;
            var skin = category == AvatarCategory.Skin;
            root.Q<Button>("colorTab").SetEnabled(!nose);
            root.Q<Button>("styleTab").SetEnabled(!skin);
            root.Q<VisualElement>("stylePanel").EnableInClassList("is-hidden", !showingStyles);
            root.Q<VisualElement>("colorPanel").EnableInClassList("is-hidden", showingStyles);
            root.Q<Button>("styleTab").EnableInClassList("selected-option", showingStyles);
            root.Q<Button>("colorTab").EnableInClassList("selected-option", !showingStyles);
            root.Q<Label>("categoryHint").text = nose ? "Nose selection is saved. Avatar preview coming later." : skin ? "Choose your skin tone." : "Choose a style or adjust its color.";
            foreach (AvatarCategory value in Enum.GetValues(typeof(AvatarCategory)))
            {
                root.Q<Button>(Key(value) + "Button")?.EnableInClassList("selected-option", value == category);
                var panel = root.Q<ScrollView>(Key(value) + "StylePanel");
                if (panel == null) continue;
                panel.EnableInClassList("is-hidden", value != category);
                if (value != category) continue;
                panel.Clear();
                var grid = new VisualElement(); grid.AddToClassList("avatar-option-grid"); panel.Add(grid);
                foreach (var option in AvatarCustomizationService.Instance.Catalog.GetOptions(category))
                {
                    if (option == null) continue;
                    var selected = option;
                    var button = new Button(() => Choose(selected)) { name = option.Id, tooltip = option.DisplayName, userData = option };
                    button.AddToClassList("pill"); button.AddToClassList("avatar-option-card");
                    button.EnableInClassList("selected-option", appearance.State.Get(category).optionId == option.Id);
                    if (option.Preview != null)
                    {
                        var image = new Image { image = option.Preview, scaleMode = ScaleMode.ScaleToFit, pickingMode = PickingMode.Ignore };
                        image.AddToClassList("option-preview"); button.Add(image);
                    }
                    var label = new Label(option.DisplayName) { pickingMode = PickingMode.Ignore };
                    label.AddToClassList("option-label"); button.Add(label); grid.Add(button);
                }
            }
            syncing = true;
            picker.SetColor(appearance.State.Get(category).color);
            syncing = false;
        }

        private void Update()
        {
            if (dirty && Time.unscaledTime >= saveAt) { appearance.Save(); dirty = false; }
        }

        private void OnDisable() { if (appearance != null) appearance.Save(); }
        private void OnDestroy()
        {
            if (picker != null) picker.ColorChanged -= OnColorChanged;
            foreach (var callback in callbacks) callback.Key.clicked -= callback.Value;
        }

        public static string Key(AvatarCategory value) => value.ToString().ToLowerInvariant();
    }
}
