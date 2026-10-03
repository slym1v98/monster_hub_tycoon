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
        public int StarsLost { get; }
        public long GoldSpent { get; }
        internal StarUpResult(bool success, GearItem consumed, int starsLost = 0, long goldSpent = 0)
        { Success = success; ConsumedItem = consumed; StarsLost = starsLost; GoldSpent = goldSpent; }
    }

    public sealed class RefineResult
    {
        public bool Success { get; }
        public int CrystalConsumed { get; }
        public int WaterConsumed { get; }
        public long GoldSpent { get; }
        internal RefineResult(bool success, int crystal, int water, long goldSpent = 0)
        { Success = success; CrystalConsumed = crystal; WaterConsumed = water; GoldSpent = goldSpent; }
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
            if (success) { item.EnhanceLevel = target; return new EnhanceResult(true, false, false, gold, config.EnhanceStoneCost); }
            if (target < model.BreakFrom) return new EnhanceResult(false, false, false, gold, config.EnhanceStoneCost);
            bool wouldBreak = random.NextDouble() < model.BreakChance;
            if (!wouldBreak) return new EnhanceResult(false, false, false, gold, config.EnhanceStoneCost);
            if (protectionCharm) return new EnhanceResult(false, false, true, gold, config.EnhanceStoneCost);
            item.IsDestroyed = true;
            return new EnhanceResult(false, true, false, gold, config.EnhanceStoneCost);
        }

        /// <summary>Nâng Sao theo xác suất GDD 13; vật hiến tế và Gold bị tiêu thụ mỗi lần thử hợp lệ.</summary>
        public static StarUpResult StarUp(GearItem item, GearItem junk, GearConfig config, SimRandom random)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            if (junk == null) throw new ArgumentNullException(nameof(junk));
            if (config == null) throw new ArgumentNullException(nameof(config));
            if (random == null) throw new ArgumentNullException(nameof(random));
            if (ReferenceEquals(item, junk) || item.IsDestroyed || junk.IsDestroyed || item.Stars >= 5 ||
                item.Slot.Id != junk.Slot.Id) return new StarUpResult(false, null);
            int target = item.Stars + 1;
            long cost = config.StarAttemptCost(target);
            if (random.NextDouble() < config.StarSuccess(target))
            {
                item.Stars = target; junk.IsDestroyed = true;
                return new StarUpResult(true, junk, 0, cost);
            }
            int lost = target >= config.StarFailureDropFromTarget ? Math.Min(1, item.Stars - 1) : 0;
            item.Stars -= lost; junk.IsDestroyed = true;
            return new StarUpResult(false, junk, lost, cost);
        }

        /// <summary>Tinh Luyện một bậc; cần đủ Tinh Thể Boss Thế Giới và Nước Cất, nếu không thì không đổi gì.</summary>
        public static RefineResult Refine(GearItem item, int worldBossCrystal, int distilledWater, long gold, GearConfig config, SimRandom random)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            if (config == null) throw new ArgumentNullException(nameof(config));
            if (random == null) throw new ArgumentNullException(nameof(random));
            if (item.IsDestroyed || item.Refine == GearRefineGrade.Mythic ||
                worldBossCrystal < config.RefineCrystalCost || distilledWater < config.RefineWaterCost || gold < config.RefineAttemptCost((int)item.Refine + 1))
                return new RefineResult(false, 0, 0);
            long cost = config.RefineAttemptCost((int)item.Refine + 1);
            bool success = random.NextDouble() < config.RefineSuccess((int)item.Refine + 1);
            if (success) item.Refine = (GearRefineGrade)((int)item.Refine + 1);
            return new RefineResult(success, config.RefineCrystalCost, config.RefineWaterCost, cost);
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
