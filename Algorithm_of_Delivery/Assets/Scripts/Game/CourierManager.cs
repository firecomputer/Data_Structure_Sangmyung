using System.Collections.Generic;
using UnityEngine;
using AlgorithmOfDelivery.Core;
using CourierCtrl = AlgorithmOfDelivery.Maze.CourierController;

namespace AlgorithmOfDelivery.Game
{
    public class CourierManager : MonoBehaviour
    {
        [Header("Gold Settings")]
        [SerializeField] private int _recruitCost = 500;
        [SerializeField] private int _maxCouriers = 4;

        private List<CourierState> _activeCouriers = new List<CourierState>();
        private List<CourierCtrl> _activeControllers = new List<CourierCtrl>();
        private Dictionary<int, CourierCtrl> _assignedVehicles = new Dictionary<int, CourierCtrl>();
        private float _totalMoney;
        private int _totalDeliveries;

        public int ActiveCourierCount => _activeCouriers.Count;
        public int MaxCouriers => _maxCouriers;
        public int RecruitCost => _recruitCost;
        public float TotalMoney => _totalMoney;
        public int TotalDeliveries => _totalDeliveries;
        public List<CourierState> ActiveCouriers => _activeCouriers;
        public List<CourierCtrl> ActiveControllers => _activeControllers;

        public static CourierManager Instance { get; private set; }

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

        public CourierState StartGame()
        {
            var firstData = CourierCatalog.All[0];
            var state = new CourierState(firstData);
            _activeCouriers.Add(state);
            return state;
        }

        public CourierState RecruitCourier()
        {
            if (_activeCouriers.Count >= _maxCouriers)
            {
                Debug.LogWarning("[CourierManager] Max couriers reached.");
                return null;
            }

            if (_totalMoney < _recruitCost)
            {
                Debug.LogWarning("[CourierManager] Not enough gold to recruit.");
                return null;
            }

            var usedIndices = new HashSet<int>();
            foreach (var c in _activeCouriers)
            {
                for (int i = 0; i < CourierCatalog.All.Count; i++)
                {
                    if (CourierCatalog.All[i].Name == c.Name)
                        usedIndices.Add(i);
                }
            }

            var available = new List<CourierData>();
            for (int i = 0; i < CourierCatalog.All.Count; i++)
            {
                if (!usedIndices.Contains(i))
                    available.Add(CourierCatalog.All[i]);
            }

            if (available.Count == 0)
            {
                Debug.LogWarning("[CourierManager] No more couriers available.");
                return null;
            }

            int randomIndex = Random.Range(0, available.Count);
            var data = available[randomIndex];
            var state = new CourierState(data);
            _activeCouriers.Add(state);

            _totalMoney -= _recruitCost;

            Debug.Log($"[CourierManager] Recruited {state.Name} for {_recruitCost} gold. Remaining: {_totalMoney}");
            return state;
        }

        public CourierCtrl CreateCourierController(int courierIndex)
        {
            if (courierIndex < 0 || courierIndex >= _activeCouriers.Count)
                return null;

            var state = _activeCouriers[courierIndex];

            if (AlgorithmOfDelivery.Maze.DeliveryManager.Instance == null)
            {
                Debug.LogError("[CourierManager] DeliveryManager.Instance is null.");
                return null;
            }

            var controller = AlgorithmOfDelivery.Maze.DeliveryManager.Instance.CreateCourier(state);
            _activeControllers.Add(controller);
            _assignedVehicles[courierIndex] = controller;

            return controller;
        }

        public CourierCtrl GetControllerForCourier(int courierIndex)
        {
            _assignedVehicles.TryGetValue(courierIndex, out var controller);
            return controller;
        }

        public void AddMoney(float amount)
        {
            _totalMoney += amount;
        }

        public void RecordDelivery()
        {
            _totalDeliveries++;
        }

        public void ClearAllCouriers()
        {
            foreach (var controller in _activeControllers)
            {
                if (controller != null && controller.gameObject != null)
                    Destroy(controller.gameObject);
            }
            _activeControllers.Clear();
            _assignedVehicles.Clear();
        }
    }
}
