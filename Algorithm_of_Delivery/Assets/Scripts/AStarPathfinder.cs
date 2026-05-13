using System.Collections.Generic;
using UnityEngine;
using static AlgorithmOfDelivery.Maze.MSTGenerator;

namespace AlgorithmOfDelivery.Maze
{
    public struct PathEdge
    {
        public Vector2 From;
        public Vector2 To;
        public TerrainType Terrain;
        public float Altitude;
    }

    public class AStarPathfinder
    {
        private Dictionary<Vector2, List<Vector2>> _graph;
        private Dictionary<(Vector2, Vector2), TerrainType> _edgeTerrains;
        private Dictionary<(Vector2, Vector2), float> _edgeAltitudes;
        private HashSet<Vector2> _nodes;

        public AStarPathfinder()
        {
            _graph = new Dictionary<Vector2, List<Vector2>>();
            _nodes = new HashSet<Vector2>();
            _edgeTerrains = new Dictionary<(Vector2, Vector2), TerrainType>();
            _edgeAltitudes = new Dictionary<(Vector2, Vector2), float>();
        }

        public void BuildGraph(List<Edge> edges)
        {
            _graph.Clear();
            _nodes.Clear();
            _edgeTerrains.Clear();
            _edgeAltitudes.Clear();

            foreach (var edge in edges)
            {
                if (!_graph.ContainsKey(edge.From))
                    _graph[edge.From] = new List<Vector2>();
                if (!_graph.ContainsKey(edge.To))
                    _graph[edge.To] = new List<Vector2>();

                _graph[edge.From].Add(edge.To);
                _graph[edge.To].Add(edge.From);
                _nodes.Add(edge.From);
                _nodes.Add(edge.To);

                var key1 = (edge.From, edge.To);
                var key2 = (edge.To, edge.From);
                _edgeTerrains[key1] = edge.Terrain;
                _edgeTerrains[key2] = edge.Terrain;
                _edgeAltitudes[key1] = edge.Altitude;
                _edgeAltitudes[key2] = edge.Altitude;
            }
        }

        public Vector2 GetClosestNode(Vector2 position)
        {
            float minDist = float.MaxValue;
            Vector2 closest = position;
            foreach (var node in _nodes)
            {
                float dist = Vector2.Distance(node, position);
                if (dist < minDist)
                {
                    minDist = dist;
                    closest = node;
                }
            }
            return closest;
        }

        public List<Vector2> FindPath(Vector2 start, Vector2 goal)
        {
            return FindPathInternal(start, goal).Path;
        }

        public (List<Vector2> Path, List<PathEdge> Edges) FindPathWithEdges(Vector2 start, Vector2 goal)
        {
            return FindPathInternal(start, goal);
        }

        private (List<Vector2> Path, List<PathEdge> Edges) FindPathInternal(Vector2 start, Vector2 goal)
        {
            if (!_graph.ContainsKey(start) || !_graph.ContainsKey(goal))
                return (new List<Vector2>(), new List<PathEdge>());

            var openSet = new PriorityQueue<Vector2>();
            var cameFrom = new Dictionary<Vector2, Vector2>();
            var gScore = new Dictionary<Vector2, float>();
            var fScore = new Dictionary<Vector2, float>();

            gScore[start] = 0;
            fScore[start] = Heuristic(start, goal);
            openSet.Enqueue(start, fScore[start]);

            while (openSet.Count > 0)
            {
                Vector2 current = openSet.Dequeue();

                if (Vector2.Distance(current, goal) < 0.1f)
                {
                    var path = ReconstructPath(cameFrom, current);
                    var edges = BuildEdgeList(path);
                    return (path, edges);
                }

                if (!_graph.ContainsKey(current))
                    continue;

                foreach (Vector2 neighbor in _graph[current])
                {
                    float tentativeGScore = gScore[current] + Vector2.Distance(current, neighbor);

                    if (!gScore.ContainsKey(neighbor) || tentativeGScore < gScore[neighbor])
                    {
                        cameFrom[neighbor] = current;
                        gScore[neighbor] = tentativeGScore;
                        fScore[neighbor] = tentativeGScore + Heuristic(neighbor, goal);
                        openSet.Enqueue(neighbor, fScore[neighbor]);
                    }
                }
            }

            return (new List<Vector2>(), new List<PathEdge>());
        }

        private float Heuristic(Vector2 a, Vector2 b)
        {
            return Vector2.Distance(a, b);
        }

        private List<Vector2> ReconstructPath(Dictionary<Vector2, Vector2> cameFrom, Vector2 current)
        {
            var path = new List<Vector2> { current };
            while (cameFrom.ContainsKey(current))
            {
                current = cameFrom[current];
                path.Insert(0, current);
            }
            return path;
        }

        private List<PathEdge> BuildEdgeList(List<Vector2> path)
        {
            var edges = new List<PathEdge>();
            for (int i = 0; i < path.Count - 1; i++)
            {
                var key = (path[i], path[i + 1]);
                var edge = new PathEdge
                {
                    From = path[i],
                    To = path[i + 1],
                    Terrain = _edgeTerrains.ContainsKey(key) ? _edgeTerrains[key] : TerrainType.Asphalt,
                    Altitude = _edgeAltitudes.ContainsKey(key) ? _edgeAltitudes[key] : 0f,
                };
                edges.Add(edge);
            }
            return edges;
        }

        public TerrainType GetEdgeTerrain(Vector2 from, Vector2 to)
        {
            var key = (from, to);
            if (_edgeTerrains.TryGetValue(key, out TerrainType terrain))
                return terrain;
            return TerrainType.Asphalt;
        }
    }

    public class PriorityQueue<T>
    {
        private List<(T item, float priority)> _items = new List<(T, float)>();

        public int Count => _items.Count;

        public void Enqueue(T item, float priority)
        {
            _items.Add((item, priority));
            _items.Sort((a, b) => a.priority.CompareTo(b.priority));
        }

        public T Dequeue()
        {
            T item = _items[0].item;
            _items.RemoveAt(0);
            return item;
        }
    }
}
