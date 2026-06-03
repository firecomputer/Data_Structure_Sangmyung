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
        public int row;
        public int col;
    }

    [Serializable]
    public class RoadTileData
    {
        public int row;
        public int col;
    }

    [Serializable]
    public class MapZoneData
    {
        public int id;
        public string name;
        public string terrain;
        public int minRow;
        public int maxRow;
        public int minCol;
        public int maxCol;
        public float minAltitude;
        public float maxAltitude;
        public float lowRatio;
        public float midRatio;
    }

    [Serializable]
    public class MapData
    {
        public float tileSize;
        public float originX;
        public float originY;
        public int gridRows;
        public int gridCols;
        public List<MapNodeData> buildings;
        public List<RoadTileData> roads;
        public List<MapZoneData> zones;
        public List<RoadTileData> sand;
        public List<RoadTileData> sea;
    }

    public class ZoneData
    {
        public int ZoneId;
        public Vector2 Center;
        public Vector2 Size;
        public string Name;
        public List<MSTGenerator.TerrainType> AllowedTerrains;
        public MSTGenerator.TerrainType ZoneTerrain;

        [Header("Altitude Settings")]
        public float MinAltitude;
        public float MaxAltitude;
        public float LowAltitudeRatio;
        public float MidAltitudeRatio;

        public int MinRow, MaxRow, MinCol, MaxCol;
        public bool UseGridBounds;

        public bool Contains(Vector2 position)
        {
            if (UseGridBounds)
            {
                var (row, col) = RhombusGrid.WorldToGrid(position);
                return Contains(row, col);
            }

            float left = Center.x - Size.x / 2f;
            float right = Center.x + Size.x / 2f;
            float bottom = Center.y - Size.y / 2f;
            float top = Center.y + Size.y / 2f;
            return position.x >= left && position.x <= right && position.y >= bottom && position.y <= top;
        }

        public bool Contains(int row, int col)
        {
            return row >= MinRow && row <= MaxRow && col >= MinCol && col <= MaxCol;
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
        [SerializeField] private int _seaBorderPadding = 25;
        [SerializeField] private float _cameraZoomPadding = 1.0f;
        [SerializeField] private float _cameraFocusZoom = 420f;

        private List<Vector2> _nodePositions;
        private List<MSTGenerator.Edge> _mstEdges;
        private List<Vector2> _roadTilePositions = new List<Vector2>();
        private List<MSTGenerator.TerrainType> _roadTileTerrains = new List<MSTGenerator.TerrainType>();
        private List<Vector2> _sandTilePositions = new List<Vector2>();
        private List<Vector2> _seaTilePositions = new List<Vector2>();
        private PointGenerator _pointGenerator;
        private DelaunayTriangulator _triangulator;
        private MSTGenerator _mstGenerator;

        private Transform _mazeParent;
        private Dictionary<Vector2, float> _nodeAltitudes = new Dictionary<Vector2, float>();
        private System.Random _altitudeRandom = new System.Random();
        private MapData _mapData;
        private bool _hasManualCameraBounds;
        private float _manualCameraMinX;
        private float _manualCameraMaxX;
        private float _manualCameraMinY;
        private float _manualCameraMaxY;

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
            if (_mapData == null || _mapData.zones == null)
            {
                Debug.LogWarning("[MazeManager] No zone data in manual map, falling back to defaults");
                return;
            }

            foreach (var z in _mapData.zones)
            {
                ZoneData zone = new ZoneData
                {
                    ZoneId = z.id,
                    Name = z.name,
                    MinRow = z.minRow,
                    MaxRow = z.maxRow,
                    MinCol = z.minCol,
                    MaxCol = z.maxCol,
                    UseGridBounds = true,
                    MinAltitude = z.minAltitude,
                    MaxAltitude = z.maxAltitude,
                    LowAltitudeRatio = z.lowRatio,
                    MidAltitudeRatio = z.midRatio,
                    ZoneTerrain = ParseTerrainType(z.terrain),
                    AllowedTerrains = new List<MSTGenerator.TerrainType> { ParseTerrainType(z.terrain) }
                };
                _zones.Add(zone);
            }
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
            if (_mapData == null || _mapData.buildings == null || _mapData.buildings.Count == 0)
            {
                Debug.LogError("[MazeManager] Invalid map data in JSON");
                return false;
            }

            RhombusGrid.Configure(_mapData.tileSize, _mapData.originX, _mapData.originY);

            _nodePositions = new List<Vector2>();
            _mstEdges = new List<MSTGenerator.Edge>();
            _roadTilePositions = new List<Vector2>();
            _roadTileTerrains = new List<MSTGenerator.TerrainType>();

            var grid = new Dictionary<(int, int), TileType>();
            var buildingNodes = new Dictionary<Vector2, MapNodeData>();

            foreach (var node in _mapData.buildings)
            {
                Vector2 worldPos = RhombusGrid.GridToWorld(node.row, node.col);
                _nodePositions.Add(worldPos);
                grid[(node.row, node.col)] = TileType.Building;
                buildingNodes[worldPos] = node;
            }

            if (_mapData.roads != null)
            {
                foreach (var road in _mapData.roads)
                {
                    (int, int) key = (road.row, road.col);
                    if (!grid.ContainsKey(key))
                        grid[key] = TileType.Road;
                }
            }

            if (_mapData.sand != null)
            {
                foreach (var s in _mapData.sand)
                {
                    (int, int) key = (s.row, s.col);
                    if (!grid.ContainsKey(key))
                        grid[key] = TileType.Sand;
                }
            }

            if (_mapData.sea != null)
            {
                foreach (var s in _mapData.sea)
                {
                    (int, int) key = (s.row, s.col);
                    if (!grid.ContainsKey(key))
                        grid[key] = TileType.Sea;
                }
            }

            InitializeManualZones();
            CaptureManualCameraBounds(grid);
            AddSeaBorderTiles(grid);

            var edgeSet = new HashSet<(Vector2, Vector2)>();
            var processed = new HashSet<(int, int)>();

            foreach (var kvp in grid)
            {
                if (kvp.Value != TileType.Road && kvp.Value != TileType.Building)
                    continue;

                int row = kvp.Key.Item1;
                int col = kvp.Key.Item2;
                Vector2 fromPos = RhombusGrid.GridToWorld(row, col);

                foreach (var (nr, nc) in RhombusGrid.GetFourNeighbors(row, col))
                {
                    var neighborKey = (nr, nc);
                    if (!grid.ContainsKey(neighborKey)) continue;
                    if (grid[neighborKey] != TileType.Road && grid[neighborKey] != TileType.Building) continue;
                    if (processed.Contains(neighborKey)) continue;

                    Vector2 toPos = RhombusGrid.GridToWorld(nr, nc);
                    var edgeKey1 = (fromPos, toPos);
                    var edgeKey2 = (toPos, fromPos);

                    if (edgeSet.Contains(edgeKey1) || edgeSet.Contains(edgeKey2)) continue;

                    int zoneId = GetZoneIdForGrid(row, col);
                    MSTGenerator.TerrainType terrain = GetTerrainForGrid(row, col);

                    var edge = new MSTGenerator.Edge(fromPos, toPos)
                    {
                        Terrain = terrain,
                        ZoneId = zoneId
                    };
                    _mstEdges.Add(edge);
                    edgeSet.Add(edgeKey1);
                }

                processed.Add((row, col));
            }

            foreach (var kvp in grid)
            {
                if (kvp.Value == TileType.Road)
                {
                    Vector2 pos = RhombusGrid.GridToWorld(kvp.Key.Item1, kvp.Key.Item2);
                    _roadTilePositions.Add(pos);
                    _roadTileTerrains.Add(GetTerrainForGrid(kvp.Key.Item1, kvp.Key.Item2));
                }
                else if (kvp.Value == TileType.Sand)
                {
                    _sandTilePositions.Add(RhombusGrid.GridToWorld(kvp.Key.Item1, kvp.Key.Item2));
                }
                else if (kvp.Value == TileType.Sea)
                {
                    _seaTilePositions.Add(RhombusGrid.GridToWorld(kvp.Key.Item1, kvp.Key.Item2));
                }
            }

            foreach (var nodePos in _nodePositions)
            {
                float altitude = GetAltitudeForPoint(nodePos);
                _nodeAltitudes[nodePos] = altitude;
            }

            if (_showDebugInfo)
            {
                Debug.Log($"[MazeManager] Loaded {_nodePositions.Count} buildings, {_mstEdges.Count} edges from grid");
            }

            return true;
        }

        private enum TileType
        {
            Empty,
            Road,
            Building,
            Sand,
            Sea
        }

        private int GetZoneIdForGrid(int row, int col)
        {
            foreach (var zone in _zones)
            {
                if (zone.UseGridBounds && zone.Contains(row, col))
                    return zone.ZoneId;
            }
            return 1;
        }

        private MSTGenerator.TerrainType GetTerrainForGrid(int row, int col)
        {
            foreach (var zone in _zones)
            {
                if (zone.UseGridBounds && zone.Contains(row, col))
                    return zone.ZoneTerrain;
            }
            return MSTGenerator.TerrainType.Asphalt;
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

                if (_seaTilePositions.Count > 0)
                {
                    float seaScale = RhombusGrid.TileSize / 100f;
                    _roadVisualizer.VisualizeTerrainTiles(_seaTilePositions, MSTGenerator.TerrainType.Sea, _mazeParent, seaScale, -5);
                }

                if (_sandTilePositions.Count > 0)
                {
                    float sandScale = RhombusGrid.TileSize / 100f;
                    _roadVisualizer.VisualizeTerrainTiles(_sandTilePositions, MSTGenerator.TerrainType.Sand, _mazeParent, sandScale, -3);
                }
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
                if (_useManualMap && _hasManualCameraBounds)
                {
                    _cameraController.ConfigureWorldBounds(
                        _manualCameraMinX,
                        _manualCameraMaxX,
                        _manualCameraMinY,
                        _manualCameraMaxY,
                        _cameraZoomPadding,
                        _cameraFocusZoom);
                }
                else if (_useManualMap)
                {
                    int rows = _mapData != null ? _mapData.gridRows : 16;
                    int cols = _mapData != null ? _mapData.gridCols : 19;
                    float tileSize = _mapData != null ? _mapData.tileSize : RhombusGrid.TileSize;
                    float worldW = cols * tileSize;
                    float worldH = rows * tileSize;
                    float centerX = _mapData != null ? _mapData.originX : 0f;
                    float centerY = _mapData != null ? _mapData.originY : 0f;
                    _cameraController.ConfigureWorldBounds(
                        centerX - worldW * 0.5f,
                        centerX + worldW * 0.5f,
                        centerY - worldH * 0.5f,
                        centerY + worldH * 0.5f,
                        _cameraZoomPadding,
                        _cameraFocusZoom);
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
            _roadTilePositions?.Clear();
            _roadTileTerrains?.Clear();
            _sandTilePositions?.Clear();
            _seaTilePositions?.Clear();
            _nodeAltitudes.Clear();
            _hasManualCameraBounds = false;
        }

        private void CaptureManualCameraBounds(Dictionary<(int, int), TileType> grid)
        {
            _hasManualCameraBounds = TryGetNonSeaPositionBounds(grid, out _manualCameraMinX, out _manualCameraMaxX, out _manualCameraMinY, out _manualCameraMaxY);
        }

        private void AddSeaBorderTiles(Dictionary<(int, int), TileType> grid)
        {
            if (grid == null || grid.Count == 0 || _seaBorderPadding <= 0)
                return;

            int minRow = int.MaxValue;
            int maxRow = int.MinValue;
            int minCol = int.MaxValue;
            int maxCol = int.MinValue;

            foreach (var key in grid.Keys)
            {
                if (key.Item1 < minRow) minRow = key.Item1;
                if (key.Item1 > maxRow) maxRow = key.Item1;
                if (key.Item2 < minCol) minCol = key.Item2;
                if (key.Item2 > maxCol) maxCol = key.Item2;
            }

            int startRow = minRow - _seaBorderPadding;
            int endRow = maxRow + _seaBorderPadding;
            int startCol = minCol - _seaBorderPadding;
            int endCol = maxCol + _seaBorderPadding;

            for (int row = startRow; row <= endRow; row++)
            {
                for (int col = startCol; col <= endCol; col++)
                {
                    var key = (row, col);
                    if (!grid.ContainsKey(key))
                        grid[key] = TileType.Sea;
                }
            }
        }

        private bool TryGetPositionBounds(List<Vector2> positions, out float minX, out float maxX, out float minY, out float maxY)
        {
            minX = maxX = minY = maxY = 0f;
            if (positions == null || positions.Count == 0)
                return false;

            minX = float.MaxValue;
            maxX = float.MinValue;
            minY = float.MaxValue;
            maxY = float.MinValue;

            float half = RhombusGrid.TileSize / 2f;
            foreach (var pos in positions)
            {
                if (pos.x - half < minX) minX = pos.x - half;
                if (pos.x + half > maxX) maxX = pos.x + half;
                if (pos.y - half < minY) minY = pos.y - half;
                if (pos.y + half > maxY) maxY = pos.y + half;
            }

            return true;
        }

        private bool TryGetNonSeaPositionBounds(Dictionary<(int, int), TileType> grid, out float minX, out float maxX, out float minY, out float maxY)
        {
            minX = maxX = minY = maxY = 0f;
            if (grid == null || grid.Count == 0)
                return false;

            minX = float.MaxValue;
            maxX = float.MinValue;
            minY = float.MaxValue;
            maxY = float.MinValue;

            bool found = false;
            float half = RhombusGrid.TileSize / 2f;

            foreach (var kvp in grid)
            {
                if (kvp.Value == TileType.Sea)
                    continue;

                Vector2 pos = RhombusGrid.GridToWorld(kvp.Key.Item1, kvp.Key.Item2);
                if (pos.x - half < minX) minX = pos.x - half;
                if (pos.x + half > maxX) maxX = pos.x + half;
                if (pos.y - half < minY) minY = pos.y - half;
                if (pos.y + half > maxY) maxY = pos.y + half;
                found = true;
            }

            return found;
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
