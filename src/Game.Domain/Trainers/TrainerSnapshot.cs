using System;

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

        public TrainerSnapshot(int id, int rank, int level, Rarity rarity, Personality personality, TrainerAttributes attributes)
        {
            if (id < 0) throw new ArgumentOutOfRangeException(nameof(id));
            if (rank < 1 || rank > 5) throw new ArgumentOutOfRangeException(nameof(rank));
            if (level < 1 || level > 100) throw new ArgumentOutOfRangeException(nameof(level));
            if (!Enum.IsDefined(typeof(Rarity), rarity)) throw new ArgumentOutOfRangeException(nameof(rarity));
            if (!Enum.IsDefined(typeof(Personality), personality)) throw new ArgumentOutOfRangeException(nameof(personality));
            Attributes = attributes ?? throw new ArgumentNullException(nameof(attributes));
            Id = id; Rank = rank; Level = level; Rarity = rarity; Personality = personality;
        }
        public static TrainerSnapshot FromTrainer(Trainer trainer)
        {
            if (trainer == null) throw new ArgumentNullException(nameof(trainer));
            var a = trainer.Attributes;
            return new TrainerSnapshot(trainer.Id, trainer.Rank, trainer.Level, trainer.Rarity, trainer.Personality,
                new TrainerAttributes(a.Dexterity, a.Luck, a.Endurance, a.Leadership));
        }
    }
}
