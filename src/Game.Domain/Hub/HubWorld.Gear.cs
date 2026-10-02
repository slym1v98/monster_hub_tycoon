using System;
using System.Collections.Generic;
using System.Linq;
using Game.Domain.Gear;
using Game.Domain.Materials;

namespace Game.Domain
{
    /// <summary>
    /// Lệnh Giám đốc về trang bị: chào hàng, Cường hóa, Nâng Sao, Tinh Luyện, sửa chữa và mua lại.
    /// Mọi thay đổi Gold/vật phẩm đi qua kết quả tường minh và đều phát sự kiện; không có fallback ẩn.
    /// </summary>
    public sealed partial class HubWorld
    {
        internal GearCatalog GearCat => Gear.GearCatalog.Default;
        internal GearConfig GearSettings => Gear.GearCatalog.Default.Config;

        /// <summary>Ảnh chụp chỉ đọc trạng thái trang bị của một Trainer và các Monster của họ.</summary>
        public IReadOnlyList<GearView> GearForTrainer(int trainerId)
        {
            if (trainerId < 0 || trainerId >= trainers.Count) return Array.Empty<GearView>();
            var trainer = trainers[trainerId];
            var views = trainer.Gear.Equipped.Select(GearView.From).ToList();
            foreach (var monster in trainer.Roster.Members)
                views.AddRange(monster.Gear.Equipped.Select(GearView.From));
            return Array.AsReadOnly(views.OrderBy(x => x.OwnerId, StringComparer.Ordinal).ThenBy(x => x.SlotId, StringComparer.Ordinal).ToArray());
        }

        /// <summary>Slot mà Giám đốc chào trang bị: Monster Combat về Active Monster, còn lại về Trainer.</summary>
        GearLoadout LoadoutForSlot(int trainerId, string slotId)
        {
            var trainer = trainers[trainerId];
            var slot = GearCat.GetSlot(slotId);
            return slot.Group == GearGroup.MonsterCombat ? trainer.Roster.Active?.Gear : trainer.Gear;
        }

        /// <summary>Chào bán một món trang bị; Trainer tự quyết theo Gear Score, giá và số Gold còn lại.</summary>
        public CommandResult OfferGear(int trainerId, GearItem offered, long price)
        {
            if (trainerId < 0 || trainerId >= trainers.Count) return CommandResult.Rejected("Trainer không tồn tại.");
            if (offered == null || offered.IsDestroyed) return CommandResult.Rejected("Không có món trang bị hợp lệ.");
            if (price <= 0) return CommandResult.Rejected("Giá chào phải lớn hơn 0.");
            var trainer = trainers[trainerId];
            GearLoadout loadout = LoadoutForSlot(trainerId, offered.Slot.Id);
            if (loadout == null) return CommandResult.Rejected("Trainer không có Active Monster cho slot này.");
            GearItem current = loadout.Get(offered.Slot.Id);
            double currentScore = current == null ? 0 : GearScore.Score(current, GearCat);
            double offeredScore = GearScore.Score(offered, GearCat);
            bool accepted = GearMarket.WouldAccept(current, offered, trainer.Gold, price, GearSettings);
            Raise(new GearOffered(now, trainerId, offered.Slot.Id, current?.Id, offered.Id, price, currentScore, offeredScore, accepted));
            if (!accepted) return CommandResult.Rejected("Trainer từ chối chào hàng trang bị.");
            trainer.Gold = checked(trainer.Gold - price);
            AddTreasury(price, "GearOffer");
            loadout.Unequip(offered.Slot.Id);   // GDD 05: đồ cũ biến mất trừ khi HUB mua lại
            loadout.Equip(offered);
            return CommandResult.Success();
        }

        /// <summary>Cường hóa +1: đốt Gold và 1 Đá Cường hóa; từ +11 có thể vỡ trừ khi dùng Bùa Bảo Hộ.</summary>
        public CommandResult EnhanceGear(int trainerId, GearItem item, bool useProtectionCharm)
        {
            if (!ValidGearInput(trainerId, item)) return CommandResult.Rejected("Thiếu tham số trang bị hợp lệ.");
            if (useProtectionCharm && trainers[trainerId].Inventory.Count(new ProductId("protection_charm")) <= 0)
                return CommandResult.Rejected("Không có Bùa Bảo Hộ.");
            var trainer = trainers[trainerId];
            if (trainer.Inventory.Count(new ProductId("enhancement_stone")) < 1) return CommandResult.Rejected("Không có Đá Cường hóa.");
            int oldLevel = item.EnhanceLevel;
            if (item.EnhanceLevel >= GearForge.MaxEnhance) return CommandResult.Rejected("Đã đạt +20.");
            EnhanceResult result = GearForge.TryEnhance(item, new EnhancementModel(), GearSettings, rng, useProtectionCharm);
            if (result.GoldSpent > trainer.Gold) return CommandResult.Rejected("Không đủ Gold.");
            trainer.Gold = checked(trainer.Gold - result.GoldSpent);
            trainer.Inventory.TryConsume(new ProductId("enhancement_stone"), 1);
            AddTreasury(result.GoldSpent, "GearEnhance");
            Raise(new TrainerProductChanged(now, trainerId, "enhancement_stone", -1,
                trainer.Inventory.Count(new ProductId("enhancement_stone"))));
            Raise(new GearEnhanced(now, trainerId, item.Id, oldLevel, item.EnhanceLevel, result.Success,
                result.Broke, result.CharmConsumed, result.GoldSpent, result.StonesSpent));
            return CommandResult.Success();
        }

        /// <summary>Nâng Sao: hiến tế một món rác cùng slot để tăng một Sao.</summary>
        public CommandResult StarUpGear(int trainerId, GearItem item, GearItem junk)
        {
            if (!ValidGearInput(trainerId, item) || junk == null || ReferenceEquals(item, junk))
                return CommandResult.Rejected("Thiếu tham số trang bị hợp lệ.");
            var equipped = trainers[trainerId].Gear.Equipped.Concat(trainers[trainerId].Roster.Members.SelectMany(m => m.Gear.Equipped)).ToList();
            if (equipped.Any(x => ReferenceEquals(x, junk))) return CommandResult.Rejected("Món hiến tế đang được trang bị.");
            int oldStars = item.Stars;
            StarUpResult result = GearForge.StarUp(item, junk);
            if (!result.Success) return CommandResult.Rejected("Không thể Nâng Sao với món này.");
            Raise(new GearStarUp(now, trainerId, item.Id, oldStars, item.Stars, result.ConsumedItem.Id, true));
            return CommandResult.Success();
        }

        /// <summary>Tinh Luyện: tốn Tinh Thể Boss Thế Giới và Nước Cất trong inventory của Trainer.</summary>
        public CommandResult RefineGear(int trainerId, GearItem item)
        {
            if (!ValidGearInput(trainerId, item)) return CommandResult.Rejected("Thiếu tham số trang bị hợp lệ.");
            var trainer = trainers[trainerId];
            var crystal = new ProductId("world_boss_crystal");
            var water = new ProductId("distilled_water");
            if (trainer.Inventory.Count(crystal) < GearSettings.RefineCrystalCost ||
                trainer.Inventory.Count(water) < GearSettings.RefineWaterCost)
                return CommandResult.Rejected("Không đủ Tinh Thể Boss Thế Giới hoặc Nước Cất.");
            var oldGrade = item.Refine;
            RefineResult result = GearForge.Refine(item, trainer.Inventory.Count(crystal), trainer.Inventory.Count(water), GearSettings);
            if (!result.Success) return CommandResult.Rejected("Không thể Tinh Luyện món này.");
            trainer.Inventory.TryConsume(crystal, result.CrystalConsumed);
            trainer.Inventory.TryConsume(water, result.WaterConsumed);
            Raise(new TrainerProductChanged(now, trainerId, crystal.Value, -result.CrystalConsumed, trainer.Inventory.Count(crystal)));
            Raise(new TrainerProductChanged(now, trainerId, water.Value, -result.WaterConsumed, trainer.Inventory.Count(water)));
            Raise(new GearRefined(now, trainerId, item.Id, oldGrade, item.Refine, result.CrystalConsumed, result.WaterConsumed, true));
            return CommandResult.Success();
        }

        /// <summary>Sửa đồ tại Lò Rèn/Xưởng Dệt: trả Gold theo độ bền thiếu.</summary>
        public CommandResult RepairGear(int trainerId, GearItem item)
        {
            if (!ValidGearInput(trainerId, item)) return CommandResult.Rejected("Thiếu tham số trang bị hợp lệ.");
            var trainer = trainers[trainerId];
            int oldDurability = item.Durability;
            RepairResult result = GearForge.Repair(item, trainer.Gold, GearSettings);
            if (!result.Success) return CommandResult.Rejected("Không đủ Gold để sửa.");
            trainer.Gold = result.GoldRemaining;
            AddTreasury(result.GoldSpent, "GearRepair");
            Raise(new GearRepaired(now, trainerId, item.Id, oldDurability, item.Durability, result.GoldSpent, true));
            return CommandResult.Success();
        }

        /// <summary>HUB mua lại đồ cũ khi Giám đốc yêu cầu, đồ nằm ở slot đã thay.</summary>
        public CommandResult BuybackGear(int trainerId, GearItem item, long salePrice)
        {
            if (!ValidGearInput(trainerId, item)) return CommandResult.Rejected("Thiếu tham số trang bị hợp lệ.");
            long buyback = GearMarket.BuybackPrice(item, salePrice, GearSettings);
            if (buyback <= 0) return CommandResult.Rejected("Giá mua lại không hợp lệ.");
            if (treasury.Balance < buyback) return CommandResult.Rejected("Kho bạc không đủ Gold mua lại.");
            treasury.TrySpend(buyback);
            trainers[trainerId].Gold = checked(trainers[trainerId].Gold + buyback);
            Raise(new TreasuryChanged(now, -buyback, treasury.Balance, "GearBuyback"));
            Raise(new GearBuyback(now, trainerId, item.Id, salePrice, buyback, true));
            return CommandResult.Success();
        }

        internal void GrantProductForTest(int trainerId, string productId, int units)
            => trainers[trainerId].Inventory.Add(new ProductId(productId), units);

        bool ValidGearInput(int trainerId, GearItem item)
            => trainerId >= 0 && trainerId < trainers.Count && item != null && !item.IsDestroyed;

        /// <summary>
        /// Hao mòn sau mỗi khúc farm: Monster trong đội mổ trang bị theo số action/hit,
        /// Trainer mở trang bị Tiện ích theo thời gian. Hào quang không hao.
        /// </summary>
        internal void ApplyGearWear(Trainer t, ExpeditionResult result, int minutes)
        {
            if (t == null || result == null) return;
            foreach (var battle in result.Battles)
                foreach (var action in battle.Actions)
                {
                    var actor = action.Kind == Combat.BattleActionKind.Skill && action.ActorId.HasValue ? t.Roster.Members.FirstOrDefault(m => m.Id == action.ActorId.Value) : null;
                    if (actor != null)
                        foreach (var item in actor.Gear.Equipped) GearWear.OnMonsterAction(item, GearSettings);
                    var target = action.Kind == Combat.BattleActionKind.Skill && action.Damage > 0 && action.TargetId.HasValue ? t.Roster.Members.FirstOrDefault(m => m.Id == action.TargetId.Value) : null;
                    if (target != null)
                        foreach (var item in target.Gear.Equipped) GearWear.OnMonsterHit(item, GearSettings);
                }
            double weather = 1.0;   // thời tiết thuộc Sub-project 6
            foreach (var item in t.Gear.Equipped) GearWear.OnFarmMinutes(item, minutes, weather, GearSettings);
        }
    }
}
