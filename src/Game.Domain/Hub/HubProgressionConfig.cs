using System;
using System.Collections.Generic;

namespace Game.Domain
{
    /// <summary>GDD progression gates and current prototype start state.</summary>
    public sealed class HubProgressionConfig
    {
        public static HubProgressionConfig Prototype { get; } = new HubProgressionConfig();
        public IReadOnlyList<int> PopulationCaps { get; }
        public IReadOnlyList<int> RankCountsToUnlock { get; }
        public IReadOnlyList<BalanceParameter> BalanceParameters { get; }
        public long DormitoryUpgradeGoldBase { get; }
        public long TownHallUpgradeGoldBase { get; }
        public long FacilityUpgradeGoldBase { get; }
        public double DormitoryUpgradeCostGrowth { get; }
        public int FacilityUpgradeMinutes { get; }
        public int TownHallMaxLevel { get; }
        public int TownHallLevelsPerTier { get; }
        public int DormitoryMaxLevel { get; }
        public int WoodIngotsPerLevel { get; }
        public int StoneIngotsPerLevel { get; }
        public int IronIngotsPerLevel { get; }
        public long RepairGoldPerBuildingLevel { get; }
        public int RepairMinutes { get; }
        public double DegradedFacilityEfficiency { get; }

        public HubProgressionConfig(IReadOnlyList<int> populationCaps = null, IReadOnlyList<int> rankCountsToUnlock = null,
            long dormitoryUpgradeGoldBase = 1000, long townHallUpgradeGoldBase = 1000,
            double dormitoryUpgradeCostGrowth = 1.5,
            int facilityUpgradeMinutes = 2880, int woodIngotsPerLevel = 1,
            int stoneIngotsPerLevel = 1, int ironIngotsPerLevel = 1,
            long repairGoldPerBuildingLevel = 200, int repairMinutes = 1440, long facilityUpgradeGoldBase = 1000,
            double degradedFacilityEfficiency = 0.5)
        {
            int[] caps = populationCaps == null ? new[] { 10, 15, 20, 25, 30 } : CopyAndValidate(populationCaps, nameof(populationCaps));
            int[] ranks = rankCountsToUnlock == null ? new[] { 5, 7, 12, 17, 22 } : CopyAndValidate(rankCountsToUnlock, nameof(rankCountsToUnlock));
            if (caps.Length != 5 || ranks.Length != 5) throw new ArgumentException("Progression needs exactly five Zone gates.");
            PopulationCaps = Array.AsReadOnly(caps);
            RankCountsToUnlock = Array.AsReadOnly(ranks);
            if (dormitoryUpgradeGoldBase < 0) throw new ArgumentOutOfRangeException(nameof(dormitoryUpgradeGoldBase));
            if (townHallUpgradeGoldBase < 0) throw new ArgumentOutOfRangeException(nameof(townHallUpgradeGoldBase));
            if (facilityUpgradeGoldBase < 0) throw new ArgumentOutOfRangeException(nameof(facilityUpgradeGoldBase));
            if (double.IsNaN(degradedFacilityEfficiency) || double.IsInfinity(degradedFacilityEfficiency) || degradedFacilityEfficiency <= 0 || degradedFacilityEfficiency > 1)
                throw new ArgumentOutOfRangeException(nameof(degradedFacilityEfficiency));
            if (double.IsNaN(dormitoryUpgradeCostGrowth) || double.IsInfinity(dormitoryUpgradeCostGrowth) || dormitoryUpgradeCostGrowth < 1) throw new ArgumentOutOfRangeException(nameof(dormitoryUpgradeCostGrowth));
            if (facilityUpgradeMinutes <= 0) throw new ArgumentOutOfRangeException(nameof(facilityUpgradeMinutes));
            if (woodIngotsPerLevel < 0 || stoneIngotsPerLevel < 0 || ironIngotsPerLevel < 0 || repairGoldPerBuildingLevel < 0 || repairMinutes <= 0)
                throw new ArgumentOutOfRangeException(nameof(woodIngotsPerLevel));
            DormitoryUpgradeGoldBase = dormitoryUpgradeGoldBase;
            TownHallUpgradeGoldBase = townHallUpgradeGoldBase;
            FacilityUpgradeGoldBase = facilityUpgradeGoldBase;
            DormitoryUpgradeCostGrowth = dormitoryUpgradeCostGrowth;
            FacilityUpgradeMinutes = facilityUpgradeMinutes;
            TownHallMaxLevel = 25;
            TownHallLevelsPerTier = 5;
            DormitoryMaxLevel = 5;
            WoodIngotsPerLevel = woodIngotsPerLevel; StoneIngotsPerLevel = stoneIngotsPerLevel;
            IronIngotsPerLevel = ironIngotsPerLevel; RepairGoldPerBuildingLevel = repairGoldPerBuildingLevel;
            RepairMinutes = repairMinutes;
            DegradedFacilityEfficiency = degradedFacilityEfficiency;
            var parameters = new List<BalanceParameter>();
            for (int i = 0; i < 5; i++)
            {
                int zone = i + 1;
                parameters.Add(new BalanceParameter($"progression.zone_{zone}.population_cap", caps[i], "Trainers", "Locked", "docs/designs/02_HUB_Economy_Infrastructure.md §1.3; docs/designs/13_Balance_Parameters.md §11"));
                parameters.Add(new BalanceParameter($"progression.zone_{zone}.rank_count_gate", ranks[i], "Trainers at Rank >= Zone", "Locked", "docs/designs/02_HUB_Economy_Infrastructure.md §1.3"));
            }
            parameters.Add(new BalanceParameter("progression.dormitory.upgrade_gold_base", dormitoryUpgradeGoldBase, "Gold for level 2", "Prototype", "docs/designs/02_HUB_Economy_Infrastructure.md §1.2: construction consumes Gold and ingots; cost is unspecified."));
            parameters.Add(new BalanceParameter("progression.town_hall.upgrade_gold_base", townHallUpgradeGoldBase, "Gold for level 2", "Prototype", "docs/designs/02_HUB_Economy_Infrastructure.md §1.2: construction consumes Gold and ingots; cost is unspecified."));
            parameters.Add(new BalanceParameter("progression.facility.upgrade_gold_base", facilityUpgradeGoldBase, "Gold for level 2 or construction", "Prototype", "docs/designs/02_HUB_Economy_Infrastructure.md §1.2: generic facility cost is unspecified."));
            parameters.Add(new BalanceParameter("progression.dormitory.upgrade_cost_growth", dormitoryUpgradeCostGrowth, "multiplier per target level", "Prototype", "docs/designs/02_HUB_Economy_Infrastructure.md §1.2; docs/designs/13_Balance_Parameters.md §11: legacy payback model needs recalculation."));
            parameters.Add(new BalanceParameter("progression.facility_upgrade_minutes", facilityUpgradeMinutes, "minutes in-game", "Prototype", "docs/designs/02_HUB_Economy_Infrastructure.md §1.2: construction takes several in-game days."));
            parameters.Add(new BalanceParameter("progression.town_hall.max_level", TownHallMaxLevel, "levels", "Locked", "docs/designs/02_HUB_Economy_Infrastructure.md §1.1."));
            parameters.Add(new BalanceParameter("progression.town_hall.levels_per_tier", TownHallLevelsPerTier, "levels/tier", "Locked", "docs/designs/02_HUB_Economy_Infrastructure.md §1.1."));
            parameters.Add(new BalanceParameter("progression.dormitory.max_level", DormitoryMaxLevel, "levels", "Locked", "docs/designs/02_HUB_Economy_Infrastructure.md §1.1."));
            parameters.Add(new BalanceParameter("progression.construction.wood_ingots_per_level", woodIngotsPerLevel, "Wood ingots/target level", "Prototype", "docs/designs/02_HUB_Economy_Infrastructure.md §1.2: quantity unspecified."));
            parameters.Add(new BalanceParameter("progression.construction.stone_ingots_per_level", stoneIngotsPerLevel, "Stone ingots/target level", "Prototype", "docs/designs/02_HUB_Economy_Infrastructure.md §1.2: quantity unspecified; mapped to Ore Tier 1."));
            parameters.Add(new BalanceParameter("progression.construction.iron_ingots_per_level", ironIngotsPerLevel, "Iron ingots/target level", "Prototype", "docs/designs/02_HUB_Economy_Infrastructure.md §1.2: quantity unspecified; mapped to Ore Tier 2."));
            parameters.Add(new BalanceParameter("buildings.repair.gold_per_level", repairGoldPerBuildingLevel, "Gold/building level", "Prototype", "docs/designs/02_HUB_Economy_Infrastructure.md §1.6: repair cost scales with level; base amount unspecified."));
            parameters.Add(new BalanceParameter("buildings.repair.minutes", repairMinutes, "in-game minutes", "Prototype", "docs/designs/02_HUB_Economy_Infrastructure.md §1.6: repair duration unspecified."));
            parameters.Add(new BalanceParameter("buildings.repair.wood_ingots_per_level", woodIngotsPerLevel, "Wood ingots/building level", "Prototype", "docs/designs/02_HUB_Economy_Infrastructure.md §1.6: repair consumes ingots; quantity unspecified."));
            parameters.Add(new BalanceParameter("buildings.repair.stone_ingots_per_level", stoneIngotsPerLevel, "Stone ingots/building level", "Prototype", "docs/designs/02_HUB_Economy_Infrastructure.md §1.6: repair consumes ingots; quantity unspecified."));
            parameters.Add(new BalanceParameter("buildings.repair.iron_ingots_per_level", ironIngotsPerLevel, "Iron ingots/building level", "Prototype", "docs/designs/02_HUB_Economy_Infrastructure.md §1.6: repair consumes ingots; quantity unspecified."));
            parameters.Add(new BalanceParameter("buildings.degraded.efficiency_multiplier", degradedFacilityEfficiency, "multiplier", "Locked", "docs/designs/02_HUB_Economy_Infrastructure.md §1.2: maintenance deficit reduces efficiency by half; §1.6: damaged buildings operate weakly like maintenance deficit."));
            BalanceParameters = Array.AsReadOnly(parameters.ToArray());
        }

        static int[] CopyAndValidate(IReadOnlyList<int> values, string name)
        {
            if (values == null) throw new ArgumentNullException(name);
            var copy = new int[values.Count];
            for (int i = 0; i < values.Count; i++)
            {
                if (values[i] <= 0 || (i > 0 && values[i] <= values[i - 1])) throw new ArgumentException("Progression gates must be positive and strictly increasing.", name);
                copy[i] = values[i];
            }
            return copy;
        }
    }
}
