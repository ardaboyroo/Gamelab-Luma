using UnityEngine;

namespace Luma.AvatarCustomization
{
    [CreateAssetMenu(menuName = "Luma/Avatar/Options/Mouth")]
    public sealed class MouthOption : AvatarTextureOption
    {
        public override AvatarCategory Category => AvatarCategory.Mouth;
    }
}
