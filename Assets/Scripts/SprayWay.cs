using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SprayWay : Activity
{
    [Header("References")]
    [SerializeField] private GameObject _gameplayRoot;

    [Header("Cutscene")]
    [SerializeField] private float _time;
    [SerializeField] private string _cutsceneName;
    [SerializeField] private Transform _endTransform;

    private bool _startingCutscenePlayed;

    public float Progress => _progress;
    private float _progress;

    [SerializeField] private List<Transform> Layers = new();
    private int _currentLayer;

    protected override void OnStart()
    {
        PlayerStateMachine.Instance.OnStateChanged += OnStateChanged;

        _gameplayRoot.SetActive(true);
        Debug.Log("Activity started!");

        _startingCutscenePlayed = false;

        var state = PlayerStateMachine.Instance.GetState<CutscenePlayerState>();
        PlayerStateMachine.Instance.ChangeState(state);
    }

    public void AddProgress(float amount)
    {
        _progress += amount;
    }

    private void Restart()
    {
        if (_currentLayer > -1)
        {
            StartCoroutine(HideLayer(_currentLayer));
        }
        PostCutscene();
    }

    private void PostCutscene()
    {
        Debug.Log("Cutscene over");

        _startingCutscenePlayed = true;

        PlayerStateMachine.Instance.ChangeState(PlayerStateMachine.Instance.GetState<SpraywayPlayerState>());

        PlayerStateMachine.Instance.transform.localScale = _endTransform.localScale;
        PlayerStateMachine.Instance.transform.SetPositionAndRotation(_endTransform.position, _endTransform.rotation);
    }

    private void OnStateChanged(PlayerState state)
    {
        if (state is CutscenePlayerState cutsceneState)
        {
            cutsceneState.PlayCutscene(_cutsceneName, PostCutscene);
        }
        if (state is SpraywayPlayerState spraywayState)
        {
            _currentLayer = -1;
            _progress = 0f;

            Debug.Log("SpraywayStart");
            var movement = PlayerStateMachine.Instance.GetComponent<SpraywayMovement>();

            movement.SetSprayWay(this);
            movement.SetWall(_gameplayRoot.transform.Find("Wall"));
            NextLayer();
        }
    }

    public void NextLayer()
    {
        _currentLayer++;
        StartCoroutine(ShowLayer(_currentLayer));
        if(_currentLayer > 0)
        {
            StartCoroutine(HideLayer(_currentLayer - 1));
        }
    }

    private IEnumerator ShowLayer(int layer)
    {
        var transform = Layers[layer];
        var initPos = transform.localPosition.y;

        while (transform.localPosition.y < initPos + 2.85f)
        {
            transform.localPosition += Vector3.up * Time.deltaTime * 2;
            yield return new WaitForEndOfFrame();
        }
    }

    private IEnumerator HideLayer(int layer)
    {
        var transform = Layers[layer];
        var initPos = transform.localPosition.y;

        while (transform.localPosition.y > initPos - 2.85f) {
            transform.localPosition += Vector3.down * Time.deltaTime * 2;
            yield return new WaitForEndOfFrame();
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            Restart();
    }

    protected override void OnStop()
    {
        PlayerStateMachine.Instance.OnStateChanged -= OnStateChanged;

        _gameplayRoot.SetActive(false);
        Debug.Log("Activity stopped!");
    }

    protected override void OnReset()
    {
        Debug.Log("Activity reset!");
    }
}