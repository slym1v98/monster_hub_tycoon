using System;
using System.Collections.Generic;
using System.Linq;
using Game.Domain.Combat;
using Game.Domain.Materials;

namespace Game.Domain
{
    public sealed class MaterialQuantity
    {
        public MaterialId MaterialId { get; }
        public int Quantity { get; }
        public MaterialQuantity(MaterialId materialId, int quantity)
        { if (materialId.Value == null) throw new ArgumentException("Material ID is required.", nameof(materialId)); if (quantity < 0) throw new ArgumentOutOfRangeException(nameof(quantity)); MaterialId = materialId; Quantity = quantity; }
    }
    public sealed class ExpeditionLoot
    {
        public IReadOnlyList<MaterialQuantity> Collected { get; }
        public IReadOnlyList<MaterialQuantity> Dropped { get; }
        public long Gold { get; }
        public long TrainerExperience { get; }
        public ExpeditionLoot(IEnumerable<MaterialQuantity> collected, IEnumerable<MaterialQuantity> dropped, long gold, long trainerExperience)
        {
            if (collected == null) throw new ArgumentNullException(nameof(collected)); if (dropped == null) throw new ArgumentNullException(nameof(dropped));
            if (gold < 0 || trainerExperience < 0) throw new ArgumentOutOfRangeException();
            Collected = Array.AsReadOnly(collected.ToArray()); Dropped = Array.AsReadOnly(dropped.ToArray()); Gold = gold; TrainerExperience = trainerExperience;
        }
    }
    public sealed class ExpeditionResult
    {
        public IReadOnlyList<BattleResult> Battles { get; }
        public ExpeditionLoot Loot { get; }
        public ExpeditionResult(IEnumerable<BattleResult> battles, ExpeditionLoot loot)
        { if (battles == null) throw new ArgumentNullException(nameof(battles)); Battles = Array.AsReadOnly(battles.ToArray()); Loot = loot ?? throw new ArgumentNullException(nameof(loot)); }
    }
}
