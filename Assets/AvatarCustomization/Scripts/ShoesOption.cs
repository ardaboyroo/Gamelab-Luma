using UnityEngine;

namespace Luma.AvatarCustomization
{
    [CreateAssetMenu(menuName = "Luma/Avatar/Options/Shoes")]
    public sealed class ShoesOption : AvatarPrefabOption
    {
        public override AvatarCategory Category => AvatarCategory.Shoes;
    }
}
