using System.Collections.Generic;
using UnityEngine;

public class FmodHipHop : MonoBehaviour
{
    public static FmodHipHop Instance;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        DontDestroyOnLoad(gameObject);
    }

    [Header("FMOD Event")]
    public string eventPath = "event:/Music/Sprayway_Music";
    private FMOD.Studio.EventInstance musicInstance;

    [Header("Parameters (LOCAL)")]
    public int gameState = 0;
    public float intensity = 15f;
    public int gameEndState = 2;

    public bool gameIsRunning = false;

    // ⭐ NEW: list of currently active zones
    private readonly List<HipHopMusicZone> activeZones = new();

    private void Start()
    {
        Debug.Log("[FMOD DEBUG] Creating event instance...");
        musicInstance = FMODUnity.RuntimeManager.CreateInstance(eventPath);
    }

    // -------------------------------------------------------------------
    // APPLY PARAMETERS
    // -------------------------------------------------------------------
    private void ApplyLocalParameters()
    {
        Debug.Log($"[FMOD DEBUG] Applying parameters → GS:{gameState} INT:{intensity} END:{gameEndState}");

        musicInstance.setParameterByName("GameState", gameState);
        musicInstance.setParameterByName("Intensity", intensity);
        musicInstance.setParameterByName("Game End", gameEndState);
    }

    // -------------------------------------------------------------------
    // ENTER ZONE
    // -------------------------------------------------------------------
    public void ColliderSetParameters(int state, float intens, int end, HipHopMusicZone zone)
    {
        if (gameIsRunning)
        {
            Debug.Log("[FMOD DEBUG] Ignoring collider because game is running.");
            return;
        }

        if (!activeZones.Contains(zone))
            activeZones.Add(zone);

        // Start music if stopped
        musicInstance.getPlaybackState(out var playback);
        if (playback == FMOD.Studio.PLAYBACK_STATE.STOPPED)
            musicInstance.start();

        gameState = state;
        intensity = intens;
        gameEndState = end;

        ApplyLocalParameters();
    }

    // -------------------------------------------------------------------
    // EXIT ZONE
    // -------------------------------------------------------------------
    public void ResetHipHopToIdle(HipHopMusicZone zone)
    {
        if (activeZones.Contains(zone))
            activeZones.Remove(zone);

        // If MAIN zone exited → stop everything
        if (zone.isMainZone)
        {
            Debug.Log("[FMOD DEBUG] Left MAIN zone → stopping music");
            musicInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            return;
        }

        // Restore last active zone, if any
        if (activeZones.Count > 0)
        {
            HipHopMusicZone top = activeZones[activeZones.Count - 1];
            Debug.Log("[FMOD DEBUG] Restoring zone → intensity " + top.intensity);

            gameState = top.gameState;
            intensity = top.intensity;
            gameEndState = top.gameEndState;

            ApplyLocalParameters();
        }
        else
        {
            Debug.Log("[FMOD DEBUG] No zones left except MAIN (which we did not exit) — doing nothing.");
        }
    }

    // -------------------------------------------------------------------
    // MANUAL PARAMETER SETTERS
    // -------------------------------------------------------------------
    public void SetIntensity(float val)
    {
        Debug.Log("[FMOD DEBUG] SetIntensity(" + val + ")");
        intensity = val;
        musicInstance.setParameterByName("Intensity", intensity);
    }

    public void SetGameState(int val)
    {
        Debug.Log("[FMOD DEBUG] SetGameState(" + val + ")");
        gameState = val;
        musicInstance.setParameterByName("GameState", gameState);
    }

    public void SetGameEnd(int val)
    {
        Debug.Log("[FMOD DEBUG] SetGameEnd(" + val + ")");
        gameEndState = val;
        musicInstance.setParameterByName("Game End", gameEndState);
    }

    private void OnDestroy()
    {
        Debug.Log("[FMOD DEBUG] Destroying music instance.");
        musicInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
        musicInstance.release();
    }
}
