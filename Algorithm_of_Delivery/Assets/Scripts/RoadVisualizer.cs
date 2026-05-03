using System.Collections.Generic;
using UnityEngine;

namespace AlgorithmOfDelivery.Maze
{
    public class RoadVisualizer : MonoBehaviour
    {
        [Header("Road Settings")]
        public float roadWidth = 8f;
        [SerializeField] private Material _roadMaterial;
        [SerializeField] private Color _roadColor = Color.gray;

        [Header("Terrain Sprites")]
        [SerializeField] private Sprite _asphaltSprite;
        [SerializeField] private Sprite _dirtSprite;
        [SerializeField] private Sprite _rockySprite;
        [SerializeField] private Sprite _hillSprite;
        [SerializeField] private Sprite _ruinsSprite;

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
