using UnityEngine;
using FMODUnity; // Required for FMOD

public class FootstepDistanceTrigger : MonoBehaviour
{
    [Header("FMOD Settings")]
    // The FMOD Event to play
    public EventReference footstepEvent;

    [Header("Step Settings")]
    [Tooltip("How far the player moves before a step triggers (in Unity Units/Meters)")]
    public float stepDistance = 1.8f;

    // Internal variables to track movement
    private float currentStepTracker = 0f;
    private Vector3 lastPosition;

    private void Start()
    {
        // Initialize position to prevent an instant step on start
        lastPosition = transform.position;
    }

    private void Update()
    {
        CheckMovement();
    }

    private void CheckMovement()
    {
        // 1. Calculate how far we moved since the last frame
        float distanceMoved = Vector3.Distance(transform.position, lastPosition);

        // 2. Add this to our tracker
        if (distanceMoved > 0)
        {
            currentStepTracker += distanceMoved;
        }

        // 3. Check if we have moved enough to trigger a step
        if (currentStepTracker >= stepDistance)
        {
            PlayFootstep();
            // Reset the tracker (subtracting stepDistance keeps it accurate if we moved too far)
            currentStepTracker = 0f;
        }

        // 4. Update the last position for the next frame
        lastPosition = transform.position;
    }

    private void PlayFootstep()
    {
        // Plays the sound at the player's feet
        if (!footstepEvent.IsNull)
        {
            RuntimeManager.PlayOneShot(footstepEvent, transform.position);
        }
    }
}