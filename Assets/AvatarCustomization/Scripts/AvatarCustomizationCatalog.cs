using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Luma.AvatarCustomization
{
    [CreateAssetMenu(menuName = "Luma/Avatar/Option Catalog")]
    public sealed class AvatarCustomizationCatalog : ScriptableObject
    {
        [SerializeField] private List<EyeOption> eyes = new();
        [SerializeField] private List<EyebrowOption> eyebrows = new();
        [SerializeField] private List<NoseOption> noses = new();
        [SerializeField] private List<MouthOption> mouths = new();
        [SerializeField] private List<HairOption> hair = new();
        [SerializeField] private List<TopOption> tops = new();
        [SerializeField] private List<PantsOption> pants = new();
        [SerializeField] private List<ShoesOption> shoes = new();
        public IReadOnlyList<EyeOption> Eyes => eyes.AsReadOnly();
        public IReadOnlyList<EyebrowOption> Eyebrows => eyebrows.AsReadOnly();
        public IReadOnlyList<NoseOption> Noses => noses.AsReadOnly();
        public IReadOnlyList<MouthOption> Mouths => mouths.AsReadOnly();
        public IReadOnlyList<HairOption> Hair => hair.AsReadOnly();
        public IReadOnlyList<TopOption> Tops => tops.AsReadOnly();
        public IReadOnlyList<PantsOption> Pants => pants.AsReadOnly();
        public IReadOnlyList<ShoesOption> Shoes => shoes.AsReadOnly();

        public IEnumerable<AvatarOption> GetOptions(AvatarCategory category) => category switch
        {
            AvatarCategory.Eyes => eyes, AvatarCategory.Eyebrows => eyebrows,
            AvatarCategory.Nose => noses, AvatarCategory.Mouth => mouths,
            AvatarCategory.Hair => hair, AvatarCategory.Top => tops,
            AvatarCategory.Pants => pants, AvatarCategory.Shoes => shoes,
            _ => Array.Empty<AvatarOption>()
        };

        public IEnumerable<string> Validate()
        {
            var ids = new HashSet<string>();
            foreach (AvatarCategory category in Enum.GetValues(typeof(AvatarCategory)))
            {
                foreach (var option in GetOptions(category))
                {
                    if (option == null) { yield return $"{category}: missing option."; continue; }
                    if (string.IsNullOrWhiteSpace(option.Id) || !ids.Add(option.Id)) yield return $"Invalid/duplicate ID: {option.name}";
                    if (option.Category != category) yield return $"Wrong category: {option.name}";
                    if (option is AvatarTextureOption texture && texture.Texture == null) yield return $"Missing texture: {option.name}";
                    if (option is AvatarPrefabOption part && !part.Unequip && part.Prefab == null) yield return $"Missing prefab: {option.name}";
                    if (option is NoseOption && option.Preview == null) yield return $"Missing nose preview: {option.name}";
                }
            }
        }

        private void OnValidate()
        {
            foreach (var issue in Validate()) Debug.LogWarning(issue, this);
        }
    }
}
