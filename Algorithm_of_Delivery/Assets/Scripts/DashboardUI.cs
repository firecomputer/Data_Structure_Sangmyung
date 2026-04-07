using UnityEngine;
using UnityEngine.UI;

namespace AlgorithmOfDelivery.UI
{
    public class DashboardUI : MonoBehaviour
    {
        [Header("UI Components")]
        [SerializeField] private Text _speedText;
        [SerializeField] private Text _positionText;
        [SerializeField] private Text _moneyText;
        [SerializeField] private Text _zoneText;

        [Header("Speed Display")]
        [SerializeField] private float _currentSpeed = 0f;
        [SerializeField] private float _maxSpeed = 120f;

        [Header("Position Display")]
        [SerializeField] private Vector2 _currentPosition;

        [Header("Money Display")]
        [SerializeField] private int _currentMoney = 0;

        [Header("Zone Display")]
        [SerializeField] private int _currentZone = 1;
        [SerializeField] private int _totalZones = 10;

        private void Start()
        {
            UpdateAllDisplays();
        }

        private void Update()
        {
            UpdateAllDisplays();
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
            {
                _speedText.text = string.Format("Speed: {0:F1} km/h", _currentSpeed);
            }
        }

        private void UpdatePositionDisplay()
        {
            if (_positionText != null)
            {
                _positionText.text = string.Format("Pos: ({0:F1}, {1:F1})", _currentPosition.x, _currentPosition.y);
            }
        }

        private void UpdateMoneyDisplay()
        {
            if (_moneyText != null)
            {
                _moneyText.text = string.Format("Money: ${0}", _currentMoney);
            }
        }

        private void UpdateZoneDisplay()
        {
            if (_zoneText != null)
            {
                _zoneText.text = string.Format("Zone: {0}/{1}", _currentZone, _totalZones);
            }
        }

        public void SetSpeed(float speed)
        {
            _currentSpeed = Mathf.Clamp(speed, 0f, _maxSpeed);
        }

        public void SetPosition(Vector2 position)
        {
            _currentPosition = position;
        }

        public void SetMoney(int money)
        {
            _currentMoney = Mathf.Max(0, money);
        }

        public void SetZone(int zone)
        {
            _currentZone = Mathf.Clamp(zone, 1, _totalZones);
        }

        public float CurrentSpeed => _currentSpeed;
        public Vector2 CurrentPosition => _currentPosition;
        public int CurrentMoney => _currentMoney;
        public int CurrentZone => _currentZone;
    }
}
