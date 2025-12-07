using UnityEngine;

public class FMODAreaTrigger : MonoBehaviour
{
    // Which area this collider represents
    public int areaValue = 0;  // 0 = Spawn, 1 = City, 2 = School, 3 = Pond

    // Volume control (0–1), shown in Inspector
    [Range(0f, 1f)]
    public float exploreVolume = 1f;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {

            // Set the area parameter
            FMODUnity.RuntimeManager.StudioSystem.setParameterByName("Area", areaValue);

            // Set the explore volume parameter
            FMODUnity.RuntimeManager.StudioSystem.setParameterByName("Explore Volume", exploreVolume);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {

            // Reset Area to Spawn (0)
            FMODUnity.RuntimeManager.StudioSystem.setParameterByName("Area", 0);

            // Reset Explore Volume to default (1)
            FMODUnity.RuntimeManager.StudioSystem.setParameterByName("Explore Volume", 1f);
        }
    }
}
