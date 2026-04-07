using System;
using System.Collections.Generic;
using UnityEngine;

namespace AlgorithmOfDelivery.Maze
{
    public class DelaunayTriangulator
    {
        private const float EPSILON = 1e-6f;

        public class Triangle
        {
            public Vector2 p0, p1, p2;

            public Triangle(Vector2 p0, Vector2 p1, Vector2 p2)
            {
                this.p0 = p0;
                this.p1 = p1;
                this.p2 = p2;
            }

            public bool ContainsVertex(Vector2 p)
            {
                return IsEqual(p, p0) || IsEqual(p, p1) || IsEqual(p, p2);
            }

            private bool IsEqual(Vector2 a, Vector2 b)
            {
                return Vector2.SqrMagnitude(a - b) < EPSILON;
            }

            public Vector2 Circumcenter()
            {
                float ax = p0.x, ay = p0.y;
                float bx = p1.x, by = p1.y;
                float cx = p2.x, cy = p2.y;

                float d = 2f * (ax * (by - cy) + bx * (cy - ay) + cx * (ay - by));
                if (Mathf.Abs(d) < EPSILON)
                    return Vector2.zero;

                float ux = ((ax * ax + ay * ay) * (by - cy) + (bx * bx + by * by) * (cy - ay) + (cx * cx + cy * cy) * (ay - by)) / d;
                float uy = ((ax * ax + ay * ay) * (cx - bx) + (bx * bx + by * by) * (ax - cx) + (cx * cx + cy * cy) * (bx - ax)) / d;

                return new Vector2(ux, uy);
            }

            public float CircumradiusSquared()
            {
                Vector2 center = Circumcenter();
                return Vector2.SqrMagnitude(p0 - center);
            }

            public bool IsInsideCircumcircle(Vector2 p)
            {
                Vector2 center = Circumcenter();
                float distSq = Vector2.SqrMagnitude(p - center);
                return distSq < CircumradiusSquared();
            }

            public List<Edge> Edges
            {
                get
                {
                    return new List<Edge>
                    {
                        new Edge(p0, p1),
                        new Edge(p1, p2),
                        new Edge(p2, p0)
                    };
                }
            }
        }

        public class Edge : IEquatable<Edge>
        {
            public Vector2 p0, p1;

            public Edge(Vector2 p0, Vector2 p1)
            {
                this.p0 = p0;
                this.p1 = p1;
            }

            public bool Equals(Edge other)
            {
                return (IsEqual(p0, other.p0) && IsEqual(p1, other.p1)) ||
                       (IsEqual(p0, other.p1) && IsEqual(p1, other.p0));
            }

            private bool IsEqual(Vector2 a, Vector2 b)
            {
                return Vector2.SqrMagnitude(a - b) < EPSILON;
            }

            public override int GetHashCode()
            {
                int h0 = p0.GetHashCode();
                int h1 = p1.GetHashCode();
                return h0 ^ (h1 << 16);
            }
        }

        public List<Triangle> Triangulate(List<Vector2> points)
        {
            if (points == null || points.Count < 3)
                return new List<Triangle>();

            float minX = points[0].x, maxX = points[0].x, minY = points[0].y, maxY = points[0].y;
            foreach (var p in points)
            {
                minX = Mathf.Min(minX, p.x); maxX = Mathf.Max(maxX, p.x);
                minY = Mathf.Min(minY, p.y); maxY = Mathf.Max(maxY, p.y);
            }

            float dx = maxX - minX;
            float dy = maxY - minY;
            float deltaMax = Mathf.Max(dx, dy);
            float midx = (minX + maxX) / 2f;
            float midy = (minY + maxY) / 2f;

            Vector2 p1 = new Vector2(midx - 20f * deltaMax, midy - 20f * deltaMax);
            Vector2 p2 = new Vector2(midx, midy + 20f * deltaMax);
            Vector2 p3 = new Vector2(midx + 20f * deltaMax, midy - 20f * deltaMax);

            List<Triangle> triangles = new List<Triangle>();
            triangles.Add(new Triangle(p1, p2, p3));

            foreach (var point in points)
            {
                List<Triangle> badTriangles = new List<Triangle>();
                List<Edge> polygon = new List<Edge>();

                foreach (var triangle in triangles)
                {
                    if (triangle.IsInsideCircumcircle(point))
                    {
                        badTriangles.Add(triangle);
                    }
                }

                foreach (var triangle in badTriangles)
                {
                    foreach (var edge in triangle.Edges)
                    {
                        bool isShared = false;
                        foreach (var other in badTriangles)
                        {
                            if (triangle == other) continue;
                            foreach (var otherEdge in other.Edges)
                            {
                                if (edge.Equals(otherEdge))
                                {
                                    isShared = true;
                                    break;
                                }
                            }
                            if (isShared) break;
                        }
                        if (!isShared)
                        {
                            polygon.Add(edge);
                        }
                    }
                }

                foreach (var bad in badTriangles)
                {
                    triangles.Remove(bad);
                }

                foreach (var edge in polygon)
                {
                    triangles.Add(new Triangle(edge.p0, edge.p1, point));
                }
            }

            List<Triangle> result = new List<Triangle>();
            foreach (var triangle in triangles)
            {
                if (!ContainsSuperTriangleVertex(triangle, p1, p2, p3))
                {
                    result.Add(triangle);
                }
            }

            return result;
        }

        private bool ContainsSuperTriangleVertex(Triangle triangle, Vector2 s0, Vector2 s1, Vector2 s2)
        {
            return triangle.ContainsVertex(s0) || triangle.ContainsVertex(s1) || triangle.ContainsVertex(s2);
        }

        public List<Edge> GetEdgesFromTriangles(List<Triangle> triangles)
        {
            List<Edge> edges = new List<Edge>();
            HashSet<(long, long)> added = new HashSet<(long, long)>();

            foreach (var triangle in triangles)
            {
                foreach (var edge in triangle.Edges)
                {
                    long hash0 = HashVector2(edge.p0);
                    long hash1 = HashVector2(edge.p1);
                    var key = hash0 < hash1 ? (hash0, hash1) : (hash1, hash0);

                    if (!added.Contains(key))
                    {
                        added.Add(key);
                        edges.Add(edge);
                    }
                }
            }

            return edges;
        }

        private long HashVector2(Vector2 v)
        {
            return ((long)(v.x * 1000) << 32) | (long)(v.y * 1000);
        }
    }
}
