using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Game.Domain;

/// <summary>Repeatable SP6 smoke scenario through the same HubWorld APIs used by the game.</summary>
public static class ProgressionEventScenarios
{
    const int Seed = 20261003;

    public static void Run(TextWriter output)
    {
        if (output == null) throw new ArgumentNullException(nameof(output));
        var config = new SimConfig { TrainerCount = 1, StartMinute = SimClock.DawnMinute,
            StartTreasury = 100000, StartTrainerGold = 10000 };
        var world = new HubWorld(config, Seed);
        var events = new List<IDomainEvent>();
        world.EventRaised += events.Add;

        world.SetBuildingPower(BuildingKind.Hospital, false);
        world.ActivateBreedingSeason();
        world.ActivateMonsterFlu();
        world.RunFor(SimClock.MinutesPerDay);

        output.WriteLine($"# progression-events: seed {Seed}; time {world.Now.TotalMinutes}");
        output.WriteLine($"TownHall {world.Progression.TownHallLevel} (tier {world.Progression.TownHallTier}), Dormitory {world.Progression.DormitoryLevel}, population {world.Progression.CurrentPopulation}/{world.Progression.PopulationCapacity}");
        output.WriteLine($"Zone1 state: {(world.Zones.Single(z => z.Id == "zone_1").IsUnlocked ? "unlocked" : "locked")}; Zone2 attempt: {world.UnlockZone("zone_2").Reason}; reputation {world.Reputation.Score:F2}, traffic x{world.Reputation.TrafficMultiplier:F2}, inspection x{world.Reputation.InspectionPressureMultiplier:F2}");
        output.WriteLine($"Active events: {string.Join(", ", world.ActiveEvents.Select(e => e.Kind))}; Domain event count {events.Count}; treasury {world.Treasury}");
        output.WriteLine($"Hospital powered: {world.Buildings.Single(b => b.Kind == BuildingKind.Hospital).PoweredOn}; flu damage events {events.OfType<HubEventChanged>().Count(e => e.Kind == HubEventKind.MonsterFlu && e.Phase == "DailyDamage")}");
    }
}
