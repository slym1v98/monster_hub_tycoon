using System;
using Game.Domain.Monsters;

namespace Game.Domain.Combat
{
    public sealed class EncounterDefinition
    {
        public string Id { get; }
        public MonsterElement Element { get; }
        public double GoldReward { get; }
        public double ExperienceReward { get; }
        public double MaterialUnits { get; }
        public string SpeciesId { get; }
        public Rarity Rarity { get; }
        public int Level { get; }
        public EncounterDefinition(string id, MonsterElement element, double goldReward, double experienceReward, double materialUnits,
            string speciesId = null, Rarity rarity = Rarity.Common, int level = 1)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Encounter ID is required.", nameof(id));
            if (!Enum.IsDefined(typeof(MonsterElement), element)) throw new ArgumentOutOfRangeException(nameof(element));
            ZoneDefinition.ValidateNonNegativeFinite(goldReward, nameof(goldReward));
            ZoneDefinition.ValidateNonNegativeFinite(experienceReward, nameof(experienceReward));
            ZoneDefinition.ValidateNonNegativeFinite(materialUnits, nameof(materialUnits));
            if (!Enum.IsDefined(typeof(Rarity), rarity)) throw new ArgumentOutOfRangeException(nameof(rarity));
            if (level < 1 || level > 100) throw new ArgumentOutOfRangeException(nameof(level));
            if (speciesId != null && string.IsNullOrWhiteSpace(speciesId)) throw new ArgumentException("Species ID must be nonempty when supplied.", nameof(speciesId));
            Id = id; Element = element; GoldReward = goldReward; ExperienceReward = experienceReward; MaterialUnits = materialUnits;
            SpeciesId = speciesId ?? id; Rarity = rarity; Level = level;
        }
    }
}
