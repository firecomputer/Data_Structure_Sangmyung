using System;
using System.Collections.Generic;
using UnityEngine;

namespace AlgorithmOfDelivery.Maze
{
    public class MSTGenerator
    {
    public enum TerrainType
    {
        Asphalt,    // 포장도로
        Dirt,       // 흙길
        Rocky,      // 돌길
        Hill,       // 오르막길
        Ruins       // 폐허길
    }

    public class Edge
    {
        public Vector2 From { get; set; }
        public Vector2 To { get; set; }
        public float Weight { get; set; }
        public TerrainType Terrain { get; set; }
        public int ZoneId { get; set; }
        public float Altitude { get; set; }

        public Edge(Vector2 from, Vector2 to)
        {
            From = from;
            To = to;
            Weight = Vector2.Distance(from, to);
            Terrain = TerrainType.Asphalt;
            ZoneId = 0;
            Altitude = 0f;
        }
    }

        private class UnionFind
        {
            private Dictionary<Vector2, Vector2> _parent = new Dictionary<Vector2, Vector2>();
            private Dictionary<Vector2, int> _rank = new Dictionary<Vector2, int>();

            public void MakeSet(Vector2 v)
            {
                if (!_parent.ContainsKey(v))
                {
                    _parent[v] = v;
                    _rank[v] = 0;
                }
            }

            public Vector2 Find(Vector2 v)
            {
                if (!_parent.ContainsKey(v))
                    MakeSet(v);

                if (_parent[v] != v)
                    _parent[v] = Find(_parent[v]);
                return _parent[v];
            }

            public void Union(Vector2 x, Vector2 y)
            {
                Vector2 rootX = Find(x);
                Vector2 rootY = Find(y);

                if (rootX == rootY) return;

                int rankX = _rank[rootX];
                int rankY = _rank[rootY];

                if (rankX < rankY)
                    _parent[rootX] = rootY;
                else if (rankX > rankY)
                    _parent[rootY] = rootX;
                else
                {
                    _parent[rootY] = rootX;
                    _rank[rootX]++;
                }
            }
        }

        public List<Edge> GenerateMST(List<Vector2> points, List<Edge> edges)
        {
            List<Edge> sortedEdges = new List<Edge>(edges);
            sortedEdges.Sort((a, b) => a.Weight.CompareTo(b.Weight));

            UnionFind uf = new UnionFind();
            foreach (var point in points)
            {
                uf.MakeSet(point);
            }

            List<Edge> mstEdges = new List<Edge>();

            foreach (var edge in sortedEdges)
            {
                if (uf.Find(edge.From) != uf.Find(edge.To))
                {
                    mstEdges.Add(edge);
                    uf.Union(edge.From, edge.To);
                }

                if (mstEdges.Count == points.Count - 1)
                    break;
            }

            return mstEdges;
        }
    }
}
