using System.Collections.Generic;
using UnityEngine;

namespace Luma.QuipService
{
    [CreateAssetMenu(fileName = "QuipCatalog", menuName = "Luma/Quips/Catalog")]
    public sealed class QuipCatalog : ScriptableObject
    {
        [SerializeField] private List<Quip> quips = new();
        public IReadOnlyList<Quip> Quips => quips.AsReadOnly();

        // Resolve network IDs locally; ScriptableObject references never travel over the network.
        // Ambiguous or invalid entries cannot be sent or resolved.
        public bool TryGetQuip(int id, out Quip quip)
        {
            quip = null;
            if (id < 0) return false;

            foreach (var entry in quips)
            {
                if (entry == null || entry.Id != id) continue;
                if (quip != null || string.IsNullOrWhiteSpace(entry.Text))
                {
                    quip = null;
                    return false;
                }
                quip = entry;
            }

            return quip != null;
        }

        public IEnumerable<string> Validate()
        {
            var ids = new HashSet<int>();
            foreach (var quip in quips)
            {
                if (quip == null) { yield return "Missing quip in catalog."; continue; }
                if (quip.Id < 0 || !ids.Add(quip.Id)) yield return $"Invalid/duplicate quip ID: {quip.name}";
                if (string.IsNullOrWhiteSpace(quip.Text)) yield return $"Missing quip text: {quip.name}";
            }
        }

        private void OnValidate()
        {
            foreach (var issue in Validate()) Debug.LogWarning(issue, this);
        }
    }
}
