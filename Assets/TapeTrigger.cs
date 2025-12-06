using UnityEngine;

public class TapeTrigger : MonoBehaviour
{
    [Header("Detection")]
    [SerializeField] private string playerTag = "Player";

    [Header("Roller hierarchy")]
    [SerializeField] private Transform roller;   // exact child named "Roller"
    [SerializeField] private GameObject warning; // child named "Warning"

    [Header("Movement")]
    [SerializeField] private Vector3 moveDirection = Vector3.right;
    [SerializeField] private float moveSpeed = 2f;

    private bool _moveRoller;

    private void Reset()
    {
        // Try to auto-wire children by exact name (case-sensitive)
        if (roller == null)
            roller = transform.root.Find("roller/Roller"); // adjust path if needed

        if (warning == null && roller != null)
            warning = roller.Find("Warning")?.gameObject;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag))
            return;

        if (warning != null)
            warning.SetActive(true);

        _moveRoller = false; // just in case player re-enters
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(playerTag))
            return;

        if (warning != null)
            warning.SetActive(false);

        // start moving the Roller after the player leaves
        _moveRoller = true;
    }

    private void Update()
    {
        if (!_moveRoller || roller == null)
            return;

        // linear, constant-speed motion in the given direction
        roller.position += moveDirection.normalized * moveSpeed * Time.deltaTime;
    }
}
