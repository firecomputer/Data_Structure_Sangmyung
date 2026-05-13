using UnityEngine;
using UnityEngine.UI;
using AlgorithmOfDelivery.Core;
using AlgorithmOfDelivery.Game;

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

        private int _selectedIndex;

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
            var state = GameManager.Instance;
            if (state != null)
            {
                state.SelectCourierByIndex(index);
                _selectedIndex = index;
                HighlightSelection();
            }
        }

        private void UpdateDisplay()
        {
            var couriers = CourierManager.Instance.ActiveCouriers;

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

            HighlightSelection();
        }

        private void HighlightSelection()
        {
            for (int i = 0; i < _portraitImages.Length; i++)
            {
                if (_portraitImages[i] != null)
                    _portraitImages[i].color = (i == _selectedIndex) ? Color.yellow : Color.white;
            }
        }
    }
}
