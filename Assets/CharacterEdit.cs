using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class CharacterEdit : MonoBehaviour
{
    private GameObject _character;
    private VisualElement _root;

    [SerializeField] private List<GameObject> _topOutfit;
    [SerializeField] private List<GameObject> _pants;
    [SerializeField] private List<GameObject> _shoes;
    [SerializeField] private List<GameObject> _hair;
    [SerializeField] private List<Texture2D> _mouths;
    [SerializeField] private List<Texture2D> _eyes;
    [SerializeField] private List<Texture2D> _brows;
    [SerializeField] private Color _hairColor;
    [SerializeField] private Color _topOutfitColor;
    [SerializeField] private Color _pantsColor;
    [SerializeField] private Color _shoesColor;
    [SerializeField] private Color _eyeColor;

    private void Awake()
    {
        _root = GetComponent<UIDocument>().rootVisualElement;
        _character = PlayerStateMachine.Instance.gameObject;
    }

    private void Subscribe()
    {

    }

    private void Unsubscribe()
    {

    }

    private void SetCharacterView()
    {
        if (_character == null)
            return;
    }
}
