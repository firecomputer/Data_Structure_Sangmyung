using static AlgorithmOfDelivery.Maze.MSTGenerator;

namespace AlgorithmOfDelivery.Core
{
    public class CourierState
    {
        public string Name;
        public CourierType Type;
        public string PortraitPath;
        public TraitData[] Traits;
        public string TypeName;

        public float MaxFatigue = 100f;
        public float Fatigue = 100f;
        public float Exp;
        public float Money;
        public float Luck;
        public float WeighRes;
        public float RepairTime;

        public float ActiveSpeedMul       = 1.00f;
        public float ActiveFatigueRate    = 1.00f;
        public float ActiveExpMul         = 1.00f;
        public float ActiveMoneyMul       = 1.00f;
        public float ActiveLuckMul        = 1.00f;
        public float ActiveRestTimeMul    = 1.00f;

        public bool IsExhausted => Fatigue <= 10f;

        public static string GetTypeName(CourierType type)
        {
            return type switch
            {
                CourierType.NormalMail => "일반 서신 집배원",
                CourierType.HeavyTransport => "대량 운송 집배원",
                CourierType.LongDistance => "광역 간선 전달자",
                CourierType.RoughTerrain => "오지 험로 배달원",
                _ => "알 수 없음"
            };
        }

        public CourierState(CourierData data)
        {
            Name = data.Name;
            Type = data.Type;
            PortraitPath = data.PortraitPath;
            Traits = data.Traits;
            TypeName = GetTypeName(data.Type);
            ApplyTraits();
        }

        private void ApplyTraits()
        {
            ActiveSpeedMul = 1.00f;
            ActiveFatigueRate = 1.00f;
            ActiveExpMul = 1.00f;
            ActiveMoneyMul = 1.00f;
            ActiveLuckMul = 1.00f;
            ActiveRestTimeMul = 1.00f;

            foreach (var trait in Traits)
            {
                ActiveSpeedMul *= trait.SpeedMul;
                ActiveFatigueRate *= trait.FatigueRate;
                ActiveExpMul *= trait.ExpMul;
                ActiveMoneyMul *= trait.MoneyMul;
                ActiveLuckMul *= trait.LuckMul;
                ActiveRestTimeMul *= trait.RestTimeMul;
            }
        }

        public float GetTerrainMultiplier(TerrainType terrain)
        {
            float baseMul = TerrainSpeedTable.GetBaseMultiplier(terrain);
            float bestReduction = 0f;

            foreach (var trait in Traits)
            {
                float reduction = trait.AllTerrainPenaltyReduction;
                switch (terrain)
                {
                    case TerrainType.Hill:
                        if (trait.HillPenaltyReduction > reduction)
                            reduction = trait.HillPenaltyReduction;
                        break;
                    case TerrainType.Rocky:
                        if (trait.RockyPenaltyReduction > reduction)
                            reduction = trait.RockyPenaltyReduction;
                        break;
                    case TerrainType.Ruins:
                        if (trait.RuinsPenaltyReduction > reduction)
                            reduction = trait.RuinsPenaltyReduction;
                        break;
                }
                if (reduction > bestReduction)
                    bestReduction = reduction;
            }

            if (bestReduction > 0f)
                return TraitData.ComputeTerrainMultiplier(baseMul, bestReduction);

            return baseMul;
        }

        public void DrainFatigue(float deltaTime, float baseRate = 2f)
        {
            Fatigue -= baseRate * ActiveFatigueRate * deltaTime;
            if (Fatigue < 0f) Fatigue = 0f;
        }

        public void RecoverFatigue(float deltaTime, float baseRate = 5f)
        {
            Fatigue += baseRate * (2f - ActiveRestTimeMul) * deltaTime;
            if (Fatigue > MaxFatigue) Fatigue = MaxFatigue;
        }
    }
}
