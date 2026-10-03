using System;
using Game.Domain.Materials;
using Game.Domain.Supply;
using System.Linq;
using Game.Domain.Combat;
using Game.Domain.Monsters;
using Game.Domain.Gear;

namespace Game.Domain
{
    public sealed partial class HubWorld
    {
        /// <summary>
        /// Cộng trừ nhu cầu cho khoảng thời gian trôi kể từ lần cập nhật trước, theo trạng thái ĐANG có.
        /// Luôn gọi trước khi đổi trạng thái của Trainer.
        /// </summary>
        void Settle(Trainer t)
        {
            int minutes = now - t.LastSettleMinute;
            t.LastSettleMinute = now;
            if (minutes <= 0) return;

            double hours = minutes / 60.0;
            PersonalityProfile p = PersonalityProfile.Of(t.Personality);
            Needs n = t.Needs;
            bool outside = t.State == TrainerState.Traveling || t.State == TrainerState.Farming || t.State == TrainerState.Returning;
            if (outside)
            {
                n.Stamina -= GearEffects.StaminaDecayPerHour(t, GearCat, cfg.FieldStaminaPerHour) * hours;
                n.Satiety -= cfg.FieldSatietyPerHour * p.SatietyDecayMult * hours;
                n.Hydration -= GearEffects.HydrationDecayPerHour(t, GearCat, cfg.FieldHydrationPerHour) * hours;
                n.Stress += cfg.FieldStressPerHour * hours;
            }
            else if (t.State != TrainerState.InService)   // đang được phục vụ thì không tụt
            {
                n.Stamina -= cfg.HubStaminaPerHour * hours;
                n.Satiety -= cfg.HubSatietyPerHour * p.SatietyDecayMult * hours;
                n.Hydration -= cfg.HubHydrationPerHour * hours;
                if (t.State == TrainerState.Queued) n.Stress += cfg.QueueStressPerHour * hours;
                if (t.State == TrainerState.WaitingForMoney) n.Stress += cfg.WaitStressPerHour * hours;
            }
            n.Clamp();
        }

        void SetState(Trainer t, TrainerState state, string reason)
        {
            if (t.State == state && t.StateReason == reason) return;
            TrainerState from = t.State;
            t.State = state;
            t.StateReason = reason;
            Raise(new TrainerStateChanged(now, t.Id, from, state, reason));
        }

        /// <summary>
        /// Trainer chọn việc tiếp theo ở HUB: dịch vụ cần dùng > nghỉ (đình công, ban đêm không kính) > đi farm.
        /// </summary>
        void OnDecide(Trainer t)
        {
            Settle(t);
            bool isNight = SimClock.IsNight(now);

            if (t.Roster.Members.Any(m => m.Custody == MonsterCustody.Hospital))
            {
                SetState(t, TrainerState.AtHub, "MonsterRecovery");
                queue.Schedule(now + 60, SimEventKind.TrainerDecide, t.Id, t.Token);
                return;
            }
            BuildingKind? need = TrainerBrain.PickService(t, cfg, isNight, !t.IsOnStrike, GearEffects.HasNightVision(t, GearCat, t.HasNightVision));   // đình công: không vào Bệnh Viện
            if (need.HasValue) { RequestService(t, need.Value); return; }

            if (t.IsOnStrike)
            {
                SetState(t, TrainerState.AtHub, "Strike");
                queue.Schedule(now + 60, SimEventKind.TrainerDecide, t.Id, t.Token);   // đình công: nghỉ 1 giờ rồi xét lại
                return;
            }
            if (isNight && !t.HasNightVision)
            {
                SetState(t, TrainerState.AtHub, "NightRest");
                queue.Schedule(SimClock.NextMinuteOfDay(now, SimClock.DawnMinute), SimEventKind.TrainerDecide, t.Id, t.Token);
                return;
            }
            if (t.Roster.Members.Count == 0)
            {
                SetState(t, TrainerState.AtHub, "NoFieldMonster");
                queue.Schedule(now + 60, SimEventKind.TrainerDecide, t.Id, t.Token);
                return;
            }
            var unlocked = unlockedZoneIds.OrderBy(id => id, StringComparer.Ordinal)
                .Select(id => cfg.ZoneCatalogSettings.Definitions.FirstOrDefault(z => z.Id == id)).Where(z => z != null).ToArray();
            var zone = new ZoneSelector(cfg.ZoneSelectionSettings).Select(TrainerSnapshot.FromTrainer(t, now), unlocked, Array.Empty<ZoneIncomeModifier>());
            if (zone == null)
            {
                SetState(t, TrainerState.AtHub, "NoEligibleZone");
                queue.Schedule(now + 60, SimEventKind.TrainerDecide, t.Id, t.Token);
                return;
            }
            BuyExpeditionSupplies(t, zone);
            t.CurrentZoneId = zone.Id;
            SetState(t, TrainerState.Traveling, "Farm");
            queue.Schedule(now + zone.WalkMinutes, SimEventKind.TrainerArriveZone, t.Id, t.Token);
        }

        void OnArriveZone(Trainer t)
        {
            Settle(t);
            SetState(t, TrainerState.Farming, "Farm");
            queue.Schedule(now + cfg.FarmChunkMinutes, SimEventKind.FarmChunk, t.Id, t.Token);
        }

        /// <summary>Hết một khúc farm: nhận kết quả, rồi quyết định ở lại hay về HUB.</summary>
        void OnFarmChunk(Trainer t)
        {
            Settle(t);
            var zone = cfg.ZoneCatalogSettings.Definitions.FirstOrDefault(z => z.Id == t.CurrentZoneId)
                ?? throw new InvalidOperationException("Trainer đang farm nhưng Zone hiện tại không còn trong catalog.");
            if (t.Roster.Members.Count == 0 || t.Roster.Members.Any(m => m.Custody == MonsterCustody.Hospital))
            {
                StartReturn(t, ReturnReason.TeamDown);
                return;
            }
            UseAvailableExpeditionItems(t);
            ExpeditionResult result = expeditions.Resolve(TrainerSnapshot.FromTrainer(t, now), zone, cfg.FarmChunkMinutes, rng);
            var loot = result.Loot;
            foreach (var battle in result.Battles)
                foreach (var state in battle.FinalMonsters.Where(x => x.Side == BattleSide.Team))
                {
                    var monster = t.Roster.Members.FirstOrDefault(x => x.Id == state.Id);
                    if (monster != null) monster.SetCurrentHp(state.CurrentHp);
                }
            foreach (var state in result.FinalMonsterHp)
            {
                var monster = t.Roster.Members.FirstOrDefault(x => x.Id == state.Key);
                if (monster != null) monster.SetCurrentHp(state.Value);
            }
            var finalActive = result.Battles.LastOrDefault()?.ActiveId;
            if (finalActive.HasValue && t.Roster.Members.Any(m => m.Id == finalActive.Value && m.CurrentHp > 0))
                t.Roster.SetActive(finalActive.Value);
            ApplyGearWear(t, result, cfg.FarmChunkMinutes);
            int available = Math.Max(0, GearEffects.BackpackCapacity(t, GearCat, t.BackpackCapacity) - t.BackpackUnits);
            var collected = new System.Collections.Generic.List<MaterialQuantity>();
            var dropped = new System.Collections.Generic.List<MaterialQuantity>(loot.Dropped);
            foreach (var lot in loot.Collected.OrderBy(x => x.MaterialId.Value, StringComparer.Ordinal))
            {
                int requested = payroll.DebtMode ? (int)(lot.Quantity * cfg.DebtModeFarmMultiplier) : lot.Quantity;
                int gained = Math.Min(available, requested); available -= gained; t.BackpackUnits += gained;
                if (gained > 0) collected.Add(new MaterialQuantity(lot.MaterialId, gained));
                if (requested > gained) dropped.Add(new MaterialQuantity(lot.MaterialId, requested - gained));
                if (gained > 0) t.BackpackMaterials[lot.MaterialId] = checked(t.BackpackMaterials.TryGetValue(lot.MaterialId, out var held) ? held + gained : gained);
            }
            long goldGained = payroll.DebtMode ? (long)(loot.Gold * cfg.DebtModeFarmMultiplier) : loot.Gold;
            ReceiveTrainerIncome(t, goldGained, "ExpeditionGold");
            TrainerProgression.AddExperience(t, result.TrainerExperience, cfg.TrainerProgressionSettings);
            Raise(new ExpeditionCompleted(now, t.Id, zone.Id, result.Battles, collected.AsReadOnly(), dropped.AsReadOnly(),
                goldGained, result.TrainerExperience, t.Level));

            ReturnReason reason = TrainerBrain.ShouldReturn(t, SimClock.IsNight(now), GearEffects.BackpackCapacity(t, GearCat, t.BackpackCapacity), GearEffects.HasNightVision(t, GearCat, t.HasNightVision));
            if (reason != ReturnReason.None) StartReturn(t, reason);
            else queue.Schedule(now + cfg.FarmChunkMinutes, SimEventKind.FarmChunk, t.Id, t.Token);
        }

        void BuyExpeditionSupplies(Trainer t, ZoneDefinition zone)
        {
            if (!useSupplyChain || cfg.ConsumablePolicySettings == null || t.Roster.Members.Count == 0) return;
            var rebellion = t.Roster.Members.Count(m => RebellionModel.IsRebellious(m.Level, m.Rarity,
                t.Rank, t.Level, t.Rarity, t.LeadershipItemBonus));
            var risk = new CombatRiskSnapshot(t.Roster.Members.Count(m => m.CurrentHp < m.MaxHp),
                rebelliousMonsterCount: rebellion, hasEligibleReserve: t.Roster.Members.Count > 1,
                expectedCombatRisk: zone.EncounterProfile.ExpectedEncountersPerHour * cfg.FarmChunkMinutes / 60.0);
            var plan = ConsumablePolicy.DecidePurchases(TrainerSnapshot.FromTrainer(t, now), ConsumableStock,
                risk, cfg.ConsumablePolicySettings);
            foreach (var purchase in plan) PurchaseProduct(t.Id, purchase.Product.Value, purchase.Units);
        }

        void UseAvailableExpeditionItems(Trainer t)
        {
            var active = t.Roster.Active;
            if (active == null) return;
            var config = cfg.MonsterItemSettings ?? MonsterItemConfig.Prototype;
            void TryUse(string id, Monster target, double management = 0, double leadership = 0,
                bool reserve = false, bool hasLock = false)
            {
                var product = new ProductId(id);
                if (target == null || t.Inventory.Count(product) == 0) return;
                var result = MonsterItemEffects.Apply(product, target, config, now, management, leadership, reserve, hasLock);
                if (!result.Applied || result.ConsumedUnits == 0 || !t.Inventory.TryConsume(product, result.ConsumedUnits)) return;
                if (result.LeadershipBonus > 0) t.LeadershipItemBonus += result.LeadershipBonus;
                if (result.BagSynergyEnabled) t.BagSynergyExpiresAtMinute = result.ExpiresAtMinute;
                Raise(new TrainerProductChanged(now, t.Id, id, -result.ConsumedUnits, t.Inventory.Count(product)));
            }

            foreach (var monster in t.Roster.Members.Where(m => m.CurrentHp < m.MaxHp).OrderBy(m => m.Id.Value, StringComparer.Ordinal))
                TryUse("potion", monster);
            TryUse("monster_buff_bottle", active);
            double leadership = RebellionModel.LeadershipScore(t.Rank, t.Level, t.Rarity, t.LeadershipItemBonus);
            foreach (var monster in t.Roster.Members.Where(m =>
                RebellionModel.ManagementScore(m.Level, m.Rarity) - m.RebellionReductionAt(now) > leadership)
                .OrderBy(m => m.Id.Value, StringComparer.Ordinal))
                TryUse("reward_cake", monster, RebellionModel.ManagementScore(monster.Level, monster.Rarity), leadership);
            bool stillRebellious = t.Roster.Members.Any(m =>
                RebellionModel.ManagementScore(m.Level, m.Rarity) - m.RebellionReductionAt(now) > leadership);
            if (stillRebellious) TryUse("pet_communication_lock", active, hasLock: t.LeadershipItemBonus > 0);
            bool synergyActive = t.BagSynergyExpiresAtMinute > now;
            if (!synergyActive) TryUse("tactics_book", active, reserve: t.Roster.Members.Count > 1);
        }

        /// <summary>Bắt đầu đường về HUB. Gọi sau <see cref="Settle"/>.</summary>
        void StartReturn(Trainer t, ReturnReason reason)
        {
            SetState(t, TrainerState.Returning, reason.ToString());
            var zone = cfg.ZoneCatalogSettings.Definitions.FirstOrDefault(z => z.Id == t.CurrentZoneId);
            queue.Schedule(now + (zone?.WalkMinutes ?? cfg.ZoneTravelMinutes), SimEventKind.TrainerArriveHub, t.Id, t.Token);
        }

        /// <summary>Ngắt việc đang làm của Trainer (hủy sự kiện cũ bằng token) và cho về HUB.</summary>
        void SendHome(Trainer t, ReturnReason reason)
        {
            t.Token++;
            Settle(t);
            StartReturn(t, reason);
        }

        /// <summary>Về tới HUB: bán nguyên liệu (HUB thu thuế), rồi chọn việc tiếp theo.</summary>
        void OnArriveHub(Trainer t)
        {
            Settle(t);
            SetState(t, TrainerState.AtHub, "Arrived");
            if (t.BackpackUnits > 0 && useSupplyChain)
            {
                if (SellBackpack(t)) { SetState(t, TrainerState.AtHub, "MarketSettled"); OnDecide(t); }
                else { t.MarketWaitSinceMinute = now; SetState(t, TrainerState.WaitingForMarket, "MerchantRoute");
                    ScheduleOrEndMarketWait(t); }
                return;
            }
            else if (t.BackpackUnits > 0)
            {
                SaleResult sale = market.Quote(t, t.BackpackUnits);
                ReceiveTrainerIncome(t, sale.GrossToTrainer - sale.Tax, "MaterialSale");
                AddTreasury(sale.Tax, "TradeTax");
                t.BackpackUnits = 0;
                t.BackpackMaterials.Clear();
            }
            OnDecide(t);
        }

        bool SellBackpack(Trainer t)
        {
            foreach (var lot in t.BackpackMaterials.OrderBy(x => x.Key.Value, StringComparer.Ordinal).ToArray())
            {
                int remaining = lot.Value;
                if (buyRequests.TryGetValue(lot.Key, out var request) && request.Enabled)
                {
                    long before = treasury.Balance;
                    SaleBreakdown direct = station.BuyFromTrainer("trainer:" + t.Id, lot.Key, remaining, request);
                    ReceiveTrainerIncome(t, direct.NetToSeller, "StationMaterialSale");
                    remaining = direct.UnsoldUnits;
                    if (direct.StationUnits > 0) Raise(new MaterialTradeSettled(now, t.Id, lot.Key.Value, "Station",
                        direct.StationUnits, direct.Gross, direct.Tax, direct.NetToSeller));
                    if (treasury.Balance != before) Raise(new TreasuryChanged(now, treasury.Balance - before, treasury.Balance, "TradeTax"));
                    if (direct.StationUnits > 0) Raise(new SupplyStockChanged(now, "material:" + lot.Key.Value,
                        station.Stock.Get(new InventoryItem(lot.Key))));
                    if (direct.StationUnits > 0) ReconcileProduction();
                }
                if (remaining > 0 && merchantFleet.Current.State == MerchantState.AtTrainerRoute)
                {
                    SaleBreakdown merchantSale = merchantFleet.Current.BuyFromTrainer("trainer:" + t.Id, lot.Key, remaining,
                        marketReferencePrice, marketTaxRate, long.MaxValue - treasury.Balance);
                    ReceiveTrainerIncome(t, merchantSale.NetToSeller, "MerchantMaterialSale");
                    if (merchantSale.MerchantUnits > 0) Raise(new MaterialTradeSettled(now, t.Id, lot.Key.Value, "Merchant",
                        merchantSale.MerchantUnits, merchantSale.Gross, merchantSale.Tax, merchantSale.NetToSeller));
                    if (merchantSale.Tax > 0)
                    {
                        treasury.Add(merchantSale.Tax);
                        Raise(new TreasuryChanged(now, merchantSale.Tax, treasury.Balance, "TradeTax"));
                    }
                    remaining = merchantSale.UnsoldUnits;
                }
                if (remaining == 0) t.BackpackMaterials.Remove(lot.Key);
                else t.BackpackMaterials[lot.Key] = remaining;
            }
            int total = 0;
            checked { foreach (int units in t.BackpackMaterials.Values) total += units; }
            t.BackpackUnits = total;
            return total == 0;
        }
    }
}
