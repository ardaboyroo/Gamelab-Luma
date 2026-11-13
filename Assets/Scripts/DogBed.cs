using UnityEngine;

/// Put on a dog bed (for example the root of a dog bed furniture prefab).
/// When the player stands still, the dog buddy sometimes walks over and lies down here.
public class DogBed : MonoBehaviour
{
    [Tooltip("Where the dog lies. Leave empty to use the middle of this object.")]
    public Transform restPoint;
    [Tooltip("Extra height above the rest point (e.g. the thickness of the cushion).")]
    public float cushionHeight = 0f;

    public Vector3 RestPosition => (restPoint != null ? restPoint.position : transform.position) + Vector3.up * cushionHeight;
    public Vector3 RestForward => restPoint != null ? restPoint.forward : transform.forward;

    /// False for the see-through 'ghost' while the bed is still being placed (its colliders are off).
    public bool IsAvailable
    {
        get
        {
            if (!isActiveAndEnabled) return false;
            var cols = GetComponentsInChildren<Collider>();
            if (cols.Length == 0) return true;
            foreach (var c in cols) if (c.enabled) return true;
            return false;
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.6f, 0.2f);
        Gizmos.DrawWireSphere(RestPosition, 0.15f);
        Gizmos.DrawLine(RestPosition, RestPosition + RestForward * 0.4f);
    }
#endif
}
