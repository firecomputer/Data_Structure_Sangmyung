using UnityEngine;
using UnityEngine.UI;
using AlgorithmOfDelivery.Core;
using AlgorithmOfDelivery.Game;

namespace AlgorithmOfDelivery.UI
{
    public class DashboardUI : MonoBehaviour
    {
        [Header("Basic Fields")]
        [SerializeField] private Text _speedText;
        [SerializeField] private Text _positionText;
        [SerializeField] private Text _moneyText;
        [SerializeField] private Text _zoneText;

        [Header("Speed Display")]
        [SerializeField] private float _currentSpeed;
        [SerializeField] private float _maxSpeed = 120f;

        [Header("Position Display")]
        [SerializeField] private Vector2 _currentPosition;

        [Header("Money Display")]
        [SerializeField] private int _currentMoney;

        [Header("Zone Display")]
        [SerializeField] private int _currentZone = 1;
        [SerializeField] private int _totalZones = 10;

        [Header("Extended Fields")]
        [SerializeField] private Text _fatigueText;
        [SerializeField] private Slider _fatigueSlider;
        [SerializeField] private Text _courierNameText;
        [SerializeField] private Text _traitsText;
        [SerializeField] private Text _dayText;
        [SerializeField] private Text _timerText;

        private int _selectedCourierIndex;

        private void Start()
        {
            UpdateGameData();
            UpdateAllDisplays();
        }

        private void Update()
        {
            UpdateGameData();
            UpdateAllDisplays();
        }

        private void UpdateGameData()
        {
            var gm = GameManager.Instance;
            if (gm != null)
            {
                _selectedCourierIndex = gm.SelectedCourierIndex;

                if (_dayText != null)
                    _dayText.text = $"Day: {gm.DayCount}";
                if (_timerText != null)
                    _timerText.text = $"Time: {gm.DayTimer:F0}s";

                var cm = CourierManager.Instance;
                if (cm != null)
                    _currentMoney = Mathf.RoundToInt(cm.TotalMoney);
            }

            var cm2 = CourierManager.Instance;
            if (cm2 == null) return;

            var couriers = cm2.ActiveCouriers;
            if (couriers != null && _selectedCourierIndex < couriers.Count)
            {
                var courier = couriers[_selectedCourierIndex];

                if (_courierNameText != null)
                    _courierNameText.text = $"{courier.Name} ({courier.TypeName})";

                if (_traitsText != null)
                {
                    string traitNames = "";
                    foreach (var trait in courier.Traits)
                    {
                        if (traitNames.Length > 0) traitNames += ", ";
                        traitNames += trait.Name;
                    }
                    _traitsText.text = traitNames;
                }

                if (_fatigueText != null)
                    _fatigueText.text = $"Fatigue: {courier.Fatigue:F0}/{courier.MaxFatigue}";

                if (_fatigueSlider != null)
                {
                    _fatigueSlider.maxValue = courier.MaxFatigue;
                    _fatigueSlider.value = courier.Fatigue;
                }
            }

            var controller = cm2.GetControllerForCourier(_selectedCourierIndex);
            if (controller != null)
            {
                _currentSpeed = controller.Speed;
                _currentPosition = controller.CurrentPosition;
            }
        }

        public void UpdateAllDisplays()
        {
            UpdateSpeedDisplay();
            UpdatePositionDisplay();
            UpdateMoneyDisplay();
            UpdateZoneDisplay();
        }

        private void UpdateSpeedDisplay()
        {
            if (_speedText != null)
                _speedText.text = string.Format("Speed: {0:F1} km/h", _currentSpeed);
        }

        private void UpdatePositionDisplay()
        {
            if (_positionText != null)
                _positionText.text = string.Format("Pos: ({0:F1}, {1:F1})", _currentPosition.x, _currentPosition.y);
        }

        private void UpdateMoneyDisplay()
        {
            if (_moneyText != null)
                _moneyText.text = string.Format("Money: ${0}", _currentMoney);
        }

        private void UpdateZoneDisplay()
        {
            if (_zoneText != null)
                _zoneText.text = string.Format("Zone: {0}/{1}", _currentZone, _totalZones);
        }

        public void SetSpeed(float speed) => _currentSpeed = Mathf.Clamp(speed, 0f, _maxSpeed);
        public void SetPosition(Vector2 position) => _currentPosition = position;
        public void SetMoney(int money) => _currentMoney = Mathf.Max(0, money);
        public void SetZone(int zone) => _currentZone = Mathf.Clamp(zone, 1, _totalZones);

        public float CurrentSpeed => _currentSpeed;
        public Vector2 CurrentPosition => _currentPosition;
        public int CurrentMoney => _currentMoney;
        public int CurrentZone => _currentZone;
    }
}
