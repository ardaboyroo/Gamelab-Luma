using System.Collections.Generic;
using UnityEngine;

namespace Luma.EmojiService
{
    [CreateAssetMenu(fileName = "EmojiCatalog", menuName = "Luma/Emojis/Catalog")]
    public sealed class EmojiCatalog : ScriptableObject
    {
        [SerializeField] private List<EmojiOption> emojis = new();
        public IReadOnlyList<EmojiOption> Emojis => emojis.AsReadOnly();

        // Only IDs travel over the network; each client resolves its own name and sprite.
        public bool TryGetEmoji(int id, out EmojiOption emoji)
        {
            emoji = null;
            if (id < 0) return false;
            foreach (var entry in emojis)
            {
                if (entry == null || entry.Id != id) continue;
                if (emoji != null || string.IsNullOrWhiteSpace(entry.DisplayName))
                {
                    emoji = null;
                    return false;
                }
                emoji = entry;
            }
            return emoji != null;
        }

        public IEnumerable<string> Validate()
        {
            var ids = new HashSet<int>();
            foreach (var emoji in emojis)
            {
                if (emoji == null) { yield return "Missing emoji in catalog."; continue; }
                if (emoji.Id < 0 || !ids.Add(emoji.Id)) yield return $"Invalid/duplicate emoji ID: {emoji.name}";
                if (string.IsNullOrWhiteSpace(emoji.DisplayName)) yield return $"Missing emoji name: {emoji.name}";
            }
        }

        private void OnValidate()
        {
            foreach (var issue in Validate()) Debug.LogWarning(issue, this);
        }
    }
}
