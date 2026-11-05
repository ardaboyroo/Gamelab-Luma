using UnityEngine;

namespace Luma.AvatarCustomization
{
    [CreateAssetMenu(menuName = "Luma/Avatar/Options/Top")]
    public sealed class TopOption : AvatarPrefabOption
    {
        public override AvatarCategory Category => AvatarCategory.Top;
    }
}
