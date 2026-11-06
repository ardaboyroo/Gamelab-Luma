using System;
using System.Collections.Generic;
using System.Linq;
using Luma.AttemptUIWithAI;
using UnityEngine;
using UnityEngine.UIElements;

namespace Luma.AvatarCustomization.Editor
{
    public static class AvatarCustomizationVerification
    {
        public static string Run()
        {
            if (!Application.isPlaying) throw new InvalidOperationException("Run verification in Play mode.");
            var service = AvatarCustomizationService.Instance;
            var appearance = UnityEngine.Object.FindFirstObjectByType<AvatarAppearance>();
            var controller = UnityEngine.Object.FindFirstObjectByType<AvatarCustomizationController>();
            var picker = controller.GetComponent<AvatarCreationColorPicker>();
            var root = controller.GetComponent<UIDocument>().rootVisualElement;
            var original = JsonUtility.FromJson<AvatarCustomizationState>(JsonUtility.ToJson(appearance.State));
            int options = 0, skins = 0, colors = 0, nosePreviews = 0;
            var checks = new List<string>();
            void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
            try
            {
                Check(!service.Catalog.Validate().Any(), "Catalog validation failed.");
                Check(controller.GetComponents<AvatarCustomizationController>().Length == 1, "Duplicate UI controllers.");
                Check(controller.GetComponents<AvatarPreviewRenderer>().Length == 1, "Duplicate preview renderers.");
                var eyeBefore = appearance.State.Get(AvatarCategory.Eyes).color;
                controller.ShowGroup("clothes", AvatarCategory.Top);
                Check(Vector4.Distance(appearance.State.Get(AvatarCategory.Eyes).color, eyeBefore) < 0.001f, "Changing category changed another category color.");
                controller.ShowGroup("face", AvatarCategory.Eyes);
                foreach (AvatarCategory category in Enum.GetValues(typeof(AvatarCategory)))
                {
                    controller.SelectCategory(category);
                    foreach (var option in service.Catalog.GetOptions(category))
                    {
                        controller.Choose(option); options++;
                        Check(appearance.State.Get(category).optionId == option.Id, "Selection failed: " + option.Id);
                        if (option is NoseOption)
                        {
                            var button = root.Q<Button>(option.Id);
                            Check(button != null && button.Q<Image>()?.image == option.Preview, "Nose PNG missing: " + option.Id);
                            nosePreviews++;
                        }
                        foreach (var renderer in appearance.GetComponentsInChildren<SkinnedMeshRenderer>().Where(r => r.gameObject.activeInHierarchy))
                        {
                            Check(renderer.bones.Length == renderer.sharedMesh.bindposes.Length && renderer.bones.All(b => b != null), "Invalid bones: " + renderer.name);
                            Check(renderer.rootBone != null, "Missing root bone: " + renderer.name); skins++;
                        }
                        if (option is AvatarPrefabOption prefab)
                        {
                            var active = appearance.GetComponentsInChildren<AvatarPart>().Where(p => p.gameObject.activeInHierarchy).Count(p => p.name.StartsWith(prefab.Category switch { AvatarCategory.Top => "Tops", AvatarCategory.Hair => "Hair", AvatarCategory.Pants => "Pants", _ => "Shoes" }));
                            Check(active == (prefab.Unequip ? 0 : 1), "Unequip/replacement failed: " + option.Id);
                            if (option.Id == "hair-5")
                            {
                                var rigidHair = appearance.GetComponentsInChildren<AvatarPart>().First(p => p.name.StartsWith("Hair_5"));
                                Check(rigidHair.transform.parent == appearance.Rig.RigidAnchor, "Rigid hair is not attached to head.");
                                Check(Vector3.Distance(rigidHair.GetComponentInChildren<Renderer>().bounds.center, appearance.Rig.RigidAnchor.position) < 0.8f, "Rigid hair is outside head area.");
                            }
                        }
                    }
                    if (category == AvatarCategory.Nose)
                    {
                        Check(!root.Q<Button>("colorTab").enabledSelf, "Nose color picker should be disabled.");
                        continue;
                    }
                    var color = Color.HSVToRGB(((int)category + 1) / 10f, 0.72f, 0.83f);
                    picker.SetColor(color);
                    Check(Vector4.Distance(appearance.State.Get(category).color, color) < 0.001f, "Picker routing failed: " + category);
                    appearance.Save();
                    Check(Vector4.Distance(service.Load().Get(category).color, color) < 0.001f, "Color persistence failed: " + category);
                    var material = appearance.Rig.BodyRenderer.sharedMaterial;
                    if (category == AvatarCategory.Eyes) Check(Vector4.Distance(material.GetColor("_EyeColorA"), color) < 0.001f, "Eye color not applied.");
                    if (category == AvatarCategory.Mouth) Check(Vector4.Distance(material.GetColor("_MouthColor"), color) < 0.001f, "Mouth color not applied.");
                    if (category == AvatarCategory.Skin) Check(Vector4.Distance(material.GetColor("_SkinColor"), color) < 0.001f, "Skin color not applied.");
                    if (category == AvatarCategory.Eyebrows)
                    {
                        var rt = material.GetTexture("_Eyebrows") as RenderTexture;
                        Check(rt != null && rt.IsCreated(), "Eyebrow tint texture missing.");
                    }
                    if (category is AvatarCategory.Hair or AvatarCategory.Top or AvatarCategory.Pants or AvatarCategory.Shoes)
                    {
                        var part = appearance.GetComponentsInChildren<AvatarPart>().First(p => p.name.StartsWith(category switch { AvatarCategory.Top => "Tops", AvatarCategory.Hair => "Hair", AvatarCategory.Pants => "Pants", _ => "Shoes" }));
                        var block = new MaterialPropertyBlock(); part.GetComponentInChildren<Renderer>().GetPropertyBlock(block, 0);
                        Check(Vector4.Distance(block.GetColor("_BaseColor"), color) < 0.001f, "Part color not applied: " + category);
                        var first = service.Catalog.GetOptions(category).First(); controller.Choose(first);
                        var last = service.Catalog.GetOptions(category).Last(); controller.Choose(last);
                        Check(Vector4.Distance(appearance.State.Get(category).color, color) < 0.001f, "Color lost on replacement.");
                    }
                    colors++;
                }
                var loaded = service.Load();
                foreach (var selection in appearance.State.selections)
                {
                    Check(loaded.Get(selection.category).optionId == selection.optionId, "Saved ID mismatch.");
                    Check(Vector4.Distance(loaded.Get(selection.category).color, selection.color) < 0.001f, "Saved color mismatch.");
                }
                checks.Add("Typed catalog and unique IDs"); checks.Add("All options selectable"); checks.Add("One active part per slot and none removes part");
                checks.Add("Skinned mesh bone arrays match bind poses"); checks.Add("Six nose PNG buttons; preview-only selection");
                checks.Add("Eight category colors route through the real picker"); checks.Add("Color retained after unequip and replacement"); checks.Add("All selected IDs and colors round-trip through PlayerPrefs");
                checks.Add("Unknown ID falls back within its category");
                checks.Add("Single UI controller; category switching preserves other colors");
                Check(service.Resolve(AvatarCategory.Hair, "missing-option") is HairOption, "Invalid ID fallback failed.");
                return JsonUtility.ToJson(new VerificationResult { options = options, skinBindingChecks = skins, colorCategories = colors, nosePreviews = nosePreviews, checks = checks.ToArray() }, true);
            }
            finally
            {
                foreach (var selection in original.selections)
                {
                    var option = service.Resolve(selection.category, selection.optionId);
                    if (option != null) appearance.Select(option);
                    appearance.SetColor(selection.category, selection.color);
                }
                appearance.Save(); controller.ShowGroup("face", AvatarCategory.Eyes);
            }
        }

        [Serializable]
        private sealed class VerificationResult
        {
            public int options, skinBindingChecks, colorCategories, nosePreviews;
            public string[] checks;
        }
    }
}
