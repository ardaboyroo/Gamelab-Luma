using UnityEngine;

namespace Luma.AvatarCustomization
{
    public enum AvatarCategory { Eyes, Eyebrows, Nose, Mouth, Hair, Top, Pants, Shoes, Skin }

    public abstract class AvatarOption : ScriptableObject
    {
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [SerializeField] private Texture2D preview;
        public string Id => id;
        public string DisplayName => displayName;
        public Texture2D Preview => preview;
        public abstract AvatarCategory Category { get; }
    }

    public abstract class AvatarTextureOption : AvatarOption
    {
        [SerializeField] private Texture2D texture;
        public Texture2D Texture => texture;
    }

    public abstract class AvatarPrefabOption : AvatarOption
    {
        [SerializeField] private AvatarPart prefab;
        [SerializeField] private bool unequip;
        public AvatarPart Prefab => prefab;
        public bool Unequip => unequip;
    }
}
