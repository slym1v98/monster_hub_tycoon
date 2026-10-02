using System;
using Game.Domain.Monsters;

namespace Game.Domain.Combat
{
    public sealed class CaptureInputs
    {
        public string SpeciesId { get; }
        public Rarity TargetRarity { get; }
        public int TargetLevel { get; }
        public int AvailableBalls { get; }
        public int AvailableTraps { get; }
        public bool ReplacementEligible { get; }
        public double Dexterity { get; }
        public TrainerClass TrainerClass { get; }
        public int BallTier { get; }
        public int TrapTier { get; }

        public CaptureInputs(string speciesId, Rarity targetRarity, int targetLevel, int availableBalls,
            int availableTraps, bool replacementEligible, double dexterity, TrainerClass trainerClass,
            int ballTier = 0, int trapTier = 0)
        {
            if (string.IsNullOrWhiteSpace(speciesId)) throw new ArgumentException("Species ID is required.", nameof(speciesId));
            if (!Enum.IsDefined(typeof(Rarity), targetRarity)) throw new ArgumentOutOfRangeException(nameof(targetRarity));
            if (targetLevel < 1 || targetLevel > 100) throw new ArgumentOutOfRangeException(nameof(targetLevel));
            if (availableBalls < 0 || availableTraps < 0) throw new ArgumentOutOfRangeException(nameof(availableBalls));
            if (double.IsNaN(dexterity) || double.IsInfinity(dexterity) || dexterity < 0) throw new ArgumentOutOfRangeException(nameof(dexterity));
            if (!Enum.IsDefined(typeof(TrainerClass), trainerClass)) throw new ArgumentOutOfRangeException(nameof(trainerClass));
            if (ballTier < 0 || trapTier < 0) throw new ArgumentOutOfRangeException(nameof(ballTier));
            SpeciesId = speciesId; TargetRarity = targetRarity; TargetLevel = targetLevel;
            AvailableBalls = availableBalls; AvailableTraps = availableTraps;
            ReplacementEligible = replacementEligible; Dexterity = dexterity; TrainerClass = trainerClass;
            BallTier = ballTier; TrapTier = trapTier;
        }
    }

    public sealed class CapturedMonsterGeneration
    {
        public string SpeciesId { get; }
        public MonsterElement Element { get; }
        public Rarity Rarity { get; }
        public int Level { get; }
        public MonsterIvGrade Iv { get; }
        public int Seed { get; }
        public CapturedMonsterGeneration(string speciesId, MonsterElement element, Rarity rarity, int level, MonsterIvGrade iv, int seed)
        { SpeciesId = speciesId; Element = element; Rarity = rarity; Level = level; Iv = iv; Seed = seed; }
    }

    public sealed class CaptureResult
    {
        public bool Attempted { get; }
        public bool Success { get; }
        public string SpeciesId { get; }
        public Rarity TargetRarity { get; }
        public int TargetLevel { get; }
        public int AvailableBalls { get; }
        public int AvailableTraps { get; }
        public bool ReplacementEligible { get; }
        public double Chance { get; }
        public double? Roll { get; }
        public double TargetHpFraction { get; }
        public int BallTier { get; }
        public int TrapTier { get; }
        public double Dexterity { get; }
        public TrainerClass TrainerClass { get; }
        public int ConsumedBallCount { get; }
        public int ConsumedTrapCount { get; }
        public CapturedMonsterGeneration Generation { get; }

        internal CaptureResult(bool attempted, bool success, double chance, double? roll, double targetHpFraction,
            CaptureInputs inputs, int consumedBallCount, int consumedTrapCount, CapturedMonsterGeneration generation)
        {
            Attempted = attempted; Success = success; Chance = chance; Roll = roll; TargetHpFraction = targetHpFraction;
            SpeciesId = inputs.SpeciesId; TargetRarity = inputs.TargetRarity; TargetLevel = inputs.TargetLevel;
            AvailableBalls = inputs.AvailableBalls; AvailableTraps = inputs.AvailableTraps;
            ReplacementEligible = inputs.ReplacementEligible;
            BallTier = inputs.BallTier; TrapTier = inputs.TrapTier; Dexterity = inputs.Dexterity; TrainerClass = inputs.TrainerClass;
            ConsumedBallCount = consumedBallCount; ConsumedTrapCount = consumedTrapCount; Generation = generation;
        }
    }
}
