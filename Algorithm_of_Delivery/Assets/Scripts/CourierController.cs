using System.Collections.Generic;
using UnityEngine;
using AlgorithmOfDelivery.Core;
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

        public float Speed => _baseSpeed * (_courierState != null ? _courierState.ActiveSpeedMul : 1f);
        public bool IsMoving => _isMoving;
        public bool IsResting => _isResting;
        public CourierState CourierState => _courierState;
        public Vector2 CurrentPosition => transform.position;

        public void SetCourierState(CourierState state)
        {
            _courierState = state;
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
                RecoverFatigue();
                if (_courierState.Fatigue >= _courierState.MaxFatigue * 0.5f)
                {
                    _isResting = false;
                    _isMoving = _path != null && _path.Count > 0;
                    Debug.Log($"[CourierController] {_courierState.Name} finished resting. Resuming.");
                }
                return;
            }

            if (!_isMoving || _path == null || _path.Count == 0)
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
