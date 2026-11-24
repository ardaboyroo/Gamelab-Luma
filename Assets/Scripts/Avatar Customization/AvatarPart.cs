using System;
using UnityEngine;

namespace Luma.AvatarCustomization
{
    [Serializable]
    public sealed class AvatarSkinBinding
    {
        public SkinnedMeshRenderer renderer;
        public string rootBonePath;
        public string[] bonePaths;
    }

    public sealed class AvatarPart : MonoBehaviour
    {
        [SerializeField] private AvatarSkinBinding[] bindings = Array.Empty<AvatarSkinBinding>();
        [SerializeField] private bool attachToHead;
        public void Bind(AvatarRig rig)
        {
            if (attachToHead) transform.SetParent(rig.RigidAnchor, false);
            foreach (var binding in bindings)
            {
                var bones = new Transform[binding.bonePaths.Length];
                for (int i = 0; i < bones.Length; i++) bones[i] = rig.ResolveBone(binding.bonePaths[i]);
                binding.renderer.bones = bones;
                binding.renderer.rootBone = rig.ResolveBone(binding.rootBonePath);
                binding.renderer.updateWhenOffscreen = true;
            }
        }
    }
}
