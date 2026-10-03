using System.Collections.Generic;
using Game.Domain;

internal static class TestConfigs
{
    public static SimConfig WithServiceFacilities(this SimConfig config)
    {
        config.StartTownHallLevel = System.Math.Max(4, config.StartBuildingLevel);
        config.StartingFacilityLevels = new Dictionary<string, int>
        {
            ["inn"] = config.StartBuildingLevel,
            ["restaurant"] = config.StartBuildingLevel,
            ["bar"] = config.StartBuildingLevel,
            ["veterinary_hospital"] = config.StartBuildingLevel
        };
        return config;
    }

    public static SimConfig WithTierThreeFacilities(this SimConfig config, params string[] facilities)
    {
        config.StartTownHallLevel = 11;
        config.StartDormitoryLevel = 3;
        config.UnlockedZoneIds = new[] { "zone_1", "zone_2", "zone_3" };
        var levels = new Dictionary<string, int>(config.StartingFacilityLevels ?? new Dictionary<string, int>());
        foreach (string id in facilities) levels[id] = 1;
        config.StartingFacilityLevels = levels;
        return config;
    }
}
