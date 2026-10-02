using System;
using System.Collections.Generic;
using System.Linq;
using Game.Domain.Combat;
using Game.Domain.Monsters;

namespace Game.Domain
{
    public sealed class ExpeditionConfig
    {
        public static ExpeditionConfig Prototype { get; } = new ExpeditionConfig();
        public double OpponentHpPerExperience { get; }
        public double OpponentAttack { get; }
        public double OpponentDefense { get; }
        public double OpponentAttackSpeed { get; }
        public IReadOnlyList<BalanceParameter> BalanceParameters { get; }
        public ExpeditionConfig(double opponentHpPerExperience = 2, double opponentAttack = 2,
            double opponentDefense = 5, double opponentAttackSpeed = 1)
        {
            foreach (var value in new[] { opponentHpPerExperience, opponentAttack, opponentDefense, opponentAttackSpeed })
                ZoneDefinition.ValidatePositiveFinite(value, nameof(opponentHpPerExperience));
            OpponentHpPerExperience = opponentHpPerExperience; OpponentAttack = opponentAttack;
            OpponentDefense = opponentDefense; OpponentAttackSpeed = opponentAttackSpeed;
            BalanceParameters = Array.AsReadOnly(new[] {
                new BalanceParameter("expedition.opponent_hp_per_exp", opponentHpPerExperience, "HP/EXP", "Prototype", "Prototype opponent scaling for the reference expedition resolver."),
                new BalanceParameter("expedition.opponent_attack", opponentAttack, "ATK", "Prototype", "Prototype opponent base Attack."),
                new BalanceParameter("expedition.opponent_defense", opponentDefense, "DEF", "Prototype", "Prototype opponent base Defense."),
                new BalanceParameter("expedition.opponent_attack_speed", opponentAttackSpeed, "ASPD", "Prototype", "Prototype opponent base Attack Speed.")
            });
        }
    }

    public interface IExpeditionResolver
    {
        ExpeditionResult Resolve(TrainerSnapshot trainer, ZoneDefinition zone, int minutes, SimRandom random);
    }

    /// <summary>Deterministic reference resolver; production encounter tables and reward rules remain data-driven.</summary>
    public sealed class DefaultExpeditionResolver : IExpeditionResolver
    {
        readonly SimConfig config;
        readonly EncounterGenerator generator = new EncounterGenerator();
        readonly LootResolver lootResolver = new LootResolver();
        public DefaultExpeditionResolver(SimConfig config) { this.config = config ?? throw new ArgumentNullException(nameof(config)); }

        public ExpeditionResult Resolve(TrainerSnapshot trainer, ZoneDefinition zone, int minutes, SimRandom random)
        {
            if (trainer == null) throw new ArgumentNullException(nameof(trainer));
            if (zone == null) throw new ArgumentNullException(nameof(zone));
            if (minutes <= 0) throw new ArgumentOutOfRangeException(nameof(minutes));
            if (random == null) throw new ArgumentNullException(nameof(random));
            var time = trainer.Time;
            double expected = zone.EncounterProfile.ExpectedEncountersPerHour * minutes / 60.0;
            if (double.IsNaN(expected) || double.IsInfinity(expected) || expected > int.MaxValue) throw new OverflowException("Encounter count exceeds supported limits.");
            int count = (int)Math.Floor(expected);
            if (random.NextDouble() < expected - count) count++;
            var battles = new List<BattleResult>();
            var currentTeam = trainer.Team.ToArray();
            MonsterId? activeId = trainer.ActiveMonsterId;
            var totals = new SortedDictionary<Materials.MaterialId, int>(Comparer<Materials.MaterialId>.Create((a, b) => string.CompareOrdinal(a.Value, b.Value)));
            long totalGold = 0, totalExperience = 0;
            for (int i = 0; i < count; i++)
            {
                var encounter = generator.Generate(zone, time, random);
                var combat = config.ExpeditionSettings ?? ExpeditionConfig.Prototype;
                long foeHp = Math.Max(1, checked((long)Math.Ceiling(encounter.ExperienceReward * combat.OpponentHpPerExperience)));
                var foe = new MonsterSnapshot(new MonsterId(zone.Id + ".wild." + i), encounter.Element,
                    new MonsterStats(foeHp, combat.OpponentAttack, combat.OpponentDefense, combat.OpponentAttackSpeed, 0), foeHp,
                    new[] { encounter.Element.ToString().ToLowerInvariant() + "_strike" });
                var battle = BattleResolver.Resolve(new BattleInput(currentTeam, new[] { foe }, activeId), CombatConfig.Prototype, random);
                battles.Add(battle);
                activeId = battle.ActiveId;
                currentTeam = battle.FinalMonsters.Where(x => x.Side == BattleSide.Team)
                    .Select(state => {
                        var old = currentTeam.First(x => x.Id == state.Id);
                        return new MonsterSnapshot(old.Id, old.Element, old.Stats, state.CurrentHp, old.SkillIds, state.Cooldowns);
                    }).ToArray();
                var lootSettings = config.LootSettings ?? LootConfig.Prototype;
                var reward = lootResolver.Resolve(battle, zone, trainer, time,
                    new LootConfig(lootSettings.LuckGoldPerPoint, lootSettings.MaterialPickupChance, int.MaxValue), random);
                totalGold = checked(totalGold + reward.Gold);
                totalExperience = checked(totalExperience + reward.TrainerExperience);
                foreach (var material in reward.Collected)
                    totals[material.MaterialId] = checked(totals.TryGetValue(material.MaterialId, out int held) ? held + material.Quantity : material.Quantity);
                if (!currentTeam.Any(x => x.CurrentHp > 0)) break;
            }
            var collected = totals.Select(x => new MaterialQuantity(x.Key, x.Value)).ToArray();
            var loot = new ExpeditionLoot(collected, Array.Empty<MaterialQuantity>(), totalGold, totalExperience);
            var finalHp = currentTeam.ToDictionary(x => x.Id, x => x.CurrentHp);
            return new ExpeditionResult(battles, loot, totalExperience, finalHp);
        }
    }
}
