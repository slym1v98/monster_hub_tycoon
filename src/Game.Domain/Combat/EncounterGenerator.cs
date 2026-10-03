using System;
using System.Linq;

namespace Game.Domain.Combat
{
    public sealed class EncounterGenerator
    {
        public EncounterDefinition Generate(ZoneDefinition zone, SimTime time, SimRandom rng)
        {
            if (zone == null) throw new ArgumentNullException(nameof(zone));
            if (rng == null) throw new ArgumentNullException(nameof(rng));
            var entries = zone.EncounterProfile.ElementWeights.OrderBy(x => x.Key).ToArray();
            double sum = entries.Sum(x => x.Value);
            double draw = rng.NextDouble() * sum;
            var selected = entries[entries.Length - 1].Key;
            foreach (var entry in entries) { draw -= entry.Value; if (draw < 0) { selected = entry.Key; break; } }
            var profile = zone.EncounterProfile;
            return new EncounterDefinition(zone.Id + ".encounter", selected, profile.GoldPerEncounter,
                profile.ExperiencePerEncounter, profile.ExpectedMaterialUnitsPerEncounter);
        }
    }
}
