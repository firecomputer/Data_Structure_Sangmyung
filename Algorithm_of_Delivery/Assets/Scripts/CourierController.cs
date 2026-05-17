using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using AlgorithmOfDelivery.Core;
using AlgorithmOfDelivery.Game;
using static AlgorithmOfDelivery.Maze.MSTGenerator;

namespace AlgorithmOfDelivery.Maze
{
    public class CourierController : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float _baseSpeed = 25f;

        private List<Vector2> _path;
        private List<PathEdge> _pathEdges;
        private int _currentWaypointIndex;
        private Vector2 _currentWaypoint;
        private bool _isMoving;
        private bool _isResting;
        private bool _isReturning;

        private System.Action _onDestinationReached;
        private System.Action _onReturnToCenter;
        private System.Action<Vector2, Vector2> _onWaypointReached;

        private CourierState _courierState;
        private float _currentEdgeProgress;
        private int _courierIndex = -1;

        public bool HasVisitedCenterSinceLastDelivery { get; set; } = true;
        public bool HasPackage { get; set; }

        public float Speed => _baseSpeed * (_courierState != null ? _courierState.ActiveSpeedMul : 1f);
        public bool IsMoving => _isMoving;
        public bool IsResting => _isResting;
        public bool IsIdle => !_isMoving && !_isResting;
        public CourierState CourierState => _courierState;
        public Vector2 CurrentPosition => transform.position;

        public void SetCourierState(CourierState state)
        {
            _courierState = state;
        }

        public void SetCourierIndex(int index)
        {
            _courierIndex = index;
        }

        public int GetCourierIndex()
        {
            return _courierIndex;
        }

        private void Start()
        {
            if (NotificationManager.Instance != null)
                NotificationManager.Instance.OnNotificationAdded += OnNotificationAdded;
            else
                StartCoroutine(SubscribeToNotifications());
        }

        private IEnumerator SubscribeToNotifications()
        {
            while (NotificationManager.Instance == null)
                yield return null;
            NotificationManager.Instance.OnNotificationAdded += OnNotificationAdded;
        }

        private void OnDestroy()
        {
            if (NotificationManager.Instance != null)
                NotificationManager.Instance.OnNotificationAdded -= OnNotificationAdded;
        }

        private void OnNotificationAdded(HouseState house)
        {
            Debug.Log($"[CourierController] OnNotificationAdded for {house.name}, courierIdx={_courierIndex}, isIdle={IsIdle}");
            CheckAutoMove(house);
        }

        public void CheckAutoMove(HouseState house)
        {
            if (!IsIdle || _courierState == null)
            {
                Debug.Log($"[CourierController] CheckAutoMove skipped: isIdle={IsIdle}, hasState={_courierState != null}");
                return;
            }

            var pm = PlanningManager.Instance;
            if (pm == null)
            {
                Debug.Log("[CourierController] CheckAutoMove skipped: PlanningManager null");
                return;
            }

            if (_courierIndex < 0)
            {
                Debug.Log("[CourierController] CheckAutoMove skipped: courierIndex < 0");
                return;
            }

            var planned = pm.GetPlannedHousesForCourier(_courierIndex);
            if (planned.Count == 0)
            {
                Debug.Log($"[CourierController] CheckAutoMove skipped: no planned houses for courier {_courierIndex}");
                return;
            }

            foreach (var ph in planned)
            {
                if (ph.House == house)
                {
                    Debug.Log($"[CourierController] CheckAutoMove: matched house {house.name}, priority {ph.Priority}, starting delivery");
                    var dm = DeliveryManager.Instance;
                    if (dm == null) return;

                    var (path, edges) = dm.FindPathToDestination(transform.position, house.transform.position);
                    Debug.Log($"[CourierController] CheckAutoMove: path count={path.Count}");
                    if (path.Count >= 2)
                    {
                        HasPackage = true;
                        SetPath(path, edges,
                            onDestinationReached: () =>
                            {
                                HasPackage = false;
                                if (DeliveryManager.Instance != null)
                                {
                                    var nm = NotificationManager.Instance;
                                    if (nm != null)
                                        nm.RemoveNotification(house);
                                    var gm = GameManager.Instance;
                                    if (gm != null)
                                        gm.OnSingleDeliveryCompleteExternal(this, house);
                                }
                                var (retPath, retEdges) = dm.FindPathFromTo(
                                    house.transform.position, dm.CenterPosition);
                                if (retPath.Count >= 2)
                                    SetPath(retPath, retEdges,
                                        onDestinationReached: () =>
                                        {
                                            HasVisitedCenterSinceLastDelivery = true;
                                        });
                            });
                    }
                    break;
                }
            }
        }

        public void SetSpeed(float speed)
        {
            _baseSpeed = speed;
        }

        public void SetPath(List<Vector2> path, List<PathEdge> pathEdges = null,
            System.Action onDestinationReached = null, System.Action onReturnToCenter = null,
            System.Action<Vector2, Vector2> onWaypointReached = null)
        {
            if (path == null || path.Count < 2)
            {
                _isMoving = false;
                return;
            }

            _path = path;
            _pathEdges = pathEdges;
            _currentWaypointIndex = 0;
            _currentWaypoint = _path[0];
            if (Vector2.Distance(transform.position, _path[0]) > 2f)
                transform.position = new Vector3(_path[0].x, _path[0].y, 0f);
            _isMoving = true;
            _isResting = false;
            _isReturning = false;
            _currentEdgeProgress = 0f;
            _onDestinationReached = onDestinationReached;
            _onReturnToCenter = onReturnToCenter;
            _onWaypointReached = onWaypointReached;
        }

        private void Update()
        {
            if (_courierState != null && _courierState.IsExhausted && !_isResting)
            {
                _isResting = true;
                _isMoving = false;
                Debug.Log($"[CourierController] {_courierState.Name} is exhausted! Entering rest mode.");
            }

            if (_isResting && _courierState != null)
            {
                _courierState.RecoverFatigue(Time.deltaTime, 1.5f);
                if (_courierState.Fatigue >= _courierState.MaxFatigue * 0.5f)
                {
                    _isResting = false;
                    _isMoving = _path != null && _path.Count > 0;
                    Debug.Log($"[CourierController] {_courierState.Name} finished resting. Resuming.");
                }
                return;
            }

            if (!_isMoving && _courierState != null)
            {
                RecoverFatigue();
                return;
            }

            if (_path == null || _path.Count == 0)
                return;

            MoveAlongPath();
        }

        private void MoveAlongPath()
        {
            if (_currentWaypointIndex >= _path.Count - 1)
            {
                _isMoving = false;
                return;
            }

            _currentWaypoint = _path[_currentWaypointIndex + 1];
            Vector2 targetPos = _currentWaypoint;
            Vector2 currentPos = transform.position;
            Vector2 direction = (targetPos - currentPos).normalized;

            float effectiveSpeed = _baseSpeed;
            if (_courierState != null)
            {
                effectiveSpeed *= _courierState.ActiveSpeedMul;

                if (_pathEdges != null && _currentWaypointIndex < _pathEdges.Count)
                {
                    TerrainType terrain = _pathEdges[_currentWaypointIndex].Terrain;
                    effectiveSpeed *= _courierState.GetTerrainMultiplier(terrain);
                }
            }

            float step = effectiveSpeed * Time.deltaTime;
            Vector2 newPos = Vector2.MoveTowards(currentPos, targetPos, step);

            transform.position = new Vector3(newPos.x, newPos.y, 0f);

            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);

            if (_courierState != null)
            {
                _courierState.DrainFatigue(Time.deltaTime);
            }

            if (Vector2.Distance(newPos, targetPos) < 0.1f)
            {
                _currentWaypointIndex++;

                _onWaypointReached?.Invoke(_path[_currentWaypointIndex - 1], _path[_currentWaypointIndex]);

                if (_currentWaypointIndex >= _path.Count - 1)
                {
                    _isMoving = false;
                    if (!_isReturning)
                    {
                        _onDestinationReached?.Invoke();
                    }
                    else
                    {
                        _onReturnToCenter?.Invoke();
                        _onReturnToCenter = null;
                    }
                }
            }
        }

        public void ReturnToCenter()
        {
            if (_path != null && _path.Count > 0)
            {
                List<Vector2> returnPath = new List<Vector2>(_path);
                returnPath.Reverse();
                _path = returnPath;

                if (_pathEdges != null)
                {
                    _pathEdges.Reverse();
                }

                _currentWaypointIndex = 0;
                _isReturning = true;
            }
        }

        public void StopMovement()
        {
            _isMoving = false;
            _onReturnToCenter?.Invoke();
            _onReturnToCenter = null;
        }

        public void Stop()
        {
            _isMoving = false;
        }

        public void ToggleRest()
        {
            _isResting = !_isResting;
            if (_isResting)
            {
                Debug.Log($"[CourierController] {(_courierState != null ? _courierState.Name : "Courier")} is resting.");
            }
            else
            {
                _isMoving = true;
                Debug.Log($"[CourierController] {(_courierState != null ? _courierState.Name : "Courier")} resumes movement.");
            }
        }

        public void ForceRest()
        {
            _isResting = true;
            _isMoving = false;
        }

        public void ForceResume()
        {
            _isResting = false;
            _isMoving = true;
        }

        public void RecoverFatigue()
        {
            if (_courierState != null)
            {
                _courierState.RecoverFatigue(Time.deltaTime);
            }
        }
    }
}
