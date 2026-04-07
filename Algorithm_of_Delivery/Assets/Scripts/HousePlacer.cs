using System.Collections.Generic;
using UnityEngine;

namespace AlgorithmOfDelivery.Maze
{
    public class HousePlacer : MonoBehaviour
    {
        [Header("House Settings")]
        public Sprite houseSprite;
        public float houseScale = 1f;
        public float placementRadius = 10f;
        public int housesPerNode = 2;

        private List<GameObject> _houseInstances = new List<GameObject>();

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

        public void ClearHouses()
        {
            foreach (var house in _houseInstances)
            {
                if (house != null)
                    Destroy(house);
            }
            _houseInstances.Clear();
        }
    }
}
