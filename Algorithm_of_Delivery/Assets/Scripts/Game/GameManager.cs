using System.Collections.Generic;
using UnityEngine;
using AlgorithmOfDelivery.Core;
using AlgorithmOfDelivery.Maze;
using AlgorithmOfDelivery.UI;

namespace AlgorithmOfDelivery.Game
{
    public enum GameState
    {
        Setup,
        DayPrep,
        Playing,
        DayEnd
    }

    public class GameManager : MonoBehaviour
    {
        [Header("Day Settings")]
        [SerializeField] private float _dayDuration = 300f;
        [SerializeField] private float _baseDeliveryReward = 100f;

        [Header("References")]
        [SerializeField] private HousePlacer _housePlacer;
        [SerializeField] private DeliveryManager _deliveryManager;

        [Header("Click Settings")]
        [SerializeField] private float _houseClickRadius = 5f;

        private GameState _state;
        private float _dayTimer;
        private int _dayCount;
        private int _selectedCourierIndex;

        private List<HouseState> _allHouses = new List<HouseState>();

        public GameState State => _state;
        public int DayCount => _dayCount;
        public float DayTimer => _dayTimer;
        public float DayDuration => _dayDuration;
        public int SelectedCourierIndex => _selectedCourierIndex;
        public float BaseDeliveryReward => _baseDeliveryReward;

        public static GameManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            _state = GameState.Setup;
            _dayCount = 1;

            if (_deliveryManager == null)
                _deliveryManager = FindObjectOfType<DeliveryManager>();

            if (_housePlacer == null)
                _housePlacer = FindObjectOfType<HousePlacer>();

            if (CourierManager.Instance != null)
            {
                var starter = CourierManager.Instance.StartGame();
                CourierManager.Instance.CreateCourierController(0);
            }

            StartCoroutine(WaitForInitialization());
        }

        private System.Collections.IEnumerator WaitForInitialization()
        {
            while (_deliveryManager == null || !_deliveryManager.IsInitialized)
            {
                yield return null;
            }

            CacheHouses();
            EnterDayPrep();
        }

        private void CacheHouses()
        {
            _allHouses.Clear();
            if (_housePlacer != null)
            {
                foreach (var houseObj in _housePlacer.HouseInstances)
                {
                    var houseState = houseObj.GetComponent<HouseState>();
                    if (houseState != null)
                        _allHouses.Add(houseState);
                }
            }
        }

        private void Update()
        {
            switch (_state)
            {
                case GameState.DayPrep:
                    UpdateDayPrep();
                    break;
                case GameState.Playing:
                    UpdatePlaying();
                    break;
            }
        }

        private void UpdateDayPrep()
        {
            _dayPrepTimeout -= Time.deltaTime;
            if (_dayPrepTimeout <= 0f)
            {
                Debug.Log("[GameManager] DayPrep timed out, auto-starting day.");
                StartDay();
            }
        }

        private float _dayPrepTimeout;

        public void EnterDayPrep()
        {
            _state = GameState.DayPrep;
            _dayPrepTimeout = 10f;

            CacheHouses();

            var prepUI = FindObjectOfType<DayPrepUI>();
            if (prepUI != null)
                prepUI.Show(_dayCount > 1);
            else
                Debug.LogError("[GameManager] DayPrepUI not found!");
        }

        public void StartDay()
        {
            _state = GameState.Playing;
            _dayTimer = _dayDuration;

            var prepUI = FindObjectOfType<DayPrepUI>();
            if (prepUI != null)
                prepUI.Hide();

            Debug.Log($"[GameManager] Day {_dayCount} started. {_dayDuration}s timer. Couriers: {CourierManager.Instance.ActiveCourierCount}");
        }

        private void UpdatePlaying()
        {
            _dayTimer -= Time.deltaTime;

            if (Input.GetMouseButtonDown(1))
            {
                HandleRightClick(Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift));
            }

            if (Input.GetKeyDown(KeyCode.H))
            {
                CancelAllDeliveries();
            }

            if (_dayTimer <= 0f)
            {
                EndDay();
            }
        }

        private void HandleRightClick(bool isShift)
        {
            Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mouseWorld.z = 0f;
            Vector2 clickPos = new Vector2(mouseWorld.x, mouseWorld.y);

            HouseState closestHouse = null;
            float closestDist = float.MaxValue;

            foreach (var house in _allHouses)
            {
                if (house == null) continue;
                float dist = Vector2.Distance(clickPos, (Vector2)house.transform.position);
                if (dist < _houseClickRadius && dist < closestDist)
                {
                    closestDist = dist;
                    closestHouse = house;
                }
            }

            if (closestHouse == null)
            {
                Debug.Log("[GameManager] No house near click position.");
                return;
            }

            Vector2 targetPos = closestHouse.transform.position;

            var controller = CourierManager.Instance.GetControllerForCourier(_selectedCourierIndex);
            if (controller == null) return;

            if (isShift)
            {
                if (DeliveryQueue.Instance != null)
                {
                    DeliveryQueue.Instance.SetActiveCourier(controller);
                    DeliveryQueue.Instance.Enqueue(targetPos, controller);
                    DeliveryQueue.Instance.StartProcessing(controller);
                    Debug.Log($"[GameManager] Shift+Click: Enqueued delivery to {closestHouse.name}");
                }
            }
            else
            {
                if (DeliveryQueue.Instance != null)
                    DeliveryQueue.Instance.Clear();

                controller.Stop();

                var (path, edges) = _deliveryManager.FindPathToDestination(targetPos);
                controller.SetPath(path, edges,
                    onDestinationReached: () =>
                    {
                        OnSingleDeliveryComplete(controller, closestHouse);
                        if (DeliveryQueue.Instance != null && DeliveryQueue.Instance.PendingCount > 0)
                        {
                            DeliveryQueue.Instance.StartProcessing(controller);
                        }
                        else
                        {
                            var (retPath, retEdges) = _deliveryManager.FindPathFromTo(
                                closestHouse.transform.position, _deliveryManager.CenterPosition);
                            if (retPath.Count >= 2)
                                controller.SetPath(retPath, retEdges);
                        }
                    });
                Debug.Log($"[GameManager] Right-click: Path set to {closestHouse.name}");
            }
        }

        private void OnSingleDeliveryComplete(CourierController controller, HouseState house)
        {
            if (controller.CourierState != null)
            {
                float reward = _baseDeliveryReward * house.RewardMultiplier * controller.CourierState.ActiveMoneyMul;
                CourierManager.Instance.AddMoney(reward);
                CourierManager.Instance.RecordDelivery();
                Debug.Log($"[GameManager] Delivery to {house.name} complete. Reward: {reward:F0}");
            }
            house.OnDelivery();
        }

        private void CancelAllDeliveries()
        {
            if (DeliveryQueue.Instance != null)
                DeliveryQueue.Instance.Clear();

            foreach (var ctrl in CourierManager.Instance.ActiveControllers)
            {
                if (ctrl != null)
                    ctrl.Stop();
            }

            Debug.Log("[GameManager] All deliveries cancelled (H key).");
        }

        private void EndDay()
        {
            _state = GameState.DayEnd;

            CancelAllDeliveries();

            float totalEarned = CourierManager.Instance.TotalMoney;
            int deliveries = CourierManager.Instance.TotalDeliveries;

            Debug.Log($"[GameManager] Day {_dayCount} ended. Deliveries: {deliveries}, Gold: {totalEarned:F0}");

            _dayCount++;
            EnterDayPrep();
        }

        public void SelectCourierByIndex(int index)
        {
            if (index >= 0 && index < CourierManager.Instance.ActiveCouriers.Count)
            {
                _selectedCourierIndex = index;
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
    }
}
