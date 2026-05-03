using System.Collections.Generic;
using UnityEngine;

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
        [SerializeField] private int _maxTrucks = 10;
        [SerializeField] private float _spawnInterval = 3f;

    [Header("Maze Reference")]
    [SerializeField] private MazeManager _mazeManager;

    [Header("Background Settings")]
    [SerializeField] private Color _backgroundColor = new Color(0.35f, 0.65f, 0.35f);

    [Header("Altitude Node Settings")]
    [SerializeField] private Sprite _altitudeLowSprite;
    [SerializeField] private Sprite _altitudeMidSprite;
    [SerializeField] private Sprite _altitudeHighSprite;
    [SerializeField] private float _altitudeNodeScale = 2f;
    [SerializeField] private float _midAltitudeThreshold = 0.4f;
    [SerializeField] private float _highAltitudeThreshold = 0.8f;

    [Header("Center Position")]
    [SerializeField] private Vector2 _centerPosition = new Vector2(50f, 50f);

        private GameObject _centerObject;
        private GameObject _altitudeNodeParent;
        private List<GameObject> _altitudeNodes = new List<GameObject>();
        private List<TruckController> _activeTrucks = new List<TruckController>();
        private AStarPathfinder _pathfinder;
        private float _spawnTimer;
        private Transform _truckParent;
        private bool _isInitialized;

        private void Awake()
        {
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

        private void Update()
        {
            if (!_isInitialized) return;

            _spawnTimer += Time.deltaTime;
            if (_spawnTimer >= _spawnInterval && _activeTrucks.Count < _maxTrucks)
            {
                SpawnTruck();
                _spawnTimer = 0f;
            }
        }

        private void BuildPathfinderGraph()
        {
            List<MSTGenerator.Edge> edges = _mazeManager.GetMSTEdges();
            _pathfinder.BuildGraph(edges);

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

        private void SpawnTruck()
        {
            if (_truckParent == null)
            {
                _truckParent = new GameObject("Trucks").transform;
                _truckParent.SetParent(transform);
            }

            List<Vector2> nodePositions = _mazeManager.GetNodePositions();
            if (nodePositions.Count < 2)
                return;

            Vector2 startNode = _pathfinder.GetClosestNode(_centerPosition);
            Vector2 destination = GetRandomDestination();
            Vector2 destNode = _pathfinder.GetClosestNode(destination);

            if (Vector2.Distance(startNode, destNode) < 1f)
            {
                int attempts = 0;
                while (attempts < 10)
                {
                    destination = GetRandomDestination();
                    destNode = _pathfinder.GetClosestNode(destination);
                    if (Vector2.Distance(startNode, destNode) >= 1f)
                        break;
                    attempts++;
                }
            }

            List<Vector2> path = _pathfinder.FindPath(startNode, destNode);
            if (path.Count == 0)
            {
                path = new List<Vector2> { startNode, destNode };
            }

            Debug.Log($"Truck path: {path.Count} waypoints from {startNode} to {destNode}");

            GameObject truckObj = new GameObject("Truck");
            truckObj.transform.SetParent(_truckParent);
            truckObj.transform.localScale = Vector3.one * _truckScale;

            SpriteRenderer sr = truckObj.AddComponent<SpriteRenderer>();
            sr.sprite = _truckSprite;
            sr.sortingOrder = 10;

            TruckController controller = truckObj.AddComponent<TruckController>();
            controller.SetSpeed(_truckSpeed);
            controller.SetPath(path, () => OnTruckDestinationReached(truckObj), () => OnTruckReturnedToCenter(truckObj));

            _activeTrucks.Add(controller);
        }

        private Vector2 GetRandomDestination()
        {
            List<Vector2> nodePositions = _mazeManager.GetNodePositions();
            return nodePositions[Random.Range(0, nodePositions.Count)];
        }

        private void OnTruckDestinationReached(GameObject truckObj)
        {
            Debug.Log("Truck arrived at destination");
        }

        private void OnTruckReturnedToCenter(GameObject truckObj)
        {
            TruckController controller = truckObj.GetComponent<TruckController>();
            if (controller != null)
                _activeTrucks.Remove(controller);

            Destroy(truckObj);
        }

        private void OnDestroy()
        {
            _activeTrucks.Clear();
        }
    }
}
