using System;
using System.Collections.Generic;
using Luma.AttemptUIWithAI;
using UnityEngine;
using UnityEngine.UIElements;

namespace Luma.AvatarCustomization
{
    /// Preset colour swatches for the avatar editor's 'Kleur' tab.
    /// Kids tap a round swatch; the last (rainbow) circle opens the full colour wheel + sliders,
    /// and 'Kleuren' goes back to the swatches. Swatches feed the existing colour picker, so the
    /// colour is applied exactly like a colour picked on the wheel.
    /// Edit the colours per category in the Inspector (on the AvatarCreation prefab).
    [RequireComponent(typeof(UIDocument), typeof(AvatarCreationColorPicker), typeof(AvatarCustomizationController))]
    public sealed class AvatarColorPresets : MonoBehaviour
    {
        [Serializable]
        public sealed class CategoryPalette
        {
            public AvatarCategory category;
            public Color[] colors = Array.Empty<Color>();
        }

        [Tooltip("Colours shown for each category. A category without a palette shows the colour wheel straight away.")]
        public List<CategoryPalette> palettes = DefaultPalettes();
        public string wheelBackText = "Kleuren";

        UIDocument _doc;
        AvatarCreationColorPicker _picker;
        AvatarCustomizationController _controller;
        VisualElement _colorPanel, _grid;
        Button _backButton;
        readonly List<(VisualElement element, Color color)> _swatches = new();
        AvatarCategory _shownCategory;
        bool _built;

        void OnEnable()
        {
            _doc = GetComponent<UIDocument>();
            _picker = GetComponent<AvatarCreationColorPicker>();
            _controller = GetComponent<AvatarCustomizationController>();
            if (_picker != null) _picker.ColorChanged += OnPickerColorChanged;
            _built = false;
        }

        void OnDisable()
        {
            if (_picker != null) _picker.ColorChanged -= OnPickerColorChanged;
            _grid?.RemoveFromHierarchy();
            _backButton?.RemoveFromHierarchy();
            _colorPanel?.RemoveFromClassList("presets-mode");
            _grid = null;
            _backButton = null;
            _built = false;
        }

        void Update()
        {
            // (re)build when the UI document was (re)created
            if (!_built || _grid == null || _grid.panel == null)
            {
                if (!Build()) return;
            }

            if (_controller != null && _controller.SelectedCategory != _shownCategory)
                ShowCategory(_controller.SelectedCategory);
        }

        bool Build()
        {
            var root = _doc != null ? _doc.rootVisualElement : null;
            _colorPanel = root?.Q<VisualElement>("colorPanel");
            if (_colorPanel == null) return false;

            _grid = new VisualElement { name = "colorPresets" };
            _grid.AddToClassList("color-presets");
            _colorPanel.Add(_grid);

            _backButton = new Button(ShowPresets) { name = "colorPresetsBack", text = wheelBackText };
            _backButton.AddToClassList("pill");
            _backButton.AddToClassList("presets-back-btn");
            _colorPanel.Add(_backButton);

            _built = true;
            ShowCategory(_controller != null ? _controller.SelectedCategory : AvatarCategory.Skin);
            return true;
        }

        void ShowCategory(AvatarCategory category)
        {
            _shownCategory = category;
            _grid.Clear();
            _swatches.Clear();

            var palette = palettes.Find(p => p.category == category);
            if (palette == null || palette.colors == null || palette.colors.Length == 0)
            {
                ShowWheel();
                _backButton.style.display = DisplayStyle.None;   // nothing to go back to
                return;
            }

            foreach (var c in palette.colors)
            {
                var swatch = new VisualElement();
                swatch.AddToClassList("color-preset");
                swatch.style.backgroundColor = new Color(c.r, c.g, c.b, 1f);
                var captured = c;
                swatch.RegisterCallback<ClickEvent>(_ => Pick(captured));
                _grid.Add(swatch);
                _swatches.Add((swatch, c));
            }

            var wheelButton = new VisualElement { name = "colorWheelButton", tooltip = "Kleurenwiel" };
            wheelButton.AddToClassList("color-preset");
            wheelButton.AddToClassList("color-preset--wheel");
            wheelButton.RegisterCallback<ClickEvent>(_ => ShowWheel());
            _grid.Add(wheelButton);

            ShowPresets();
        }

        void Pick(Color c)
        {
            if (_picker != null) _picker.SetColor(c);
            HighlightSelected();
        }

        public void ShowPresets()
        {
            _colorPanel?.AddToClassList("presets-mode");
            if (_backButton != null) _backButton.style.display = DisplayStyle.None;
            HighlightSelected();
        }

        public void ShowWheel()
        {
            _colorPanel?.RemoveFromClassList("presets-mode");
            if (_backButton != null) _backButton.style.display = DisplayStyle.Flex;
        }

        void OnPickerColorChanged(Color _) => HighlightSelected();

        void HighlightSelected()
        {
            if (_picker == null) return;
            Color current = _picker.SelectedColor;
            foreach (var (element, color) in _swatches)
            {
                bool same = Mathf.Abs(current.r - color.r) < 0.02f && Mathf.Abs(current.g - color.g) < 0.02f && Mathf.Abs(current.b - color.b) < 0.02f;
                element.EnableInClassList("color-preset--selected", same);
            }
        }

        // ---------- default colours ----------
        static Color Hex(string hex) => ColorUtility.TryParseHtmlString(hex, out var c) ? c : Color.magenta;

        static CategoryPalette P(AvatarCategory cat, params string[] hex)
        {
            var colors = new Color[hex.Length];
            for (int i = 0; i < hex.Length; i++) colors[i] = Hex(hex[i]);
            return new CategoryPalette { category = cat, colors = colors };
        }

        static readonly string[] HairColors = { "#1C1C1C", "#3B2A20", "#5A3825", "#8B5A2B", "#A0522D", "#D9A066", "#F2D16B", "#FAF0BE", "#C0392B", "#E87AA4", "#6C5CE7", "#3498DB", "#2ECC71" };
        static readonly string[] ClothesColors = { "#E74C3C", "#F39C12", "#F1C40F", "#2ECC71", "#1ABC9C", "#3498DB", "#2C3E7A", "#9B59B6", "#FF8FB1", "#FFFFFF", "#95A5A6", "#2D2D2D", "#8B5A2B", "#4A6FA5" };

        public static List<CategoryPalette> DefaultPalettes() => new()
        {
            P(AvatarCategory.Skin, "#FFE0BD", "#F9D3B4", "#F1C27D", "#E0AC69", "#C68642", "#A0663A", "#8D5524", "#6B3E26", "#4A2C1A", "#3B2219"),
            P(AvatarCategory.Hair, HairColors),
            P(AvatarCategory.Eyebrows, HairColors),
            P(AvatarCategory.Eyes, "#5D3A1A", "#8E5B2E", "#3E7C3A", "#3A7BD5", "#7FB3D5", "#8A8D91", "#C99A2E", "#8E44AD", "#2C2C2C"),
            P(AvatarCategory.Mouth, "#E8A0A0", "#D9777F", "#C0504D", "#A63C3C", "#F4A6B8", "#9C5B5B"),
            P(AvatarCategory.Top, ClothesColors),
            P(AvatarCategory.Pants, ClothesColors),
            P(AvatarCategory.Shoes, ClothesColors),
        };
    }
}
