using UnityEngine;

public abstract class PlayerState : MonoBehaviour
{
    [Header("Preset for this state")]
    [Tooltip("Prefab that contains ONLY the components you want active in this state." +
             " Its Transform is ignored; only components are copied.")]
    [SerializeField] private GameObject _componentListPrefab;

    protected PlayerStateMachine _machine;

    public GameObject ComponentListPrefab => _componentListPrefab;

    internal void Initialize(PlayerStateMachine machine)
    {
        _machine = machine;
    }

    public virtual void OnEnter() { }
    public virtual void OnExit() { }
}
