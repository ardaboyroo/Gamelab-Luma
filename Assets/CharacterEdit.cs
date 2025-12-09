using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class CharacterEdit : MonoBehaviour
{
    private GameObject _character;
    private VisualElement _root;

    [Header("Mesh Variants")]
    [SerializeField] private List<GameObject> _topOutfit;
    [SerializeField] private List<GameObject> _pants;
    [SerializeField] private List<GameObject> _shoes;
    [SerializeField] private List<GameObject> _hair;

    [Header("Face Textures")]
    [SerializeField] private List<Texture2D> _mouths;
    [SerializeField] private List<Texture2D> _eyes;
    [SerializeField] private List<Texture2D> _brows;

    [Header("Colors (0–1 RGB)")]
    [SerializeField] private Color _hairColor = Color.white;
    [SerializeField] private Color _topOutfitColor = Color.white;
    [SerializeField] private Color _pantsColor = Color.white;
    [SerializeField] private Color _shoesColor = Color.white;
    [SerializeField] private Color _eyeColor = Color.white;

    // unique material on BaseMesh controlling eyes / mouth / brows, etc
    private Material _faceMaterial;

    private void Awake()
    {
        _root = GetComponent<UIDocument>().rootVisualElement;
    }

    private void Start()
    {
        if (PlayerStateMachine.Instance != null)
            _character = PlayerStateMachine.Instance.gameObject;

        CacheFaceMaterial();

        Subscribe();
        SetCharacterView();
    }

    private void CacheFaceMaterial()
    {
        if (_character == null)
        {
            Debug.LogError("[CharacterEdit] _character is null in CacheFaceMaterial.");
            return;
        }

        Transform baseMesh = _character.transform.Find("Model Container/BaseMesh");
        if (baseMesh == null)
        {
            Debug.LogError("[CharacterEdit] Could not find 'Model Container/BaseMesh' on character.");
            return;
        }

        // IMPORTANT: we want the renderer on BaseMesh itself, not children like Hair/Boots.
        var renderer = baseMesh.GetComponent<Renderer>();
        if (renderer == null)
            renderer = baseMesh.GetComponent<SkinnedMeshRenderer>();

        if (renderer == null)
        {
            Debug.LogError("[CharacterEdit] No Renderer/SkinnedMeshRenderer found on BaseMesh.");
            return;
        }

        _faceMaterial = renderer.material; // per-instance
        if (_faceMaterial == null)
        {
            Debug.LogError("[CharacterEdit] renderer.material is null on BaseMesh.");
        }
        else
        {
            Debug.Log($"[CharacterEdit] Face material cached: {renderer.gameObject.name} -> {_faceMaterial.name}");
            Debug.Log($"[CharacterEdit] Material shader: {_faceMaterial.shader.name}");
        }
    }

    private void Subscribe()
    {
        if (_root == null)
        {
            Debug.LogError("[CharacterEdit] _root is null in Subscribe.");
            return;
        }

        // Mesh variant buttons
        BindVariantButtons("hair", OnHairSelected);
        BindVariantButtons("topPart", OnTopPartSelected);
        BindVariantButtons("pants", OnPantsSelected);
        BindVariantButtons("boots", OnShoesSelected);

        // Face texture buttons
        BindVariantButtons("eye", OnEyeSelected);
        BindVariantButtons("mouth", OnMouthSelected);
        BindVariantButtons("brows", OnBrowsSelected);

        // Color sliders (R/G/B)
        BindHairColorSliders();
        BindTopColorSliders();
        BindPantsColorSliders();
        BindShoesColorSliders();
        BindEyeColorSliders();
    }

    private void SetCharacterView()
    {
        if (_character == null)
        {
            Debug.LogWarning("[CharacterEdit] SetCharacterView() skipped, _character is null.");
            return;
        }

        OnHairSelected(0);
        OnTopPartSelected(0);
        OnPantsSelected(0);
        OnShoesSelected(0);

        ApplyHairColor();
        ApplyTopColor();
        ApplyPantsColor();
        ApplyShoesColor();
        ApplyEyeColor();

        if (_eyes != null && _eyes.Count > 0)
            OnEyeSelected(0);
        if (_mouths != null && _mouths.Count > 0)
            OnMouthSelected(0);
        if (_brows != null && _brows.Count > 0)
            OnBrowsSelected(0);
    }

    #region Button wiring

    private void BindVariantButtons(string prefix, Action<int> onClicked)
    {
        if (onClicked == null || _root == null)
            return;

        string fullPrefix = prefix + "_";

        _root.Query<Button>().ForEach(button =>
        {
            if (string.IsNullOrEmpty(button.name))
                return;

            if (!button.name.StartsWith(fullPrefix, StringComparison.Ordinal))
                return;

            if (!TryExtractIndex(button.name, fullPrefix.Length, out int index))
                return;

            Debug.Log($"[CharacterEdit] Binding button '{button.name}' -> {prefix} index {index}");
            button.clicked += () => onClicked(index);
        });
    }

    private static bool TryExtractIndex(string name, int offset, out int index)
    {
        index = -1;
        if (name.Length <= offset)
            return false;

        string numberPart = name.Substring(offset);
        return int.TryParse(numberPart, out index);
    }

    #endregion

    #region Mesh variant selection

    private void OnHairSelected(int index)
    {
        Debug.Log($"[CharacterEdit] OnHairSelected({index})");
        SetActiveVariant(_hair, index);
        ApplyHairColor();
    }

    private void OnTopPartSelected(int index)
    {
        Debug.Log($"[CharacterEdit] OnTopPartSelected({index})");
        SetActiveVariant(_topOutfit, index);
        ApplyTopColor();
    }

    private void OnPantsSelected(int index)
    {
        Debug.Log($"[CharacterEdit] OnPantsSelected({index})");
        SetActiveVariant(_pants, index);
        ApplyPantsColor();
    }

    private void OnShoesSelected(int index)
    {
        Debug.Log($"[CharacterEdit] OnShoesSelected({index})");
        SetActiveVariant(_shoes, index);
        ApplyShoesColor();
    }

    private static void SetActiveVariant(List<GameObject> list, int index)
    {
        if (list == null || list.Count == 0)
        {
            Debug.LogWarning("[CharacterEdit] SetActiveVariant: list is null or empty.");
            return;
        }

        for (int i = 0; i < list.Count; i++)
        {
            GameObject go = list[i];
            if (go == null)
                continue;

            bool active = (i == index);
            go.SetActive(active);
        }
    }

    #endregion

    #region Face textures

    private void OnEyeSelected(int index)
    {
        if (_faceMaterial == null)
        {
            Debug.LogWarning("[CharacterEdit] OnEyeSelected: _faceMaterial is null.");
            return;
        }

        if (_eyes == null || _eyes.Count == 0)
        {
            Debug.LogWarning("[CharacterEdit] OnEyeSelected: _eyes list is null/empty.");
            return;
        }

        if (index < 0 || index >= _eyes.Count)
        {
            Debug.LogWarning($"[CharacterEdit] OnEyeSelected: index {index} out of range.");
            return;
        }

        Texture2D tex = _eyes[index];
        Debug.Log($"[CharacterEdit] Setting Eye texture index {index}, tex='{(tex ? tex.name : "NULL")}'");

        _faceMaterial.SetTexture("_Eye", tex);
    }

    private void OnMouthSelected(int index)
    {
        if (_faceMaterial == null)
        {
            Debug.LogWarning("[CharacterEdit] OnMouthSelected: _faceMaterial is null.");
            return;
        }

        if (_mouths == null || _mouths.Count == 0)
        {
            Debug.LogWarning("[CharacterEdit] OnMouthSelected: _mouths list is null/empty.");
            return;
        }

        if (index < 0 || index >= _mouths.Count)
        {
            Debug.LogWarning($"[CharacterEdit] OnMouthSelected: index {index} out of range.");
            return;
        }

        Texture2D tex = _mouths[index];
        Debug.Log($"[CharacterEdit] Setting Mouth texture index {index}, tex='{(tex ? tex.name : "NULL")}'");

        _faceMaterial.SetTexture("_Mouth", tex);
    }

    private void OnBrowsSelected(int index)
    {
        if (_faceMaterial == null)
        {
            Debug.LogWarning("[CharacterEdit] OnBrowsSelected: _faceMaterial is null.");
            return;
        }

        if (_brows == null || _brows.Count == 0)
        {
            Debug.LogWarning("[CharacterEdit] OnBrowsSelected: _brows list is null/empty.");
            return;
        }

        if (index < 0 || index >= _brows.Count)
        {
            Debug.LogWarning($"[CharacterEdit] OnBrowsSelected: index {index} out of range.");
            return;
        }

        Texture2D tex = _brows[index];
        Debug.Log($"[CharacterEdit] Setting Brows texture index {index}, tex='{(tex ? tex.name : "NULL")}'");

        _faceMaterial.SetTexture("_Eyebrows", tex);
    }

    #endregion

    #region Color sliders + application
    // (unchanged except for logs in ApplyEyeColor)

    private void BindHairColorSliders()
    {
        var r = _root.Q<Slider>("hair_r");
        var g = _root.Q<Slider>("hair_g");
        var b = _root.Q<Slider>("hair_b");

        if (r != null) r.value = _hairColor.r;
        if (g != null) g.value = _hairColor.g;
        if (b != null) b.value = _hairColor.b;

        if (r != null)
            r.RegisterValueChangedCallback(e =>
            {
                _hairColor.r = e.newValue * 0.01f;
                ApplyHairColor();
            });
        if (g != null)
            g.RegisterValueChangedCallback(e =>
            {
                _hairColor.g = e.newValue * 0.01f;
                ApplyHairColor();
            });
        if (b != null)
            b.RegisterValueChangedCallback(e =>
            {
                _hairColor.b = e.newValue * 0.01f;
                ApplyHairColor();
            });
    }

    private void BindTopColorSliders()
    {
        var r = _root.Q<Slider>("topPart_r");
        var g = _root.Q<Slider>("topPart_g");
        var b = _root.Q<Slider>("topPart_b");

        if (r != null) r.value = _topOutfitColor.r;
        if (g != null) g.value = _topOutfitColor.g;
        if (b != null) b.value = _topOutfitColor.b;

        if (r != null)
            r.RegisterValueChangedCallback(e =>
            {
                _topOutfitColor.r = e.newValue * 0.01f;
                ApplyTopColor();
            });
        if (g != null)
            g.RegisterValueChangedCallback(e =>
            {
                _topOutfitColor.g = e.newValue * 0.01f;
                ApplyTopColor();
            });
        if (b != null)
            b.RegisterValueChangedCallback(e =>
            {
                _topOutfitColor.b = e.newValue * 0.01f;
                ApplyTopColor();
            });
    }

    private void BindPantsColorSliders()
    {
        var r = _root.Q<Slider>("pants_r");
        var g = _root.Q<Slider>("pants_g");
        var b = _root.Q<Slider>("pants_b");

        if (r != null) r.value = _pantsColor.r;
        if (g != null) g.value = _pantsColor.g;
        if (b != null) b.value = _pantsColor.b;

        if (r != null)
            r.RegisterValueChangedCallback(e =>
            {
                _pantsColor.r = e.newValue * 0.01f;
                ApplyPantsColor();
            });
        if (g != null)
            g.RegisterValueChangedCallback(e =>
            {
                _pantsColor.g = e.newValue * 0.01f;
                ApplyPantsColor();
            });
        if (b != null)
            b.RegisterValueChangedCallback(e =>
            {
                _pantsColor.b = e.newValue * 0.01f;
                ApplyPantsColor();
            });
    }

    private void BindShoesColorSliders()
    {
        var r = _root.Q<Slider>("boots_r");
        var g = _root.Q<Slider>("boots_g");
        var b = _root.Q<Slider>("boots_b");

        if (r != null) r.value = _shoesColor.r;
        if (g != null) g.value = _shoesColor.g;
        if (b != null) b.value = _shoesColor.b;

        if (r != null)
            r.RegisterValueChangedCallback(e =>
            {
                _shoesColor.r = e.newValue * 0.01f;
                ApplyShoesColor();
            });
        if (g != null)
            g.RegisterValueChangedCallback(e =>
            {
                _shoesColor.g = e.newValue * 0.01f;
                ApplyShoesColor();
            });
        if (b != null)
            b.RegisterValueChangedCallback(e =>
            {
                _shoesColor.b = e.newValue * 0.01f;
                ApplyShoesColor();
            });
    }

    private void BindEyeColorSliders()
    {
        var r = _root.Q<Slider>("eye_r");
        var g = _root.Q<Slider>("eye_g");
        var b = _root.Q<Slider>("eye_b");

        if (r != null) r.value = _eyeColor.r;
        if (g != null) g.value = _eyeColor.g;
        if (b != null) b.value = _eyeColor.b;

        if (r != null)
            r.RegisterValueChangedCallback(e =>
            {
                _eyeColor.r = e.newValue * 0.01f;
                ApplyEyeColor();
            });
        if (g != null)
            g.RegisterValueChangedCallback(e =>
            {
                _eyeColor.g = e.newValue * 0.01f;
                ApplyEyeColor();
            });
        if (b != null)
            b.RegisterValueChangedCallback(e =>
            {
                _eyeColor.b = e.newValue * 0.01f;
                ApplyEyeColor();
            });
    }

    private void ApplyHairColor() => ApplyColorToList(_hair, _hairColor);
    private void ApplyTopColor() => ApplyColorToList(_topOutfit, _topOutfitColor);
    private void ApplyPantsColor() => ApplyColorToList(_pants, _pantsColor);
    private void ApplyShoesColor() => ApplyColorToList(_shoes, _shoesColor);

    private void ApplyEyeColor()
    {
        if (_faceMaterial == null)
        {
            Debug.LogWarning("[CharacterEdit] ApplyEyeColor: _faceMaterial is null.");
            return;
        }

        Debug.Log($"[CharacterEdit] ApplyEyeColor: {_eyeColor}");

        if (_faceMaterial.HasProperty("_EyeColorA"))
            _faceMaterial.SetColor("_EyeColorA", _eyeColor);
        if (_faceMaterial.HasProperty("_EyeColorB"))
            _faceMaterial.SetColor("_EyeColorB", _eyeColor);
    }

    private static void ApplyColorToList(List<GameObject> list, Color color)
    {
        if (list == null || list.Count == 0)
        {
            Debug.LogWarning("[CharacterEdit] ApplyColorToList: list is null/empty.");
            return;
        }

        foreach (var go in list)
        {
            if (go == null)
                continue;

            var renderers = go.GetComponentsInChildren<Renderer>(true);
            foreach (var renderer in renderers)
            {
                if (renderer == null)
                    continue;

                var mat = renderer.material;
                if (mat == null)
                    continue;

                if (mat.HasProperty("_BaseColor"))
                    mat.SetColor("_BaseColor", color);
                else if (mat.HasProperty("_Color"))
                    mat.SetColor("_Color", color);
            }
        }
    }

    #endregion
}
