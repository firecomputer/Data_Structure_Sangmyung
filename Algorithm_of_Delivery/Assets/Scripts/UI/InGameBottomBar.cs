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
        [SerializeField] private GameObject _panel;

        [Header("Courier Portrait Slots")]
        [SerializeField] private Image[] _portraitImages;
        [SerializeField] private Text[] _nameTexts;
        [SerializeField] private int _maxSlots = 4;

        private float[] _lastClickTimes = new float[4] { -1f, -1f, -1f, -1f };
        private const float DoubleClickThreshold = 0.35f;

        public System.Action<int> OnPortraitDoubleClicked;

        private bool _initialized;

        private void Start()
        {
            UpdateDisplay();
        }

        private void Update()
        {
            if (_state == null && GameManager.Instance != null)
                _state = GameManager.Instance;

            UpdateDisplay();
        }

        private GameManager _state;

        public void OnPortraitClicked(int index)
        {
            float now = Time.time;
            if (index >= 0 && index < _lastClickTimes.Length && now - _lastClickTimes[index] < DoubleClickThreshold)
            {
                _lastClickTimes[index] = 0f;
                OnPortraitDoubleClicked?.Invoke(index);
                return;
            }
            if (index >= 0 && index < _lastClickTimes.Length)
                _lastClickTimes[index] = now;

            var state = GameManager.Instance;
            if (state != null)
            {
                state.SelectCourierByIndex(index);
            }
        }

        private void UpdateDisplay()
        {
            var couriers = CourierManager.Instance.ActiveCouriers;
            int selectedFromGame = GameManager.Instance != null ? GameManager.Instance.SelectedCourierIndex : -1;

            for (int i = 0; i < _maxSlots; i++)
            {
                bool hasCourier = i < couriers.Count;

                if (i < _portraitImages.Length && _portraitImages[i] != null)
                {
                    if (hasCourier)
                    {
                        var sprite = Resources.Load<Sprite>(couriers[i].PortraitPath);
                        _portraitImages[i].sprite = sprite;
                        _portraitImages[i].gameObject.SetActive(true);

                        if (PlanningManager.Instance != null && PlanningManager.Instance.IsFullyPlanned(i)
                            && GameManager.Instance != null && GameManager.Instance.State == GameState.Planning)
                            _portraitImages[i].color = new Color(0.4f, 0.4f, 0.4f, 0.7f);
                        else
                            _portraitImages[i].color = (i == selectedFromGame) ? Color.yellow : Color.white;
                    }
                    else
                    {
                        _portraitImages[i].gameObject.SetActive(false);
                    }
                }

                if (i < _nameTexts.Length && _nameTexts[i] != null)
                {
                    _nameTexts[i].text = hasCourier ? couriers[i].Name : "";
                }
            }
        }
    }
}
