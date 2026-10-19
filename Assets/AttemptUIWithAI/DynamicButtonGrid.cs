using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Luma.AttemptUIWithAI
{
    public static class DynamicButtonGrid
    {
        private const string GridName = "generatedButtonGrid";
        private const int Columns = 3;

        public static void Populate(VisualElement panel, IReadOnlyList<AvatarTextureClass> options, Action<AvatarTextureClass> onClicked)
        {
            PopulateOptions(panel, options, onClicked, option =>
                !string.IsNullOrWhiteSpace(option.avatarOptionName) ? option.avatarOptionName :
                option.avatarOptionTexture != null ? option.avatarOptionTexture.name : "Unassigned texture");
        }

        public static void Populate(VisualElement panel, IReadOnlyList<AvatarObjectClass> options, Action<AvatarObjectClass> onClicked)
        {
            PopulateOptions(panel, options, onClicked, option =>
                !string.IsNullOrWhiteSpace(option.avatarOptionName) ? option.avatarOptionName :
                option.avatarOptionObject != null ? option.avatarOptionObject.name : "Unassigned object");
        }

        private static void PopulateOptions<T>(VisualElement panel, IReadOnlyList<T> values, Action<T> onClicked, Func<T, string> label) where T : class
        {
            if (panel == null) throw new ArgumentNullException(nameof(panel));
            if (values == null) throw new ArgumentNullException(nameof(values));

            // ScrollView.Add routes children to its scrollable content container.
            var content = panel.contentContainer;
            foreach (var child in content.Children())
            {
                if (child.name != GridName) continue;
                child.RemoveFromHierarchy();
                break;
            }

            var grid = new VisualElement { name = GridName };
            grid.AddToClassList("dynamic-button-grid");
            panel.Add(grid);

            for (int first = 0; first < values.Count; first += Columns)
            {
                var row = new VisualElement();
                row.AddToClassList("dynamic-button-row");
                grid.Add(row);
                for (int column = 0; column < Columns; column++)
                {
                    var cell = new VisualElement();
                    cell.AddToClassList("dynamic-button-cell");
                    row.Add(cell);
                    int index = first + column;
                    if (index >= values.Count) continue;
                    T value = values[index];
                    var button = new Button(() => onClicked?.Invoke(value))
                    {
                        name = "optionButton" + index,
                        text = value != null ? label(value) : "Unassigned option",
                        userData = value
                    };
                    button.AddToClassList("pill");
                    button.AddToClassList("dynamic-option-button");
                    button.SetEnabled(value != null);
                    cell.Add(button);
                }
            }
        }
    }
}
