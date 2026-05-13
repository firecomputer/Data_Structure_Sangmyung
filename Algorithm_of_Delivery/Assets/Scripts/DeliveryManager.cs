using System.Collections.Generic;
using UnityEngine;
using AlgorithmOfDelivery.Core;

namespace AlgorithmOfDelivery.Maze
{
    public class DeliveryManager : MonoBehaviour
    {
        [Header("Center Settings")]
        [SerializeField] private Sprite _centerSprite;
        [SerializeField] private float _centerScale = 3f;
        [SerializeField] private Color _centerColor = Color.yellow;

        [Header("Truck Settings")]
        [SerializeField] private Sprite _truckSprite;
        [SerializeField] private float _truckScale = 1f;
        [SerializeField] private float _truckSpeed = 15f;

        [Header("Maze Reference")]
        [SerializeField] private MazeManager _mazeManager;

        [Header("Manual Map Settings")]
        [SerializeField] private bool _useManualCenter = true;
        [SerializeField] private Vector2 _manualCenterPosition = new Vector2(0f, -360f);

        [Header("Background Settings")]
        [SerializeField] private Color _backgroundColor = new Color(0.35f, 0.65f, 0.35f);

        [Header("Altitude Node Settings")]
        [SerializeField] private Sprite _altitudeLowSprite;
        [SerializeField] private Sprite _altitudeMidSprite;
        [SerializeField] private Sprite _altitudeHighSprite;
        [SerializeField] private float _altitudeNodeScale = 2f;
        [SerializeField] private float _midAltitudeThreshold = 0.4f;
        [SerializeField] private float _highAltitudeThreshold = 0.8f;

        [SerializeField] private Vector2 _centerPosition = new Vector2(50f, 50f);

        private GameObject _centerObject;
        private GameObject _altitudeNodeParent;
        private List<GameObject> _altitudeNodes = new List<GameObject>();
        private List<CourierController> _activeCouriers = new List<CourierController>();
        private AStarPathfinder _pathfinder;
        private Transform _courierParent;
        private bool _isInitialized;

        public Vector2 CenterPosition => _centerPosition;
        public AStarPathfinder Pathfinder => _pathfinder;
        public bool IsInitialized => _isInitialized;
        public MazeManager MazeManager => _mazeManager;
        public Sprite TruckSprite => _truckSprite;
        public float TruckScale => _truckScale;
        public float TruckSpeed => _truckSpeed;

        public static DeliveryManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            _pathfinder = new AStarPathfinder();
        }

        private void Start()
        {
            if (_mazeManager != null)
            {
                _mazeManager.GenerateMaze();
            }
        }

        private void LateUpdate()
        {
            if (!_isInitialized && _mazeManager != null)
            {
                List<Vector2> nodePositions = _mazeManager.GetNodePositions();
                if (nodePositions.Count > 0)
                {
                    BuildPathfinderGraph();
                    CreateAltitudeNodes();
                    _isInitialized = true;
                }
            }
        }

        private void OnDestroy()
        {
            _activeCouriers.Clear();
            if (Instance == this)
                Instance = null;
        }

        public CourierController CreateCourier(CourierState courierState)
        {
            if (_courierParent == null)
            {
                _courierParent = new GameObject("Couriers").transform;
                _courierParent.SetParent(transform);
            }

            GameObject courierObj = new GameObject($"Courier_{courierState.Name}");
            courierObj.transform.SetParent(_courierParent);
            courierObj.transform.localScale = Vector3.one * _truckScale;

            SpriteRenderer sr = courierObj.AddComponent<SpriteRenderer>();
            sr.sprite = _truckSprite;
            sr.sortingOrder = 10;

            CourierController controller = courierObj.AddComponent<CourierController>();
            controller.SetCourierState(courierState);
            controller.SetSpeed(_truckSpeed);

            _activeCouriers.Add(controller);
            return controller;
        }

        public void RemoveCourier(CourierController controller)
        {
            _activeCouriers.Remove(controller);
            if (controller != null && controller.gameObject != null)
                Destroy(controller.gameObject);
        }

        public (List<Vector2> Path, List<PathEdge> Edges) FindPathToDestination(Vector2 destination)
        {
            Vector2 startNode = _pathfinder.GetClosestNode(_centerPosition);
            Vector2 destNode = _pathfinder.GetClosestNode(destination);

            if (Vector2.Distance(startNode, destNode) < 1f)
            {
                return (new List<Vector2> { startNode, destNode }, new List<PathEdge>());
            }

            return _pathfinder.FindPathWithEdges(startNode, destNode);
        }

        public (List<Vector2> Path, List<PathEdge> Edges) FindPathFromTo(Vector2 from, Vector2 to)
        {
            Vector2 startNode = _pathfinder.GetClosestNode(from);
            Vector2 destNode = _pathfinder.GetClosestNode(to);
            return _pathfinder.FindPathWithEdges(startNode, destNode);
        }

        private void BuildPathfinderGraph()
        {
            List<MSTGenerator.Edge> edges = _mazeManager.GetMSTEdges();
            _pathfinder.BuildGraph(edges);

            if (_useManualCenter)
            {
                _centerPosition = _manualCenterPosition;
            }
            else
            {
                List<Vector2> nodePositions = _mazeManager.GetNodePositions();
                if (nodePositions.Count > 0)
                {
                    float minDist = float.MaxValue;
                    Vector2 closestNode = nodePositions[0];
                    foreach (var node in nodePositions)
                    {
                        float dist = Vector2.Distance(node, _centerPosition);
                        if (dist < minDist)
                        {
                            minDist = dist;
                            closestNode = node;
                        }
                    }
                    _centerPosition = closestNode;
                }
            }

            CreateCenterSprite();
        }

        private void CreateCenterSprite()
        {
            if (_centerObject != null)
                Destroy(_centerObject);

            _centerObject = new GameObject("Center");
            _centerObject.transform.position = new Vector3(_centerPosition.x, _centerPosition.y, 0f);
            _centerObject.transform.SetParent(transform);
            _centerObject.transform.localScale = Vector3.one * _centerScale;

            SpriteRenderer sr = _centerObject.AddComponent<SpriteRenderer>();
            sr.sprite = _centerSprite != null ? _centerSprite : CreateDefaultCenterSprite();
            sr.color = _centerColor;
            sr.sortingOrder = 5;
        }

        private void CreateAltitudeNodes()
        {
            if (_altitudeNodeParent != null)
                Destroy(_altitudeNodeParent);

            _altitudeNodeParent = new GameObject("AltitudeNodes");
            _altitudeNodeParent.transform.SetParent(transform);

            foreach (var node in _altitudeNodes)
            {
                if (node != null) Destroy(node);
            }
            _altitudeNodes.Clear();

            List<Vector2> nodePositions = _mazeManager.GetNodePositions();
            float maxAlt = _mazeManager.MaxAltitude > 0 ? _mazeManager.MaxAltitude : 500f;

            foreach (var nodePos in nodePositions)
            {
                float altitude = _mazeManager.GetAltitudeAtPosition(nodePos);
                float normalizedAlt = Mathf.Clamp01(altitude / maxAlt);

                GameObject altNode = new GameObject($"AltitudeNode_{nodePos.x:F0}_{nodePos.y:F0}");
                altNode.transform.SetParent(_altitudeNodeParent.transform);
                altNode.transform.position = new Vector3(nodePos.x, nodePos.y, 1f);
                altNode.transform.localScale = Vector3.one * _altitudeNodeScale;

                SpriteRenderer sr = altNode.AddComponent<SpriteRenderer>();
                sr.sprite = GetAltitudeSprite(normalizedAlt);
                sr.sortingOrder = -5;

                _altitudeNodes.Add(altNode);
            }

            Camera.main.backgroundColor = _backgroundColor;
        }

        private Sprite GetAltitudeSprite(float normalizedAltitude)
        {
            if (normalizedAltitude >= _highAltitudeThreshold)
                return _altitudeHighSprite != null ? _altitudeHighSprite : CreateDefaultAltitudeSprite(2);
            if (normalizedAltitude >= _midAltitudeThreshold)
                return _altitudeMidSprite != null ? _altitudeMidSprite : CreateDefaultAltitudeSprite(1);
            return _altitudeLowSprite != null ? _altitudeLowSprite : CreateDefaultAltitudeSprite(0);
        }

        private Sprite CreateDefaultAltitudeSprite(int level)
        {
            int size = 32;
            Texture2D tex = new Texture2D(size, size);
            Color[] colors = new Color[size * size];

            Color baseColor = level switch
            {
                0 => new Color(0.7f, 0.95f, 0.7f),
                1 => new Color(0.5f, 0.7f, 0.5f),
                2 => new Color(0.2f, 0.5f, 0.2f),
                _ => Color.green
            };

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distFromCenter = Vector2.Distance(new Vector2(x, y), new Vector2(size / 2f, size / 2f));
                    float alpha = distFromCenter < size / 2f - 2 ? 1f : 0f;
                    colors[y * size + x] = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
                }
            }

            tex.SetPixels(colors);
            tex.Apply();

            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        private Sprite CreateDefaultCenterSprite()
        {
            Texture2D tex = new Texture2D(64, 64);
            Color[] colors = new Color[64 * 64];
            for (int i = 0; i < colors.Length; i++)
                colors[i] = Color.yellow;
            tex.SetPixels(colors);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f), 64);
        }
    }
}
