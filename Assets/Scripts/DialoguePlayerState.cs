using UnityEngine;

public class DialoguePlayerState : PlayerState
{
    [Header("Rotate Toward Camera")]
    [SerializeField] private float _rotateSpeed = 180f;

    [SerializeField]
    private string SnapshotPath = "snapshot:/PauseMenu";
    private FMOD.Studio.EventInstance _pauseMenuSnapshot;

    private Transform _player;
    private Transform _camera;

    private bool _active;

    public override void OnEnter()
    {
        _camera = Camera.main.transform;
        _player = transform;

        var animator = transform.Find("Model Container").GetComponent<Animator>();
        animator.SetBool("mid_air", false);
        animator.SetFloat("speed", 0);

        _active = true;


        StartSnapshot();
    }

    public override void OnExit()
    {
        _active = false;

        StopSnapshot();
    }

    private void Update()
    {
        if (!_active || _camera == null)
            return;

        RotateToCamera();
    }

    private void RotateToCamera()
    {
        Vector3 dir = _camera.position - _player.position;
        dir.y = 0f;

        if (dir.sqrMagnitude < 0.01f)
            return;

        Quaternion targetRot = Quaternion.LookRotation(dir.normalized, Vector3.up);

        _player.rotation = Quaternion.RotateTowards(
            _player.rotation,
            targetRot,
            _rotateSpeed * Time.deltaTime
        );
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

        Debug.Log("[FMOD] Snapshot stopped: " + SnapshotPath);

        _pauseMenuSnapshot = default;
    }
}