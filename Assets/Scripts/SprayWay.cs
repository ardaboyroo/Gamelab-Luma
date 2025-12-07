using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using Unity.Android.Gradle.Manifest;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

public class SprayWay : Activity
{
    [Header("References")]
    [SerializeField] private GameObject _gameplayRoot;
    [SerializeField] private GraphittiProgress _graphittiProgress;
    [SerializeField] private UIDocument _gameOver, _gameWin;

    [Header("Cutscene")]
    [SerializeField] private float _time;
    [SerializeField] private string _cutsceneName;
    [SerializeField] private Transform _endTransform;
    [SerializeField] private Transform _initTransform;

    private bool _startingCutscenePlayed;

    public float Progress => _progress;
    private float _progress;

    [SerializeField] private List<Transform> Layers = new();
    private int _currentLayer;

    [SerializeField] private GameObject NormalCamera;
    [SerializeField] private GameObject GameEndCamera;

    private bool _finished;

    private void Awake()
    {
        if (_gameWin != null)
            _gameWin.rootVisualElement.style.display = DisplayStyle.None;

        if (_gameOver != null)
            _gameOver.rootVisualElement.style.display = DisplayStyle.None;
    }

    protected override void OnStart()
    {
        // Start Music 
        _finished = false;

        GameEndCamera.SetActive(false);
        NormalCamera.SetActive(true);
        PlayerStateMachine.Instance.OnStateChanged += OnStateChanged;

        _gameplayRoot.SetActive(true);
        Debug.Log("Activity started!");

        _startingCutscenePlayed = false;

        var state = PlayerStateMachine.Instance.GetState<CutscenePlayerState>();
        PlayerStateMachine.Instance.ChangeState(state);
        _graphittiProgress.SetMaskFromValue(0);
    }

    public void AddProgress(float amount)
    {
        _progress += amount;
        _graphittiProgress.SetMaskFromValue(_progress);

        if (_progress >= 1)
            GameWin();
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
            _finished = false;
            _currentLayer = -1;
            _graphittiProgress.SetMaskFromValue(0);
            _progress = 0f;

            GameEndCamera.SetActive(false);
            NormalCamera.SetActive(true);
            PlayerStateMachine.Instance.GetComponent<SpraywayMovement>().Ressurect();

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

        for (int i = 0; i < Layers.Count; i++)
        {
            if (i == _currentLayer)
                continue;
            StartCoroutine(HideLayer(i));
        }
    }

    private IEnumerator ShowLayer(int layer)
    {
        var transform = Layers[layer];
        var initPos = -6;

        while (transform.localPosition.y < initPos + 6f)
        {
            transform.localPosition += Vector3.up * Time.deltaTime * 6;
            yield return new WaitForEndOfFrame();
        }
    }

    private IEnumerator HideLayer(int layer)
    {
        var transform = Layers[layer];
        var initPos = 0;

        while (transform.localPosition.y > initPos - 6) {
            transform.localPosition += Vector3.down * Time.deltaTime * 6;
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

    public void GameOver()
    {
        FmodHipHop.Instance.SetGameEnd(1); // lose
        FmodHipHop.Instance.gameIsRunning = false;
        if (_finished)
            return;
        StartCoroutine(ShowGraffiti(false));
    }

    public void GameWin()
    {
        FmodHipHop.Instance.SetGameEnd(0); // win
        FmodHipHop.Instance.gameIsRunning = false;
        if (_finished)
            return;
        StartCoroutine(ShowGraffiti(true));
    }

    private IEnumerator ShowGraffiti(bool win)
    {
        _finished = true;
        PlayerStateMachine.Instance.GetComponent<SpraywayMovement>().Stop();
        NormalCamera.SetActive(false);
        GameEndCamera.SetActive(true);

        yield return new WaitForSeconds(6f);

        if (win)
        {
            ShowUI(true);
        }
        else
        {
            ShowUI(false);
        }
    }

    private void ShowUI(bool win)
    {
        if (win)
        {

            if (_gameWin == null) return;
            var root = _gameWin.rootVisualElement;
            root.style.display = DisplayStyle.Flex;

            var startBtn = root.Q<Button>("restart-btn");
            var closeBtn = root.Q<Button>("close-btn");

            startBtn.clicked += OnRestartClicked;
            closeBtn.clicked += OnCloseClicked;
        }
        else
        {
            if (_gameOver == null) return;
            var root = _gameOver.rootVisualElement;
            root.style.display = DisplayStyle.Flex;

            var startBtn = root.Q<Button>("restart-btn");
            var closeBtn = root.Q<Button>("close-btn");

            startBtn.clicked += OnRestartClicked;
            closeBtn.clicked += OnCloseClicked;
        }
    }

    private void HideUI()
    {
        if (_gameOver != null)
        {
            var root = _gameOver.rootVisualElement;

            root.Q<Button>("restart-btn").clicked -= OnRestartClicked;
            root.Q<Button>("close-btn").clicked -= OnCloseClicked;

            root.style.display = DisplayStyle.None;
        }

        if (_gameWin != null)
        {
            var root = _gameWin.rootVisualElement;

            root.Q<Button>("restart-btn").clicked -= OnRestartClicked;
            root.Q<Button>("close-btn").clicked -= OnCloseClicked;

            root.style.display = DisplayStyle.None;
        }
    }

    private void OnRestartClicked()
    {
        //FmodHipHop.Instance.ResetToIdleHipHop();
        //FmodHipHop.Instance.gameIsRunning = false;
        HideUI();
        Restart();
    }

    private void OnCloseClicked()
    {
        _gameplayRoot.SetActive(false);

        var sprayParticles = PlayerStateMachine.Instance.transform.Find("Model Container").Find("BaseMesh").Find("Spraycan").Find("SprayWayParticleSystem").GetComponent<ParticleSystem>();
        sprayParticles.transform.parent.GetComponent<Renderer>().enabled = false;

        PlayerStateMachine.Instance.ChangeState(PlayerStateMachine.Instance.GetState<StandardPlayerState>());
        PlayerStateMachine.Instance.transform.localScale = _initTransform.localScale;
        PlayerStateMachine.Instance.transform.SetPositionAndRotation(_initTransform.position, _initTransform.rotation);
        
        HideUI();
    }
}
