using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

public class SprayWay : Activity
{
    [Header("References")]
    [SerializeField] private GameObject _gameplayRoot;
    [SerializeField] private GraphittiProgress _graphittiProgress;
    [SerializeField] private UIDocument _gameOver, _gameWin;

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
        _finished = false;

        GameEndCamera.SetActive(false);
        NormalCamera.SetActive(true);

        _gameplayRoot.SetActive(true);
        Debug.Log("Activity started directly!");

        _graphittiProgress.SetMaskFromValue(0);
        _progress = 0f;
        _currentLayer = -1;

        // Initialize player movement directly
        var movement = PlayerStateMachine.Instance.GetComponent<SpraywayMovement>();
        if (movement != null)
        {
            movement.Ressurect();
            movement.SetSprayWay(this);
            movement.SetWall(_gameplayRoot.transform.Find("Wall"));
        }

        NextLayer();
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

        _finished = false;
        _currentLayer = -1;
        _graphittiProgress.SetMaskFromValue(0);
        _progress = 0f;

        GameEndCamera.SetActive(false);
        NormalCamera.SetActive(true);
        PlayerStateMachine.Instance.GetComponent<SpraywayMovement>().Ressurect();
        NextLayer();
    }

    public void NextLayer()
    {
        _currentLayer++;
        if (_currentLayer < Layers.Count)
        {
            StartCoroutine(ShowLayer(_currentLayer));
        }

        for (int i = 0; i < Layers.Count; i++)
        {
            if (i == _currentLayer)
                continue;
            StartCoroutine(HideLayer(i));
        }
    }

    private IEnumerator ShowLayer(int layer)
    {
        if (layer >= Layers.Count) yield break;
        var layerTransform = Layers[layer];
        var initPos = -6;

        while (layerTransform.localPosition.y < initPos + 6f)
        {
            layerTransform.localPosition += Vector3.up * Time.deltaTime * 6;
            yield return new WaitForEndOfFrame();
        }
    }

    private IEnumerator HideLayer(int layer)
    {
        if (layer >= Layers.Count) yield break;
        var layerTransform = Layers[layer];
        var initPos = 0;

        while (layerTransform.localPosition.y > initPos - 6)
        {
            layerTransform.localPosition += Vector3.down * Time.deltaTime * 6;
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
        HideUI();
        Restart();
    }

    private void OnCloseClicked()
    {
        _gameplayRoot.SetActive(false);
        _finished = false;
        StopActivity();

        var sprayParticles = PlayerStateMachine.Instance.transform.Find("Model Container").Find("BaseMesh").Find("Spraycan").Find("SprayWayParticleSystem").GetComponent<ParticleSystem>();
        sprayParticles.transform.parent.GetComponent<Renderer>().enabled = false;

        HideUI();
    }
}