using System;
using System.Collections.Generic;
using System.Linq;
using Game.Domain.Monsters;

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

        public TrainerSnapshot(int id, int rank, int level, Rarity rarity, Personality personality, TrainerAttributes attributes, bool hasNightVision = false,
            IEnumerable<MonsterSnapshot> team = null, MonsterId? activeMonsterId = null, SimTime? time = null)
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
        }
        public static TrainerSnapshot FromTrainer(Trainer trainer, int totalMinutes = 0)
        {
            if (trainer == null) throw new ArgumentNullException(nameof(trainer));
            var a = trainer.Attributes;
            var team = trainer.Roster.Members.Select(m => MonsterSnapshot.FromMonster(m, new[] { m.Element.ToString().ToLowerInvariant() + "_strike" }));
            return new TrainerSnapshot(trainer.Id, trainer.Rank, trainer.Level, trainer.Rarity, trainer.Personality,
                new TrainerAttributes(a.Dexterity, a.Luck, a.Endurance, a.Leadership), trainer.HasNightVision, team, trainer.Roster.Active?.Id, new SimTime(totalMinutes));
        }
    }
}
