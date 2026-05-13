using UnityEngine;
using UnityEngine.EventSystems;

namespace AlgorithmOfDelivery.Game
{
    public class HouseState : MonoBehaviour
    {
        [Header("Happiness")]
        [SerializeField] private float _happiness = 60f;
        [SerializeField] private float _maxHappiness = 100f;
        [SerializeField] private float _decayRate = 1f / 30f;
        [SerializeField] private float _recoveryAmount = 20f;

        [Header("Display")]
        [SerializeField] private Color _highHappinessColor = Color.green;
        [SerializeField] private Color _midHappinessColor = Color.yellow;
        [SerializeField] private Color _lowHappinessColor = Color.red;
        [SerializeField] private float _lowThreshold = 30f;
        [SerializeField] private float _midThreshold = 60f;
        [SerializeField] private float _barWidth = 1.5f;
        [SerializeField] private float _barHeight = 0.15f;
        [SerializeField] private float _barYOffset = 0.6f;

        private SpriteRenderer _barFill;
        private SpriteRenderer _barBg;

        public float Happiness => _happiness;
        public float MaxHappiness => _maxHappiness;
        public bool IsLowHappiness => _happiness <= _lowThreshold;
        public float RewardMultiplier => _happiness / _maxHappiness;

        private void Start()
        {
            _happiness = Mathf.Clamp(_happiness, 0f, _maxHappiness);
            BuildHappinessBar();
        }

        public void Init(float initialHappiness)
        {
            _happiness = Mathf.Clamp(initialHappiness, 0f, _maxHappiness);
        }

        public void OnDelivery(out float happinessGain, out float rewardMultiplier)
        {
            _happiness = Mathf.Min(_happiness + _recoveryAmount, _maxHappiness);
            happinessGain = _recoveryAmount;
            rewardMultiplier = RewardMultiplier;
        }

        public void OnDelivery()
        {
            _happiness = Mathf.Min(_happiness + _recoveryAmount, _maxHappiness);
        }

        private void Update()
        {
            _happiness -= _decayRate * Time.deltaTime;
            if (_happiness < 0f) _happiness = 0f;
            UpdateBarVisual();
        }

        private void BuildHappinessBar()
        {
            GameObject root = new GameObject("HappinessBar");
            root.transform.SetParent(transform);
            root.transform.localPosition = Vector3.zero;

            _barBg = CreateBarSprite(root, Color.gray, 0);
            _barBg.transform.localPosition = new Vector3(0f, _barYOffset, 0f);
            _barBg.size = new Vector2(_barWidth, _barHeight);

            _barFill = CreateBarSprite(root, _highHappinessColor, -1);
            _barFill.transform.localPosition = new Vector3(0f, _barYOffset, 0f);
            _barFill.size = new Vector2(_barWidth, _barHeight);

            UpdateBarVisual();
        }

        private SpriteRenderer CreateBarSprite(GameObject parent, Color color, int sortingOffset)
        {
            GameObject go = new GameObject("BarPart");
            go.transform.SetParent(parent.transform);
            go.transform.localScale = Vector3.one;

            Texture2D tex = new Texture2D(4, 4);
            Color[] colors = new Color[16];
            for (int i = 0; i < 16; i++) colors[i] = Color.white;
            tex.SetPixels(colors);
            tex.Apply();

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
            sr.color = color;
            sr.sortingOrder = 10 + sortingOffset;
            sr.drawMode = SpriteDrawMode.Sliced;

            return sr;
        }

        private void UpdateBarVisual()
        {
            if (_barFill == null) return;

            float ratio = _happiness / _maxHappiness;

            if (_happiness <= _lowThreshold)
                _barFill.color = _lowHappinessColor;
            else if (_happiness <= _midThreshold)
                _barFill.color = _midHappinessColor;
            else
                _barFill.color = _highHappinessColor;

            _barFill.size = new Vector2(_barWidth * ratio, _barHeight);
            _barFill.transform.localPosition = new Vector3(-_barWidth * (1f - ratio) / 2f, _barYOffset, 0f);
        }
    }
}
