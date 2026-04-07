using System.Collections.Generic;
using UnityEngine;

namespace AlgorithmOfDelivery.Maze
{
    public class MazeManager : MonoBehaviour
    {
        [Header("Generation Settings")]
        [SerializeField] private float _zoneWidth = 100f;
        [SerializeField] private float _zoneHeight = 100f;
        [SerializeField] private int _pointCount = 20;
        [SerializeField] private float _pointMargin = 10f;
        [SerializeField] private float _minPointDistance = 30f;

        [Header("Visualization")]
        [SerializeField] private RoadVisualizer _roadVisualizer;
        [SerializeField] private HousePlacer _housePlacer;
        [SerializeField] private CameraController _cameraController;

        [Header("Road Settings")]
        [SerializeField] private float _roadWidth = 8f;
        [SerializeField] private Material _roadMaterial;

        [Header("House Settings")]
        [SerializeField] private Sprite _houseSprite;
        [SerializeField] private float _houseScale = 1f;
        [SerializeField] private float _housePlacementRadius = 10f;
        [SerializeField] private int _housesPerNode = 2;

        [Header("Debug")]
        [SerializeField] private bool _showDebugInfo = true;

        private List<Vector2> _nodePositions;
        private List<MSTGenerator.Edge> _mstEdges;
        private PointGenerator _pointGenerator;
        private DelaunayTriangulator _triangulator;
        private MSTGenerator _mstGenerator;

        private Transform _mazeParent;

        private void Awake()
        {
            InitializeComponents();
        }

        private void Start()
        {
            GenerateMaze();
        }

        private void InitializeComponents()
        {
            _pointGenerator = new PointGenerator(_zoneWidth, _zoneHeight, _pointMargin, _minPointDistance);
            _triangulator = new DelaunayTriangulator();
            _mstGenerator = new MSTGenerator();
        }

        public void GenerateMaze()
        {
            ClearMaze();

            _mazeParent = new GameObject("Maze").transform;
            _mazeParent.transform.SetParent(transform);

            _nodePositions = _pointGenerator.Generate(_pointCount);

            if (_showDebugInfo)
            {
                Debug.Log($"[MazeManager] Generated {_nodePositions.Count} nodes");
            }

            List<DelaunayTriangulator.Triangle> triangles = _triangulator.Triangulate(_nodePositions);

            if (_showDebugInfo)
            {
                Debug.Log($"[MazeManager] Created {triangles.Count} triangles");
            }

            List<DelaunayTriangulator.Edge> delaunayEdges = _triangulator.GetEdgesFromTriangles(triangles);

            List<MSTGenerator.Edge> mstInputEdges = new List<MSTGenerator.Edge>();
            foreach (var edge in delaunayEdges)
            {
                mstInputEdges.Add(new MSTGenerator.Edge(edge.p0, edge.p1));
            }

            _mstEdges = _mstGenerator.GenerateMST(_nodePositions, mstInputEdges);

            if (_showDebugInfo)
            {
                Debug.Log($"[MazeManager] MST has {_mstEdges.Count} edges");
            }

            if (_roadVisualizer != null)
            {
                _roadVisualizer.roadWidth = _roadWidth;
                _roadVisualizer.Visualize(_mstEdges, _mazeParent);
            }

            List<Vector2> nodePosList = _roadVisualizer.GetNodePositions(_mstEdges);

            if (_housePlacer != null)
            {
                _housePlacer.houseSprite = _houseSprite;
                _housePlacer.houseScale = _houseScale;
                _housePlacer.placementRadius = _housePlacementRadius;
                _housePlacer.housesPerNode = _housesPerNode;
                _housePlacer.PlaceHouses(nodePosList, _mazeParent);
            }

            if (_cameraController != null)
            {
                _cameraController.SetBounds(_zoneWidth, _zoneHeight);
            }

            if (_showDebugInfo)
            {
                LogMazeInfo();
            }
        }

        private void ClearMaze()
        {
            if (_mazeParent != null)
            {
                Destroy(_mazeParent.gameObject);
            }
            _nodePositions?.Clear();
            _mstEdges?.Clear();
        }

        private void LogMazeInfo()
        {
            float totalLength = 0f;
            foreach (var edge in _mstEdges)
            {
                totalLength += edge.Weight;
            }

            Debug.Log($"[MazeManager] Total road length: {totalLength:F2} units");
            Debug.Log($"[MazeManager] Zone size: {_zoneWidth}x{_zoneHeight} units");
        }

        public List<Vector2> GetNodePositions() => new List<Vector2>(_nodePositions);
        public List<MSTGenerator.Edge> GetMSTEdges() => new List<MSTGenerator.Edge>(_mstEdges);

        private void OnDrawGizmos()
        {
            if (!_showDebugInfo || _nodePositions == null) return;

            Gizmos.color = Color.yellow;
            foreach (var node in _nodePositions)
            {
                Gizmos.DrawWireSphere(new Vector3(node.x, node.y, 0f), 2f);
            }

            if (_mstEdges == null) return;

            Gizmos.color = Color.green;
            foreach (var edge in _mstEdges)
            {
                Gizmos.DrawLine(new Vector3(edge.From.x, edge.From.y, 0f),
                               new Vector3(edge.To.x, edge.To.y, 0f));
            }
        }
    }
}
