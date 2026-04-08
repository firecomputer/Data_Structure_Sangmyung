using System.Collections.Generic;
using UnityEngine;

namespace AlgorithmOfDelivery.Maze
{
    public class AStarPathfinder
    {
        private Dictionary<Vector2, List<Vector2>> _graph;
        private HashSet<Vector2> _nodes;

        public AStarPathfinder()
        {
            _graph = new Dictionary<Vector2, List<Vector2>>();
            _nodes = new HashSet<Vector2>();
        }

        public void BuildGraph(List<MSTGenerator.Edge> edges)
        {
            _graph.Clear();
            _nodes.Clear();
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
            if (!_graph.ContainsKey(start) || !_graph.ContainsKey(goal))
                return new List<Vector2>();

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
                    return ReconstructPath(cameFrom, current);

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

            return new List<Vector2>();
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