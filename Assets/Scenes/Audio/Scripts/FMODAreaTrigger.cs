using UnityEngine;

public class FMODAreaTrigger : MonoBehaviour
{
    // Set this value per collider in the inspector
    public int areaValue = 0;  // 0 = Spawn, 1 = City, 2 = School, 3 = Pond

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("Entered area: " + areaValue);
            FMODUnity.RuntimeManager.StudioSystem.setParameterByName("Area", areaValue);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("Exited area, returning to Spawn Point default.");
            FMODUnity.RuntimeManager.StudioSystem.setParameterByName("Area", 0);
        }
    }
}
