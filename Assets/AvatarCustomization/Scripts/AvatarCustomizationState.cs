using System;
using System.Collections.Generic;
using UnityEngine;

namespace Luma.AvatarCustomization
{
    [Serializable]
    public sealed class AvatarSelection
    {
        public AvatarCategory category;
        public string optionId;
        public Color color = Color.white;
    }

    [Serializable]
    public sealed class AvatarCustomizationState
    {
        public int version = 1;
        public List<AvatarSelection> selections = new();

        public AvatarSelection Get(AvatarCategory category)
        {
            selections ??= new List<AvatarSelection>();
            var entry = selections.Find(s => s != null && s.category == category);
            if (entry != null) return entry;
            entry = new AvatarSelection { category = category, color = DefaultColor(category) };
            selections.Add(entry);
            return entry;
        }

        public static Color DefaultColor(AvatarCategory category) => category switch
        {
            AvatarCategory.Hair or AvatarCategory.Eyebrows => new Color(0.12f, 0.07f, 0.04f),
            AvatarCategory.Eyes => new Color(0.19f, 0.40f, 0.29f),
            AvatarCategory.Mouth => new Color(0.45f, 0.12f, 0.15f),
            AvatarCategory.Top => new Color(0.95f, 0.55f, 0.16f),
            AvatarCategory.Pants => new Color(0.18f, 0.27f, 0.40f),
            AvatarCategory.Shoes => new Color(0.14f, 0.14f, 0.16f),
            _ => Color.white
        };
    }
}
