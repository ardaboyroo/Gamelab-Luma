using UnityEngine;

namespace Luma.AvatarCustomization
{
    [CreateAssetMenu(menuName = "Luma/Avatar/Options/Nose")]
    public sealed class NoseOption : AvatarOption
    {
        public override AvatarCategory Category => AvatarCategory.Nose;
    }
}
