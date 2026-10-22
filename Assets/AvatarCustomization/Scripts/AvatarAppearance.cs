using System;
using System.Collections.Generic;
using UnityEngine;

namespace Luma.AvatarCustomization
{
    [RequireComponent(typeof(AvatarRig))]
    public sealed class AvatarAppearance : MonoBehaviour
    {
        [SerializeField] private Shader bodyShader;
        [SerializeField] private Shader tintShader;
        [SerializeField] private GameObject[] legacyParts;
        private AvatarRig rig;
        private Material face, tint;
        private readonly Dictionary<AvatarCategory, AvatarPart> equipped = new();
        private readonly Dictionary<AvatarCategory, RenderTexture> tintedTextures = new();
        private readonly Dictionary<AvatarCategory, string> applied = new();
        public AvatarCustomizationState State { get; private set; }
        public AvatarRig Rig => rig;

        private void Start()
        {
            rig = GetComponent<AvatarRig>();
            face = new Material(rig.BodyRenderer.sharedMaterial) { name = "Avatar face (instance)" };
            if (bodyShader != null) face.shader = bodyShader;
            rig.BodyRenderer.material = face;
            tint = new Material(tintShader);
            foreach (var part in legacyParts) if (part != null) part.SetActive(false);
            var service = AvatarCustomizationService.Instance;
            if (service == null) { Debug.LogError("Avatar customization service is missing.", this); return; }
            State = service.Load();
            foreach (AvatarCategory category in Enum.GetValues(typeof(AvatarCategory))) Apply(category);
        }

        public void Select(AvatarOption option)
        {
            if (option == null || State == null) return;
            var service = AvatarCustomizationService.Instance;
            if (service.Resolve(option.Category, option.Id) != option) return;
            State.Get(option.Category).optionId = option.Id;
            Apply(option.Category);
            service.Save(State);
        }

        public void SetColor(AvatarCategory category, Color color)
        {
            if (State == null || category == AvatarCategory.Nose) return;
            State.Get(category).color = new Color(Mathf.Clamp01(color.r), Mathf.Clamp01(color.g), Mathf.Clamp01(color.b), 1f);
            Apply(category);
        }

        public void Save() { if (State != null) AvatarCustomizationService.Instance?.Save(State); }
        private void OnApplicationPause(bool paused) { if (paused) Save(); }
        private void OnApplicationQuit() => Save();

        public void Apply(AvatarCategory category)
        {
            var selection = State.Get(category);
            var option = AvatarCustomizationService.Instance.Resolve(category, selection.optionId);
            if (option is AvatarPrefabOption prefab)
            {
                if (!applied.TryGetValue(category, out var oldId) || oldId != prefab.Id)
                {
                    if (equipped.TryGetValue(category, out var old) && old != null) { old.gameObject.SetActive(false); Destroy(old.gameObject); }
                    equipped.Remove(category);
                    if (!prefab.Unequip && prefab.Prefab != null)
                    {
                        var part = Instantiate(prefab.Prefab, rig.PartsParent, false);
                        try { part.Bind(rig); part.gameObject.SetActive(true); equipped[category] = part; }
                        catch (Exception e) { Destroy(part.gameObject); Debug.LogError(e.Message, this); }
                    }
                    applied[category] = prefab.Id;
                }
                if (equipped.TryGetValue(category, out var current) && current != null)
                    foreach (var renderer in current.GetComponentsInChildren<Renderer>(true)) SetRendererColor(renderer, selection.color);
            }
            else if (option is AvatarTextureOption texture)
            {
                if (category == AvatarCategory.Eyes)
                {
                    face.SetTexture("_Eye", texture.Texture);
                    face.SetColor("_EyeColorA", selection.color); face.SetColor("_EyeColorB", selection.color);
                    face.SetColor("_EyeOutlineColor", selection.color);
                }
                if (category == AvatarCategory.Mouth)
                {
                    face.SetTexture("_Mouth", texture.Texture);
                    face.SetColor("_MouthColor", selection.color); face.SetColor("_Lipstick", selection.color);
                }
                if (category == AvatarCategory.Eyebrows)
                {
                    if (!tintedTextures.TryGetValue(category, out var rt))
                    {
                        rt = new RenderTexture(texture.Texture.width, texture.Texture.height, 0) { name = "Avatar eyebrow tint" };
                        rt.Create(); tintedTextures[category] = rt;
                    }
                    tint.SetColor("_Tint", selection.color);
                    Graphics.Blit(texture.Texture, rt, tint);
                    face.SetTexture("_Eyebrows", rt);
                }
            }
            if (category == AvatarCategory.Skin) face.SetColor("_SkinColor", selection.color);
        }

        private static void SetRendererColor(Renderer renderer, Color color)
        {
            for (int i = 0; i < renderer.sharedMaterials.Length; i++)
            {
                var block = new MaterialPropertyBlock(); renderer.GetPropertyBlock(block, i);
                block.SetColor("_BaseColor", color); block.SetColor("_Color", color);
                renderer.SetPropertyBlock(block, i);
            }
        }

        private void OnDestroy()
        {
            if (face != null) Destroy(face);
            if (tint != null) Destroy(tint);
            foreach (var rt in tintedTextures.Values) { rt.Release(); Destroy(rt); }
        }
    }
}
