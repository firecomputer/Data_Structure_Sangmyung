using UnityEngine;
using UnityEngine.UI;

namespace AlgorithmOfDelivery.UI
{
    public class StartMenuUI : MonoBehaviour
    {
        [SerializeField] private Button _startButton;

        private GameSetup _gameSetup;
        private bool _initialized;

        public void Initialize(GameSetup gameSetup, Button startButton)
        {
            _gameSetup = gameSetup;
            _startButton = startButton;

            if (_initialized)
                return;

            _initialized = true;

            if (_startButton != null)
            {
                _startButton.onClick.RemoveAllListeners();
                _startButton.onClick.AddListener(OnStartButtonClicked);
            }
        }

        public void OnStartButtonClicked()
        {
            if (_gameSetup != null)
                _gameSetup.BeginGame();
        }

        public void Show()
        {
            if (!gameObject.activeSelf)
                gameObject.SetActive(true);

            transform.SetAsLastSibling();
        }

        public void Hide()
        {
            if (gameObject.activeSelf)
                gameObject.SetActive(false);
        }
    }
}
