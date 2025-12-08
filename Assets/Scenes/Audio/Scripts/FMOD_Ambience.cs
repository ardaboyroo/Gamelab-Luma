using UnityEngine;
using System.Collections;


public class AmbienceAreaTrigger : MonoBehaviour
{
    [Header("Ambience Settings")]
    [Range(0f, 1f)]
    public float ambienceIntensity = 0.2f;

    [Range(0f, 1f)]
    public float exitResetIntensity = 0.2f;

    [Tooltip("How long the fade should take (in seconds)")]
    public float fadeTime = 2f;

    private Coroutine fadeRoutine;

    private void FadeTo(float target)
    {
        // stop previous fade if still running
        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);

        fadeRoutine = StartCoroutine(FadeAmbience(target));
    }

    private IEnumerator FadeAmbience(float target)
    {
        FMODUnity.RuntimeManager.StudioSystem.getParameterByName("Ambience_Intensity", out float start);

        float t = 0f;
        while (t < fadeTime)
        {
            t += Time.deltaTime;
            float value = Mathf.Lerp(start, target, t / fadeTime);

            FMODUnity.RuntimeManager.StudioSystem
                .setParameterByName("Ambience_Intensity", value);

            yield return null;
        }

        FMODUnity.RuntimeManager.StudioSystem
            .setParameterByName("Ambience_Intensity", target);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            FadeTo(ambienceIntensity);
            Debug.Log("[AMBIENCE] Enter zone → fade to " + ambienceIntensity);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            FadeTo(exitResetIntensity);
            Debug.Log("[AMBIENCE] Exit zone → fade to " + exitResetIntensity);
        }
    }
}
