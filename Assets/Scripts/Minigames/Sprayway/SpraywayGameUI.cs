using UnityEngine;
using UnityEngine.UIElements;

namespace Minigames.Sprayway
{
    public class SpraywayGameUI : MonoBehaviour
    {
        private static SpraywayGameUI _instance;
        private VisualElement _root;
        private Button _retryBtn, _hubBtn;
        private Label _title;

        private SpraywayGameManager _manager;

        private void Awake()
        {
            _instance = this;

            _root = GetComponent<UIDocument>().rootVisualElement;
            _title = _root.Q<Label>("Title");
            _retryBtn = _root.Q<Button>("RetryButton");
            _hubBtn = _root.Q<Button>("HubButton");

            _root.visible = false;
        }

        public static void Initialize(SpraywayGameManager manager)
        {
            if (_instance._manager != null) 
            {
                Debug.Log("unsubscribing");
                _instance._retryBtn.clicked -= () => _instance._manager.RetryServerRpc();
                _instance._hubBtn.clicked -= () => _instance._manager.ReturnToHubServerRpc();
            }

            _instance._manager = manager;
            Debug.Log("subscribing");
            _instance._retryBtn.clicked += () => _instance._manager.RetryServerRpc();
            _instance._hubBtn.clicked += () => _instance._manager.ReturnToHubServerRpc();
        }

        public static void ShowGameOver()
        {
            _instance._title.text = "Game Over!";

            _instance._retryBtn.visible = true;
            _instance._root.visible = true;
        }

        public static void ShowGameWin()
        {
            _instance._title.text = "You Win!";
            _instance._retryBtn.visible = false;
            _instance._root.visible = true;
        }

        public static void Hide()
        {
            _instance._root.visible = false;
            _instance._retryBtn.visible = false;
        }
    }
}
