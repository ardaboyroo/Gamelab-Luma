using UnityEngine;

public class MenuPlayerState : PlayerState
{
    [SerializeField]
    private string SnapshotPath = "snapshot:/PauseMenu";
    private FMOD.Studio.EventInstance _pauseMenuSnapshot;


    public override void OnEnter()
    {
        base.OnEnter();

        StartSnapshot();
    }

    public override void OnExit()
    {
        base.OnExit();
        StopSnapshot();
    }

    public void StartSnapshot()
    {
        _pauseMenuSnapshot = FMODUnity.RuntimeManager.CreateInstance(SnapshotPath);
        _pauseMenuSnapshot.start();
        Debug.Log("[FMOD] Snapshot started: " + SnapshotPath);
    }

    public void StopSnapshot()
    {
        _pauseMenuSnapshot.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
        _pauseMenuSnapshot.release();
        _pauseMenuSnapshot = default;

        Debug.Log("[FMOD] Snapshot stopped: " + SnapshotPath);
    }

    // Extra logic for this state if you need it later.
    // For now OnEnter/OnExit can stay inherited.
}
