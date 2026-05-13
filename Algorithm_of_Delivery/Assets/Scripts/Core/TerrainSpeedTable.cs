using System.Collections.Generic;
using static AlgorithmOfDelivery.Maze.MSTGenerator;

namespace AlgorithmOfDelivery.Core
{
    public static class TerrainSpeedTable
    {
        public static readonly Dictionary<TerrainType, float> BaseMultiplier = new Dictionary<TerrainType, float>
        {
            { TerrainType.Asphalt, 1.00f },
            { TerrainType.Dirt,    0.85f },
            { TerrainType.Rocky,   0.65f },
            { TerrainType.Hill,    0.55f },
            { TerrainType.Ruins,   0.50f },
        };

        public static float GetBaseMultiplier(TerrainType terrain)
        {
            if (BaseMultiplier.TryGetValue(terrain, out float mul))
                return mul;
            return 1.00f;
        }
    }
}
