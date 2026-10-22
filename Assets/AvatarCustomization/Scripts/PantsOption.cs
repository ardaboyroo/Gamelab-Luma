using UnityEngine;

namespace Luma.AvatarCustomization
{
    [CreateAssetMenu(menuName = "Luma/Avatar/Options/Pants")]
    public sealed class PantsOption : AvatarPrefabOption
    {
        public override AvatarCategory Category => AvatarCategory.Pants;
    }
}
