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
        [SerializeField] private Button[] _courierSlotButtons;
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

        private bool _planningMode;
        private bool _initialized;
        private bool _panelLayoutCached;
        private Transform _panelOriginalParent;
        private int _panelOriginalSiblingIndex;
        private Transform _canvasTransform;

        private RectTransform _completeButtonRect;
        private bool _completeButtonLayoutCached;
        private Transform _completeButtonOriginalParent;
        private int _completeButtonOriginalSiblingIndex;
        private Vector2 _completeButtonOriginalAnchorMin;
        private Vector2 _completeButtonOriginalAnchorMax;
        private Vector2 _completeButtonOriginalPivot;
        private Vector2 _completeButtonOriginalAnchoredPosition;
        private Vector2 _completeButtonOriginalSizeDelta;
        private Vector3 _completeButtonOriginalLocalScale;
        private Quaternion _completeButtonOriginalLocalRotation;

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

            CacheLayoutRoots();

            if (_panel != null)
                _panel.SetActive(false);
        }

        public void Show(bool canRecruit)
        {
            RestorePanelHierarchy();

            if (_panel != null)
                _panel.SetActive(true);
            BringPanelToFront();

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

        public void BeginPlanningMode(Button completeButton)
        {
            if (_planningMode)
                return;

            _planningMode = true;
            CacheLayoutRoots();
            MoveCompleteButtonToPlanningPosition(completeButton);
            DetachPanelForPlanning();
        }

        public void EndPlanningMode(Button completeButton)
        {
            RestorePanelHierarchy();
            RestoreCompleteButtonLayout(completeButton);
            _planningMode = false;
        }

        public void ShowDayResult(int dayCount, int deliveries, float totalEarned)
        {
            Debug.Log($"[DayPrepUI] ShowDayResult called — panel:{_panel != null}, resultPanel:{_dayResultPanel != null}, resultText:{_dayResultText != null}");

            RestorePanelHierarchy();

            if (_panel != null)
                _panel.SetActive(true);
            BringPanelToFront();

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

            if (_courierPortraits == null)
                return;

            for (int i = 0; i < _courierPortraits.Length; i++)
            {
                if (_courierPortraits[i] != null)
                    _courierPortraits[i].color = (i == index) ? Color.yellow : Color.white;
            }
        }

        public void OnVehicleClicked(int index)
        {
            _selectedVehicleIndex = index;

            if (_vehicleIcons != null)
            {
                for (int i = 0; i < _vehicleIcons.Length; i++)
                {
                    if (_vehicleIcons[i] != null)
                        _vehicleIcons[i].color = (i == index) ? Color.yellow : Color.white;
                }
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

        private void BringPanelToFront()
        {
            if (_panel != null)
                _panel.transform.SetAsLastSibling();
        }

        private void OnPlanClicked()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.EnterPlanning();
        }

        private void UpdateCourierSlots()
        {
            if (_courierPortraits == null && _courierNameTexts == null && _courierTypeTexts == null)
                return;

            var couriers = CourierManager.Instance.ActiveCouriers;
            int portraitCount = _courierPortraits != null ? _courierPortraits.Length : 0;
            int nameCount = _courierNameTexts != null ? _courierNameTexts.Length : 0;
            int typeCount = _courierTypeTexts != null ? _courierTypeTexts.Length : 0;
            int slotButtonCount = _courierSlotButtons != null ? _courierSlotButtons.Length : 0;

            for (int i = 0; i < _maxPortraitSlots; i++)
            {
                bool hasCourier = i < couriers.Count;

                if (i < slotButtonCount && _courierSlotButtons[i] != null)
                    _courierSlotButtons[i].interactable = hasCourier;

                if (i < portraitCount && _courierPortraits[i] != null)
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

                if (i < nameCount && _courierNameTexts[i] != null)
                {
                    _courierNameTexts[i].text = hasCourier ? couriers[i].Name : "";
                }

                if (i < typeCount && _courierTypeTexts[i] != null)
                {
                    _courierTypeTexts[i].text = hasCourier ? couriers[i].TypeName : "";
                }
            }
        }

        private void UpdateVehicleSlots()
        {
            if (_vehicleIcons == null)
                return;

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

        private void CacheLayoutRoots()
        {
            if (!_panelLayoutCached && _panel != null)
            {
                _panelOriginalParent = _panel.transform.parent;
                _panelOriginalSiblingIndex = _panel.transform.GetSiblingIndex();
                _panelLayoutCached = true;
            }

            if (_canvasTransform == null && _panel != null)
            {
                Canvas canvas = _panel.GetComponentInParent<Canvas>();
                if (canvas != null)
                    _canvasTransform = canvas.transform;
            }
        }

        private void RestorePanelHierarchy()
        {
            if (_panel == null || !_panelLayoutCached)
                return;

            if (_panel.transform.parent != _panelOriginalParent)
                _panel.transform.SetParent(_panelOriginalParent, false);

            if (_panelOriginalSiblingIndex >= 0 && _panel.transform.parent != null)
            {
                int siblingIndex = Mathf.Clamp(_panelOriginalSiblingIndex, 0, _panel.transform.parent.childCount - 1);
                _panel.transform.SetSiblingIndex(siblingIndex);
            }
        }

        private void DetachPanelForPlanning()
        {
            if (_panel == null)
                return;

            if (_panel.transform.parent != null)
                _panel.transform.SetParent(null, false);

            _panel.SetActive(false);
        }

        private void MoveCompleteButtonToPlanningPosition(Button completeButton)
        {
            if (completeButton == null)
                return;

            if (_canvasTransform == null)
                CacheLayoutRoots();

            if (_canvasTransform == null)
                return;

            CacheCompleteButtonLayout(completeButton);

            if (_completeButtonRect == null)
                _completeButtonRect = completeButton.GetComponent<RectTransform>();

            if (_completeButtonRect == null)
                return;

            _completeButtonRect.SetParent(_canvasTransform, false);
            _completeButtonRect.anchorMin = new Vector2(0.5f, 1f);
            _completeButtonRect.anchorMax = new Vector2(0.5f, 1f);
            _completeButtonRect.pivot = new Vector2(0.5f, 1f);
            _completeButtonRect.anchoredPosition = new Vector2(0f, -22f);
            _completeButtonRect.sizeDelta = new Vector2(180f, 70f);
            _completeButtonRect.SetAsLastSibling();

            completeButton.gameObject.SetActive(true);
        }

        private void RestoreCompleteButtonLayout(Button completeButton)
        {
            if (completeButton == null)
                return;

            if (_completeButtonRect == null)
                _completeButtonRect = completeButton.GetComponent<RectTransform>();

            if (_completeButtonRect == null || !_completeButtonLayoutCached)
            {
                completeButton.gameObject.SetActive(false);
                return;
            }

            if (_completeButtonOriginalParent != null)
                _completeButtonRect.SetParent(_completeButtonOriginalParent, false);

            _completeButtonRect.anchorMin = _completeButtonOriginalAnchorMin;
            _completeButtonRect.anchorMax = _completeButtonOriginalAnchorMax;
            _completeButtonRect.pivot = _completeButtonOriginalPivot;
            _completeButtonRect.anchoredPosition = _completeButtonOriginalAnchoredPosition;
            _completeButtonRect.sizeDelta = _completeButtonOriginalSizeDelta;
            _completeButtonRect.localScale = _completeButtonOriginalLocalScale;
            _completeButtonRect.localRotation = _completeButtonOriginalLocalRotation;

            if (_completeButtonOriginalSiblingIndex >= 0 && _completeButtonRect.parent != null)
            {
                int siblingIndex = Mathf.Clamp(_completeButtonOriginalSiblingIndex, 0, _completeButtonRect.parent.childCount - 1);
                _completeButtonRect.SetSiblingIndex(siblingIndex);
            }

            completeButton.gameObject.SetActive(false);
        }

        private void CacheCompleteButtonLayout(Button completeButton)
        {
            if (_completeButtonLayoutCached || completeButton == null)
                return;

            _completeButtonRect = completeButton.GetComponent<RectTransform>();
            if (_completeButtonRect == null)
                return;

            _completeButtonOriginalParent = _completeButtonRect.parent;
            _completeButtonOriginalSiblingIndex = _completeButtonRect.GetSiblingIndex();
            _completeButtonOriginalAnchorMin = _completeButtonRect.anchorMin;
            _completeButtonOriginalAnchorMax = _completeButtonRect.anchorMax;
            _completeButtonOriginalPivot = _completeButtonRect.pivot;
            _completeButtonOriginalAnchoredPosition = _completeButtonRect.anchoredPosition;
            _completeButtonOriginalSizeDelta = _completeButtonRect.sizeDelta;
            _completeButtonOriginalLocalScale = _completeButtonRect.localScale;
            _completeButtonOriginalLocalRotation = _completeButtonRect.localRotation;
            _completeButtonLayoutCached = true;
        }
    }
}
