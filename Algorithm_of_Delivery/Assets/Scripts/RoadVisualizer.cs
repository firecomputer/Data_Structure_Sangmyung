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
                CreateRoadSegment(edge.From, edge.To, parent);
            }
        }

        private void CreateRoadSegment(Vector2 start, Vector2 end, Transform parent)
        {
            GameObject roadObj = new GameObject("Road");
            roadObj.transform.SetParent(parent);
            roadObj.transform.position = Vector3.zero;
            _roadInstances.Add(roadObj);

            MeshFilter mf = roadObj.AddComponent<MeshFilter>();
            MeshRenderer mr = roadObj.AddComponent<MeshRenderer>();
            mr.material = _roadMaterial;

            Vector2 direction = (end - start).normalized;
            Vector2 perpendicular = new Vector2(-direction.y, direction.x);
            float halfWidth = roadWidth / 2f;

            Vector2 leftStart = start + perpendicular * halfWidth;
            Vector2 rightStart = start - perpendicular * halfWidth;
            Vector2 leftEnd = end + perpendicular * halfWidth;
            Vector2 rightEnd = end - perpendicular * halfWidth;

            Vector3[] vertices = new Vector3[4];
            vertices[0] = new Vector3(leftStart.x, leftStart.y, 0);
            vertices[1] = new Vector3(rightStart.x, rightStart.y, 0);
            vertices[2] = new Vector3(leftEnd.x, leftEnd.y, 0);
            vertices[3] = new Vector3(rightEnd.x, rightEnd.y, 0);

            int[] triangles = new int[6];
            triangles[0] = 0; triangles[1] = 1; triangles[2] = 2;
            triangles[3] = 1; triangles[4] = 3; triangles[5] = 2;

            Vector2[] uvs = new Vector2[4];
            uvs[0] = new Vector2(0, 0);
            uvs[1] = new Vector2(1, 0);
            uvs[2] = new Vector2(0, 1);
            uvs[3] = new Vector2(1, 1);

            Mesh mesh = new Mesh();
            mesh.name = "RoadMesh";
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.uv = uvs;
            mesh.RecalculateNormals();

            mf.mesh = mesh;
            mr.sortingOrder = 0;
        }

        private Material CreateDefaultRoadMaterial()
        {
            Material mat = new Material(Shader.Find("Sprites/Default"));
            mat.color = _roadColor;
            return mat;
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
