using System;
using System.Collections.Generic;
using System.Linq;
using Game.Domain.Materials;
using Game.Domain.Monsters;

namespace Game.Domain
{
    public sealed class ZoneCatalog
    {
        public IReadOnlyList<ZoneDefinition> Definitions { get; }
        public static ZoneCatalog Default { get; } = CreateDefault();
        public ZoneCatalog(IEnumerable<ZoneDefinition> definitions)
        {
            if (definitions == null) throw new ArgumentNullException(nameof(definitions));
            var copy = definitions.ToArray();
            if (copy.Length == 0 || copy.Any(x => x == null)) throw new ArgumentException("Zone catalog must be non-empty and contain no null entries.", nameof(definitions));
            if (copy.Select(x => x.Id).Distinct(StringComparer.Ordinal).Count() != copy.Length) throw new ArgumentException("Zone IDs must be unique.", nameof(definitions));
            Definitions = Array.AsReadOnly(copy.OrderBy(x => x.Id, StringComparer.Ordinal).ToArray());
        }

        static ZoneCatalog CreateDefault()
        {
            var names = new[] { "Đồng Cỏ", "Núi Lửa", "Hầm Băng", "Đầm Lầy", "Vực Thẳm" };
            var elements = new[] {
                new[] { MonsterElement.Grass, MonsterElement.Water, MonsterElement.Ground },
                new[] { MonsterElement.Fire, MonsterElement.Ground },
                new[] { MonsterElement.Ice, MonsterElement.Water },
                new[] { MonsterElement.Poison, MonsterElement.Water, MonsterElement.Grass },
                new[] { MonsterElement.Dark }
            };
            var result = new List<ZoneDefinition>();
            for (int tier = 1; tier <= 5; tier++)
            {
                var mats = new List<ZoneMaterialWeight>();
                foreach (MaterialFamily family in Enum.GetValues(typeof(MaterialFamily)))
                    for (int t = 1; t <= tier; t++) mats.Add(new ZoneMaterialWeight(MaterialId.For(family, t), t == tier ? 10 : 1, 10 * t));
                var em = elements[tier - 1].ToDictionary(x => x, _ => 1d);
                var encounter = new EncounterProfile(em, 2.0 / tier, 10 * tier, 2, 20 * tier);
                result.Add(new ZoneDefinition("zone_" + tier, names[tier - 1], tier, tier * 30, mats, encounter));
            }
            return new ZoneCatalog(result);
        }
    }
}
