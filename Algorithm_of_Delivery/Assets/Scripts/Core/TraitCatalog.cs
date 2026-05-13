using static AlgorithmOfDelivery.Maze.MSTGenerator;

namespace AlgorithmOfDelivery.Core
{
    public class TraitData
    {
        public string Name;

        public float SpeedMul          = 1.00f;
        public float FatigueRate       = 1.00f;
        public float ExpMul            = 1.00f;
        public float MoneyMul          = 1.00f;
        public float LuckMul           = 1.00f;
        public float RestTimeMul       = 1.00f;

        public float HillPenaltyReduction;
        public float RockyPenaltyReduction;
        public float RuinsPenaltyReduction;
        public float AllTerrainPenaltyReduction;

        public static float ComputeTerrainMultiplier(float baseMultiplier, float reduction)
        {
            float penalty = 1f - baseMultiplier;
            float reducedPenalty = penalty * (1f - reduction);
            return 1f - reducedPenalty;
        }

        public float GetTerrainMultiplier(TerrainType terrain)
        {
            float baseMul = TerrainSpeedTable.GetBaseMultiplier(terrain);

            float reduction = AllTerrainPenaltyReduction;

            switch (terrain)
            {
                case TerrainType.Hill:
                    if (HillPenaltyReduction > reduction)
                        reduction = HillPenaltyReduction;
                    break;
                case TerrainType.Rocky:
                    if (RockyPenaltyReduction > reduction)
                        reduction = RockyPenaltyReduction;
                    break;
                case TerrainType.Ruins:
                    if (RuinsPenaltyReduction > reduction)
                        reduction = RuinsPenaltyReduction;
                    break;
            }

            if (reduction > 0f)
                return ComputeTerrainMultiplier(baseMul, reduction);

            return baseMul;
        }
    }

    public static class TraitCatalog
    {
        public static readonly TraitData IMPATIENT = new TraitData
        {
            Name = "성급함",
            SpeedMul = 1.20f,
            FatigueRate = 1.20f,
        };

        public static readonly TraitData CAREFUL = new TraitData
        {
            Name = "신중함",
            ExpMul = 1.25f,
            SpeedMul = 0.80f,
        };

        public static readonly TraitData INDOMITABLE = new TraitData
        {
            Name = "불굴",
            RestTimeMul = 0.75f,
            SpeedMul = 0.85f,
        };

        public static readonly TraitData GAMBLER = new TraitData
        {
            Name = "도박꾼",
            LuckMul = 1.50f,
            MoneyMul = 0.70f,
        };

        public static readonly TraitData NEGOTIATOR = new TraitData
        {
            Name = "협상의 달인",
            MoneyMul = 1.35f,
            FatigueRate = 1.10f,
        };

        public static readonly TraitData BRIGHT = new TraitData
        {
            Name = "명석한",
            ExpMul = 1.30f,
        };

        public static readonly TraitData HILL_EXPERT = new TraitData
        {
            Name = "언덕 전문가",
            HillPenaltyReduction = 0.50f,
        };

        public static readonly TraitData ROUGH_EXPERT = new TraitData
        {
            Name = "험지 전문가",
            RockyPenaltyReduction = 0.50f,
            RuinsPenaltyReduction = 0.50f,
        };

        public static readonly TraitData ADAPTIVE = new TraitData
        {
            Name = "적응가",
            AllTerrainPenaltyReduction = 0.25f,
        };
    }
}
