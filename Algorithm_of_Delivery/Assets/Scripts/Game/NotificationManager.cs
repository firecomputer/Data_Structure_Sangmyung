using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AlgorithmOfDelivery.UI;

namespace AlgorithmOfDelivery.Game
{
    public class NotificationManager : MonoBehaviour
    {
        public struct NotificationRecord
        {
            public string HouseName;
            public string Message;
            public int Reward;
            public Vector2 HouseWorldPos;
        }

        [SerializeField] private float _minInterval = 8f;
        [SerializeField] private float _maxInterval = 18f;
        [SerializeField] private int _minReward = 50;
        [SerializeField] private int _maxReward = 500;

        private NotificationUI _notificationUI;
        private List<HouseState> _allHouses = new List<HouseState>();
        private HashSet<HouseState> _notifiedHouses = new HashSet<HouseState>();
        private List<NotificationRecord> _history = new List<NotificationRecord>();
        private Coroutine _spawnCoroutine;
        private bool _isSpawning;
        private const int MaxHistoryEntries = 24;

        public event System.Action<HouseState> OnNotificationAdded;

        public static NotificationManager Instance { get; private set; }

        public IReadOnlyList<NotificationRecord> RecentNotifications => _history;

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
            _notificationUI = FindObjectOfType<NotificationUI>();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public void CacheHouses(List<HouseState> houses)
        {
            _allHouses = new List<HouseState>(houses);
        }

        public void StartSpawning()
        {
            if (_isSpawning) return;
            _isSpawning = true;
            _spawnCoroutine = StartCoroutine(SpawnLoop());
            Debug.Log("[NotificationManager] Started spawning notifications.");
        }

        public void StopSpawning()
        {
            _isSpawning = false;
            if (_spawnCoroutine != null)
            {
                StopCoroutine(_spawnCoroutine);
                _spawnCoroutine = null;
            }
            _notifiedHouses.Clear();
            Debug.Log("[NotificationManager] Stopped spawning notifications.");
        }

        private IEnumerator SpawnLoop()
        {
            while (_isSpawning)
            {
                float waitTime = Random.Range(_minInterval, _maxInterval);
                yield return new WaitForSeconds(waitTime);

                SpawnNotification();
            }
        }

        private void SpawnNotification()
        {
            if (_notificationUI == null)
            {
                _notificationUI = FindObjectOfType<NotificationUI>();
                if (_notificationUI == null) return;
            }

            var candidates = new List<HouseState>();
            foreach (var house in _allHouses)
            {
                if (house == null) continue;
                if (!_notifiedHouses.Contains(house))
                    candidates.Add(house);
            }

            if (candidates.Count == 0)
            {
                _notifiedHouses.Clear();
                foreach (var house in _allHouses)
                {
                    if (house == null) continue;
                    candidates.Add(house);
                }
            }

            if (candidates.Count == 0) return;

            var chosen = candidates[Random.Range(0, candidates.Count)];
            _notifiedHouses.Add(chosen);

            int reward = Random.Range(_minReward, _maxReward + 1);
            string message = $"{reward}달러의 소포를 원합니다.";
            chosen.PendingNotificationReward = reward;
            _notificationUI.AddNotification(message, chosen.transform.position);
            _history.Add(new NotificationRecord
            {
                HouseName = chosen.name,
                Message = message,
                Reward = reward,
                HouseWorldPos = chosen.transform.position
            });
            if (_history.Count > MaxHistoryEntries)
            {
                _history.RemoveAt(0);
            }
            Debug.Log($"[NotificationManager] Firing OnNotificationAdded for {chosen.name}, reward={reward}");
            OnNotificationAdded?.Invoke(chosen);

            Debug.Log($"[NotificationManager] Notification spawned for {chosen.name}");
        }

        public bool HasActiveNotification(HouseState house)
        {
            if (_notificationUI == null) return false;
            return _notificationUI.HasNotificationFor(house.transform.position);
        }

        public void RemoveNotification(HouseState house)
        {
            if (_notificationUI != null)
                _notificationUI.RemoveNotificationFor(house.transform.position);
            _notifiedHouses.Remove(house);
        }
    }
}
