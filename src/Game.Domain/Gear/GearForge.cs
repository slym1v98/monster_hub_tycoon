using System;

namespace Game.Domain.Gear
{
    public sealed class EnhanceResult
    {
        public bool Success { get; }
        public bool Broke { get; }
        public bool CharmConsumed { get; }
        public long GoldSpent { get; }
        public int StonesSpent { get; }
        internal EnhanceResult(bool success, bool broke, bool charm, long gold, int stones)
        { Success = success; Broke = broke; CharmConsumed = charm; GoldSpent = gold; StonesSpent = stones; }
    }

    public sealed class StarUpResult
    {
        public bool Success { get; }
        public GearItem ConsumedItem { get; }
        internal StarUpResult(bool success, GearItem consumed) { Success = success; ConsumedItem = consumed; }
    }

    public sealed class RefineResult
    {
        public bool Success { get; }
        public int CrystalConsumed { get; }
        public int WaterConsumed { get; }
        internal RefineResult(bool success, int crystal, int water) { Success = success; CrystalConsumed = crystal; WaterConsumed = water; }
    }

    public sealed class RepairResult
    {
        public bool Success { get; }
        public long GoldSpent { get; }
        public long GoldRemaining { get; }
        internal RepairResult(bool success, long spent, long remaining) { Success = success; GoldSpent = spent; GoldRemaining = remaining; }
    }

    /// <summary>
    /// Ba trục nâng cấp và sửa chữa theo GDD 05. Hàm thuần: chỉ đổi trạng thái món; việc trừ Gold/vật phẩm
    /// của chủ sở hữu do HubWorld thực hiện từ kết quả.
    /// </summary>
    public static class GearForge
    {
        public const int MaxEnhance = 20;

        /// <summary>Cường hóa một cấp. Tốn Gold (làm tròn lên) và 1 Đá. Thất bại từ cấp đích ≥ BreakFrom có thể làm vỡ đồ trừ khi có Bùa.</summary>
        public static EnhanceResult TryEnhance(GearItem item, EnhancementModel model, GearConfig config, SimRandom random, bool protectionCharm)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            if (model == null) throw new ArgumentNullException(nameof(model));
            if (config == null) throw new ArgumentNullException(nameof(config));
            if (random == null) throw new ArgumentNullException(nameof(random));
            if (item.IsDestroyed) throw new InvalidOperationException("Món đã vỡ.");
            if (item.EnhanceLevel >= MaxEnhance) throw new InvalidOperationException("Đã đạt +20.");
            int target = item.EnhanceLevel + 1;
            long gold = checked((long)Math.Ceiling(model.AttemptCost(target)));
            bool success = random.NextDouble() < model.Success(target);
            if (success) { item.EnhanceLevel = target; return new EnhanceResult(true, false, false, gold, 1); }
            if (target < model.BreakFrom) return new EnhanceResult(false, false, false, gold, 1);
            bool wouldBreak = random.NextDouble() < model.BreakChance;
            if (!wouldBreak) return new EnhanceResult(false, false, false, gold, 1);
            if (protectionCharm) return new EnhanceResult(false, false, true, gold, 1);
            item.IsDestroyed = true;
            return new EnhanceResult(false, true, false, gold, 1);
        }

        /// <summary>Nâng Sao bằng cách hiến tế đồ rác cùng slot (không phải chính nó). Luôn thành công khi hợp lệ.</summary>
        public static StarUpResult StarUp(GearItem item, GearItem junk)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            if (junk == null) throw new ArgumentNullException(nameof(junk));
            if (ReferenceEquals(item, junk) || item.IsDestroyed || junk.IsDestroyed || item.Stars >= 5 ||
                item.Slot.Id != junk.Slot.Id) return new StarUpResult(false, null);
            item.Stars++;
            junk.IsDestroyed = true;
            return new StarUpResult(true, junk);
        }

        /// <summary>Tinh Luyện một bậc; cần đủ Tinh Thể Boss Thế Giới và Nước Cất, nếu không thì không đổi gì.</summary>
        public static RefineResult Refine(GearItem item, int worldBossCrystal, int distilledWater, GearConfig config)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            if (config == null) throw new ArgumentNullException(nameof(config));
            if (item.IsDestroyed || item.Refine == GearRefineGrade.Mythic ||
                worldBossCrystal < config.RefineCrystalCost || distilledWater < config.RefineWaterCost)
                return new RefineResult(false, 0, 0);
            item.Refine = (GearRefineGrade)((int)item.Refine + 1);
            return new RefineResult(true, config.RefineCrystalCost, config.RefineWaterCost);
        }

        /// <summary>Sửa về độ bền tối đa; phí theo điểm thiếu. Hào quang không hao nên phí 0.</summary>
        public static RepairResult Repair(GearItem item, long gold, GearConfig config)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            if (config == null) throw new ArgumentNullException(nameof(config));
            if (gold < 0) throw new ArgumentOutOfRangeException(nameof(gold));
            if (item.IsDestroyed) return new RepairResult(false, 0, gold);
            long missing = item.MaxDurability - item.Durability;
            long cost = item.Slot.Group == GearGroup.Aura ? 0 : checked((long)Math.Ceiling(missing * config.RepairGoldPerDurability));
            if (cost > gold) return new RepairResult(false, 0, gold);
            item.Durability = item.MaxDurability;
            return new RepairResult(true, cost, gold - cost);
        }
    }
}
