using System;
using UnityEngine;

public class CutscenePlayerState : PlayerState
{
    [SerializeField] private float _timeScale = 1f;

    private Animator _animator;
    private Action _onCutsceneOver;
    private bool _playingCutscene;
    private int _currentHash;

    public override void OnEnter()
    {
        _animator = GetComponent<Animator>();
    }

    public override void OnExit()
    {
        _playingCutscene = false;
        _onCutsceneOver = null;
    }

    /// <summary>
    /// Plays a cutscene animation and invokes callback when finished.
    /// </summary>
    public void PlayCutscene(string clipName, Action onCutsceneOver)
    {
        _onCutsceneOver = onCutsceneOver;
        _currentHash = Animator.StringToHash(clipName);

        _animator.speed = _timeScale;
        _animator.Play(_currentHash, 0, 0f);

        _playingCutscene = true;
    }

    private void Update()
    {
        if (!_playingCutscene)
            return;

        var stateInfo = _animator.GetCurrentAnimatorStateInfo(0);

        // Check if we are still in the same cutscene clip
        if (stateInfo.shortNameHash != _currentHash)
            return;

        // Animation finished if normalized time >= 1.0
        if (stateInfo.normalizedTime >= 1f)
            EndCutscene();
    }

    private void EndCutscene()
    {
        _playingCutscene = false;

        var callback = _onCutsceneOver;
        _onCutsceneOver = null;

        callback?.Invoke();
    }
}