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
        public EncounterDefinition(string id, MonsterElement element, double goldReward, double experienceReward, double materialUnits)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Encounter ID is required.", nameof(id));
            if (!Enum.IsDefined(typeof(MonsterElement), element)) throw new ArgumentOutOfRangeException(nameof(element));
            ZoneDefinition.ValidateNonNegativeFinite(goldReward, nameof(goldReward));
            ZoneDefinition.ValidateNonNegativeFinite(experienceReward, nameof(experienceReward));
            ZoneDefinition.ValidateNonNegativeFinite(materialUnits, nameof(materialUnits));
            Id = id; Element = element; GoldReward = goldReward; ExperienceReward = experienceReward; MaterialUnits = materialUnits;
        }
    }
}
