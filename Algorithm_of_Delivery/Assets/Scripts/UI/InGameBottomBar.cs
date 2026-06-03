using UnityEngine;
using UnityEngine.UI;
using AlgorithmOfDelivery.Core;
using AlgorithmOfDelivery.Game;
using AlgorithmOfDelivery.Maze;

namespace AlgorithmOfDelivery.UI
{
    public class InGameBottomBar : MonoBehaviour
    {
        [Header("Panel")]
        [SerializeField] private RectTransform _panelRect;

        [Header("Courier Slots")]
        [SerializeField] private Button[] _slotButtons;
        [SerializeField] private RectTransform[] _slotRects;
        [SerializeField] private Image[] _portraitImages;
        [SerializeField] private RectTransform[] _fatigueFillRects;
        [SerializeField] private int _maxSlots = 4;

        [Header("Bubble")]
        [SerializeField] private GameObject _bubbleRoot;
        [SerializeField] private RectTransform _bubbleRect;
        [SerializeField] private Text _bubbleNameText;
        [SerializeField] private Text _bubbleTypeText;
        [SerializeField] private Text _bubbleTraitsText;

        private readonly float[] _lastClickTimes = new float[4] { -1f, -1f, -1f, -1f };
        private const float DoubleClickThreshold = 0.35f;

        private int _bubbleCourierIndex = -1;
        private bool _bubbleVisible;

        public System.Action<int> OnPortraitDoubleClicked;

        private void Awake()
        {
            if (_panelRect == null)
                _panelRect = transform as RectTransform;
        }

        private void Start()
        {
            HideCourierBubble();
            UpdateDisplay();
        }

        private void Update()
        {
            UpdateDisplay();
        }

        public void OnPortraitClicked(int index)
        {
            var cm = CourierManager.Instance;
            if (cm == null || index < 0 || index >= cm.ActiveCouriers.Count)
            {
                GameManager.Instance?.SelectCourierByIndex(-1);
                HideCourierBubble();
                return;
            }

            float now = Time.time;
            bool isDoubleClick = index >= 0 && index < _lastClickTimes.Length && now - _lastClickTimes[index] < DoubleClickThreshold;
            if (index >= 0 && index < _lastClickTimes.Length)
                _lastClickTimes[index] = now;

            GameManager.Instance?.SelectCourierByIndex(index);
            ShowCourierBubble(index);

            if (isDoubleClick)
                OnPortraitDoubleClicked?.Invoke(index);
        }

        public void ShowCourierBubble(int index)
        {
            var cm = CourierManager.Instance;
            if (cm == null || index < 0 || index >= cm.ActiveCouriers.Count)
            {
                HideCourierBubble();
                return;
            }

            _bubbleCourierIndex = index;
            _bubbleVisible = true;

            if (_bubbleRoot != null)
                _bubbleRoot.SetActive(true);

            RefreshBubbleContent(index);
            PositionBubble(index);
        }

        public void HideCourierBubble()
        {
            _bubbleCourierIndex = -1;
            _bubbleVisible = false;

            if (_bubbleRoot != null)
                _bubbleRoot.SetActive(false);
        }

        private void UpdateDisplay()
        {
            var cm = CourierManager.Instance;
            if (cm == null)
                return;

            var couriers = cm.ActiveCouriers;
            int selectedFromGame = GameManager.Instance != null ? GameManager.Instance.SelectedCourierIndex : -1;

            for (int i = 0; i < _maxSlots; i++)
            {
                bool hasCourier = i < couriers.Count;

                if (_slotButtons != null && i < _slotButtons.Length && _slotButtons[i] != null)
                    _slotButtons[i].interactable = hasCourier;

                if (_portraitImages != null && i < _portraitImages.Length && _portraitImages[i] != null)
                {
                    var portraitImage = _portraitImages[i];
                    portraitImage.gameObject.SetActive(hasCourier);

                    if (hasCourier)
                    {
                        var sprite = Resources.Load<Sprite>(couriers[i].PortraitPath);
                        portraitImage.sprite = sprite;
                        portraitImage.preserveAspect = true;

                        if (PlanningManager.Instance != null && PlanningManager.Instance.IsFullyPlanned(i)
                            && GameManager.Instance != null && GameManager.Instance.State == GameState.Planning)
                        {
                            portraitImage.color = new Color(0.45f, 0.45f, 0.45f, 0.8f);
                        }
                        else
                        {
                            portraitImage.color = Color.white;
                        }
                    }
                    else
                    {
                        portraitImage.sprite = null;
                    }
                }

                if (_fatigueFillRects != null && i < _fatigueFillRects.Length && _fatigueFillRects[i] != null)
                {
                    var fillRect = _fatigueFillRects[i];
                    if (hasCourier)
                    {
                        float ratio = 0f;
                        if (couriers[i].MaxFatigue > 0f)
                            ratio = Mathf.Clamp01(couriers[i].Fatigue / couriers[i].MaxFatigue);

                        fillRect.sizeDelta = new Vector2(170f * ratio, fillRect.sizeDelta.y);
                        fillRect.gameObject.SetActive(true);
                    }
                    else
                    {
                        fillRect.sizeDelta = new Vector2(0f, fillRect.sizeDelta.y);
                        fillRect.gameObject.SetActive(false);
                    }
                }
            }

            if (_bubbleVisible)
            {
                if (selectedFromGame < 0 || selectedFromGame != _bubbleCourierIndex || _bubbleCourierIndex >= couriers.Count)
                {
                    HideCourierBubble();
                }
                else
                {
                    PositionBubble(_bubbleCourierIndex);
                }
            }
        }

        private void RefreshBubbleContent(int index)
        {
            var cm = CourierManager.Instance;
            if (cm == null || index < 0 || index >= cm.ActiveCouriers.Count)
                return;

            var courier = cm.ActiveCouriers[index];

            if (_bubbleNameText != null)
                _bubbleNameText.text = courier.Name;

            if (_bubbleTypeText != null)
                _bubbleTypeText.text = courier.TypeName;

            if (_bubbleTraitsText != null)
            {
                string traits = "";
                if (courier.Traits != null)
                {
                    for (int i = 0; i < courier.Traits.Length; i++)
                    {
                        if (courier.Traits[i] == null)
                            continue;

                        if (traits.Length > 0)
                            traits += ", ";
                        traits += courier.Traits[i].Name;
                    }
                }

                _bubbleTraitsText.text = traits;
            }
        }

        private void PositionBubble(int index)
        {
            if (_bubbleRect == null || _slotRects == null || index < 0 || index >= _slotRects.Length || _slotRects[index] == null)
                return;

            Vector2 slotPos = _slotRects[index].anchoredPosition;
            Vector2 bubbleSize = _bubbleRect.sizeDelta;
            Vector2 target = new Vector2(slotPos.x + 92f, slotPos.y + 160f);

            if (_panelRect != null)
            {
                float halfWidth = _panelRect.rect.width * 0.5f;
                float minX = -halfWidth + bubbleSize.x * 0.5f + 24f;
                float maxX = halfWidth - bubbleSize.x * 0.5f - 24f;
                float minY = bubbleSize.y * 0.5f + 20f;
                float maxY = _panelRect.rect.height - bubbleSize.y * 0.5f - 24f;

                target.x = Mathf.Clamp(target.x, minX, maxX);
                target.y = Mathf.Clamp(target.y, minY, maxY);
            }

            _bubbleRect.anchoredPosition = target;
        }
    }
}
