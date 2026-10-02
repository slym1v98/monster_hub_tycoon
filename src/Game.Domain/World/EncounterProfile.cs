using System;
using System.Collections.Generic;
using System.Linq;
using Game.Domain.Monsters;

namespace Game.Domain
{
    public sealed class EncounterProfile
    {
        public IReadOnlyDictionary<MonsterElement, double> ElementWeights { get; }
        public double ExpectedEncountersPerHour { get; }
        public double GoldPerEncounter { get; }
        public double ExpectedMaterialUnitsPerEncounter { get; }
        public double ExperiencePerEncounter { get; }

        public EncounterProfile(IDictionary<MonsterElement, double> elementWeights, double expectedEncountersPerHour,
            double goldPerEncounter, double expectedMaterialUnitsPerEncounter, double experiencePerEncounter)
        {
            if (elementWeights == null) throw new ArgumentNullException(nameof(elementWeights));
            if (elementWeights.Count == 0) throw new ArgumentException("Cần ít nhất một hệ quái.", nameof(elementWeights));
            foreach (var pair in elementWeights)
            {
                if (!Enum.IsDefined(typeof(MonsterElement), pair.Key)) throw new ArgumentOutOfRangeException(nameof(elementWeights));
                ZoneDefinition.ValidatePositiveFinite(pair.Value, nameof(elementWeights));
            }
            ZoneDefinition.ValidateNonNegativeFinite(expectedEncountersPerHour, nameof(expectedEncountersPerHour));
            ZoneDefinition.ValidateNonNegativeFinite(goldPerEncounter, nameof(goldPerEncounter));
            ZoneDefinition.ValidateNonNegativeFinite(expectedMaterialUnitsPerEncounter, nameof(expectedMaterialUnitsPerEncounter));
            ZoneDefinition.ValidateNonNegativeFinite(experiencePerEncounter, nameof(experiencePerEncounter));
            ElementWeights = new System.Collections.ObjectModel.ReadOnlyDictionary<MonsterElement, double>(new Dictionary<MonsterElement, double>(elementWeights));
            ExpectedEncountersPerHour = expectedEncountersPerHour;
            GoldPerEncounter = goldPerEncounter;
            ExpectedMaterialUnitsPerEncounter = expectedMaterialUnitsPerEncounter;
            ExperiencePerEncounter = experiencePerEncounter;
        }
    }
}
