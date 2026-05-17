using System.Collections.Generic;
using UnityEngine;
using AlgorithmOfDelivery.Game;

namespace AlgorithmOfDelivery.Maze
{
    public class PlanningManager : MonoBehaviour
    {
        private Dictionary<int, List<PlannedHouse>> _courierPlans = new Dictionary<int, List<PlannedHouse>>();
        private Dictionary<HouseState, GameObject> _numberIndicators = new Dictionary<HouseState, GameObject>();
        private Dictionary<HouseState, GameObject> _priorityLabels = new Dictionary<HouseState, GameObject>();
        private int _selectedCourierIndex = -1;

        public const int MaxPrioritiesPerCourier = 10;
        public int SelectedCourierIndex => _selectedCourierIndex;

        public static PlanningManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public void SelectCourier(int index)
        {
            _selectedCourierIndex = index;
            if (!_courierPlans.ContainsKey(index))
                _courierPlans[index] = new List<PlannedHouse>();
        }

        public void DeselectCourier()
        {
            _selectedCourierIndex = -1;
        }

        public int AssignPriority(HouseState house, int courierIdx)
        {
            if (!_courierPlans.ContainsKey(courierIdx))
                _courierPlans[courierIdx] = new List<PlannedHouse>();

            var plans = _courierPlans[courierIdx];
            if (plans.Count >= MaxPrioritiesPerCourier)
                return -1;

            foreach (var p in plans)
            {
                if (p.House == house)
                    return -1;
            }

            int nextPriority = plans.Count + 1;
            var planned = new PlannedHouse { House = house, Priority = nextPriority };
            plans.Add(planned);

            CreateOrUpdateIndicator(house, nextPriority);

            if (plans.Count >= MaxPrioritiesPerCourier)
                _selectedCourierIndex = -1;

            return nextPriority;
        }

        public bool IsFullyPlanned(int courierIdx)
        {
            if (!_courierPlans.TryGetValue(courierIdx, out var plans))
                return false;
            return plans.Count >= MaxPrioritiesPerCourier;
        }

        public List<PlannedHouse> GetPlannedHousesForCourier(int courierIdx)
        {
            if (!_courierPlans.TryGetValue(courierIdx, out var plans))
                return new List<PlannedHouse>();
            return new List<PlannedHouse>(plans);
        }

        public bool HasHouse(int courierIdx, HouseState house, out int priority)
        {
            priority = -1;
            if (!_courierPlans.TryGetValue(courierIdx, out var plans))
                return false;
            foreach (var p in plans)
            {
                if (p.House == house)
                {
                    priority = p.Priority;
                    return true;
                }
            }
            return false;
        }

        public void ClearPlan(int courierIdx)
        {
            if (!_courierPlans.TryGetValue(courierIdx, out var plans))
                return;
            foreach (var p in plans)
            {
                RemoveIndicator(p.House);
            }
            plans.Clear();
        }

        public void ClearAll()
        {
            foreach (var kvp in _numberIndicators)
            {
                if (kvp.Value != null)
                    Destroy(kvp.Value);
            }
            _numberIndicators.Clear();
            _priorityLabels.Clear();
            _courierPlans.Clear();
        }

        public void ClearVisualIndicators()
        {
            foreach (var kvp in _numberIndicators)
            {
                if (kvp.Value != null)
                    Destroy(kvp.Value);
            }
            _numberIndicators.Clear();
            _priorityLabels.Clear();
        }

        private void CreateOrUpdateIndicator(HouseState house, int priority)
        {
            RemoveIndicator(house);

            GameObject indicator = new GameObject($"PriorityIndicator_{house.name}");
            indicator.transform.SetParent(house.transform, false);
            indicator.transform.localPosition = new Vector3(0f, 1.2f, 0f);
            indicator.transform.localScale = Vector3.one * 0.6f;

            CreateCircleSprite(indicator, Color.yellow);

            GameObject label = new GameObject("PriorityLabel");
            label.transform.SetParent(indicator.transform, false);
            label.transform.localPosition = Vector3.zero;
            label.transform.localScale = Vector3.one;

            var textMesh = label.AddComponent<TextMesh>();
            textMesh.text = priority.ToString();
            textMesh.fontSize = 48;
            textMesh.characterSize = 0.4f;
            textMesh.color = Color.black;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.fontStyle = FontStyle.Bold;

            MeshRenderer labelRenderer = label.GetComponent<MeshRenderer>();
            labelRenderer.sortingOrder = 20;

            _numberIndicators[house] = indicator;
            _priorityLabels[house] = label;
        }

        private void RemoveIndicator(HouseState house)
        {
            if (_numberIndicators.TryGetValue(house, out var indicator))
            {
                if (indicator != null) Destroy(indicator);
                _numberIndicators.Remove(house);
            }
            if (_priorityLabels.TryGetValue(house, out var label))
            {
                _priorityLabels.Remove(house);
            }
        }

        private void CreateCircleSprite(GameObject go, Color color)
        {
            int size = 64;
            Texture2D tex = new Texture2D(size, size);
            Color[] colors = new Color[size * size];
            Vector2 center = new Vector2(size / 2f, size / 2f);
            float radius = size / 2f - 2f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    colors[y * size + x] = dist <= radius ? color : Color.clear;
                }
            }
            tex.SetPixels(colors);
            tex.Apply();

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size / 0.6f);
            sr.sortingOrder = 15;
        }

        public struct PlannedHouse
        {
            public HouseState House;
            public int Priority;
        }
    }
}
