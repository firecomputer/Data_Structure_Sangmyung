using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using AlgorithmOfDelivery.Maze;

namespace AlgorithmOfDelivery.UI
{
    public class NotificationUI : MonoBehaviour
    {
        [SerializeField] private GameObject _panelRoot;
        [SerializeField] private RectTransform _container;
        [SerializeField] private float _itemWidth = 240f;
        [SerializeField] private float _itemHeight = 36f;
        [SerializeField] private float _gap = 6f;
        [SerializeField] private int _maxNotifications = 5;
        [SerializeField] private float _autoRemoveTime = 30f;

        private List<NotificationEntry> _entries = new List<NotificationEntry>();
        private int _nextId;

        private CameraController _cameraController;

        private void Start()
        {
            _cameraController = FindObjectOfType<CameraController>();
        }

        public void Initialize(GameObject panelRoot, RectTransform container)
        {
            _panelRoot = panelRoot;
            _container = container;
            SetVisible(false);
        }

        public void SetVisible(bool visible)
        {
            if (_panelRoot != null)
                _panelRoot.SetActive(visible);
            else if (_container != null)
                _container.gameObject.SetActive(visible);
        }

        public void ToggleVisible()
        {
            if (_panelRoot != null)
                SetVisible(!_panelRoot.activeSelf);
            else if (_container != null)
                SetVisible(!_container.gameObject.activeSelf);
        }

        public bool IsVisible
        {
            get
            {
                if (_panelRoot != null)
                    return _panelRoot.activeSelf;
                return _container != null && _container.gameObject.activeSelf;
            }
        }

        public void AddNotification(string message, Vector2 houseWorldPos)
        {
            if (_container == null)
                return;

            if (_entries.Count >= _maxNotifications)
            {
                RemoveEntry(_entries[0]);
            }

            int id = _nextId++;
            GameObject entryGo = new GameObject($"Notification_{id}", typeof(RectTransform));
            entryGo.transform.SetParent(_container, false);

            var entryRect = entryGo.GetComponent<RectTransform>();
            entryRect.sizeDelta = new Vector2(_itemWidth, _itemHeight);
            entryRect.anchorMin = new Vector2(1, 1);
            entryRect.anchorMax = new Vector2(1, 1);
            entryRect.pivot = new Vector2(1, 1);

            var bg = entryGo.AddComponent<Image>();
            bg.color = new Color(0.15f, 0.15f, 0.2f, 0.85f);

            var btn = entryGo.AddComponent<Button>();
            btn.targetGraphic = bg;
            btn.transition = Selectable.Transition.None;

            var labelGo = new GameObject("Label", typeof(RectTransform));
            labelGo.transform.SetParent(entryGo.transform, false);
            var labelRect = labelGo.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.sizeDelta = Vector2.zero;
            var label = labelGo.AddComponent<Text>();
            label.text = message;
            label.fontSize = 14;
            label.color = Color.white;
            label.alignment = TextAnchor.MiddleLeft;
            label.raycastTarget = false;
            Font font = Resources.Load<Font>("Fonts/NanumGothic-Regular");
            if (font != null) label.font = font;

            Vector2 worldPos = houseWorldPos;
            btn.onClick.AddListener(() => OnNotificationClicked(worldPos));

            var entry = new NotificationEntry
            {
                Id = id,
                GameObject = entryGo,
                HouseWorldPos = houseWorldPos,
                RemoveTimer = _autoRemoveTime
            };

            _entries.Add(entry);
            RepositionStack();
        }

        private void Update()
        {
            for (int i = _entries.Count - 1; i >= 0; i--)
            {
                _entries[i].RemoveTimer -= Time.deltaTime;
                if (_entries[i].RemoveTimer <= 0f)
                {
                    RemoveEntry(_entries[i]);
                }
            }
        }

        private void OnNotificationClicked(Vector2 houseWorldPos)
        {
            if (_cameraController != null)
                _cameraController.ZoomTo(houseWorldPos, 0.5f);

            for (int i = _entries.Count - 1; i >= 0; i--)
            {
                if (Vector2.Distance(_entries[i].HouseWorldPos, houseWorldPos) < 0.1f)
                {
                    RemoveEntry(_entries[i]);
                }
            }
        }

        private void RemoveEntry(NotificationEntry entry)
        {
            if (entry.GameObject != null)
                Destroy(entry.GameObject);
            _entries.Remove(entry);
            RepositionStack();
        }

        private void RepositionStack()
        {
            for (int i = 0; i < _entries.Count; i++)
            {
                var rt = _entries[i].GameObject.GetComponent<RectTransform>();
                if (rt != null)
                {
                    float yOffset = -10f - i * (_itemHeight + _gap);
                    rt.anchoredPosition = new Vector2(-10f, yOffset);
                }
            }
        }

        public bool HasNotificationFor(Vector2 houseWorldPos)
        {
            foreach (var entry in _entries)
            {
                if (Vector2.Distance(entry.HouseWorldPos, houseWorldPos) < 0.1f)
                    return true;
            }
            return false;
        }

        public void RemoveNotificationFor(Vector2 houseWorldPos)
        {
            for (int i = _entries.Count - 1; i >= 0; i--)
            {
                if (Vector2.Distance(_entries[i].HouseWorldPos, houseWorldPos) < 0.1f)
                {
                    RemoveEntry(_entries[i]);
                }
            }
        }

        private class NotificationEntry
        {
            public int Id;
            public GameObject GameObject;
            public Vector2 HouseWorldPos;
            public float RemoveTimer;
        }
    }
}
