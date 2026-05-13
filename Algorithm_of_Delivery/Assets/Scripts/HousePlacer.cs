using System.Collections.Generic;
using UnityEngine;
using AlgorithmOfDelivery.Game;

namespace AlgorithmOfDelivery.Maze
{
    public class HousePlacer : MonoBehaviour
    {
        [Header("House Settings")]
        public Sprite houseSprite;
        public float houseScale = 1f;
        public float placementRadius = 10f;
        public int housesPerNode = 2;

        [Header("Building Sprites")]
        [SerializeField] private Sprite _postOfficeSprite;
        [SerializeField] private Sprite _shopSprite;
        [SerializeField] private Sprite _schoolSprite;
        [SerializeField] private Sprite _campSprite;

        [Header("Per-Type Scale")]
        [SerializeField] private float _postOfficeScale = 1f;
        [SerializeField] private float _houseTypeScale = 1f;
        [SerializeField] private float _shopScale = 1f;
        [SerializeField] private float _schoolScale = 1f;
        [SerializeField] private float _campScale = 1f;

        [Header("Happiness")]
        [SerializeField] private float _minInitialHappiness = 40f;
        [SerializeField] private float _maxInitialHappiness = 80f;

        private List<GameObject> _houseInstances = new List<GameObject>();

        public List<GameObject> HouseInstances => _houseInstances;

        public void ClearHouses()
        {
            foreach (var house in _houseInstances)
            {
                if (house != null)
                    Destroy(house);
            }
            _houseInstances.Clear();
        }

        public void PlaceHouses(List<Vector2> nodePositions, Transform parent)
        {
            ClearHouses();

            foreach (var nodePos in nodePositions)
            {
                PlaceHousesAtNode(nodePos, parent);
            }
        }

        private void PlaceHousesAtNode(Vector2 nodePos, Transform parent)
        {
            for (int i = 0; i < housesPerNode; i++)
            {
                Vector2 offset = GetRandomOffset();
                Vector2 housePos = nodePos + offset;

                GameObject house = CreateHouse(housePos, parent);
                AttachHouseState(house);
                _houseInstances.Add(house);
            }
        }

        private Vector2 GetRandomOffset()
        {
            float angle = Random.Range(0f, Mathf.PI * 2f);
            float distance = Random.Range(placementRadius * 0.5f, placementRadius);
            return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;
        }

        private GameObject CreateHouse(Vector2 position, Transform parent)
        {
            GameObject houseObj = new GameObject("House");
            houseObj.transform.SetParent(parent);
            houseObj.transform.position = new Vector3(position.x, position.y, 0f);
            houseObj.transform.localScale = Vector3.one * houseScale;

            SpriteRenderer sr = houseObj.AddComponent<SpriteRenderer>();
            if (houseSprite != null)
            {
                sr.sprite = houseSprite;
            }
            else
            {
                sr.color = new Color(0.8f, 0.6f, 0.4f);
            }

            sr.sortingOrder = 1;
            return houseObj;
        }

        public void PlaceBuildingsFromMapData(MapData mapData, Transform parent)
        {
            ClearHouses();

            System.Random offsetRand = new System.Random(42);

            foreach (var node in mapData.nodes)
            {
                Vector2 position = new Vector2(node.x, node.y);
                Sprite sprite = GetSpriteForBuildingType(node.sprite);
                if (sprite == null)
                {
                    sprite = houseSprite;
                }

                float scale = GetScaleForBuildingType(node.sprite);
                Vector2 offset = Vector2.zero;
                if (placementRadius > 0f)
                {
                    float angle = (float)(offsetRand.NextDouble() * Mathf.PI * 2f);
                    float distance = (float)(offsetRand.NextDouble() * 0.5f + 0.5f) * placementRadius;
                    offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;
                }

                GameObject building = new GameObject(node.name);
                building.transform.SetParent(parent);
                building.transform.position = new Vector3(position.x + offset.x, position.y + offset.y, 0f);
                building.transform.localScale = Vector3.one * scale;

                SpriteRenderer sr = building.AddComponent<SpriteRenderer>();
                sr.sprite = sprite;
                sr.sortingOrder = 1;

                AttachHouseState(building);

                _houseInstances.Add(building);
            }
        }

        private void AttachHouseState(GameObject building)
        {
            HouseState state = building.AddComponent<HouseState>();
            float initialHappiness = Random.Range(_minInitialHappiness, _maxInitialHappiness);
            state.Init(initialHappiness);
        }

        private Sprite GetSpriteForBuildingType(string spriteType)
        {
            return spriteType switch
            {
                "post_office_dock" => _postOfficeSprite,
                "house" => houseSprite,
                "shop" => _shopSprite,
                "school" => _schoolSprite,
                "camp" => _campSprite,
                _ => null
            };
        }

        private float GetScaleForBuildingType(string spriteType)
        {
            return spriteType switch
            {
                "post_office_dock" => _postOfficeScale,
                "house" => _houseTypeScale,
                "shop" => _shopScale,
                "school" => _schoolScale,
                "camp" => _campScale,
                _ => houseScale
            };
        }
    }
}
