using UnityEngine;
using UnityEngine.UI;
using AlgorithmOfDelivery.Game;

namespace AlgorithmOfDelivery.UI
{
    public class TopStatusBarUI : MonoBehaviour
    {
        [SerializeField] private Text _moneyText;
        [SerializeField] private Text _timeText;
        [SerializeField] private Text _dayText;

        public void Configure(Text moneyText, Text timeText, Text dayText)
        {
            _moneyText = moneyText;
            _timeText = timeText;
            _dayText = dayText;
            UpdateDisplay();
        }

        private void Update()
        {
            UpdateDisplay();
        }

        private void UpdateDisplay()
        {
            var gm = GameManager.Instance;
            if (gm == null)
                return;

            var cm = CourierManager.Instance;
            if (_moneyText != null)
            {
                float money = cm != null ? cm.TotalMoney : 0f;
                _moneyText.text = money.ToString("N0");
            }

            if (_timeText != null)
            {
                float displaySeconds;
                switch (gm.State)
                {
                    case GameState.Playing:
                    case GameState.Paused:
                        displaySeconds = gm.DayTimer;
                        break;
                    case GameState.DayEnd:
                        displaySeconds = 0f;
                        break;
                    default:
                        displaySeconds = gm.DayDuration;
                        break;
                }

                _timeText.text = FormatCountdown(displaySeconds);
            }

            if (_dayText != null)
                _dayText.text = $"D-{Mathf.Max(1, gm.DayCount)}";
        }

        private string FormatCountdown(float seconds)
        {
            seconds = Mathf.Max(0f, seconds);
            int totalSeconds = Mathf.CeilToInt(seconds);
            int minutes = totalSeconds / 60;
            int remainingSeconds = totalSeconds % 60;
            return $"{minutes:00}:{remainingSeconds:00}";
        }
    }
}
