using System.Collections.Generic;
using UnityEngine;

namespace AlgorithmOfDelivery.Maze
{
    public static class RhombusGrid
    {
        public static float TileSize = 120f;
        public static float OriginX = 0f;
        public static float OriginY = 0f;

        public static void Configure(float tileSize, float originX, float originY)
        {
            TileSize = tileSize;
            OriginX = originX;
            OriginY = originY;
        }

        public static Vector2 GridToWorld(int row, int col)
        {
            float wx = OriginX + (col - row) * TileSize / 2f;
            float wy = OriginY + (col + row) * TileSize / 2f;
            return new Vector2(wx, wy);
        }

        public static Vector2 GridToWorld(float row, float col)
        {
            float wx = OriginX + (col - row) * TileSize / 2f;
            float wy = OriginY + (col + row) * TileSize / 2f;
            return new Vector2(wx, wy);
        }

        public static (int row, int col) WorldToGrid(Vector2 worldPos)
        {
            float fx = worldPos.x - OriginX;
            float fy = worldPos.y - OriginY;
            int col = Mathf.RoundToInt((fx + fy) / TileSize);
            int row = Mathf.RoundToInt((fy - fx) / TileSize);
            return (row, col);
        }

        public static List<(int row, int col)> GetFourNeighbors(int row, int col)
        {
            return new List<(int, int)>
            {
                (row - 1, col),
                (row + 1, col),
                (row, col - 1),
                (row, col + 1)
            };
        }
    }
}
