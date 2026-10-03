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
        readonly Dictionary<int, Dictionary<string, GearItem>> retiredGear = new Dictionary<int, Dictionary<string, GearItem>>();
        readonly HashSet<string> hubBoughtBackGearIds = new HashSet<string>(StringComparer.Ordinal);
        internal GearCatalog GearCat => Gear.GearCatalog.Default;
        internal GearConfig GearSettings => Gear.GearCatalog.Default.Config;

        /// <summary>Ảnh chụp chỉ đọc trạng thái trang bị của một Trainer và các Monster của họ.</summary>
        public IReadOnlyList<GearView> GearForTrainer(int trainerId)
        {
            if (trainerId < 0 || trainerId >= trainers.Count) return Array.Empty<GearView>();
            var trainer = trainers[trainerId];
            var views = trainer.Gear.Equipped.Select(item => GearView.From(item, "trainer:" + trainerId)).ToList();
            foreach (var monster in trainer.Roster.Members)
                views.AddRange(monster.Gear.Equipped.Select(item => GearView.From(item, "monster:" + monster.Id.Value)));
            return Array.AsReadOnly(views.OrderBy(x => x.OwnerId, StringComparer.Ordinal).ThenBy(x => x.SlotId, StringComparer.Ordinal).ToArray());
        }

        /// <summary>Trang bị dự trữ của Trainer; có thể dùng làm phôi Nâng Sao.</summary>
        public IReadOnlyList<GearView> GearStorageForTrainer(int trainerId)
        {
            if (trainerId < 0 || trainerId >= trainers.Count) return Array.Empty<GearView>();
            return Array.AsReadOnly(trainers[trainerId].GearInventory.Items.Select(item => GearView.From(item, "trainer:" + trainerId + ":storage")).ToArray());
        }

        /// <summary>Slot mà Giám đốc chào trang bị: Monster Combat về Active Monster, còn lại về Trainer.</summary>
        GearLoadout LoadoutForSlot(int trainerId, string slotId)
        {
            var trainer = trainers[trainerId];
            var slot = GearCat.GetSlot(slotId);
            return slot.Group == GearGroup.MonsterCombat ? trainer.Roster.Active?.Gear : trainer.Gear;
        }

        GearLoadout LoadoutForMonsterSlot(int trainerId, string slotId, Game.Domain.Monsters.MonsterId monsterId)
        {
            if (GearCat.GetSlot(slotId).Group != GearGroup.MonsterCombat) return null;
            return trainers[trainerId].Roster.Members.FirstOrDefault(m => m.Id == monsterId)?.Gear;
        }

        /// <summary>Chào bán một món trang bị; Trainer tự quyết theo Gear Score, giá và số Gold còn lại.</summary>
        public CommandResult OfferGear(int trainerId, GearItem offered, long price)
            => OfferGearCore(trainerId, offered, price, null);

        /// <summary>Chào trang bị chiến đấu cho một Monster cụ thể trong đội.</summary>
        public CommandResult OfferGearForMonster(int trainerId, Game.Domain.Monsters.MonsterId monsterId, GearItem offered, long price)
            => OfferGearCore(trainerId, offered, price, monsterId);

        CommandResult OfferGearCore(int trainerId, GearItem offered, long price, Game.Domain.Monsters.MonsterId? monsterId)
        {
            if (trainerId < 0 || trainerId >= trainers.Count) return CommandResult.Rejected("Trainer không tồn tại.");
            if (offered == null || offered.IsDestroyed) return CommandResult.Rejected("Không có món trang bị hợp lệ.");
            if (price <= 0) return CommandResult.Rejected("Giá chào phải lớn hơn 0.");
            try
            {
                var canonical = GearCat.GetSlot(offered.Slot.Id);
                if (canonical.Group != offered.Slot.Group || canonical.StatKind != offered.Slot.StatKind || canonical.OwnerUnit != offered.Slot.OwnerUnit)
                    return CommandResult.Rejected("Slot trang bị không khớp catalog.");
            }
            catch (ArgumentException) { return CommandResult.Rejected("Slot trang bị không có trong catalog."); }
            if (IsKnownGearId(offered.Id)) return CommandResult.Rejected("Món trang bị này đã được sở hữu hoặc giao dịch.");
            var trainer = trainers[trainerId];
            GearLoadout loadout = monsterId.HasValue ? LoadoutForMonsterSlot(trainerId, offered.Slot.Id, monsterId.Value) : LoadoutForSlot(trainerId, offered.Slot.Id);
            if (loadout == null) return CommandResult.Rejected("Trainer không có Active Monster cho slot này.");
            GearItem current = loadout.Get(offered.Slot.Id);
            double currentScore = current == null ? 0 : GearScore.Score(current, GearCat);
            double offeredScore = GearScore.Score(offered, GearCat);
            if (!offered.IsBroken && offeredScore > currentScore && price > trainer.Gold * GearSettings.AcceptanceGoldFraction)
            {
                long minimumGold = checked((long)Math.Ceiling(price / GearSettings.AcceptanceGoldFraction));
                EnsureTrainerCanPayFromLoan(trainer, minimumGold);
            }
            bool accepted = GearMarket.WouldAccept(current, offered, trainer.Gold, price, GearSettings);
            Raise(new GearOffered(now, trainerId, offered.Slot.Id, current?.Id, offered.Id, price, currentScore, offeredScore, accepted));
            if (!accepted) return CommandResult.Rejected("Trainer từ chối chào hàng trang bị.");
            trainer.Gold = checked(trainer.Gold - price);
            AddTreasury(price, "GearOffer");
            var displaced = loadout.Unequip(offered.Slot.Id);
            if (displaced != null)
            {
                if (!retiredGear.TryGetValue(trainerId, out var retired)) retiredGear.Add(trainerId, retired = new Dictionary<string, GearItem>(StringComparer.Ordinal));
                retired.Add(displaced.Id, displaced);
            }
            loadout.Equip(offered);
            return CommandResult.Success();
        }

        /// <summary>Giám đốc chào món cùng loại làm phôi; Trainer chỉ mua nếu có món tương ứng chưa đạt 5 Sao.</summary>
        public CommandResult OfferGearForSacrifice(int trainerId, GearItem offered, long price)
        {
            if (trainerId < 0 || trainerId >= trainers.Count) return CommandResult.Rejected("Trainer không tồn tại.");
            if (offered == null || offered.IsDestroyed || price <= 0 || IsKnownGearId(offered.Id)) return CommandResult.Rejected("Chào hàng trang bị hiến tế không hợp lệ.");
            try { if (GearCat.GetSlot(offered.Slot.Id).Group != offered.Slot.Group) return CommandResult.Rejected("Slot trang bị không hợp lệ."); }
            catch (ArgumentException) { return CommandResult.Rejected("Slot trang bị không có trong catalog."); }
            var trainer = trainers[trainerId];
            var target = (trainer.Gear.Get(offered.Slot.Id) != null ? new[] { trainer.Gear.Get(offered.Slot.Id) } : Array.Empty<GearItem>())
                .Concat(trainer.Roster.Members.Select(m => m.Gear.Get(offered.Slot.Id)))
                .FirstOrDefault(x => x != null && x.Stars < 5);
            if (target == null || target.Slot.Id != offered.Slot.Id)
                return CommandResult.Rejected("Trainer không cần phôi cho slot này.");
            if (price > trainer.Gold * GearSettings.AcceptanceGoldFraction)
            {
                long minimumGold = checked((long)Math.Ceiling(price / GearSettings.AcceptanceGoldFraction));
                EnsureTrainerCanPayFromLoan(trainer, minimumGold);
            }
            if (price > trainer.Gold || (double)price > trainer.Gold * GearSettings.AcceptanceGoldFraction)
                return CommandResult.Rejected("Giá phôi vượt ngân sách Trainer.");
            if (!trainer.GearInventory.TryAdd(offered)) return CommandResult.Rejected("Không thể thêm phôi vào kho trang bị.");
            trainer.Gold = checked(trainer.Gold - price);
            AddTreasury(price, "GearFodderOffer");
            Raise(new GearFodderPurchased(now, trainerId, offered.Id, offered.Slot.Id, price, true));
            return CommandResult.Success();
        }

        /// <summary>Hành động AI: cân nhắc mua Bùa theo giá trị tránh tổn thất rồi thực hiện lần Cường hóa.</summary>
        public CommandResult TrainerEnhanceGear(int trainerId, GearItem item)
        {
            if (!ValidGearInput(trainerId, item)) return CommandResult.Rejected("Thiếu tham số trang bị hợp lệ.");
            if (!CanFacilityOperate(EnhancementFacility)) return CommandResult.Rejected("facility.unavailable");
            if (trainers[trainerId].Inventory.Count(new ProductId("enhancement_stone")) < GearSettings.EnhanceStoneCost)
                return CommandResult.Rejected("Không có Đá Cường hóa.");
            TrainerPrepareEnhancementProtection(trainerId, item);
            bool useProtection = trainers[trainerId].Inventory.Count(new ProductId("protection_charm")) >= GearSettings.EnhanceProtectionCharmCost;
            return EnhanceGear(trainerId, item, useProtection);
        }

        /// <summary>Cường hóa +1: đốt Gold và 1 Đá Cường hóa; từ +11 có thể vỡ trừ khi dùng Bùa Bảo Hộ.</summary>
        public CommandResult EnhanceGear(int trainerId, GearItem item, bool useProtectionCharm)
        {
            if (!ValidGearInput(trainerId, item)) return CommandResult.Rejected("Thiếu tham số trang bị hợp lệ.");
            if (!CanFacilityOperate(EnhancementFacility)) return CommandResult.Rejected("facility.unavailable");
            if (useProtectionCharm && trainers[trainerId].Inventory.Count(new ProductId("protection_charm")) < GearSettings.EnhanceProtectionCharmCost)
                return CommandResult.Rejected("Không có Bùa Bảo Hộ.");
            var trainer = trainers[trainerId];
            if (trainer.Inventory.Count(new ProductId("enhancement_stone")) < GearSettings.EnhanceStoneCost) return CommandResult.Rejected("Không có Đá Cường hóa.");
            int oldLevel = item.EnhanceLevel;
            if (item.EnhanceLevel >= GearForge.MaxEnhance) return CommandResult.Rejected("Đã đạt +20.");
            var model = new EnhancementModel();
            long attemptCost = checked((long)Math.Ceiling(model.AttemptCost(item.EnhanceLevel + 1)));
            if (!EnsureTrainerCanPayFromLoan(trainer, attemptCost)) return CommandResult.Rejected("Không đủ Gold hoặc hạn mức vay.");
            EnhanceResult result = GearForge.TryEnhance(item, model, GearSettings, rng, useProtectionCharm);
            trainer.Gold = checked(trainer.Gold - result.GoldSpent);
            trainer.Inventory.TryConsume(new ProductId("enhancement_stone"), result.StonesSpent);
            if (result.CharmConsumed) trainer.Inventory.TryConsume(new ProductId("protection_charm"), GearSettings.EnhanceProtectionCharmCost);
            AddTreasury(result.GoldSpent, "GearEnhance");
            Raise(new TrainerProductChanged(now, trainerId, "enhancement_stone", -result.StonesSpent,
                trainer.Inventory.Count(new ProductId("enhancement_stone"))));
            if (result.CharmConsumed)
                Raise(new TrainerProductChanged(now, trainerId, "protection_charm", -GearSettings.EnhanceProtectionCharmCost,
                    trainer.Inventory.Count(new ProductId("protection_charm"))));
            if (result.Broke) FindEquippedLoadout(trainerId, item)?.Unequip(item.Slot.Id);
            Raise(new GearEnhanced(now, trainerId, item.Id, oldLevel, item.EnhanceLevel, result.Success,
                result.Broke, result.CharmConsumed, result.GoldSpent, result.StonesSpent));
            return CommandResult.Success();
        }

        /// <summary>Nâng Sao: hiến tế một món rác cùng slot để tăng một Sao.</summary>
        public CommandResult StarUpGear(int trainerId, GearItem item, GearItem junk)
        {
            if (!ValidGearInput(trainerId, item) || junk == null || ReferenceEquals(item, junk))
                return CommandResult.Rejected("Thiếu tham số trang bị hợp lệ.");
            if (!CanFacilityOperate("jeweler")) return CommandResult.Rejected("facility.unavailable");
            var equipped = trainers[trainerId].Gear.Equipped.Concat(trainers[trainerId].Roster.Members.SelectMany(m => m.Gear.Equipped)).ToList();
            if (equipped.Any(x => ReferenceEquals(x, junk)) || !ReferenceEquals(trainers[trainerId].GearInventory.Get(junk.Id), junk))
                return CommandResult.Rejected("Món hiến tế không nằm trong kho trang bị Trainer.");
            var trainer = trainers[trainerId];
            int oldStars = item.Stars;
            if (item.Stars >= 5) return CommandResult.Rejected("Đã đạt 5 Sao.");
            long cost = GearSettings.StarAttemptCost(item.Stars + 1);
            if (!EnsureTrainerCanPayFromLoan(trainer, cost)) return CommandResult.Rejected("Không đủ Gold hoặc hạn mức vay để Nâng Sao.");
            StarUpResult result = GearForge.StarUp(item, junk, GearSettings, rng);
            if (result.ConsumedItem != null)
            {
                trainer.GearInventory.TryRemove(result.ConsumedItem.Id, out _);
                trainer.Gold = checked(trainer.Gold - result.GoldSpent);
                AddTreasury(result.GoldSpent, "GearStarUp");
            }
            Raise(new GearStarUp(now, trainerId, item.Id, oldStars, item.Stars, result.ConsumedItem?.Id, result.Success,
                result.StarsLost, result.GoldSpent));
            if (result.ConsumedItem == null) return CommandResult.Rejected("Không thể Nâng Sao với món này.");
            if (!result.Success) return CommandResult.Success();
            return CommandResult.Success();
        }

        /// <summary>Tinh Luyện: tốn Tinh Thể Boss Thế Giới và Nước Cất trong inventory của Trainer.</summary>
        public CommandResult RefineGear(int trainerId, GearItem item)
        {
            if (!ValidGearInput(trainerId, item)) return CommandResult.Rejected("Thiếu tham số trang bị hợp lệ.");
            if (!CanFacilityOperate("jeweler")) return CommandResult.Rejected("facility.unavailable");
            var trainer = trainers[trainerId];
            var crystal = new ProductId("world_boss_crystal");
            var water = new ProductId("distilled_water");
            long goldCost = GearSettings.RefineAttemptCost((int)item.Refine + 1);
            if (trainer.Inventory.Count(crystal) < GearSettings.RefineCrystalCost ||
                trainer.Inventory.Count(water) < GearSettings.RefineWaterCost)
                return CommandResult.Rejected("Không đủ Gold, Tinh Thể Boss Thế Giới hoặc Nước Cất.");
            if (!EnsureTrainerCanPayFromLoan(trainer, goldCost)) return CommandResult.Rejected("Không đủ Gold hoặc hạn mức vay.");
            var oldGrade = item.Refine;
            RefineResult result = GearForge.Refine(item, trainer.Inventory.Count(crystal), trainer.Inventory.Count(water), trainer.Gold, GearSettings, rng);
            if (result.CrystalConsumed == 0) return CommandResult.Rejected("Không thể Tinh Luyện món này.");
            trainer.Gold = checked(trainer.Gold - result.GoldSpent);
            AddTreasury(result.GoldSpent, "GearRefine");
            trainer.Inventory.TryConsume(crystal, result.CrystalConsumed);
            trainer.Inventory.TryConsume(water, result.WaterConsumed);
            Raise(new TrainerProductChanged(now, trainerId, crystal.Value, -result.CrystalConsumed, trainer.Inventory.Count(crystal)));
            Raise(new TrainerProductChanged(now, trainerId, water.Value, -result.WaterConsumed, trainer.Inventory.Count(water)));
            Raise(new GearRefined(now, trainerId, item.Id, oldGrade, item.Refine, result.CrystalConsumed, result.WaterConsumed, result.Success, result.GoldSpent));
            return CommandResult.Success();
        }

        /// <summary>Sửa đồ tại Lò Rèn/Xưởng Dệt: trả Gold theo độ bền thiếu.</summary>
        public CommandResult RepairGear(int trainerId, GearItem item)
        {
            if (!ValidGearInput(trainerId, item)) return CommandResult.Rejected("Thiếu tham số trang bị hợp lệ.");
            if (item.Slot.Group == GearGroup.Aura) return CommandResult.Rejected("gear.repair_not_supported");
            string facilityId = item.Slot.Group == GearGroup.MonsterCombat ? "monster_forge" : "textile_workshop";
            if (!CanFacilityOperate(facilityId)) return CommandResult.Rejected("facility.unavailable");
            var trainer = trainers[trainerId];
            int oldDurability = item.Durability;
            long repairCost = checked((long)Math.Ceiling((item.MaxDurability - item.Durability) * GearSettings.RepairGoldPerDurability));
            if (item.Slot.Group == GearGroup.Aura) repairCost = 0;
            if (!EnsureTrainerCanPayFromLoan(trainer, repairCost)) return CommandResult.Rejected("Không đủ Gold hoặc hạn mức vay để sửa.");
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
            if (trainerId < 0 || trainerId >= trainers.Count || item == null || item.IsDestroyed ||
                !retiredGear.TryGetValue(trainerId, out var retired) || !retired.TryGetValue(item.Id, out var held) ||
                !ReferenceEquals(item, held) || hubBoughtBackGearIds.Contains(item.Id))
                return CommandResult.Rejected("Món này không còn là trang bị cũ của Trainer cần mua lại.");
            long buyback = GearMarket.BuybackPrice(item, salePrice, GearSettings);
            if (buyback <= 0) return CommandResult.Rejected("Giá mua lại không hợp lệ.");
            if (treasury.Balance < buyback) return CommandResult.Rejected("Kho bạc không đủ Gold mua lại.");
            treasury.TrySpend(buyback);
            trainers[trainerId].Gold = checked(trainers[trainerId].Gold + buyback);
            retired.Remove(item.Id);
            hubBoughtBackGearIds.Add(item.Id);
            Raise(new TreasuryChanged(now, -buyback, treasury.Balance, "GearBuyback"));
            Raise(new GearBuyback(now, trainerId, item.Id, salePrice, buyback, true));
            return CommandResult.Success();
        }

        internal void GrantProductForTest(int trainerId, string productId, int units)
            => trainers[trainerId].Inventory.Add(new ProductId(productId), units);

        bool ValidGearInput(int trainerId, GearItem item)
            => trainerId >= 0 && trainerId < trainers.Count && item != null && !item.IsDestroyed && IsEquippedByTrainer(trainerId, item);

        // GDD 12 maps Enhancement Stone use to the Forge for all gear groups.
        const string EnhancementFacility = "monster_forge";

        bool IsEquippedByTrainer(int trainerId, GearItem item)
        {
            var trainer = trainers[trainerId];
            return trainer.Gear.Equipped.Concat(trainer.Roster.Members.SelectMany(m => m.Gear.Equipped)).Any(x => ReferenceEquals(x, item));
        }

        GearLoadout FindEquippedLoadout(int trainerId, GearItem item)
        {
            var trainer = trainers[trainerId];
            if (trainer.Gear.Equipped.Any(x => ReferenceEquals(x, item))) return trainer.Gear;
            return trainer.Roster.Members.FirstOrDefault(m => m.Gear.Equipped.Any(x => ReferenceEquals(x, item)))?.Gear;
        }

        bool IsKnownGearId(string itemId)
            => hubBoughtBackGearIds.Contains(itemId) || retiredGear.Values.Any(x => x.ContainsKey(itemId)) ||
               trainers.Any(t => t.GearInventory.Get(itemId) != null ||
                   t.Gear.Equipped.Concat(t.Roster.Members.SelectMany(m => m.Gear.Equipped)).Any(x => x.Id == itemId));

        /// <summary>
        /// Hao mòn sau mỗi khúc farm: Monster trong đội mổ trang bị theo số action/hit,
        /// Trainer mở trang bị Tiện ích theo thời gian. Hào quang không hao.
        /// </summary>
        internal void ApplyGearWear(Trainer t, ExpeditionResult result, int minutes)
        {
            if (t == null || result == null) return;
            foreach (var battle in result.Battles)
            {
                var wornSkillCasts = new HashSet<string>(StringComparer.Ordinal);
                foreach (var action in battle.Actions)
                {
                    var actor = action.Kind == Combat.BattleActionKind.Skill && action.ActorId.HasValue ? t.Roster.Members.FirstOrDefault(m => m.Id == action.ActorId.Value) : null;
                    string castId = action.Kind == Combat.BattleActionKind.Skill && action.ActorId.HasValue
                        ? action.Turn + ":" + action.ActorId.Value.Value + ":" + action.SkillId : null;
                    if (actor != null && wornSkillCasts.Add(castId))
                        foreach (var item in actor.Gear.Equipped) WearAndReport(t.Id, item, () => GearWear.OnMonsterAction(item, GearSettings), "MonsterAction");
                    var target = action.Kind == Combat.BattleActionKind.Skill && action.Damage > 0 && action.TargetId.HasValue ? t.Roster.Members.FirstOrDefault(m => m.Id == action.TargetId.Value) : null;
                    if (target != null)
                        foreach (var item in target.Gear.Equipped) WearAndReport(t.Id, item, () => GearWear.OnMonsterHit(item, GearSettings), "MonsterHit");
                }
            }
            double weather = GearSettings.UtilityWeatherFactorDefault;   // thời tiết động thuộc Sub-project 6
            foreach (var item in t.Gear.Equipped) WearAndReport(t.Id, item, () => GearWear.OnFarmMinutes(item, minutes, weather, GearSettings), "FarmTime");
        }

        void WearAndReport(int trainerId, GearItem item, Action wear, string cause)
        {
            int oldDurability = item.Durability;
            wear();
            if (item.Durability != oldDurability)
                Raise(new GearDurabilityChanged(now, trainerId, item.Id, oldDurability, item.Durability, cause));
        }
    }
}
