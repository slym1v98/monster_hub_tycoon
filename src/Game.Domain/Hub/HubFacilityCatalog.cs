using System;
using System.Collections.Generic;

namespace Game.Domain
{
    public sealed record HubFacilityDefinition(string Id, int MaxLevel, int TownHallUnlockLevel,
        int RequiredZone, bool StartsRebuilt);

    public sealed record HubFacilityView(string Id, int Level, int MaxLevel, int MaxLevelAllowed, int TownHallUnlockLevel,
        int RequiredZone, bool IsUnlocked, bool StartsRebuilt, string State, int? CompletionMinute,
        bool PoweredOn, bool Maintained, bool Damaged);

    internal sealed class HubFacilityRuntimeState
    {
        public int Level { get; set; }
        public int PendingLevel { get; set; } = -1;
        public int CompletionMinute { get; set; } = -1;
        public int RepairFinishMinute { get; set; } = -1;
        public bool PoweredOn { get; set; } = true;
        public bool Maintained { get; set; } = true;
        public bool Damaged { get; set; }
        public HubFacilityRuntimeState(int level) => Level = level;
    }

    /// <summary>GDD facility unlock milestones. Construction and costs are separately balanced.</summary>
    public static class HubFacilityCatalog
    {
        public static IReadOnlyList<HubFacilityDefinition> Definitions { get; } = Array.AsReadOnly(new[]
        {
            D("town_hall", 25, 1, 1, true), D("dormitory", 5, 1, 1, true), D("trading_station", 5, 1, 1, true),
            D("veterinary_hospital", 25, 1, 1, true), D("inn", 25, 2, 1, false),
            D("restaurant", 25, 2, 1, false), D("refinery", 5, 3, 1, false),
            D("tool_workshop", 25, 3, 1, false), D("bar", 25, 4, 1, false),
            D("monster_forge", 5, 4, 1, false), D("textile_workshop", 5, 5, 1, false),
            D("bounty_board", 1, 5, 1, false), D("reactor", 5, 6, 2, false),
            D("warp_gate", 25, 6, 2, false), D("soda_factory", 25, 6, 2, false),
            D("general_store", 25, 6, 2, false), D("gene_bank", 25, 6, 2, false),
            D("jeweler", 5, 11, 3, false), D("evolution_lab", 25, 11, 3, false),
            D("academy", 25, 11, 3, false), D("stock_exchange", 25, 11, 3, false)
        });

        public static IReadOnlyList<BalanceParameter> BalanceParameters { get; } = BuildParameters();

        static IReadOnlyList<BalanceParameter> BuildParameters()
        {
            var rows = new List<BalanceParameter>
            {
                new BalanceParameter("buildings.facility_catalog_entries", Definitions.Count, "facilities", "Locked", "docs/designs/02_HUB_Economy_Infrastructure.md §1.1"),
                new BalanceParameter("buildings.level_25.max_level", 25, "levels", "Locked", "docs/designs/02_HUB_Economy_Infrastructure.md §1.1"),
                new BalanceParameter("buildings.level_5.max_level", 5, "levels", "Locked", "docs/designs/02_HUB_Economy_Infrastructure.md §1.1"),
                new BalanceParameter("buildings.level_1.max_level", 1, "levels", "Locked", "docs/designs/02_HUB_Economy_Infrastructure.md §1.1: Bounty Board is a prop")
            };
            foreach (var definition in Definitions)
            {
                string source = definition.TownHallUnlockLevel <= 5
                    ? "docs/designs/02_HUB_Economy_Infrastructure.md §1.1 facility unlock table."
                    : "docs/designs/02_HUB_Economy_Infrastructure.md §1.1 tier 2/3 unlock table.";
                rows.Add(new BalanceParameter($"buildings.facility.{definition.Id}.max_level", definition.MaxLevel,
                    "levels", "Locked", source));
                rows.Add(new BalanceParameter($"buildings.facility.{definition.Id}.town_hall_unlock_level",
                    definition.TownHallUnlockLevel, "Town Hall level", "Locked", source));
                if (definition.RequiredZone > 1)
                    rows.Add(new BalanceParameter($"buildings.facility.{definition.Id}.required_zone",
                        definition.RequiredZone, "Zone", "Locked", "docs/designs/02_HUB_Economy_Infrastructure.md §1.1: tier requires corresponding Zone."));
            }
            return Array.AsReadOnly(rows.ToArray());
        }

        static HubFacilityDefinition D(string id, int max, int hall, int zone, bool rebuilt)
            => new HubFacilityDefinition(id, max, hall, zone, rebuilt);
    }
}
