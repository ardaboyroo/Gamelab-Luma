using UnityEngine;

namespace Luma.AvatarCustomization
{
    [CreateAssetMenu(menuName = "Luma/Avatar/Options/Eyebrow")]
    public sealed class EyebrowOption : AvatarTextureOption
    {
        public override AvatarCategory Category => AvatarCategory.Eyebrows;
    }
}
