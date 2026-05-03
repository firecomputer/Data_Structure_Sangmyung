using System.Collections.Generic;
using UnityEngine;

namespace AlgorithmOfDelivery.Maze
{
    public class ZoneData
    {
        public int ZoneId;
        public Vector2 Center;
        public Vector2 Size;
        public string Name;
        public List<MSTGenerator.TerrainType> AllowedTerrains;

        [Header("Altitude Settings")]
        public float MinAltitude;
        public float MaxAltitude;
        public float LowAltitudeRatio;
        public float MidAltitudeRatio;

        public bool Contains(Vector2 position)
        {
            float left = Center.x - Size.x / 2f;
            float right = Center.x + Size.x / 2f;
            float bottom = Center.y - Size.y / 2f;
            float top = Center.y + Size.y / 2f;
            return position.x >= left && position.x <= right && position.y >= bottom && position.y <= top;
        }

        public float GetRandomAltitude(System.Random rand)
        {
            float roll = (float)rand.NextDouble();
            float baseAlt;

            if (roll < LowAltitudeRatio)
            {
                baseAlt = Mathf.Lerp(MinAltitude, MinAltitude + (MaxAltitude - MinAltitude) * 0.33f, (float)rand.NextDouble());
            }
            else if (roll < LowAltitudeRatio + MidAltitudeRatio)
            {
                baseAlt = Mathf.Lerp(MinAltitude + (MaxAltitude - MinAltitude) * 0.33f, MinAltitude + (MaxAltitude - MinAltitude) * 0.66f, (float)rand.NextDouble());
            }
            else
            {
                baseAlt = Mathf.Lerp(MinAltitude + (MaxAltitude - MinAltitude) * 0.66f, MaxAltitude, (float)rand.NextDouble());
            }

            return baseAlt;
        }
    }

    public class MazeManager : MonoBehaviour
    {
        [Header("Generation Settings")]
        [SerializeField] private float _zoneWidth = 100f;
        [SerializeField] private float _zoneHeight = 100f;
        [SerializeField] private int _pointCount = 20;
        [SerializeField] private float _pointMargin = 10f;
        [SerializeField] private float _minPointDistance = 30f;

        [Header("Zone Settings")]
        [SerializeField] private int _zoneCount = 5;
        [SerializeField] private List<ZoneData> _zones = new List<ZoneData>();

        [Header("Terrain Distribution")]
        [SerializeField] private float _asphaltRatio = 0.3f;
        [SerializeField] private float _dirtRatio = 0.25f;
        [SerializeField] private float _rockyRatio = 0.2f;
        [SerializeField] private float _hillRatio = 0.15f;
        [SerializeField] private float _ruinsRatio = 0.1f;

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
        private Dictionary<Vector2, float> _nodeAltitudes = new Dictionary<Vector2, float>();
        private System.Random _altitudeRandom = new System.Random();

        private void Awake()
        {
            InitializeComponents();
            InitializeZones();
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

        private void InitializeZones()
        {
            _zones.Clear();
            float totalWidth = _zoneWidth * _zoneCount;
            float totalHeight = _zoneHeight;

            for (int i = 0; i < _zoneCount; i++)
            {
                ZoneData zone = new ZoneData
                {
                    ZoneId = i + 1,
                    Center = new Vector2(_zoneWidth * i + _zoneWidth / 2f, totalHeight / 2f),
                    Size = new Vector2(_zoneWidth, totalHeight),
                    Name = GetZoneName(i + 1),
                    AllowedTerrains = GetTerrainForZone(i + 1),
                    MinAltitude = GetZoneMinAltitude(i + 1),
                    MaxAltitude = GetZoneMaxAltitude(i + 1),
                    LowAltitudeRatio = GetLowAltitudeRatio(i + 1),
                    MidAltitudeRatio = GetMidAltitudeRatio(i + 1)
                };
                _zones.Add(zone);
            }
        }

        private string GetZoneName(int zoneId)
        {
            return zoneId switch
            {
                1 => "항구 마을",
                2 => "들판",
                3 => "잊혀진 왕도",
                4 => "안개 산맥",
                5 => "천체 연구소",
                _ => $"Zone_{zoneId}"
            };
        }

        private float GetZoneMinAltitude(int zoneId)
        {
            return zoneId switch
            {
                1 => 0f,
                2 => 20f,
                3 => 60f,
                4 => 150f,
                5 => 280f,
                _ => 0f
            };
        }

        private float GetZoneMaxAltitude(int zoneId)
        {
            return zoneId switch
            {
                1 => 40f,
                2 => 80f,
                3 => 140f,
                4 => 250f,
                5 => 400f,
                _ => 100f
            };
        }

        private float GetLowAltitudeRatio(int zoneId)
        {
            return zoneId switch
            {
                1 => 0.8f,
                2 => 0.5f,
                3 => 0.3f,
                4 => 0.2f,
                5 => 0.1f,
                _ => 0.33f
            };
        }

        private float GetMidAltitudeRatio(int zoneId)
        {
            return zoneId switch
            {
                1 => 0.2f,
                2 => 0.3f,
                3 => 0.4f,
                4 => 0.3f,
                5 => 0.3f,
                _ => 0.33f
            };
        }

        public float GetAltitudeAtPosition(Vector2 position)
        {
            if (_nodeAltitudes.TryGetValue(position, out float altitude))
            {
                return altitude;
            }

            foreach (var zone in _zones)
            {
                if (zone.Contains(position))
                {
                    return zone.GetRandomAltitude(_altitudeRandom);
                }
            }
            return 0f;
        }

        public float MaxAltitude
        {
            get
            {
                float max = 0f;
                foreach (var zone in _zones)
                {
                    if (zone.MaxAltitude > max) max = zone.MaxAltitude;
                }
                return max;
            }
        }

        private List<MSTGenerator.TerrainType> GetTerrainForZone(int zoneId)
        {
            List<MSTGenerator.TerrainType> terrains = new List<MSTGenerator.TerrainType>();
            
            switch (zoneId)
            {
                case 1:
                    terrains.Add(MSTGenerator.TerrainType.Asphalt);
                    terrains.Add(MSTGenerator.TerrainType.Dirt);
                    break;
                case 2:
                    terrains.Add(MSTGenerator.TerrainType.Dirt);
                    terrains.Add(MSTGenerator.TerrainType.Asphalt);
                    break;
                case 3:
                    terrains.Add(MSTGenerator.TerrainType.Rocky);
                    terrains.Add(MSTGenerator.TerrainType.Ruins);
                    break;
                case 4:
                    terrains.Add(MSTGenerator.TerrainType.Hill);
                    terrains.Add(MSTGenerator.TerrainType.Rocky);
                    break;
                case 5:
                    terrains.Add(MSTGenerator.TerrainType.Hill);
                    terrains.Add(MSTGenerator.TerrainType.Ruins);
                    break;
            }
            return terrains;
        }

        private void AssignZoneAndTerrain(List<MSTGenerator.Edge> edges)
        {
            System.Random rand = new System.Random();
            float totalRatio = _asphaltRatio + _dirtRatio + _rockyRatio + _hillRatio + _ruinsRatio;

            foreach (var edge in edges)
            {
                Vector2 midpoint = (edge.From + edge.To) / 2f;
                edge.ZoneId = GetZoneId(midpoint);
                edge.Terrain = GetTerrainType(edge.ZoneId, rand, totalRatio);
                edge.Altitude = GetAltitudeForPoint(midpoint);
            }

            foreach (var node in _nodePositions)
            {
                int zoneId = GetZoneId(node);
                float altitude = GetAltitudeForPoint(node);
                _nodeAltitudes[node] = altitude;
            }
        }

        private float GetAltitudeForPoint(Vector2 point)
        {
            foreach (var zone in _zones)
            {
                if (zone.Contains(point))
                {
                    return zone.GetRandomAltitude(_altitudeRandom);
                }
            }
            return 0f;
        }

        private int GetZoneId(Vector2 position)
        {
            foreach (var zone in _zones)
            {
                if (zone.Contains(position))
                    return zone.ZoneId;
            }
            return 1;
        }

        private MSTGenerator.TerrainType GetTerrainType(int zoneId, System.Random rand, float totalRatio)
        {
            float roll = (float)rand.NextDouble() * totalRatio;

            if (roll < _asphaltRatio)
                return MSTGenerator.TerrainType.Asphalt;
            if (roll < _asphaltRatio + _dirtRatio)
                return MSTGenerator.TerrainType.Dirt;
            if (roll < _asphaltRatio + _dirtRatio + _rockyRatio)
                return MSTGenerator.TerrainType.Rocky;
            if (roll < _asphaltRatio + _dirtRatio + _rockyRatio + _hillRatio)
                return MSTGenerator.TerrainType.Hill;
            return MSTGenerator.TerrainType.Ruins;
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

            AssignZoneAndTerrain(_mstEdges);

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
                _cameraController.SetBounds(_zoneWidth * _zoneCount, _zoneHeight);
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
            _nodeAltitudes.Clear();
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

        public float ZoneWidth => _zoneWidth;
        public float ZoneHeight => _zoneHeight;
        public int ZoneCount => _zoneCount;

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
