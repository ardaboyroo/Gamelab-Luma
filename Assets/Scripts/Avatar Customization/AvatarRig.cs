using System;
using UnityEngine;

namespace Luma.AvatarCustomization
{
    public sealed class AvatarRig : MonoBehaviour
    {
        [SerializeField] private Transform skeletonRoot;
        [SerializeField] private Renderer bodyRenderer;
        [SerializeField] private Transform partsParent;
        [SerializeField] private Transform rigidAnchor;
        public Transform SkeletonRoot => skeletonRoot;
        public Renderer BodyRenderer => bodyRenderer;
        public Transform PartsParent => partsParent;
        public Transform RigidAnchor => rigidAnchor;
        public Transform ResolveBone(string path)
        {
            var bone = string.IsNullOrEmpty(path) ? skeletonRoot : skeletonRoot.Find(path);
            if (bone == null) throw new InvalidOperationException($"Avatar bone not found: {path}");
            return bone;
        }
    }
}
