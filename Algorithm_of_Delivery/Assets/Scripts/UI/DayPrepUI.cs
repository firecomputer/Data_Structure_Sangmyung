using UnityEngine;
using UnityEngine.UI;
using AlgorithmOfDelivery.Core;
using AlgorithmOfDelivery.Game;

namespace AlgorithmOfDelivery.UI
{
    public class DayPrepUI : MonoBehaviour
    {
        [Header("Panel")]
        [SerializeField] private GameObject _panel;

        [Header("Courier Slots")]
        [SerializeField] private Image[] _courierPortraits;
        [SerializeField] private Text[] _courierNameTexts;
        [SerializeField] private Text[] _courierTypeTexts;
        [SerializeField] private int _maxPortraitSlots = 4;

        [Header("Vehicle Slots")]
        [SerializeField] private Image[] _vehicleIcons;
        [SerializeField] private Text[] _vehicleNameTexts;

        [Header("Buttons")]
        [SerializeField] private Button _recruitButton;
        [SerializeField] private Button _startButton;
        [SerializeField] private Button _planButton;
        [SerializeField] private Text _recruitCostText;
        [SerializeField] private Text _goldText;

        [Header("Day Result")]
        [SerializeField] private GameObject _dayResultPanel;
        [SerializeField] private Text _dayResultText;
        [SerializeField] private Button _dayResultContinueButton;

        private int _selectedCourierIndex = -1;
        private int _selectedVehicleIndex = -1;

        private bool _initialized;

        private void Start()
        {
            TryInit();
        }

        public void LateInit()
        {
            TryInit();
        }

        private void TryInit()
        {
            if (_initialized) return;

            _initialized = true;

            if (_recruitButton != null)
                _recruitButton.onClick.AddListener(OnRecruitClicked);

            if (_startButton != null)
                _startButton.onClick.AddListener(OnStartClicked);

            if (_planButton != null)
                _planButton.onClick.AddListener(OnPlanClicked);

            if (_dayResultContinueButton != null)
                _dayResultContinueButton.onClick.AddListener(OnDayResultContinueClicked);

            if (_panel != null)
                _panel.SetActive(false);
        }

        public void Show(bool canRecruit)
        {
            if (_panel != null)
                _panel.SetActive(true);

            if (_dayResultPanel != null)
                _dayResultPanel.SetActive(false);

            UpdateCourierSlots();
            UpdateVehicleSlots();

            if (_recruitButton != null)
                _recruitButton.interactable = canRecruit && CourierManager.Instance.ActiveCourierCount < CourierManager.Instance.MaxCouriers;

            if (_recruitCostText != null)
                _recruitCostText.text = $"모집: {CourierManager.Instance.RecruitCost}G";

            if (_goldText != null)
                _goldText.text = $"보유: {CourierManager.Instance.TotalMoney:F0}G";

            if (_planButton != null)
                _planButton.gameObject.SetActive(true);

            if (_startButton != null)
                _startButton.gameObject.SetActive(true);
        }

        public void Hide()
        {
            if (_panel != null)
                _panel.SetActive(false);
        }

        public void ShowDayResult(int dayCount, int deliveries, float totalEarned)
        {
            Debug.Log($"[DayPrepUI] ShowDayResult called — panel:{_panel != null}, resultPanel:{_dayResultPanel != null}, resultText:{_dayResultText != null}");

            if (_panel != null)
                _panel.SetActive(true);

            if (_dayResultPanel != null)
                _dayResultPanel.SetActive(true);

            if (_dayResultText != null)
                _dayResultText.text = $"{dayCount}일차 종료!\n배달: {deliveries}건\n수익: {totalEarned:F0}G";

            if (_recruitButton != null)
                _recruitButton.interactable = false;

            if (_planButton != null)
                _planButton.gameObject.SetActive(false);

            if (_startButton != null)
                _startButton.gameObject.SetActive(false);

            if (_recruitCostText != null)
                _recruitCostText.text = $"모집: {CourierManager.Instance.RecruitCost}G";

            if (_goldText != null)
                _goldText.text = $"보유: {CourierManager.Instance.TotalMoney:F0}G";
        }

        private void OnDayResultContinueClicked()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.EnterDayPrep();
        }

        public void OnCourierClicked(int index)
        {
            _selectedCourierIndex = index;

            if (GameManager.Instance != null)
                GameManager.Instance.SelectCourierByIndex(index);

            for (int i = 0; i < _courierPortraits.Length; i++)
            {
                if (_courierPortraits[i] != null)
                    _courierPortraits[i].color = (i == index) ? Color.yellow : Color.white;
            }
        }

        public void OnVehicleClicked(int index)
        {
            _selectedVehicleIndex = index;

            for (int i = 0; i < _vehicleIcons.Length; i++)
            {
                if (_vehicleIcons[i] != null)
                    _vehicleIcons[i].color = (i == index) ? Color.yellow : Color.white;
            }

            if (_selectedCourierIndex >= 0 && _selectedVehicleIndex >= 0)
            {
                AssignVehicle();
            }
        }

        private void AssignVehicle()
        {
            if (_selectedCourierIndex < 0 || _selectedVehicleIndex < 0)
                return;

            var controller = CourierManager.Instance.GetControllerForCourier(_selectedCourierIndex);
            if (controller == null)
            {
                CourierManager.Instance.CreateCourierController(_selectedCourierIndex);
            }

            Debug.Log($"[DayPrepUI] Assigned vehicle {_selectedVehicleIndex} to courier {_selectedCourierIndex}");
        }

        private void OnRecruitClicked()
        {
            int prevCount = CourierManager.Instance.ActiveCourierCount;
            var state = CourierManager.Instance.RecruitCourier();
            if (state != null)
            {
                UpdateCourierSlots();
                UpdateGoldDisplay();

                int newIndex = CourierManager.Instance.ActiveCouriers.Count - 1;
                CourierManager.Instance.CreateCourierController(newIndex);
                Debug.Log($"[DayPrepUI] Auto-assigned truck to newly recruited courier {state.Name}");
            }
        }

        private void OnStartClicked()
        {
            Hide();
            if (GameManager.Instance != null)
                GameManager.Instance.StartDay();
        }

        private void OnPlanClicked()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.EnterPlanning();
        }

        private void UpdateCourierSlots()
        {
            var couriers = CourierManager.Instance.ActiveCouriers;

            for (int i = 0; i < _maxPortraitSlots; i++)
            {
                bool hasCourier = i < couriers.Count;

                if (i < _courierPortraits.Length && _courierPortraits[i] != null)
                {
                    if (hasCourier)
                    {
                        var sprite = Resources.Load<Sprite>(couriers[i].PortraitPath);
                        _courierPortraits[i].sprite = sprite;
                        _courierPortraits[i].color = Color.white;
                        _courierPortraits[i].gameObject.SetActive(true);
                    }
                    else
                    {
                        _courierPortraits[i].sprite = null;
                        _courierPortraits[i].color = new Color(0.3f, 0.3f, 0.3f, 0.5f);
                    }
                }

                if (i < _courierNameTexts.Length && _courierNameTexts[i] != null)
                {
                    _courierNameTexts[i].text = hasCourier ? couriers[i].Name : "";
                }

                if (i < _courierTypeTexts.Length && _courierTypeTexts[i] != null)
                {
                    _courierTypeTexts[i].text = hasCourier ? couriers[i].TypeName : "";
                }
            }
        }

        private void UpdateVehicleSlots()
        {
            for (int i = 0; i < _vehicleIcons.Length; i++)
            {
                if (_vehicleIcons[i] != null)
                {
                    _vehicleIcons[i].color = Color.white;
                    _vehicleIcons[i].gameObject.SetActive(true);
                }
            }
        }

        private void UpdateGoldDisplay()
        {
            if (_goldText != null)
                _goldText.text = $"보유: {CourierManager.Instance.TotalMoney:F0}G";

            if (_recruitButton != null)
                _recruitButton.interactable = CourierManager.Instance.TotalMoney >= CourierManager.Instance.RecruitCost
                    && CourierManager.Instance.ActiveCourierCount < CourierManager.Instance.MaxCouriers;
        }
    }
}
