using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine.UIElements;

namespace Luma.AttemptUIWithAI
{
    public static class DynamicButtonGrid
    {
        private const string GridName = "generatedButtonGrid";
        private const int Columns = 3;

        public static void Populate(VisualElement panel, IReadOnlyList<int> values, Action<int> onClicked)
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
                    int value = values[index];
                    var button = new Button(() => onClicked?.Invoke(value))
                    {
                        name = "optionButton" + index,
                        text = value.ToString(CultureInfo.InvariantCulture),
                        userData = value
                    };
                    button.AddToClassList("pill");
                    button.AddToClassList("dynamic-option-button");
                    cell.Add(button);
                }
            }
        }
    }
}
