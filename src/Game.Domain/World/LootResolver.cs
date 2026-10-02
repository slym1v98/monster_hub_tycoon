using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.ObjectModel;
using Game.Domain.Combat;
using Game.Domain.Materials;

namespace Game.Domain
{
    public sealed class LootConfig
    {
        public double LuckGoldPerPoint { get; }
        public double MaterialPickupChance { get; }
        public int BackpackCapacity { get; }
        public IReadOnlyList<BalanceParameter> BalanceParameters { get; }
        public LootConfig(double luckGoldPerPoint = 0.01, double materialPickupChance = 1, int backpackCapacity = int.MaxValue)
        {
            ZoneDefinition.ValidateNonNegativeFinite(luckGoldPerPoint, nameof(luckGoldPerPoint));
            if (double.IsNaN(materialPickupChance) || materialPickupChance < 0 || materialPickupChance > 1) throw new ArgumentOutOfRangeException(nameof(materialPickupChance));
            if (backpackCapacity < 0) throw new ArgumentOutOfRangeException(nameof(backpackCapacity));
            LuckGoldPerPoint = luckGoldPerPoint; MaterialPickupChance = materialPickupChance; BackpackCapacity = backpackCapacity;
            BalanceParameters = Array.AsReadOnly(new[] {
                new BalanceParameter("loot.luck_gold_per_point", luckGoldPerPoint, "gold/point", "Prototype", "docs/designs/03_Trainer_AI_System.md: Luck affects Gold quantity; conversion rate is prototype."),
                new BalanceParameter("loot.material_pickup_chance", materialPickupChance, "probability", "Prototype", "Prototype pickup roll multiplied by PersonalityProfile.MaterialPickRate."),
                new BalanceParameter("loot.backpack_capacity", backpackCapacity, "units", "Prototype", "SimConfig backpack-capacity behavior; capacity remains configurable.")
            });
        }
    }
    public sealed class LootResolver
    {
        public ExpeditionLoot Resolve(BattleResult battle, ZoneDefinition zone, TrainerSnapshot trainer, SimTime time, LootConfig config, SimRandom rng)
        {
            if (battle == null) throw new ArgumentNullException(nameof(battle)); if (zone == null) throw new ArgumentNullException(nameof(zone)); if (trainer == null) throw new ArgumentNullException(nameof(trainer));
            if (config == null) throw new ArgumentNullException(nameof(config)); if (rng == null) throw new ArgumentNullException(nameof(rng));
            bool won = battle.Outcome == BattleOutcome.TeamWon;
            var empty = new ExpeditionLoot(Array.Empty<MaterialQuantity>(), Array.Empty<MaterialQuantity>(), 0, 0);
            if (!won) return empty;
            var nightMultiplier = time.IsNight && trainer.HasNightVision ? zone.NightLootMultiplier : 1;
            var profile = zone.EncounterProfile;
            long gold = checked((long)Math.Floor(profile.GoldPerEncounter * (1 + trainer.Attributes.Luck * config.LuckGoldPerPoint)));
            long xp = checked((long)Math.Floor(profile.ExperiencePerEncounter * (time.IsNight && trainer.HasNightVision ? zone.NightExperienceMultiplier : 1)));
            int generated = checked((int)Math.Floor(profile.ExpectedMaterialUnitsPerEncounter * nightMultiplier));
            double pickup = PersonalityProfile.Of(trainer.Personality).MaterialPickRate * config.MaterialPickupChance;
            var weights = zone.MaterialWeights.OrderBy(x => x.MaterialId.Value, StringComparer.Ordinal).ToArray();
            var totals = new SortedDictionary<MaterialId, int>(Comparer<MaterialId>.Create((a,b) => string.CompareOrdinal(a.Value,b.Value)));
            for (int i = 0; i < generated; i++)
            {
                if (rng.NextDouble() >= pickup) continue;
                double total = weights.Sum(x => x.Weight), draw = rng.NextDouble() * total;
                var chosen = weights[weights.Length - 1];
                foreach (var w in weights) { draw -= w.Weight; if (draw < 0) { chosen = w; break; } }
                totals[chosen.MaterialId] = totals.TryGetValue(chosen.MaterialId, out var old) ? old + 1 : 1;
            }
            var collected = new List<MaterialQuantity>(); var dropped = new List<MaterialQuantity>(); int remaining = config.BackpackCapacity;
            foreach (var pair in totals)
            {
                int take = Math.Min(pair.Value, remaining); remaining -= take;
                if (take > 0) collected.Add(new MaterialQuantity(pair.Key, take));
                if (pair.Value > take) dropped.Add(new MaterialQuantity(pair.Key, pair.Value - take));
            }
            return new ExpeditionLoot(collected, dropped, gold, xp);
        }
    }
}
