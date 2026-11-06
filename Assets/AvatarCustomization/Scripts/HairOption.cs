using UnityEngine;

namespace Luma.AvatarCustomization
{
    [CreateAssetMenu(menuName = "Luma/Avatar/Options/Hair")]
    public sealed class HairOption : AvatarPrefabOption
    {
        public override AvatarCategory Category => AvatarCategory.Hair;
    }
}
