using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using AlgorithmOfDelivery.Game;
using AlgorithmOfDelivery.Maze;

namespace AlgorithmOfDelivery.UI
{
    public class MailInboxUI : MonoBehaviour
    {
        [SerializeField] private GameObject _panelRoot;
        [SerializeField] private RectTransform _listRoot;
        [SerializeField] private Button _closeButton;
        [SerializeField] private float _rowHeight = 48f;
        [SerializeField] private float _rowGap = 8f;
        [SerializeField] private int _maxVisibleEntries = 8;

        private readonly List<GameObject> _rows = new List<GameObject>();
        private NotificationManager _notificationManager;
        private CameraController _cameraController;

        public void Initialize(NotificationManager notificationManager, GameObject panelRoot, RectTransform listRoot, Button closeButton)
        {
            _notificationManager = notificationManager;
            _panelRoot = panelRoot;
            _listRoot = listRoot;
            _closeButton = closeButton;

            _cameraController = FindObjectOfType<CameraController>();

            if (_closeButton != null)
            {
                _closeButton.onClick.RemoveAllListeners();
                _closeButton.onClick.AddListener(Hide);
            }

            Hide();
        }

        public void Toggle()
        {
            SetVisible(!IsVisible);
        }

        public void Show()
        {
            SetVisible(true);
        }

        public void Hide()
        {
            SetVisible(false);
        }

        public bool IsVisible => _panelRoot != null && _panelRoot.activeSelf;

        public void SetVisible(bool visible)
        {
            if (_panelRoot != null)
                _panelRoot.SetActive(visible);

            if (visible)
                Refresh();
        }

        public void Refresh()
        {
            ClearRows();

            if (_notificationManager == null || _listRoot == null)
                return;

            IReadOnlyList<NotificationManager.NotificationRecord> history = _notificationManager.RecentNotifications;
            int startIndex = Mathf.Max(0, history.Count - _maxVisibleEntries);

            float y = -8f;
            for (int i = history.Count - 1; i >= startIndex; i--)
            {
                CreateRow(history[i], y);
                y -= _rowHeight + _rowGap;
            }
        }

        private void CreateRow(NotificationManager.NotificationRecord record, float y)
        {
            float width = _listRoot.rect.width > 0f ? _listRoot.rect.width - 16f : 320f;

            GameObject row = new GameObject("InboxEntry", typeof(RectTransform), typeof(Image), typeof(Button));
            row.transform.SetParent(_listRoot, false);

            RectTransform rt = row.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(8f, y);
            rt.sizeDelta = new Vector2(width, _rowHeight);

            Image bg = row.GetComponent<Image>();
            bg.color = new Color(1f, 1f, 1f, 0.9f);

            Button btn = row.GetComponent<Button>();
            btn.targetGraphic = bg;
            btn.transition = Selectable.Transition.None;
            Vector2 worldPos = record.HouseWorldPos;
            btn.onClick.AddListener(() =>
            {
                if (_cameraController != null)
                    _cameraController.ZoomTo(worldPos, 0.5f);
            });

            GameObject labelGo = new GameObject("Label", typeof(RectTransform));
            labelGo.transform.SetParent(row.transform, false);

            RectTransform labelRt = labelGo.GetComponent<RectTransform>();
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = new Vector2(12f, 6f);
            labelRt.offsetMax = new Vector2(-12f, -6f);

            Text label = labelGo.AddComponent<Text>();
            label.font = LoadFont(15);
            label.fontSize = 15;
            label.color = Color.black;
            label.alignment = TextAnchor.MiddleLeft;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            label.raycastTarget = false;
            label.text = $"{record.HouseName}\n{record.Message}";

            _rows.Add(row);
        }

        private void ClearRows()
        {
            for (int i = 0; i < _rows.Count; i++)
            {
                if (_rows[i] != null)
                    Destroy(_rows[i]);
            }

            _rows.Clear();
        }

        private Font LoadFont(int size)
        {
            Font font = Resources.Load<Font>("Fonts/NanumGothic-Regular");
            if (font != null)
                return font;

            string[] candidates = { "NanumGothic", "Noto Sans CJK KR", "Noto Sans", "DejaVu Sans", "Arial Unicode MS", "Arial" };
            foreach (var name in candidates)
            {
                font = Font.CreateDynamicFontFromOSFont(name, size);
                if (font != null)
                    return font;
            }

            return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }
    }
}
