using UnityEngine;

public class SpraywayHazard : MonoBehaviour
{
    [SerializeField] private SprayWay _sprayway;   // Reference to main SprayWay controller
    [SerializeField] private string _playerTag = "Player";

    private void OnTriggerEnter(Collider other)
    {
        // Only react to the player
        if (!other.CompareTag(_playerTag))
            return;

        // Try to get movement component on the player
        var movement = other.GetComponent<SpraywayMovement>();
        if (movement != null)
        {
            movement.Stop();
        }
        else
        {
            Debug.LogWarning("SpraywayHazard: Player entered hazard but has no SpraywayMovement component.");
        }

        if (_sprayway != null)
        {
            _sprayway.GameOver();
        }
        else
        {
            Debug.LogWarning("SpraywayHazard: _sprayway reference not set in Inspector.");
        }
    }
}