using System.Collections.Generic;
using UnityEngine;

namespace AlgorithmOfDelivery.Maze
{
    public class TruckController : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float _speed = 10f;

        private List<Vector2> _path;
        private int _currentWaypointIndex;
        private Vector2 _currentWaypoint;
        private bool _isMoving;
        private System.Action _onDestinationReached;
        private System.Action _onReturnToCenter;
        private bool _isReturning;

        public float Speed => _speed;
        public bool IsMoving => _isMoving;

        public void SetSpeed(float speed)
        {
            _speed = speed;
        }

        private void Update()
        {
            if (!_isMoving || _path == null || _path.Count == 0)
                return;

            MoveAlongPath();
        }

        public void SetPath(List<Vector2> path, System.Action onDestinationReached = null, System.Action onReturnToCenter = null)
        {
            if (path == null || path.Count < 2)
            {
                _isMoving = false;
                return;
            }

            _path = path;
            _currentWaypointIndex = 0;
            _currentWaypoint = _path[0];
            _isMoving = true;
            _isReturning = false;
            _onDestinationReached = onDestinationReached;
            _onReturnToCenter = onReturnToCenter;

            transform.position = new Vector3(_path[0].x, _path[0].y, 0f);
        }

        private void MoveAlongPath()
        {
            if (_currentWaypointIndex >= _path.Count - 1)
            {
                if (_isReturning)
                {
                    StopMovement();
                    return;
                }
                else
                {
                    ReturnToCenter();
                    return;
                }
            }

            _currentWaypoint = _path[_currentWaypointIndex + 1];
            Vector2 targetPos = _currentWaypoint;
            Vector2 currentPos = transform.position;
            Vector2 direction = (targetPos - currentPos).normalized;

            float step = _speed * Time.deltaTime;
            Vector2 newPos = Vector2.MoveTowards(currentPos, targetPos, step);

            transform.position = new Vector3(newPos.x, newPos.y, 0f);

            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);

            if (Vector2.Distance(newPos, targetPos) < 0.1f)
            {
                _currentWaypointIndex++;
                if (_currentWaypointIndex >= _path.Count - 1)
                {
                    if (_isReturning)
                    {
                        StopMovement();
                    }
                    else
                    {
                        _onDestinationReached?.Invoke();
                        ReturnToCenter();
                    }
                }
            }
        }

        private void ReturnToCenter()
        {
            if (_path != null && _path.Count > 0)
            {
                List<Vector2> returnPath = new List<Vector2>(_path);
                returnPath.Reverse();
                _path = returnPath;
                _currentWaypointIndex = 0;
                _isReturning = true;
            }
        }

        private void StopMovement()
        {
            _isMoving = false;
            _onReturnToCenter?.Invoke();
            _onReturnToCenter = null;
        }

        public void Stop()
        {
            _isMoving = false;
        }
    }
}