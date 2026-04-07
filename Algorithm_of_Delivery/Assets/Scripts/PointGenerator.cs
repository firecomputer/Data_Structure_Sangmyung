using System;
using System.Collections.Generic;
using UnityEngine;

namespace AlgorithmOfDelivery.Maze
{
    public class PointGenerator
    {
        private readonly float _width;
        private readonly float _height;
        private readonly float _margin;
        private readonly float _minDistance;

        public PointGenerator(float width, float height, float margin = 20f, float minDistance = 50f)
        {
            _width = width;
            _height = height;
            _margin = margin;
            _minDistance = minDistance;
        }

        public List<Vector2> Generate(int pointCount)
        {
            List<Vector2> points = new List<Vector2>();
            float effectiveWidth = _width - 2f * _margin;
            float effectiveHeight = _height - 2f * _margin;

            for (int i = 0; i < pointCount; i++)
            {
                Vector2 candidate;
                int attempts = 0;
                do
                {
                    float x = UnityEngine.Random.Range(_margin, _margin + effectiveWidth);
                    float y = UnityEngine.Random.Range(_margin, _margin + effectiveHeight);
                    candidate = new Vector2(x, y);
                    attempts++;
                } while (IsTooClose(candidate, points) && attempts < 100);

                if (attempts < 100)
                {
                    points.Add(candidate);
                }
            }

            return points;
        }

        private bool IsTooClose(Vector2 candidate, List<Vector2> existingPoints)
        {
            foreach (var point in existingPoints)
            {
                if (Vector2.Distance(candidate, point) < _minDistance)
                    return true;
            }
            return false;
        }
    }
}
