using UnityEngine;

namespace Luma.AvatarCustomization
{
    [CreateAssetMenu(menuName = "Luma/Avatar/Options/Eye")]
    public sealed class EyeOption : AvatarTextureOption
    {
        public override AvatarCategory Category => AvatarCategory.Eyes;
    }
}
