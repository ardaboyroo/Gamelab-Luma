using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Luma.AvatarCustomization
{
    [DefaultExecutionOrder(-200)]
    public sealed class AvatarCustomizationService : MonoBehaviour
    {
        public const string SaveKey = "Luma.AvatarCustomization.v1";
        public static AvatarCustomizationService Instance { get; private set; }
        [SerializeField] private AvatarCustomizationCatalog catalog;
        public AvatarCustomizationCatalog Catalog => catalog;
        public IReadOnlyList<EyeOption> Eyes => catalog.Eyes;
        public IReadOnlyList<EyebrowOption> Eyebrows => catalog.Eyebrows;
        public IReadOnlyList<NoseOption> Noses => catalog.Noses;
        public IReadOnlyList<MouthOption> Mouths => catalog.Mouths;
        public IReadOnlyList<HairOption> Hair => catalog.Hair;
        public IReadOnlyList<TopOption> Tops => catalog.Tops;
        public IReadOnlyList<PantsOption> Pants => catalog.Pants;
        public IReadOnlyList<ShoesOption> Shoes => catalog.Shoes;
        private readonly Dictionary<string, AvatarOption> lookup = new();

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            foreach (AvatarCategory category in Enum.GetValues(typeof(AvatarCategory)))
                foreach (var option in catalog.GetOptions(category))
                    if (option != null && !string.IsNullOrWhiteSpace(option.Id)) lookup.TryAdd(option.Id, option);
        }

        public AvatarOption Resolve(AvatarCategory category, string id)
        {
            if (id != null && lookup.TryGetValue(id, out var option) && option.Category == category) return option;
            return catalog.GetOptions(category).FirstOrDefault(o => o != null);
        }

        public AvatarCustomizationState Load()
        {
            AvatarCustomizationState state = null;
            try { state = JsonUtility.FromJson<AvatarCustomizationState>(PlayerPrefs.GetString(SaveKey, "")); }
            catch (ArgumentException) { Debug.LogWarning("Avatar save was unreadable; using defaults."); }
            state ??= new AvatarCustomizationState();
            foreach (AvatarCategory category in Enum.GetValues(typeof(AvatarCategory)))
            {
                var selection = state.Get(category);
                selection.optionId = Resolve(category, selection.optionId)?.Id;
                var c = selection.color;
                if (!float.IsFinite(c.r) || !float.IsFinite(c.g) || !float.IsFinite(c.b)) c = AvatarCustomizationState.DefaultColor(category);
                selection.color = new Color(Mathf.Clamp01(c.r), Mathf.Clamp01(c.g), Mathf.Clamp01(c.b), 1f);
            }
            return state;
        }

        public void Save(AvatarCustomizationState state)
        {
            PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(state));
            PlayerPrefs.Save();
        }

        private void OnDestroy() { if (Instance == this) Instance = null; }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatic() => Instance = null;
    }
}
