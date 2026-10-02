using System;
using System.Collections.Generic;
using System.Linq;
using Game.Domain.Materials;
using Game.Domain.Production;
using Game.Domain.Supply;

namespace Game.Domain
{
    public sealed partial class HubWorld
    {
        internal SupplyChain Supply => supplyChain;

        public IReadOnlyList<MaterialStockView> SupplyStocks
        {
            get
            {
                if (!useSupplyChain) return Array.Empty<MaterialStockView>();
                return MaterialCatalog.Default.Materials
                    .Select(x => (id: "material:" + x.Id.Value, balance: station.Stock.Get(new InventoryItem(x.Id))))
                    .Where(x => TotalStock(x.balance) > 0)
                    .Select(x => new MaterialStockView(x.id, x.balance.Available, x.balance.Reserved, x.balance.InProduction))
                    .Concat(MaterialCatalog.Default.Products
                        .Select(x => (id: "product:" + x.Id.Value, balance: station.Stock.Get(new InventoryItem(x.Id))))
                        .Where(x => TotalStock(x.balance) > 0)
                        .Select(x => new MaterialStockView(x.id, x.balance.Available, x.balance.Reserved, x.balance.InProduction)))
                    .ToArray();
            }
        }

        public IReadOnlyList<BuyRequestView> BuyRequests => buyRequests.Values
            .OrderBy(x => x.Material.Value, StringComparer.Ordinal)
            .Select(x => new BuyRequestView(x.Material.Value, x.TargetStock, x.BidPrice, x.Enabled,
                x.Deficit(station == null ? 0 : TotalStock(station.Stock.Get(new InventoryItem(x.Material))))))
            .ToArray();

        static int TotalStock(InventoryBalance balance)
            => (int)Math.Min(int.MaxValue, (long)balance.Available + balance.Reserved + balance.InProduction);

        public MerchantView Merchant => merchantFleet == null ? null : new MerchantView(merchantFleet.Current.Id,
            merchantFleet.Current.State, merchantFleet.Current.Cash, merchantFleet.Current.LoadUnits,
            merchantFleet.Current.Config.CapacityUnits);

        public IReadOnlyList<ProductionJobView> ProductionJobs => production == null
            ? Array.Empty<ProductionJobView>()
            : production.Jobs.Select(x => new ProductionJobView(x.Id, x.Recipe.Id.Value, x.StartMinute,
                x.FinishMinute, x.State.ToString())).ToArray();

        public IReadOnlyList<ProductionRestockDemand> ProductionRestockDemands => production == null
            ? Array.Empty<ProductionRestockDemand>() : production.RestockDemands.ToArray();

        public IReadOnlyList<MoneyTransaction> SupplyTransactions => supplyLedger == null
            ? Array.Empty<MoneyTransaction>() : supplyLedger.Transactions.ToArray();

        public CommandResult SetBuyRequest(string materialId, int targetStock, long bidPrice, bool enabled = true)
        {
            if (!useSupplyChain) return CommandResult.Rejected("Supply chain không được bật cho HubWorld này.");
            if (string.IsNullOrWhiteSpace(materialId) || !MaterialCatalog.Default.Materials.Any(x => x.Id.Value == materialId))
                return CommandResult.Rejected("Mã nguyên liệu không tồn tại trong catalog.");
            if (targetStock < 0) return CommandResult.Rejected("Mức tồn mục tiêu không được âm.");
            if (bidPrice < 0) return CommandResult.Rejected("Giá mua không được âm.");
            var id = new MaterialId(materialId);
            buyRequests[id] = new BuyRequest(id, targetStock, bidPrice, enabled);
            Raise(new BuyRequestChanged(now, materialId, targetStock, bidPrice, enabled));
            return CommandResult.Success();
        }

        public CommandResult SetProductionTarget(string productId, int targetStock)
        {
            if (!useSupplyChain) return CommandResult.Rejected("Supply chain không được bật cho HubWorld này.");
            if (string.IsNullOrWhiteSpace(productId)) return CommandResult.Rejected("Thiếu mã sản phẩm.");
            if (targetStock < 0) return CommandResult.Rejected("Mức tồn mục tiêu không được âm.");
            try
            {
                var previous = new HashSet<long>(production.ActiveJobs.Select(x => x.Id));
                long treasuryBefore = treasury.Balance;
                production.SetTarget(new ProductId(productId), targetStock, now);
                EmitProductionTreasuryChange(treasuryBefore);
                EmitRestockDemandChanges();
                EmitStartedJobs(previous);
                return CommandResult.Success();
            }
            catch (ArgumentException ex) { return CommandResult.Rejected(ex.Message); }
        }

        public CommandResult SetMaterialReferencePrice(long price)
        {
            if (!useSupplyChain) return CommandResult.Rejected("Supply chain không được bật cho HubWorld này.");
            if (price < 0) return CommandResult.Rejected("Giá tham chiếu không được âm.");
            marketReferencePrice = price;
            return CommandResult.Success();
        }

        public CommandResult SetMarketTaxRate(double rate)
        {
            if (!useSupplyChain) return CommandResult.Rejected("Supply chain không được bật cho HubWorld này.");
            if (double.IsNaN(rate) || rate < 0 || rate > 1) return CommandResult.Rejected("Thuế phải trong khoảng 0..1.");
            marketTaxRate = rate;
            station.SetTaxRate(rate);
            return CommandResult.Success();
        }

        void OnProductionComplete()
        {
            if (production == null) return;
            if (productionEventMinute <= now) productionEventMinute = -1;
            var previous = new HashSet<long>(production.ActiveJobs.Select(x => x.Id));
            var due = production.ActiveJobs.Where(x => x.FinishMinute <= now).ToArray();
            long treasuryBefore = treasury.Balance;
            production.AdvanceTo(now);
            EmitProductionTreasuryChange(treasuryBefore);
            EmitRestockDemandChanges();
            foreach (var job in due)
            {
                Raise(new ProductionJobChanged(now, job.Id, job.Recipe.Id.Value, job.State.ToString(), job.FinishMinute));
                foreach (var input in job.Inputs)
                    Raise(new SupplyStockChanged(now, input.Item.Value, station.Stock.Get(input.Item)));
                foreach (var output in job.ActualOutputs)
                    Raise(new SupplyStockChanged(now, output.Item.Value, station.Stock.Get(output.Item)));
            }
            EmitStartedJobs(previous);
            ScheduleProductionCompletion();
        }

        void ReconcileProduction()
        {
            if (production == null) return;
            var previous = new HashSet<long>(production.ActiveJobs.Select(x => x.Id));
            long treasuryBefore = treasury.Balance;
            production.AdvanceTo(now);
            EmitProductionTreasuryChange(treasuryBefore);
            EmitRestockDemandChanges();
            EmitStartedJobs(previous);
        }

        void EmitRestockDemandChanges()
        {
            var current = production.RestockDemands.ToDictionary(x => x.Item.Value, x => x.Quantity, StringComparer.Ordinal);
            foreach (var old in reportedRestockDemands.ToArray())
                if (!current.ContainsKey(old.Key))
                {
                    Raise(new ProductionRestockDemandChanged(now, old.Key, 0));
                    reportedRestockDemands.Remove(old.Key);
                }
            foreach (var demand in current.OrderBy(x => x.Key, StringComparer.Ordinal))
                if (!reportedRestockDemands.TryGetValue(demand.Key, out int previous) || previous != demand.Value)
                {
                    Raise(new ProductionRestockDemandChanged(now, demand.Key, demand.Value));
                    reportedRestockDemands[demand.Key] = demand.Value;
                }
        }

        void EmitProductionTreasuryChange(long before)
        {
            long delta = treasury.Balance - before;
            if (delta != 0) Raise(new TreasuryChanged(now, delta, treasury.Balance, "ProductionCost"));
        }

        void EmitStartedJobs(HashSet<long> previous)
        {
            foreach (ProductionJob job in production.ActiveJobs.Where(x => !previous.Contains(x.Id)).OrderBy(x => x.Id))
            {
                Raise(new ProductionJobChanged(now, job.Id, job.Recipe.Id.Value, job.State.ToString(), job.FinishMinute));
                foreach (var input in job.Inputs)
                    Raise(new SupplyStockChanged(now, input.Item.Value, station.Stock.Get(input.Item)));
            }
            ScheduleProductionCompletion();
        }

        void ScheduleProductionCompletion()
        {
            if (production == null || production.ActiveJobs.Count == 0) return;
            int finish = production.ActiveJobs.Min(x => x.FinishMinute);
            if (productionEventMinute >= 0 && productionEventMinute <= finish) return;
            productionEventMinute = finish;
            queue.Schedule(finish, SimEventKind.ProductionComplete);
        }

        void OnMerchantRouteStep()
        {
            Merchant merchant = merchantFleet.Current;
            if (merchant.State == MerchantState.Bankrupt)
            {
                if (merchantFleet.AdvanceTo(now))
                {
                    RaiseMerchantState();
                    WakeWaitingMarketTrainers();
                    queue.Schedule(now + Math.Max(1, merchantFleet.Current.Config.RouteCycleMinutes / 2), SimEventKind.MerchantRouteStep);
                }
                else queue.Schedule(merchantFleet.ReplacementMinute.Value, SimEventKind.MerchantRouteStep);
                return;
            }

            if (merchant.State == MerchantState.AtTrainerRoute)
            {
                merchant.DepartForStation();
                RaiseMerchantState();
                queue.Schedule(now + Math.Max(1, merchant.Config.RouteCycleMinutes / 2), SimEventKind.MerchantRouteStep);
                return;
            }

            if (merchant.State == MerchantState.TravelingToStation)
            {
                bool solvent = merchant.ArriveAtStation();
                RaiseMerchantState();
                if (solvent)
                {
                    long before = treasury.Balance;
                    foreach (var request in buyRequests.Values.Where(x => x.Enabled).OrderBy(x => x.Material.Value, StringComparer.Ordinal))
                    {
                        SaleBreakdown result = merchant.SellToStation(station, request.Material, request);
                        if (result.StationUnits > 0) Raise(new MaterialTradeSettled(now, -1, request.Material.Value,
                            "MerchantToStation", result.StationUnits, result.Gross, result.Tax, result.NetToSeller));
                        if (result.StationUnits > 0)
                            Raise(new SupplyStockChanged(now, "material:" + request.Material.Value,
                                station.Stock.Get(new InventoryItem(request.Material))));
                    }
                    if (treasury.Balance != before) Raise(new TreasuryChanged(now, treasury.Balance - before, treasury.Balance, "MerchantPurchase"));
                    ReconcileProduction();
                    merchant.DepartForTrainers();
                    RaiseMerchantState();
                    queue.Schedule(now + Math.Max(1, merchant.Config.RouteCycleMinutes / 2), SimEventKind.MerchantRouteStep);
                }
                else ScheduleMerchantReplacement();
                return;
            }

            if (merchant.State == MerchantState.TravelingToTrainers)
            {
                bool solvent = merchant.ArriveAtTrainerRoute();
                RaiseMerchantState();
                if (solvent)
                {
                    WakeWaitingMarketTrainers();
                    queue.Schedule(now + Math.Max(1, merchant.Config.RouteCycleMinutes / 2), SimEventKind.MerchantRouteStep);
                }
                else ScheduleMerchantReplacement();
            }
        }

        void WakeWaitingMarketTrainers()
        {
            foreach (Trainer t in trainers.Where(x => x.State == TrainerState.WaitingForMarket).OrderBy(x => x.Id).ToArray())
            {
                if (!SellBackpack(t)) continue;
                t.Token++;
                SetState(t, TrainerState.AtHub, "MarketSettled");
                OnDecide(t);
            }
        }

        void ScheduleMerchantReplacement()
        {
            merchantFleet.AdvanceTo(now);
            if (merchantFleet.ReplacementMinute.HasValue)
                queue.Schedule(merchantFleet.ReplacementMinute.Value, SimEventKind.MerchantRouteStep);
        }

        void OnMarketRetry(Trainer trainer)
        {
            if (trainer.State != TrainerState.WaitingForMarket) return;
            // At an exact deadline, a retry event may have been queued before the replacement route event.
            // Materialize the due replacement here before applying the wait limit.
            if (merchantFleet.Current.State == MerchantState.Bankrupt &&
                merchantFleet.ReplacementMinute.HasValue && merchantFleet.ReplacementMinute.Value <= now &&
                merchantFleet.AdvanceTo(now))
            {
                RaiseMerchantState();
                WakeWaitingMarketTrainers();
                if (trainer.State == TrainerState.WaitingForMarket)
                    ScheduleOrEndMarketWait(trainer);
                return;
            }
            if (SellBackpack(trainer))
            {
                trainer.Token++;
                SetState(trainer, TrainerState.AtHub, "MarketSettled");
                OnDecide(trainer);
            }
            else if (now - trainer.MarketWaitSinceMinute >= merchantFleet.Current.Config.MaximumWaitMinutes)
            {
                trainer.Token++;
                SetState(trainer, TrainerState.AtHub, "MarketWaitLimit");
                OnDecide(trainer); // Phần chưa bán tiếp tục ở trong balo; không phát sinh giao dịch giả.
            }
            else ScheduleOrEndMarketWait(trainer);
        }

        void ScheduleOrEndMarketWait(Trainer trainer)
        {
            int maximumWait = merchantFleet.Current.Config.MaximumWaitMinutes;
            int elapsed = now - trainer.MarketWaitSinceMinute;
            if (elapsed >= maximumWait)
            {
                trainer.Token++;
                SetState(trainer, TrainerState.AtHub, "MarketWaitLimit");
                OnDecide(trainer); // Phần chưa bán tiếp tục ở trong balo; không phát sinh giao dịch giả.
                return;
            }
            queue.Schedule(now + Math.Min(10, maximumWait - elapsed), SimEventKind.MarketRetry,
                trainer.Id, trainer.Token);
        }

        void RaiseMerchantState()
        {
            Merchant merchant = merchantFleet.Current;
            Raise(new MerchantStateChanged(now, merchant.Id, merchant.State, merchant.Cash, merchant.LoadUnits));
        }
    }
}
