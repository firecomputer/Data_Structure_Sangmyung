using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using AlgorithmOfDelivery.Core;
using AlgorithmOfDelivery.Maze;
using AlgorithmOfDelivery.UI;
using System.Collections;

namespace AlgorithmOfDelivery.Game
{
    public enum GameState
    {
        Setup,
        DayPrep,
        Planning,
        Playing,
        DayEnd,
        Paused
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
        [SerializeField] private float _houseClickRadius = 15f;

        [Header("Plan Mode")]
        [SerializeField] private Button _planDoneButton;

        private GameState _state;
        private float _dayTimer;
        private int _dayCount;
        private int _selectedCourierIndex = -1;

        private List<HouseState> _allHouses = new List<HouseState>();
        private DayPrepUI _prepUI;
        public DayPrepUI PrepUI
        {
            get => _prepUI;
            set => _prepUI = value;
        }

        public GameState State => _state;
        public int DayCount => _dayCount;
        public float DayTimer => _dayTimer;
        public float DayDuration => _dayDuration;
        public int SelectedCourierIndex => _selectedCourierIndex;
        public float BaseDeliveryReward => _baseDeliveryReward;

        private PlanningManager _planningManager;
        private NotificationManager _notificationManager;
        private NotificationUI _notificationUI;
        private DashboardUI _dashboardUI;
        private InGameBottomBar _bottomBar;
        private CameraController _cameraController;
        private bool _isDashboardVisible = true;

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

            _cameraController = FindObjectOfType<CameraController>();

            if (CourierManager.Instance != null)
            {
                var starter = CourierManager.Instance.StartGame();
                CourierManager.Instance.CreateCourierController(0);
                _selectedCourierIndex = 0;
            }

            StartCoroutine(WaitForInitialization());
        }

        private void LateUpdate()
        {
            if (_bottomBar == null)
                _bottomBar = FindObjectOfType<InGameBottomBar>(true);
            if (_dashboardUI == null)
                _dashboardUI = FindObjectOfType<DashboardUI>(true);
        }

        private System.Collections.IEnumerator WaitForInitialization()
        {
            while (_deliveryManager == null || !_deliveryManager.IsInitialized)
            {
                yield return null;
            }

            CacheHouses();

            _planningManager = FindObjectOfType<PlanningManager>();
            _notificationManager = FindObjectOfType<NotificationManager>();
            _notificationUI = FindObjectOfType<NotificationUI>();

            while (GameSetup.Instance != null && !GameSetup.Instance.DayPrepReady)
            {
                yield return null;
            }

            if (_state == GameState.Setup)
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

        private void TryCachePrepUI()
        {
            if (_prepUI == null)
                _prepUI = FindObjectOfType<DayPrepUI>(true);
        }

        private void Update()
        {
            switch (_state)
            {
                case GameState.Planning:
                    UpdatePlanning();
                    break;
                case GameState.Playing:
                    UpdatePlaying();
                    break;
                case GameState.Paused:
                    UpdatePaused();
                    break;
            }
        }

        public void EnterDayPrep()
        {
            _state = GameState.DayPrep;
            _bottomBar?.HideCourierBubble();
            GameSetup.Instance?.HideIntroSequence();

            CacheHouses();

            TryCachePrepUI();
            if (_prepUI != null)
            {
                _prepUI.Show(_dayCount > 1);
            }
            else
            {
                Debug.LogWarning("[GameManager] DayPrepUI not found, skipping preparation phase.");
                StartDay();
            }
        }

        public void EnterPlanning()
        {
            TryCachePrepUI();
            _state = GameState.Planning;
            _bottomBar?.HideCourierBubble();

            var courierCount = CourierManager.Instance.ActiveCourierCount;
            for (int i = 0; i < courierCount; i++)
            {
                CourierManager.Instance.CreateCourierController(i);
            }

            _planningManager.ClearAll();

            _prepUI?.BeginPlanningMode(_planDoneButton);

            if (_dashboardUI != null)
                _dashboardUI.gameObject.SetActive(false);

            if (_bottomBar != null)
                _bottomBar.gameObject.SetActive(true);

            try
            {
                if (_planDoneButton != null)
                    _planDoneButton.onClick.RemoveAllListeners();
                _planDoneButton.onClick.AddListener(ExitPlanning);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[GameManager] Failed to wire planDoneButton: {ex.Message}");
            }

            Debug.Log("[GameManager] Entered Planning mode.");
        }

        private void ExitPlanning()
        {
            TryCachePrepUI();
            _planningManager.DeselectCourier();

            _prepUI?.EndPlanningMode(_planDoneButton);

            if (_dashboardUI != null)
                _dashboardUI.gameObject.SetActive(true);

            Debug.Log("[GameManager] Exited Planning mode.");
            EnterDayPrep();
        }

        private void UpdatePlanning()
        {
            if (Input.GetMouseButtonDown(0))
            {
                if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                    return;

                SelectCourierByIndex(-1);
                _planningManager.DeselectCourier();
                if (_dashboardUI != null)
                    _dashboardUI.gameObject.SetActive(false);
            }

            if (Input.GetMouseButtonDown(1))
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

                if (closestHouse == null) return;

                int idx = _planningManager.SelectedCourierIndex;
                if (idx < 0) return;

                int priority = _planningManager.AssignPriority(closestHouse, idx);
                if (priority > 0)
                {
                    Debug.Log($"[GameManager] Assigned priority {priority} to {closestHouse.name} for courier {idx}");
                }

                if (_planningManager.IsFullyPlanned(idx))
                {
                    _planningManager.DeselectCourier();
                    if (_dashboardUI != null)
                        _dashboardUI.gameObject.SetActive(false);
                }
            }
        }

        public void StartDay()
        {
            _state = GameState.Playing;
            _dayTimer = _dayDuration;
            Time.timeScale = 1f;
            _bottomBar?.HideCourierBubble();

            TryCachePrepUI();
            if (_prepUI != null)
                _prepUI.Hide();

            if (_planningManager != null)
                _planningManager.ClearVisualIndicators();

            var courierCount = CourierManager.Instance.ActiveCourierCount;
            if (_selectedCourierIndex < 0 || _selectedCourierIndex >= courierCount)
                _selectedCourierIndex = 0;

            if (_dashboardUI != null)
                _dashboardUI.gameObject.SetActive(true);

            if (_notificationManager != null)
            {
                _notificationManager.CacheHouses(_allHouses);
                _notificationManager.StartSpawning();
            }

            Debug.Log($"[GameManager] Day {_dayCount} started. {_dayDuration}s timer. Couriers: {courierCount}");
        }

        private void UpdatePlaying()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                PauseGame();
                return;
            }

            _dayTimer = Mathf.Max(0f, _dayTimer - Time.deltaTime);

            if (Input.GetMouseButtonDown(0))
            {
                if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                    return;

                SelectCourierByIndex(-1);
                if (_dashboardUI != null)
                    _dashboardUI.gameObject.SetActive(false);
            }

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

        private void PauseGame()
        {
            _state = GameState.Paused;
            Time.timeScale = 0f;
            Debug.Log("[GameManager] Game paused.");
        }

        private void ResumeGame()
        {
            _state = GameState.Playing;
            Time.timeScale = 1f;
            Debug.Log("[GameManager] Game resumed.");
        }

        private void UpdatePaused()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                ResumeGame();
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

                float distToCenter = Vector2.Distance(controller.transform.position, _deliveryManager.CenterPosition);
                bool atCenter = distToCenter < 20f;

                if (controller.HasPackage || atCenter)
                {
                    if (atCenter)
                        controller.HasVisitedCenterSinceLastDelivery = true;

                    var (path, edges) = _deliveryManager.FindPathToDestination(controller.transform.position, targetPos);
                    controller.HasPackage = true;
                    controller.SetPath(path, edges,
                        onDestinationReached: () =>
                        {
                            OnSingleDeliveryComplete(controller, closestHouse);
                            controller.HasPackage = false;
                            ReturnToCenterAfterDelivery(controller, closestHouse);
                        });
                    Debug.Log($"[GameManager] Right-click: Direct path to {closestHouse.name}");
                }
                else
                {
                    var (pathToCenter, edgesToCenter) = _deliveryManager.FindPathFromTo(
                        controller.transform.position, _deliveryManager.CenterPosition);
                    if (pathToCenter.Count < 2)
                    {
                        controller.HasVisitedCenterSinceLastDelivery = true;
                        var (path, edges) = _deliveryManager.FindPathToDestination(
                            controller.transform.position, targetPos);
                        controller.HasPackage = true;
                        controller.SetPath(path, edges,
                            onDestinationReached: () =>
                            {
                                OnSingleDeliveryComplete(controller, closestHouse);
                                controller.HasPackage = false;
                                ReturnToCenterAfterDelivery(controller, closestHouse);
                            });
                    }
                    else
                    {
                        controller.SetPath(pathToCenter, edgesToCenter,
                            onDestinationReached: () =>
                            {
                                controller.HasVisitedCenterSinceLastDelivery = true;
                                var (path, edges) = _deliveryManager.FindPathFromTo(
                                    _deliveryManager.CenterPosition, targetPos);
                                controller.HasPackage = true;
                                controller.SetPath(path, edges,
                                    onDestinationReached: () =>
                                    {
                                        OnSingleDeliveryComplete(controller, closestHouse);
                                        controller.HasPackage = false;
                                        ReturnToCenterAfterDelivery(controller, closestHouse);
                                    });
                            });
                    }
                    Debug.Log($"[GameManager] Right-click (no package): Center first then {closestHouse.name}");
                }
            }
        }

        private void ReturnToCenterAfterDelivery(CourierController controller, HouseState house)
        {
            if (DeliveryQueue.Instance != null && DeliveryQueue.Instance.PendingCount > 0)
            {
                DeliveryQueue.Instance.StartProcessing(controller);
                return;
            }

            var (retPath, retEdges) = _deliveryManager.FindPathFromTo(
                house.transform.position, _deliveryManager.CenterPosition);
            if (retPath.Count >= 2)
                controller.SetPath(retPath, retEdges,
                    onDestinationReached: () =>
                    {
                        controller.HasVisitedCenterSinceLastDelivery = true;
                        Debug.Log($"[GameManager] Courier arrived at center. Can now deliver again.");
                    });
        }

        private void OnSingleDeliveryComplete(CourierController controller, HouseState house)
        {
            if (controller.CourierState != null)
            {
                if (controller.HasVisitedCenterSinceLastDelivery)
                {
                    float baseReward = _baseDeliveryReward;
                    if (house.PendingNotificationReward > 0)
                    {
                        baseReward = house.PendingNotificationReward;
                        house.PendingNotificationReward = -1f;
                        Debug.Log($"[GameManager] Applied notification reward: {baseReward:F0} for {house.name}");
                    }
                    else
                    {
                        baseReward = 0f;
                    }
                    float reward = baseReward * house.RewardMultiplier * controller.CourierState.ActiveMoneyMul;
                    CourierManager.Instance.AddMoney(reward);
                    CourierManager.Instance.RecordDelivery();
                    controller.HasVisitedCenterSinceLastDelivery = false;
                    Debug.Log($"[GameManager] Delivery to {house.name} complete. Reward: {reward:F0}");
                }
                else
                {
                    Debug.Log($"[GameManager] Delivery to {house.name} failed: courier must return to center first.");
                }
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
            _bottomBar?.HideCourierBubble();

            CancelAllDeliveries();

            if (_notificationManager != null)
                _notificationManager.StopSpawning();

            float totalEarned = CourierManager.Instance.TotalMoney;
            int deliveries = CourierManager.Instance.TotalDeliveries;

            Debug.Log($"[GameManager] Day {_dayCount} ended. Deliveries: {deliveries}, Gold: {totalEarned:F0}");

            TryCachePrepUI();
            if (_prepUI != null)
            {
                _prepUI.ShowDayResult(_dayCount, deliveries, totalEarned);
            }
            else
            {
                Debug.LogWarning("[GameManager] DayPrepUI null at EndDay — skipping result, entering DayPrep directly.");
                EnterDayPrep();
            }

            _dayCount++;
        }

        public void SelectCourierByIndex(int index)
        {
            if (index < 0)
            {
                _selectedCourierIndex = -1;
                _bottomBar?.HideCourierBubble();
                if (_state == GameState.Playing && _dashboardUI != null)
                    _dashboardUI.gameObject.SetActive(false);
                if (_planningManager != null)
                    _planningManager.DeselectCourier();
                return;
            }

            if (index < CourierManager.Instance.ActiveCouriers.Count)
            {
                _selectedCourierIndex = index;
                if (_state == GameState.Playing && _dashboardUI != null)
                    _dashboardUI.gameObject.SetActive(true);
                if (_planningManager != null && _state == GameState.Planning)
                    _planningManager.SelectCourier(index);
            }
        }

        public void OnSingleDeliveryCompleteExternal(CourierController controller, HouseState house)
        {
            OnSingleDeliveryComplete(controller, house);
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
    }
}
