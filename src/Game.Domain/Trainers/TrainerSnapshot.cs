using System;
using System.Collections.Generic;
using System.Linq;
using Game.Domain.Monsters;
using Game.Domain.Gear;
using Game.Domain.Materials;

namespace Game.Domain
{
    public sealed class TrainerSnapshot
    {
        public int Id { get; }
        public int Rank { get; }
        public int Level { get; }
        public Rarity Rarity { get; }
        public Personality Personality { get; }
        public TrainerAttributes Attributes { get; }
        public bool HasNightVision { get; }
        public IReadOnlyList<MonsterSnapshot> Team { get; }
        public MonsterId? ActiveMonsterId { get; }
        public SimTime Time { get; }
        public long Gold { get; }
        public IReadOnlyDictionary<ProductId, int> Products { get; }
        public double LeadershipBonus { get; }
        public bool BagSynergyEnabled { get; }

        public TrainerSnapshot(int id, int rank, int level, Rarity rarity, Personality personality, TrainerAttributes attributes, bool hasNightVision = false,
            IEnumerable<MonsterSnapshot> team = null, MonsterId? activeMonsterId = null, SimTime? time = null, long gold = 0,
            IEnumerable<KeyValuePair<ProductId, int>> products = null, double leadershipBonus = 0, bool bagSynergyEnabled = false,
            GearLoadout gear = null, GearCatalog catalog = null)
        {
            if (id < 0) throw new ArgumentOutOfRangeException(nameof(id));
            if (rank < 1 || rank > 5) throw new ArgumentOutOfRangeException(nameof(rank));
            if (level < 1 || level > 100) throw new ArgumentOutOfRangeException(nameof(level));
            if (!Enum.IsDefined(typeof(Rarity), rarity)) throw new ArgumentOutOfRangeException(nameof(rarity));
            if (!Enum.IsDefined(typeof(Personality), personality)) throw new ArgumentOutOfRangeException(nameof(personality));
            Attributes = attributes ?? throw new ArgumentNullException(nameof(attributes));
            Id = id; Rank = rank; Level = level; Rarity = rarity; Personality = personality;
            HasNightVision = hasNightVision;
            var teamCopy = (team ?? Array.Empty<MonsterSnapshot>()).ToArray();
            if (teamCopy.Length > MonsterRoster.Capacity || teamCopy.Any(x => x == null) || teamCopy.Select(x => x.Id).Distinct().Count() != teamCopy.Length)
                throw new ArgumentException("Trainer snapshot team must be valid and contain at most three unique Monsters.", nameof(team));
            if (activeMonsterId.HasValue && !teamCopy.Any(x => x.Id == activeMonsterId.Value)) throw new ArgumentException("Active Monster must belong to the team.", nameof(activeMonsterId));
            Team = Array.AsReadOnly(teamCopy.OrderBy(x => x.Id.Value, StringComparer.Ordinal).ToArray());
            ActiveMonsterId = activeMonsterId ?? Team.FirstOrDefault()?.Id;
            Time = time ?? new SimTime(0);
            if (gold < 0) throw new ArgumentOutOfRangeException(nameof(gold));
            Gold = gold;
            if (double.IsNaN(leadershipBonus) || double.IsInfinity(leadershipBonus) || leadershipBonus < 0) throw new ArgumentOutOfRangeException(nameof(leadershipBonus));
            LeadershipBonus = leadershipBonus; BagSynergyEnabled = bagSynergyEnabled;
            if (gear != null && gear.Equipped.Count > 0)
            {
                catalog = catalog ?? GearCatalog.Default;
                var gstats = GearLoadout.TotalStats(gear.Equipped, catalog).Add(GearLoadout.SetBonus(gear.Equipped, catalog));
                HasNightVision = gstats.NightVision;
                // BackpackCapacity & hydration handled by HubWorld at runtime
            }
            var productCopy = new Dictionary<ProductId, int>();
            foreach (var product in products ?? Array.Empty<KeyValuePair<ProductId, int>>())
            {
                if (string.IsNullOrWhiteSpace(product.Key.Value) || product.Value <= 0 || productCopy.ContainsKey(product.Key))
                    throw new ArgumentException("Trainer snapshot product stacks must have valid positive counts and unique IDs.", nameof(products));
                productCopy.Add(product.Key, product.Value);
            }
            Products = new System.Collections.ObjectModel.ReadOnlyDictionary<ProductId, int>(productCopy);
        }
        public static TrainerSnapshot FromTrainer(Trainer trainer, int totalMinutes = 0, GearCatalog catalog = null)
        {
            if (trainer == null) throw new ArgumentNullException(nameof(trainer));
            var a = trainer.Attributes;
            var team = trainer.Roster.Members.Select(m => MonsterSnapshot.FromMonster(m,
                m.CombatSkillIds, null, totalMinutes, m.Gear, catalog));
            return new TrainerSnapshot(trainer.Id, trainer.Rank, trainer.Level, trainer.Rarity, trainer.Personality,
                new TrainerAttributes(a.Dexterity, a.Luck, a.Endurance, a.Leadership), trainer.HasNightVision, team, trainer.Roster.Active?.Id,
                new SimTime(totalMinutes), trainer.Gold, trainer.Inventory.Products, trainer.LeadershipItemBonus,
                trainer.BagSynergyExpiresAtMinute > totalMinutes, trainer.Gear, catalog);
        }
    }
}
