using System.Collections.Generic;
using UnityEngine;

namespace AlgorithmOfDelivery.Maze
{
    public class RoadVisualizer : MonoBehaviour
    {
        [Header("Road Settings")]
        public float roadWidth = 8f;
        public float tileScale = 1f;
        [SerializeField] private Material _roadMaterial;
        [SerializeField] private Color _roadColor = Color.gray;

        [Header("Terrain Sprites")]
        [SerializeField] private Sprite _asphaltSprite;
        [SerializeField] private Sprite _dirtSprite;
        [SerializeField] private Sprite _rockySprite;
        [SerializeField] private Sprite _hillSprite;
        [SerializeField] private Sprite _ruinsSprite;
        [SerializeField] private Sprite _sandSprite;
        [SerializeField] private Sprite _seaSprite;

        [Header("Terrain Colors (fallback when no sprite)")]
        [SerializeField] private Color _sandColor = new Color(0.9f, 0.85f, 0.65f);
        [SerializeField] private Color _seaColor = new Color(0.15f, 0.35f, 0.6f);

        private List<GameObject> _roadInstances = new List<GameObject>();

        public void Visualize(List<MSTGenerator.Edge> edges, Transform parent)
        {
            ClearRoads();

            if (_roadMaterial == null)
            {
                _roadMaterial = CreateDefaultRoadMaterial();
            }

            foreach (var edge in edges)
            {
                CreateRoadSegment(edge.From, edge.To, edge.Terrain, edge.ZoneId, parent);
            }
        }

        public void VisualizeGridTiles(List<Vector2> roadPositions, List<MSTGenerator.TerrainType> roadTerrains, Transform parent)
        {
            for (int i = 0; i < roadPositions.Count && i < roadTerrains.Count; i++)
            {
                CreateRoadTile(roadPositions[i], roadTerrains[i], parent);
            }
        }

        public void VisualizeTerrainTiles(List<Vector2> positions, MSTGenerator.TerrainType terrain, Transform parent, float scale = 1f, int sortingOrder = -1)
        {
            foreach (var pos in positions)
            {
                CreateTerrainTile(pos, terrain, parent, scale, sortingOrder);
            }
        }

        public void VisualizeGridEdges(List<MSTGenerator.Edge> edges, Transform parent)
        {
            if (_roadMaterial == null)
            {
                _roadMaterial = CreateDefaultRoadMaterial();
            }

            foreach (var edge in edges)
            {
                CreateRoadSegment(edge.From, edge.To, edge.Terrain, edge.ZoneId, parent);
            }
        }

        private void CreateTerrainTile(Vector2 position, MSTGenerator.TerrainType terrain, Transform parent, float scale, int sortingOrder)
        {
            GameObject tileObj = new GameObject($"Tile_{terrain}");
            tileObj.transform.SetParent(parent);
            tileObj.transform.position = new Vector3(position.x, position.y, 0f);
            _roadInstances.Add(tileObj);

            SpriteRenderer sr = tileObj.AddComponent<SpriteRenderer>();
            sr.sprite = GetSpriteForTerrain(terrain);
            if (sr.sprite == null)
            {
                Texture2D tex = new Texture2D(128, 128);
                Color color = terrain switch
                {
                    MSTGenerator.TerrainType.Sand => _sandColor,
                    MSTGenerator.TerrainType.Sea => _seaColor,
                    _ => _roadColor
                };
                Color[] pixels = new Color[128 * 128];
                for (int i = 0; i < pixels.Length; i++)
                    pixels[i] = color;
                tex.SetPixels(pixels);
                tex.Apply();
                sr.sprite = Sprite.Create(tex, new Rect(0, 0, 128, 128), new Vector2(0.5f, 0.5f), 1f);
                tileObj.transform.localScale = Vector3.one * (RhombusGrid.TileSize * 0.9f / 128f);
            }
            else
            {
                float targetWidth = RhombusGrid.TileSize * 0.9f;
                float spriteWidth = sr.sprite.bounds.size.x;
                if (spriteWidth > 0f)
                    tileObj.transform.localScale = Vector3.one * (targetWidth / spriteWidth);
                else
                    tileObj.transform.localScale = Vector3.one * scale;
            }

            sr.sortingOrder = sortingOrder;
        }

        private void CreateRoadTile(Vector2 position, MSTGenerator.TerrainType terrain, Transform parent)
        {
            GameObject roadObj = new GameObject($"RoadTile_{terrain}");
            roadObj.transform.SetParent(parent);
            roadObj.transform.position = new Vector3(position.x, position.y, 0f);
            _roadInstances.Add(roadObj);

            SpriteRenderer sr = roadObj.AddComponent<SpriteRenderer>();
            sr.sprite = GetSpriteForTerrain(terrain);
            if (sr.sprite == null)
            {
                Texture2D tex = new Texture2D(128, 128);
                Color[] pixels = new Color[128 * 128];
                for (int i = 0; i < pixels.Length; i++)
                    pixels[i] = _roadColor;
                tex.SetPixels(pixels);
                tex.Apply();
                sr.sprite = Sprite.Create(tex, new Rect(0, 0, 128, 128), new Vector2(0.5f, 0.5f), 1f);
                roadObj.transform.localScale = Vector3.one * (RhombusGrid.TileSize * 0.7f / 128f);
            }
            else
            {
                float targetWidth = RhombusGrid.TileSize * 0.7f;
                float spriteWidth = sr.sprite.bounds.size.x;
                if (spriteWidth > 0f)
                    roadObj.transform.localScale = Vector3.one * (targetWidth / spriteWidth);
                else
                    roadObj.transform.localScale = Vector3.one * tileScale;
            }

            sr.sortingOrder = 0;
        }

        private void CreateRoadSegment(Vector2 start, Vector2 end, MSTGenerator.TerrainType terrain, int zoneId, Transform parent)
        {
            GameObject roadObj = new GameObject($"Road_Zone{zoneId}_{terrain}");
            roadObj.transform.SetParent(parent);
            _roadInstances.Add(roadObj);

            Vector2 midpoint = (start + end) / 2f;
            roadObj.transform.position = new Vector3(midpoint.x, midpoint.y, 0f);

            float length = Vector2.Distance(start, end);
            float angle = Vector2.SignedAngle(Vector2.right, end - start);

            SpriteRenderer sr = roadObj.AddComponent<SpriteRenderer>();
            sr.sprite = GetSpriteForTerrain(terrain);
            if (sr.sprite == null)
            {
                Texture2D tex = new Texture2D(1, 1);
                tex.SetPixel(0, 0, _roadColor);
                tex.Apply();
                tex.wrapMode = TextureWrapMode.Repeat;
                sr.sprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
            }
            
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.size = new Vector2(length, roadWidth);
            sr.transform.rotation = Quaternion.Euler(0, 0, angle);
            sr.sortingOrder = 0;
        }

        private Material CreateDefaultRoadMaterial()
        {
            Material mat = new Material(Shader.Find("Sprites/Default"));
            mat.color = _roadColor;
            return mat;
        }

        private Sprite GetSpriteForTerrain(MSTGenerator.TerrainType terrain)
        {
            return terrain switch
            {
                MSTGenerator.TerrainType.Asphalt => _asphaltSprite,
                MSTGenerator.TerrainType.Dirt => _dirtSprite,
                MSTGenerator.TerrainType.Rocky => _rockySprite,
                MSTGenerator.TerrainType.Hill => _hillSprite,
                MSTGenerator.TerrainType.Ruins => _ruinsSprite,
                MSTGenerator.TerrainType.Sand => _sandSprite,
                MSTGenerator.TerrainType.Sea => _seaSprite,
                _ => null
            };
        }

        public void ClearRoads()
        {
            foreach (var road in _roadInstances)
            {
                if (road != null)
                    Destroy(road.gameObject);
            }
            _roadInstances.Clear();
        }

        public List<Vector2> GetNodePositions(List<MSTGenerator.Edge> edges)
        {
            HashSet<Vector2> nodes = new HashSet<Vector2>();
            foreach (var edge in edges)
            {
                nodes.Add(edge.From);
                nodes.Add(edge.To);
            }
            return new List<Vector2>(nodes);
        }
    }
}
