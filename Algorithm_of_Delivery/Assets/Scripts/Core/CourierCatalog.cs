using System.Collections.Generic;

namespace AlgorithmOfDelivery.Core
{
    public enum CourierType
    {
        NormalMail,
        HeavyTransport,
        LongDistance,
        RoughTerrain
    }

    public class CourierData
    {
        public string Name;
        public CourierType Type;
        public string PortraitPath;
        public TraitData[] Traits;

        public CourierData(string name, CourierType type, string portraitPath, TraitData[] traits)
        {
            Name = name;
            Type = type;
            PortraitPath = portraitPath;
            Traits = traits;
        }
    }

    public static class CourierCatalog
    {
        public static readonly List<CourierData> All = new List<CourierData>
        {
            new CourierData(
                "안나 하이더",
                CourierType.NormalMail,
                "Characters/Anna Heider",
                new[] { TraitCatalog.CAREFUL, TraitCatalog.INDOMITABLE, TraitCatalog.NEGOTIATOR }
            ),
            new CourierData(
                "에블린 레이나",
                CourierType.HeavyTransport,
                "Characters/Evelyn Reyna",
                new[] { TraitCatalog.IMPATIENT, TraitCatalog.GAMBLER, TraitCatalog.ROUGH_EXPERT }
            ),
            new CourierData(
                "루나 모건",
                CourierType.RoughTerrain,
                "Characters/Luna Morga",
                new[] { TraitCatalog.HILL_EXPERT, TraitCatalog.ADAPTIVE }
            ),
            new CourierData(
                "데바 스칼렛",
                CourierType.LongDistance,
                "Characters/Deva Scarlet",
                new[] { TraitCatalog.IMPATIENT, TraitCatalog.GAMBLER, TraitCatalog.BRIGHT }
            ),
        };
    }
}
