using System.Collections.Generic;
using UnityEngine;
using AlgorithmOfDelivery.Maze;

namespace AlgorithmOfDelivery.Game
{
    public class DeliveryQueue : MonoBehaviour
    {
        private Queue<Vector2> _destinations = new Queue<Vector2>();
        private CourierController _activeCourier;
        private bool _isProcessing;
        private Vector2 _currentDestination;
        private bool _returningToCenter;

        public int PendingCount => _destinations.Count;
        public bool IsProcessing => _isProcessing;
        public CourierController ActiveCourier => _activeCourier;

        public static DeliveryQueue Instance { get; private set; }

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

        public void Enqueue(Vector2 destination, CourierController courier = null)
        {
            _destinations.Enqueue(destination);

            if (courier != null && _activeCourier == null)
            {
                _activeCourier = courier;
            }
        }

        public void StartProcessing(CourierController courier = null)
        {
            if (courier != null)
                _activeCourier = courier;

            if (!_isProcessing && _activeCourier != null && _destinations.Count > 0)
            {
                ProcessNext();
            }
        }

        public void SetActiveCourier(CourierController courier)
        {
            _activeCourier = courier;
        }

        public void Clear()
        {
            _destinations.Clear();
            _isProcessing = false;
            _returningToCenter = false;

            if (_activeCourier != null)
            {
                _activeCourier.Stop();
            }
        }

        private void ProcessNext()
        {
            if (_activeCourier == null || _destinations.Count == 0)
            {
                _isProcessing = false;
                return;
            }

            _isProcessing = true;

            Vector2 from;
            if (_returningToCenter)
            {
                from = DeliveryManager.Instance.CenterPosition;
                _returningToCenter = false;
            }
            else if (_activeCourier.IsMoving)
            {
                from = _activeCourier.transform.position;
            }
            else
            {
                from = DeliveryManager.Instance.CenterPosition;
            }

            _currentDestination = _destinations.Dequeue();

            var (path, edges) = DeliveryManager.Instance.FindPathFromTo(from, _currentDestination);

            _activeCourier.SetPath(path, edges,
                onDestinationReached: () => OnDelivered());
        }

        private void OnDelivered()
        {
            Vector2 pos = _activeCourier.transform.position;
            float radius = 12f;

            var houses = FindObjectsOfType<HouseState>();
            foreach (var house in houses)
            {
                float dist = Vector2.Distance(pos, house.transform.position);
                if (dist < radius)
                {
                    if (_activeCourier.HasVisitedCenterSinceLastDelivery)
                    {
                        var courierState = _activeCourier.CourierState;
                        if (courierState != null)
                        {
                            float baseReward = 100f;
                            float reward = baseReward * house.RewardMultiplier * courierState.ActiveMoneyMul;
                            CourierManager.Instance.AddMoney(reward);
                            CourierManager.Instance.RecordDelivery();
                            _activeCourier.HasVisitedCenterSinceLastDelivery = false;
                            Debug.Log($"[DeliveryQueue] Delivered! Reward: {reward:F0}");
                        }
                    }
                    else
                    {
                        Debug.Log("[DeliveryQueue] Delivery failed: courier must return to center first.");
                    }
                    house.OnDelivery();
                    break;
                }
            }

            if (_destinations.Count > 0)
            {
                _returningToCenter = true;
                var (path, edges) = DeliveryManager.Instance.FindPathFromTo(_activeCourier.transform.position,
                    DeliveryManager.Instance.CenterPosition);
                _activeCourier.SetPath(path, edges,
                    onDestinationReached: () =>
                    {
                        _activeCourier.HasVisitedCenterSinceLastDelivery = true;
                        ProcessNext();
                    });
            }
            else
            {
                _isProcessing = false;
                var (retPath, retEdges) = DeliveryManager.Instance.FindPathFromTo(
                    _activeCourier.transform.position, DeliveryManager.Instance.CenterPosition);
                if (retPath.Count >= 2)
                    _activeCourier.SetPath(retPath, retEdges,
                        onDestinationReached: () =>
                        {
                            _activeCourier.HasVisitedCenterSinceLastDelivery = true;
                        });
            }
        }
    }
}
