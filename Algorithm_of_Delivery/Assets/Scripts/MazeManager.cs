using System;
using System.Collections.Generic;
using UnityEngine;

namespace AlgorithmOfDelivery.Maze
{
    [Serializable]
    public class MapNodeData
    {
        public string name;
        public string sprite;
        public float x;
        public float y;
    }

    [Serializable]
    public class MapEdgeData
    {
        public string from;
        public string to;
        public string terrain;
        public int zoneId;
    }

    [Serializable]
    public class MapData
    {
        public List<MapNodeData> nodes;
        public List<MapEdgeData> edges;
    }

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
        [SerializeField] private bool _useManualMap = true;
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
        private MapData _mapData;

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

            if (_useManualMap)
            {
                InitializeManualZones();
                return;
            }

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

        private void InitializeManualZones()
        {
            _zones.Add(CreateZone(1, "우체국 부두", new Vector2(0, -270), new Vector2(240, 240),
                new List<MSTGenerator.TerrainType> { MSTGenerator.TerrainType.Asphalt, MSTGenerator.TerrainType.Dirt },
                0f, 40f, 0.8f, 0.2f));
            _zones.Add(CreateZone(2, "들판", new Vector2(-270, -180), new Vector2(720, 300),
                new List<MSTGenerator.TerrainType> { MSTGenerator.TerrainType.Dirt, MSTGenerator.TerrainType.Asphalt },
                20f, 80f, 0.5f, 0.3f));
            _zones.Add(CreateZone(3, "잊혀진 왕도", new Vector2(510, -120), new Vector2(360, 360),
                new List<MSTGenerator.TerrainType> { MSTGenerator.TerrainType.Rocky, MSTGenerator.TerrainType.Ruins },
                60f, 140f, 0.3f, 0.4f));
            _zones.Add(CreateZone(4, "안개 산맥", new Vector2(-360, 150), new Vector2(420, 270),
                new List<MSTGenerator.TerrainType> { MSTGenerator.TerrainType.Hill, MSTGenerator.TerrainType.Rocky },
                150f, 250f, 0.2f, 0.3f));
            _zones.Add(CreateZone(5, "천체 연구소", new Vector2(0, 330), new Vector2(240, 180),
                new List<MSTGenerator.TerrainType> { MSTGenerator.TerrainType.Hill, MSTGenerator.TerrainType.Ruins },
                200f, 280f, 0.1f, 0.3f));
            _zones.Add(CreateZone(6, "상점가", new Vector2(60, 105), new Vector2(300, 165),
                new List<MSTGenerator.TerrainType> { MSTGenerator.TerrainType.Asphalt, MSTGenerator.TerrainType.Dirt },
                10f, 50f, 0.8f, 0.2f));
            _zones.Add(CreateZone(7, "캠프 지대", new Vector2(570, 600), new Vector2(180, 180),
                new List<MSTGenerator.TerrainType> { MSTGenerator.TerrainType.Ruins, MSTGenerator.TerrainType.Hill },
                300f, 400f, 0.1f, 0.3f));
        }

        private ZoneData CreateZone(int id, string name, Vector2 center, Vector2 size,
            List<MSTGenerator.TerrainType> terrains, float minAlt, float maxAlt, float lowRatio, float midRatio)
        {
            return new ZoneData
            {
                ZoneId = id,
                Name = name,
                Center = center,
                Size = size,
                AllowedTerrains = terrains,
                MinAltitude = minAlt,
                MaxAltitude = maxAlt,
                LowAltitudeRatio = lowRatio,
                MidAltitudeRatio = midRatio
            };
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

        private bool LoadManualMap()
        {
            TextAsset jsonAsset = Resources.Load<TextAsset>("jsons/hypr_map_coordinates");
            if (jsonAsset == null)
            {
                Debug.LogError("[MazeManager] Failed to load hypr_map_coordinates.json from Resources/jsons/");
                return false;
            }

            _mapData = JsonUtility.FromJson<MapData>(jsonAsset.text);
            if (_mapData == null || _mapData.nodes == null || _mapData.nodes.Count == 0)
            {
                Debug.LogError("[MazeManager] Invalid map data in JSON");
                return false;
            }

            _nodePositions = new List<Vector2>();
            var nameToPos = new Dictionary<string, Vector2>();
            foreach (var node in _mapData.nodes)
            {
                Vector2 pos = new Vector2(node.x, node.y);
                _nodePositions.Add(pos);
                nameToPos[node.name] = pos;
            }

            _mstEdges = new List<MSTGenerator.Edge>();
            foreach (var edgeData in _mapData.edges)
            {
                if (!nameToPos.TryGetValue(edgeData.from, out Vector2 from) ||
                    !nameToPos.TryGetValue(edgeData.to, out Vector2 to))
                {
                    Debug.LogWarning($"[MazeManager] Edge references unknown node: {edgeData.from} -> {edgeData.to}");
                    continue;
                }

                var edge = new MSTGenerator.Edge(from, to)
                {
                    Terrain = ParseTerrainType(edgeData.terrain),
                    ZoneId = edgeData.zoneId
                };
                _mstEdges.Add(edge);
            }

            foreach (var node in _nodePositions)
            {
                int zoneId = GetZoneId(node);
                float altitude = GetAltitudeForPoint(node);
                _nodeAltitudes[node] = altitude;
            }

            if (_showDebugInfo)
            {
                Debug.Log($"[MazeManager] Loaded {_nodePositions.Count} manual nodes and {_mstEdges.Count} edges");
            }

            return true;
        }

        private MSTGenerator.TerrainType ParseTerrainType(string terrain)
        {
            if (Enum.TryParse(terrain, out MSTGenerator.TerrainType result))
                return result;
            return MSTGenerator.TerrainType.Asphalt;
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

            if (_useManualMap)
            {
                if (!LoadManualMap())
                {
                    Debug.LogError("[MazeManager] Manual map loading failed, falling back to procedural");
                    _useManualMap = false;
                    GenerateProceduralMaze();
                    return;
                }
            }
            else
            {
                GenerateProceduralMaze();
            }

            if (_roadVisualizer != null)
            {
                _roadVisualizer.roadWidth = _roadWidth;
                _roadVisualizer.Visualize(_mstEdges, _mazeParent);
            }

            List<Vector2> nodePosList = _roadVisualizer.GetNodePositions(_mstEdges);

            if (_housePlacer != null)
            {
                if (_useManualMap && _mapData != null)
                {
                    _housePlacer.houseSprite = _houseSprite;
                    _housePlacer.houseScale = _houseScale;
                    _housePlacer.placementRadius = _housePlacementRadius;
                    _housePlacer.PlaceBuildingsFromMapData(_mapData, _mazeParent);
                }
                else
                {
                    _housePlacer.houseSprite = _houseSprite;
                    _housePlacer.houseScale = _houseScale;
                    _housePlacer.placementRadius = _housePlacementRadius;
                    _housePlacer.housesPerNode = _housesPerNode;
                    _housePlacer.PlaceHouses(nodePosList, _mazeParent);
                }
            }

            if (_cameraController != null)
            {
                if (_useManualMap)
                {
                    _cameraController.SetBounds(1300f, 1100f, new Vector3(60, 150, -10f));
                }
                else
                {
                    _cameraController.SetBounds(_zoneWidth * _zoneCount, _zoneHeight);
                }
            }

            if (_showDebugInfo)
            {
                LogMazeInfo();
            }
        }

        private void GenerateProceduralMaze()
        {
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
