using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Luma.AttemptUIWithAI;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Luma.AvatarCustomization.Editor
{
    public static class AvatarCustomizationMigration
    {
        public const string Root = "Assets/AvatarCustomization";
        public const string CatalogPath = Root + "/Data/AvatarCustomizationCatalog.asset";

        [MenuItem("Luma/Avatar/Migrate customization in current scene")]
        public static void Migrate()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Leave Play mode before migrating.");
            var legacy = Object.FindFirstObjectByType<CharacterEdit>(FindObjectsInactive.Include);
            if (legacy == null) throw new InvalidOperationException("No legacy CharacterEdit found in this scene.");
            var player = Object.FindFirstObjectByType<PlayerStateMachine>(FindObjectsInactive.Include);
            var model = player.transform.Find("Model Container");
            var body = model.Find("BaseMesh");
            var skeleton = model.Find("mixamorig:Hips");
            EnsureFolder(Root + "/Data/Options"); EnsureFolder(Root + "/Prefabs/Parts"); EnsureFolder(Root + "/Shaders");
            var catalog = AssetDatabase.LoadAssetAtPath<AvatarCustomizationCatalog>(CatalogPath);
            if (catalog == null) { catalog = ScriptableObject.CreateInstance<AvatarCustomizationCatalog>(); AssetDatabase.CreateAsset(catalog, CatalogPath); }
            var serializedCatalog = new SerializedObject(catalog);
            var serializedLegacy = new SerializedObject(legacy);
            var oldParts = new List<GameObject>();
            AddTextures<EyeOption>(serializedCatalog, "eyes", serializedLegacy.FindProperty("_eyes"), "Eyes");
            AddTextures<EyebrowOption>(serializedCatalog, "eyebrows", serializedLegacy.FindProperty("_brows"), "Eyebrows");
            AddTextures<MouthOption>(serializedCatalog, "mouths", serializedLegacy.FindProperty("_mouths"), "Mouths");
            var noses = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Resources/Models/Noses" }).Select(AssetDatabase.GUIDToAssetPath).OrderBy(p => p).ToArray();
            var noseList = serializedCatalog.FindProperty("noses"); noseList.arraySize = noses.Length;
            for (int i = 0; i < noses.Length; i++)
            {
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(noses[i]);
                var option = GetOption<NoseOption>("Noses", "nose-" + (i + 1), texture.name, texture);
                noseList.GetArrayElementAtIndex(i).objectReferenceValue = option;
            }
            AddParts<HairOption>(serializedCatalog, "hair", serializedLegacy.FindProperty("_hair"), "Hair", body, skeleton, oldParts);
            AddParts<TopOption>(serializedCatalog, "tops", serializedLegacy.FindProperty("_topOutfit"), "Tops", body, skeleton, oldParts);
            AddParts<PantsOption>(serializedCatalog, "pants", serializedLegacy.FindProperty("_pants"), "Pants", body, skeleton, oldParts);
            AddParts<ShoesOption>(serializedCatalog, "shoes", serializedLegacy.FindProperty("_shoes"), "Shoes", body, skeleton, oldParts);
            serializedCatalog.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(catalog);
            var issues = catalog.Validate().ToArray();
            if (issues.Length > 0) throw new InvalidOperationException(string.Join("\n", issues));

            var rig = model.GetComponent<AvatarRig>() ?? model.gameObject.AddComponent<AvatarRig>();
            Set(rig, "skeletonRoot", skeleton); Set(rig, "bodyRenderer", body.GetComponent<Renderer>());
            Set(rig, "partsParent", body);
            var head = skeleton.GetComponentsInChildren<Transform>(true).First(t => t.name == "mixamorig:Head");
            Set(rig, "rigidAnchor", head);
            FitRigidHairPrefab();
            var appearance = model.GetComponent<AvatarAppearance>() ?? model.gameObject.AddComponent<AvatarAppearance>();
            Set(appearance, "bodyShader", CreateBodyShader());
            Set(appearance, "tintShader", AssetDatabase.LoadAssetAtPath<Shader>(Root + "/Shaders/AvatarTextureTint.shader"));
            var a = new SerializedObject(appearance); var list = a.FindProperty("legacyParts");
            list.arraySize = oldParts.Count;
            for (int i = 0; i < oldParts.Count; i++) { list.GetArrayElementAtIndex(i).objectReferenceValue = oldParts[i]; oldParts[i].SetActive(false); }
            a.ApplyModifiedPropertiesWithoutUndo();
            var service = Object.FindFirstObjectByType<AvatarCustomizationService>(FindObjectsInactive.Include);
            if (service == null) service = new GameObject("Avatar Customization Service").AddComponent<AvatarCustomizationService>();
            Set(service, "catalog", catalog);

            var doc = Object.FindObjectsByType<UIDocument>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .FirstOrDefault(d => d.visualTreeAsset != null && AssetDatabase.GetAssetPath(d.visualTreeAsset) == "Assets/AttemptUIWithAI/AvatarCreation.uxml");
            if (doc == null)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/AttemptUIWithAI/AvatarCreation.prefab");
                doc = ((GameObject)PrefabUtility.InstantiatePrefab(prefab)).GetComponent<UIDocument>();
            }
            ConfigureUI(doc.gameObject, appearance);
            foreach (var standard in Object.FindObjectsByType<StandardStateUI>(FindObjectsInactive.Include, FindObjectsSortMode.None)) Set(standard, "_chrEdit", doc);
            legacy.enabled = false; legacy.gameObject.SetActive(false);
            var uiPrefab = PrefabUtility.LoadPrefabContents("Assets/AttemptUIWithAI/AvatarCreation.prefab");
            try { ConfigureUI(uiPrefab, null); PrefabUtility.SaveAsPrefabAsset(uiPrefab, "Assets/AttemptUIWithAI/AvatarCreation.prefab"); }
            finally { PrefabUtility.UnloadPrefabContents(uiPrefab); }
            ConfigureUI(doc.gameObject, appearance);
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(model.gameObject.scene);
            EditorSceneManager.SaveScene(model.gameObject.scene);
            Debug.Log("Avatar migration complete: " + Enum.GetValues(typeof(AvatarCategory)).Cast<AvatarCategory>().Sum(c => catalog.GetOptions(c).Count()) + " typed options.");
        }

        private static void ConfigureUI(GameObject go, AvatarAppearance appearance)
        {
            var oldOptions = go.GetComponent<FaceStyleButtonOptions>(); if (oldOptions != null) Object.DestroyImmediate(oldOptions);
            var oldTabs = go.GetComponent<AvatarCreationPanelTabs>(); if (oldTabs != null) Object.DestroyImmediate(oldTabs);
            RemoveDuplicates<AvatarCustomizationController>(go);
            RemoveDuplicates<AvatarPreviewRenderer>(go);
            var controller = go.GetComponent<AvatarCustomizationController>() ?? go.AddComponent<AvatarCustomizationController>();
            var preview = go.GetComponent<AvatarPreviewRenderer>() ?? go.AddComponent<AvatarPreviewRenderer>();
            Set(controller, "appearance", appearance); Set(preview, "appearance", appearance);
            go.SetActive(true);
        }

        private static void RemoveDuplicates<T>(GameObject go) where T : Component
        {
            var components = go.GetComponents<T>();
            var keep = components.FirstOrDefault(c => PrefabUtility.GetCorrespondingObjectFromSource(c) != null) ?? components.FirstOrDefault();
            foreach (var component in components) if (component != keep) Object.DestroyImmediate(component);
        }

        public static void RepairSceneUI()
        {
            var appearance = Object.FindFirstObjectByType<AvatarAppearance>(FindObjectsInactive.Include);
            var doc = Object.FindObjectsByType<UIDocument>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .First(d => d.visualTreeAsset != null && AssetDatabase.GetAssetPath(d.visualTreeAsset) == "Assets/AttemptUIWithAI/AvatarCreation.uxml");
            ConfigureUI(doc.gameObject, appearance);
            EditorSceneManager.MarkSceneDirty(doc.gameObject.scene);
            EditorSceneManager.SaveScene(doc.gameObject.scene);
        }

        public static void FitRigidHairPrefab()
        {
            var model = Object.FindFirstObjectByType<PlayerStateMachine>(FindObjectsInactive.Include).transform.Find("Model Container");
            var head = model.GetComponentsInChildren<Transform>(true).First(t => t.name == "mixamorig:Head");
            string path = Root + "/Prefabs/Parts/Hair_5.prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) return;
            var sample = Object.Instantiate(prefab, head, false);
            sample.SetActive(true);
            sample.transform.localScale = Vector3.one * 7.5f;
            sample.transform.rotation = model.parent.rotation * Quaternion.Euler(-90, 0, 0);
            var renderer = sample.GetComponentInChildren<Renderer>();
            sample.transform.position += head.position + model.parent.up * 0.35f - renderer.bounds.center;
            sample.name = "Hair_5";
            PrefabUtility.SaveAsPrefabAsset(sample, path);
            Object.DestroyImmediate(sample);
        }

        private static void AddTextures<T>(SerializedObject catalog, string field, SerializedProperty oldList, string folder) where T : AvatarTextureOption
        {
            var textures = new List<Texture2D>();
            for (int i = 0; i < oldList.arraySize; i++) if (oldList.GetArrayElementAtIndex(i).objectReferenceValue is Texture2D texture) textures.Add(texture);
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Resources/Models/" + folder }).OrderBy(g => AssetDatabase.GUIDToAssetPath(g)))
            {
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath(guid));
                if (!textures.Contains(texture)) textures.Add(texture);
            }
            var list = catalog.FindProperty(field); list.arraySize = textures.Count;
            for (int i = 0; i < textures.Count; i++)
            {
                var texture = textures[i];
                var option = GetOption<T>(folder, field + "-" + texture.name.ToLowerInvariant(), texture.name, texture);
                Set(option, "texture", texture); list.GetArrayElementAtIndex(i).objectReferenceValue = option;
            }
        }

        private static void AddParts<T>(SerializedObject catalog, string field, SerializedProperty oldList, string folder, Transform body, Transform skeleton, List<GameObject> oldParts) where T : AvatarPrefabOption
        {
            var list = catalog.FindProperty(field); list.arraySize = oldList.arraySize;
            for (int i = 0; i < oldList.arraySize; i++)
            {
                var source = oldList.GetArrayElementAtIndex(i).objectReferenceValue as GameObject;
                GameObject recovered = null;
                if (source == null && typeof(T) == typeof(HairOption) && i == 5)
                {
                    recovered = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Models/Hair/Hair_5.fbx"));
                    recovered.transform.SetParent(body, false); recovered.transform.localPosition = new Vector3(0, 0, -1.133f);
                    recovered.transform.localRotation = Quaternion.Euler(0, 180, 0); recovered.transform.localScale = Vector3.one;
                    source = recovered;
                }
                if (source == null) throw new InvalidOperationException($"Missing {folder} option {i}");
                if (recovered == null) oldParts.Add(source);
                bool none = source.GetComponentsInChildren<Renderer>(true).Length == 0;
                var label = none ? "None" : source.GetComponentsInChildren<SkinnedMeshRenderer>(true).FirstOrDefault()?.sharedMesh?.name ?? "Hair 5";
                var option = GetOption<T>(folder, field + "-" + i, label, null);
                var serialized = new SerializedObject(option); serialized.FindProperty("unequip").boolValue = none;
                if (!none)
                {
                    string path = Root + "/Prefabs/Parts/" + folder + "_" + i + ".prefab";
                    var clone = Object.Instantiate(source); clone.name = folder + "_" + i;
                    clone.transform.position = source.transform.position; clone.transform.rotation = source.transform.rotation;
                    clone.transform.SetParent(body, true);
                    clone.transform.localScale = source.transform.localScale;
                    bool rigid = clone.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length == 0;
                    if (rigid)
                    {
                        var head = skeleton.GetComponentsInChildren<Transform>(true).First(t => t.name == "mixamorig:Head");
                        clone.transform.SetParent(head, true);
                    }
                    var part = clone.AddComponent<AvatarPart>();
                    var p = new SerializedObject(part); p.FindProperty("attachToHead").boolValue = rigid;
                    var skins = clone.GetComponentsInChildren<SkinnedMeshRenderer>(true); var bindings = p.FindProperty("bindings"); bindings.arraySize = skins.Length;
                    for (int j = 0; j < skins.Length; j++)
                    {
                        var binding = bindings.GetArrayElementAtIndex(j);
                        binding.FindPropertyRelative("renderer").objectReferenceValue = skins[j];
                        binding.FindPropertyRelative("rootBonePath").stringValue = AnimationUtility.CalculateTransformPath(skins[j].rootBone, skeleton);
                        var bones = binding.FindPropertyRelative("bonePaths"); bones.arraySize = skins[j].bones.Length;
                        for (int b = 0; b < bones.arraySize; b++) bones.GetArrayElementAtIndex(b).stringValue = AnimationUtility.CalculateTransformPath(skins[j].bones[b], skeleton);
                        skins[j].bones = new Transform[skins[j].bones.Length]; skins[j].rootBone = null;
                    }
                    p.ApplyModifiedPropertiesWithoutUndo(); clone.SetActive(true);
                    var prefab = PrefabUtility.SaveAsPrefabAsset(clone, path); Object.DestroyImmediate(clone);
                    serialized.FindProperty("prefab").objectReferenceValue = prefab.GetComponent<AvatarPart>();
                }
                else serialized.FindProperty("prefab").objectReferenceValue = null;
                serialized.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(option);
                list.GetArrayElementAtIndex(i).objectReferenceValue = option;
                if (recovered != null) Object.DestroyImmediate(recovered);
            }
        }

        private static T GetOption<T>(string folder, string id, string label, Texture2D preview) where T : AvatarOption
        {
            EnsureFolder(Root + "/Data/Options/" + folder);
            string path = Root + "/Data/Options/" + folder + "/" + id + ".asset";
            var option = AssetDatabase.LoadAssetAtPath<T>(path);
            if (option == null) { option = ScriptableObject.CreateInstance<T>(); AssetDatabase.CreateAsset(option, path); }
            var so = new SerializedObject(option); so.FindProperty("id").stringValue = id; so.FindProperty("displayName").stringValue = label;
            so.FindProperty("preview").objectReferenceValue = preview; so.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(option);
            return option;
        }

        private static void Set(Object target, string field, Object value)
        {
            var so = new SerializedObject(target); so.FindProperty(field).objectReferenceValue = value; so.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(target);
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = Path.GetDirectoryName(path).Replace('\\', '/'); EnsureFolder(parent); AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        private static Shader CreateBodyShader()
        {
            var text = File.ReadAllText("Assets/Materials/BaseBodyMaterial.shadergraph");
            var objects = new List<JObject>();
            using (var reader = new JsonTextReader(new StringReader(text)) { SupportMultipleContent = true })
                while (reader.Read()) if (reader.TokenType == JsonToken.StartObject) objects.Add(JObject.Load(reader));
            var graph = objects[0];
            JObject Find(string id) => objects.First(o => (string)o["m_ObjectId"] == id);
            string NewId() => Guid.NewGuid().ToString("N");
            JObject Clone(string id)
            {
                var clone = (JObject)Find(id).DeepClone(); clone["m_ObjectId"] = NewId(); objects.Add(clone); return clone;
            }
            JObject AddColor(string name, string reference, Color color)
            {
                var property = Clone("c36734c788924e45966f9ac1bb3e131f");
                property["m_Name"] = name; property["m_RefNameGeneratedByDisplayName"] = name;
                property["m_DefaultReferenceName"] = reference; property["m_OverrideReferenceName"] = reference;
                property["m_Guid"]["m_GuidSerialized"] = Guid.NewGuid().ToString();
                property["m_Value"] = new JObject { ["r"] = color.r, ["g"] = color.g, ["b"] = color.b, ["a"] = 1f };
                ((JArray)graph["m_Properties"]).Add(new JObject { ["m_Id"] = property["m_ObjectId"].DeepClone() }); return property;
            }
            var mouth = AddColor("Mouth Color", "_MouthColor", new Color(0.45f, 0.12f, 0.15f));
            Find("58dfa4d2719d4392a172b2f6515d6dc3")["m_Property"]["m_Id"] = mouth["m_ObjectId"].DeepClone();
            var eyeOutline = AddColor("Eye Outline Color", "_EyeOutlineColor", new Color(0.19f, 0.4f, 0.29f));
            Find("7d8a2d2b382649368cc48e347e2d7cdc")["m_Property"]["m_Id"] = eyeOutline["m_ObjectId"].DeepClone();
            var skin = AddColor("Skin Color", "_SkinColor", Color.white);
            var skinNode = Clone("004a364c5a8045ffae2758b0d3d5715b");
            var skinSlot = Clone("1e458fd34d9e43a69fd4fd881291b406");
            skinNode["m_Property"]["m_Id"] = skin["m_ObjectId"].DeepClone(); skinSlot["m_DisplayName"] = "Skin Color";
            skinNode["m_Slots"] = new JArray(new JObject { ["m_Id"] = skinSlot["m_ObjectId"].DeepClone() });
            var multiply = Clone("7e8fcd901ee64860bcfbeb9a85eab60b");
            var slots = new JArray();
            foreach (var id in new[] { "ae2407f9ffc740b39fbd771e07d6b1a8", "c1d637019df6403685522e31be5abdb4", "bda8a7109a9d4eb8866b4e8d7d7f8deb" })
                slots.Add(new JObject { ["m_Id"] = Clone(id)["m_ObjectId"].DeepClone() });
            multiply["m_Slots"] = slots;
            foreach (var node in new[] { skinNode, multiply }) ((JArray)graph["m_Nodes"]).Add(new JObject { ["m_Id"] = node["m_ObjectId"].DeepClone() });
            JObject Edge(string output, int outputSlot, string input, int inputSlot) => new JObject
            {
                ["m_OutputSlot"] = new JObject { ["m_Node"] = new JObject { ["m_Id"] = output }, ["m_SlotId"] = outputSlot },
                ["m_InputSlot"] = new JObject { ["m_Node"] = new JObject { ["m_Id"] = input }, ["m_SlotId"] = inputSlot }
            };
            var edges = (JArray)graph["m_Edges"];
            var existing = edges.First(e => (string)e["m_OutputSlot"]["m_Node"]["m_Id"] == "36c006fefb3949fea8d8be7a50379e0e" && (string)e["m_InputSlot"]["m_Node"]["m_Id"] == "a0f2c466b0884c5baba5c7b3c97b503e");
            existing["m_OutputSlot"]["m_Node"]["m_Id"] = multiply["m_ObjectId"].DeepClone(); existing["m_OutputSlot"]["m_SlotId"] = 2;
            edges.Add(Edge("36c006fefb3949fea8d8be7a50379e0e", 0, (string)multiply["m_ObjectId"], 0));
            edges.Add(Edge((string)skinNode["m_ObjectId"], 0, (string)multiply["m_ObjectId"], 1));
            string path = Root + "/Shaders/AvatarBody.shadergraph";
            File.WriteAllText(path, string.Join("\n\n", objects.Select(o => o.ToString(Formatting.Indented))));
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            return AssetDatabase.LoadAssetAtPath<Shader>(path);
        }

        [MenuItem("Luma/Avatar/Regenerate avatar body shader")]
        public static void RegenerateBodyShader() => CreateBodyShader();
    }
}
