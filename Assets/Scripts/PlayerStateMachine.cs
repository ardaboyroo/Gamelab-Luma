using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

[DisallowMultipleComponent]
public class PlayerStateMachine : MonoBehaviour
{
    public event Action<PlayerState> OnStateChanged;
    public static PlayerStateMachine Instance { get; private set; }

    [Header("States attached to this Player")]
    [SerializeField] private List<PlayerState> _states = new List<PlayerState>();
    [SerializeField] private PlayerState _initialState;

    private PlayerState _currentState;
    private PlayerState _pendingState;
    private bool _isApplyingState;

    public PlayerState Current => _currentState;

    private static readonly string[] _ignoredTransformProps =
    {
        "m_LocalPosition",
        "m_LocalRotation",
        "m_LocalScale",
        "m_Position",
        "m_Rotation"
    };

    private void Awake()
    {
        Instance = this;

        foreach (var s in _states)
        {
            if (s != null)
                s.Initialize(this);
        }

        if (_initialState != null)
            ChangeState(_initialState);
    }

    public void ChangeState(PlayerState newState)
    {
        if (newState == null || newState == _currentState)
            return;

        if (_isApplyingState)
            return;

        _pendingState = newState;
        StartCoroutine(ApplyStateRoutine());
    }

    private System.Collections.IEnumerator ApplyStateRoutine()
    {
        _isApplyingState = true;

        // exit old state
        _currentState?.OnExit();

        // 1) destroy all dynamic components (deferred)
        foreach (var comp in GetComponents<Component>())
        {
            if (comp is Transform) continue;
            if (comp == this) continue;
            if (comp is PlayerState) continue;

            Destroy(comp);
        }

        // 2) wait until destruction actually happens
        yield return new WaitForEndOfFrame();

        // 3) copy components from the pending state's prefab
        var pendingStateType = _pendingState.GetType();
        ApplyComponentPreset(_pendingState);

        // 4) enter new state
        _pendingState = GetComponent(pendingStateType) as PlayerState;
        _currentState = _pendingState;
        _currentState?.OnEnter();

        _pendingState = null;
        _isApplyingState = false;

        OnStateChanged?.Invoke(_currentState);

    }

    public T GetState<T>() where T : PlayerState
    {
        foreach (var s in _states)
            if (s is T typed) return typed;
        return null;
    }

    /// <summary>
    /// Copy components from state's prefab onto this GameObject.
    /// </summary>
    private void ApplyComponentPreset(PlayerState state)
    {
        var prefab = state.ComponentListPrefab;
        if (prefab == null)
            return;

        Dictionary<Component, Component> remap = new Dictionary<Component, Component>();

        // 1) Add and copy all components
        foreach (var src in prefab.GetComponents<Component>())
        {
            if (src is Transform) continue;

            // If a component of this type already exists, skip (the state system will handle this)
            if (GetComponent(src.GetType()) != null)
                continue;

            var clone = gameObject.AddComponent(src.GetType());
            CopySerialized(src, clone);

            remap.Add(src, clone);
        }

        foreach (var pair in remap)
            FixComponentReferences(pair.Value, remap);
    }

    /// <summary>
    /// Copy all (serializable) instance fields from src to dst.
    /// </summary>
    private static void CopySerialized(Component src, Component dst)
    {
        if (src is Transform || dst is Transform)
            return;

        var srcSO = new SerializedObject(src);
        var dstSO = new SerializedObject(dst);

        var prop = srcSO.GetIterator();
        bool enterChildren = true;

        while (prop.NextVisible(enterChildren))
        {
            if (prop.name == "m_ObjectHideFlags")
                continue;

            if (Array.Exists(_ignoredTransformProps, ig => ig == prop.name))
                continue;

            dstSO.CopyFromSerializedProperty(prop);
            enterChildren = false;
        }

        dstSO.ApplyModifiedPropertiesWithoutUndo();
    }

    private void FixComponentReferences(Component target, Dictionary<Component, Component> remap)
    {
        if (target is Transform)
            return;

        var so = new SerializedObject(target);
        var prop = so.GetIterator();
        bool enterChildren = true;

        while (prop.NextVisible(enterChildren))
        {
            enterChildren = false;

            if (prop.propertyType == SerializedPropertyType.ObjectReference)
            {
                UnityEngine.Object reference = prop.objectReferenceValue;

                if (reference is Transform)
                    continue;

                if (reference is Component prefabComp && remap.TryGetValue(prefabComp, out Component replacement))
                {
                    prop.objectReferenceValue = replacement;
                }
            }
        }

        so.ApplyModifiedPropertiesWithoutUndo();
    }
}