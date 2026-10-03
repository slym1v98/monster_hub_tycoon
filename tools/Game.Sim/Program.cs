using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Game.Domain;
using Game.Domain.Materials;
using Game.Domain.Production;

// Console runner mô phỏng cân bằng. Chạy: dotnet run --project tools/Game.Sim [core|market|ladders|stock|monster|expedition]
static class Program
{
    static double[] Geo(double a, double r, int n) => Enumerable.Range(0, n).Select(i => a * Math.Pow(r, i)).ToArray();

    // Chi phí kỳ vọng của các thang nâng cấp (Nâng Sao, Tinh Luyện, tăng tư chất).
    static void Ladders()
    {
        const double costBase = 100, dailyGross = 400;   // Trainer Common: 400 Gold/ngày
        void Row(string name, UpgradeLadder l)
        {
            double e = l.ExpectedCost();
            Console.WriteLine($"{name,-34} chi phí kỳ vọng {e,10:F0} Gold = {e / costBase,7:F0} x CostBase = {e / dailyGross,6:F1} ngày thu nhập Common");
        }
        Console.WriteLine("# Thang nâng cấp (giá trị khởi điểm)");
        var star = new UpgradeLadder(new[] { 0.90, 0.75, 0.55, 0.35, 0.20 }, Geo(300, 1.8, 5).Select(c => c + 100).ToArray(), new[] { 0, 0, 1, 1, 1 });
        Row("Nâng Sao 0->5 (rớt 1 sao từ bước 3)", star);
        var starSafe = new UpgradeLadder(star.Success, star.Cost, new[] { 0, 0, 0, 0, 0 });
        Row("Nâng Sao 0->5 (không rớt sao)", starSafe);
        var refine = new UpgradeLadder(new[] { 0.70, 0.50, 0.30, 0.15 }, Geo(1000, 2.5, 4), new[] { 0, 0, 0, 0 });
        Row("Tinh Luyện Normal->Mythic", refine);
        var rarityUp = new UpgradeLadder(new[] { 0.60, 0.40, 0.25, 0.10 }, Geo(2000, 3.0, 4), new[] { 0, 0, 0, 0 });
        Row("Tăng tư chất Common->Ultimate", rarityUp);
        Console.WriteLine("\n# Từng bước: chi phí kỳ vọng để qua bước i (Gold)");
        foreach (var (name, l) in new[] { ("Nâng Sao", star), ("Tinh Luyện", refine), ("Tư chất", rarityUp) })
        {
            var parts = Enumerable.Range(0, l.Steps).Select(i => (l.Cost[i] / l.Success[i]).ToString("F0"));
            Console.WriteLine($"{name,-10}: " + string.Join(" | ", parts));
        }
    }

    // Tần suất sự kiện cổ phiếu trong 360 ngày.
    static void Stock()

    {
        Console.WriteLine("# Cổ phiếu: tần suất sự kiện trong 360 ngày (200 lần chạy)");
        Console.WriteLine("Biến động/ngày  hoảng loạn (giảm >=10% trong 3 ngày)/năm  sập (giảm >=20% trong 15 ngày)/năm  pump (tăng >=20% trong 15 ngày)/năm");
        foreach (var vol in new[] { 0.01, 0.02, 0.03, 0.05 })
        {
            double panic = 0, crash = 0, pump = 0; int runs = 200;
            for (int r = 0; r < runs; r++)
            {
                var m = new StockMarket(r) { DailyVolatility = vol };
                var path = new double[361]; path[0] = m.Price;
                for (int d = 1; d <= 360; d++) path[d] = m.StepDay();
                bool inPanic = false, inCrash = false, inPump = false;
                for (int d = 3; d <= 360; d++)
                {
                    bool pn = path[d] <= 0.90 * path[d - 3];
                    if (pn && !inPanic) panic++; inPanic = pn;
                }
                for (int d = 15; d <= 360; d++)
                {
                    bool cr = path[d] <= 0.80 * path[d - 15], pu = path[d] >= 1.20 * path[d - 15];
                    if (cr && !inCrash) crash++; inCrash = cr;
                    if (pu && !inPump) pump++; inPump = pu;
                }
            }
            Console.WriteLine($"{vol,13:P0}  {panic / runs,40:F1}  {crash / runs,36:F1}  {pump / runs,34:F1}");
        }
    }

    // Kịch bản core: 10 Trainer Common, 3 tháng, báo cáo dòng tiền và cách Trainer dùng thời gian.
    static void Core()
    {
        const int months = 3, trainerCount = 10;
        var sw = Stopwatch.StartNew();
        var config = new SimConfig { TrainerCount = trainerCount };
        var world = new HubWorld(config, 2026);
        ConfigureSupplyScenario(world, config, trainerCount);
        var supply = new SupplyRunMetrics(world, config);

        // Thời gian Trainer ở từng trạng thái: cộng dồn khi trạng thái đổi.
        var minutesInState = new Dictionary<TrainerState, long>();
        foreach (TrainerState s in Enum.GetValues(typeof(TrainerState))) minutesInState[s] = 0;
        var lastChange = new int[trainerCount];
        for (int i = 0; i < trainerCount; i++) lastChange[i] = world.Now.TotalMinutes;
        int donations = 0;
        world.EventRaised += e =>
        {
            if (e is TrainerStateChanged s) { minutesInState[s.From] += s.Minute - lastChange[s.TrainerId]; lastChange[s.TrainerId] = s.Minute; }
            else if (e is DonationReceived d && d.Source == "Patron") donations++;
        };

        Console.WriteLine("# Core: 10 Trainer Common, 3 tháng in-game (tham số chuỗi cung ứng Prototype/TBD)");
        Console.WriteLine("Tháng  Kho bạc trước Payday  Lợi nhuận tháng  Quỹ lương   Trả được  Đình công  Gold TB Trainer");
        long afterPrevious = config.StartTreasury;   // Kho bạc sau Payday trước (tháng 1: số vốn khởi điểm)
        for (int m = 1; m <= months; m++)
        {
            PaydayOutcome o = null;
            long before = world.Treasury;
            for (int day = 0; day < SimClock.DaysPerMonth; day++)
            {
                PaydayOutcome resolved = AdvanceAcrossPaydays(world, SimClock.MinutesPerDay, balance => before = balance);
                if (resolved != null) o = resolved;
                supply.SampleStockoutDay(world);
            }
            long profit = before - afterPrevious;   // lợi nhuận HUB trong tháng, trước khi trả lương
            if (o == null) throw new InvalidOperationException("Kịch bản core không đi qua Payday trong tháng mô phỏng.");
            afterPrevious = world.Treasury;
            double avgGold = 0;
            foreach (TrainerView t in world.Trainers) avgGold += t.Gold;
            avgGold /= trainerCount;
            Console.WriteLine($"{m,5}  {before,20}  {profit,15}  {o.TotalDue,9}  {o.PaidRatio,8:P0}  {(o.StrikeStarted ? "có" : "không"),9}  {avgGold,14:F0}");
        }

        int end = world.Now.TotalMinutes;
        for (int i = 0; i < trainerCount; i++) minutesInState[world.Trainers[i].State] += end - lastChange[i];
        long total = 0; foreach (long v in minutesInState.Values) total += v;
        double Share(params TrainerState[] states) { long s = 0; foreach (var st in states) s += minutesInState[st]; return (double)s / total; }

        Console.WriteLine("\n# Thời gian của Trainer");
        Console.WriteLine($"Farm {Share(TrainerState.Farming):P1} | Đi lại {Share(TrainerState.Traveling, TrainerState.Returning):P1} | " +
                          $"Xếp hàng {Share(TrainerState.Queued):P1} | Dịch vụ {Share(TrainerState.InService):P1} | " +
                          $"Chờ tiền {Share(TrainerState.WaitingForMoney):P1} | Chờ Merchant {Share(TrainerState.WaitingForMarket):P1} | " +
                          $"Rảnh ở HUB {Share(TrainerState.AtHub):P1}");

        Console.WriteLine("\n# Công trình");
        foreach (BuildingView b in world.Buildings)
            Console.WriteLine($"{b.Kind,-11} cấp {b.Level} {b.Slots,2} chỗ, hàng đợi dài nhất {b.MaxQueueLength}");
        Console.WriteLine($"\nTổng tài donate {donations} lần; chờ tiền dài nhất {world.MaxMoneyWaitMinutes / 60.0:F1} giờ in-game.");
        supply.Print(world, config, "Core", sw.ElapsedMilliseconds);
        world.ValidateInvariants();
        sw.Stop();
        Console.WriteLine($"Thời gian chạy: {sw.ElapsedMilliseconds} ms (mục tiêu dưới 1000 ms)");
    }

    static void ConfigureSupplyScenario(HubWorld world, SimConfig config, int trainerCount)
    {
        // FarmResult hiện chỉ sinh ore_tier_1. Mục tiêu lấy từ tổng sức chứa balo để
        // không đưa thêm một ngưỡng cân bằng ngoài các tham số prototype hiện có.
        int target = checked(config.BackpackCapacity * trainerCount);
        var buy = world.SetBuyRequest(MaterialId.For(MaterialFamily.Ore, 1).Value, target, config.MaterialPrice);
        var production = world.SetProductionTarget("blank_ore_tier_1", target);
        var tax = world.SetMarketTaxRate(config.TaxRate);
        if (!buy.Ok || !production.Ok || !tax.Ok)
            throw new InvalidOperationException("Không cấu hình được kịch bản chuỗi cung ứng: " + buy.Reason + " " + production.Reason + " " + tax.Reason);
    }

    static void Market()
    {
        Console.WriteLine("# Kịch bản thị trường (mọi mục tiêu/giá là Prototype/TBD, không cân bằng)");
        Console.WriteLine("Trainer | Ngày | Gold ngoài vào | Bán trực tiếp Trạm (gross/net) | Bán Merchant (gross/net) | Thuế | Tồn kho | Merchant cash | Phá sản | SX input/output | Thiếu đầu vào ngày | Lead time job | Lệch đối soát | Runtime");
        foreach (int trainers in new[] { 10, 30 })
        foreach (int days in new[] { 30, 90 })
            RunMarketScenario(trainers, days);
    }

    static void RunMarketScenario(int trainerCount, int days)
    {
        var stopwatch = Stopwatch.StartNew();
        var config = new SimConfig { TrainerCount = trainerCount };
        var world = new HubWorld(config, 2026);
        ConfigureSupplyScenario(world, config, trainerCount);
        var metrics = new SupplyRunMetrics(world, config);
        int minutes = checked(days * SimClock.MinutesPerDay);
        while (minutes > 0)
        {
            int step = Math.Min(minutes, SimClock.MinutesPerDay);
            AdvanceAcrossPaydays(world, step);
            minutes -= step;
            metrics.SampleStockoutDay(world);
        }
        stopwatch.Stop();
        metrics.Print(world, config, $"Market {trainerCount}T/{days}d", stopwatch.ElapsedMilliseconds);
        world.ValidateInvariants();
    }

    static PaydayOutcome AdvanceAcrossPaydays(HubWorld world, int minutes, Action<long> beforePayday = null)
    {
        int remaining = minutes;
        PaydayOutcome resolved = null;
        while (remaining > 0)
        {
            RunResult result = world.RunFor(remaining);
            remaining = result.RemainingMinutes;
            if (result.Stop == StopReason.PaydayDue)
            {
                beforePayday?.Invoke(world.Treasury);
                resolved = world.ResolvePayday();
            }
            else break;
        }
        return resolved;
    }

    sealed class SupplyRunMetrics
    {
        readonly Dictionary<int, long> trainerStartGold;
        readonly Dictionary<long, int> jobStarts = new Dictionary<long, int>();
        readonly Dictionary<string, int> demandSince = new Dictionary<string, int>(StringComparer.Ordinal);
        readonly Dictionary<string, int> closedDemandSinceJob = new Dictionary<string, int>(StringComparer.Ordinal);
        readonly Dictionary<string, int> priorStock = new Dictionary<string, int>(StringComparer.Ordinal);
        readonly Dictionary<string, long> productionOutput = new Dictionary<string, long>(StringComparer.Ordinal);
        readonly Dictionary<string, long> productionInput = new Dictionary<string, long>(StringComparer.Ordinal);
        long stationGross, stationNet, merchantGross, merchantNet, tax;
        long donations, servicesPaid, wagesPaid;
        long tradeTreasuryEvents;
        int bankruptcies, completedJobs;
        long totalJobLeadMinutes;
        int stockoutMaterialDays, productionInputShortageDays;
        long totalMarketWaitMinutes, maximumMarketWaitMinutes, restockDemandMinutes, demandToJobMinutes;
        int marketWaitEpisodes, dependentJobsAfterRestock;
        readonly Dictionary<int, int> marketWaitSince = new Dictionary<int, int>();

        public SupplyRunMetrics(HubWorld world, SimConfig config)
        {
            trainerStartGold = world.Trainers.ToDictionary(x => x.Id, x => x.Gold);
            foreach (var demand in world.ProductionRestockDemands)
                demandSince[demand.Item.Value] = world.Now.TotalMinutes;
            world.EventRaised += OnEvent;
        }

        void OnEvent(IDomainEvent e)
        {
            if (e is MaterialTradeSettled trade)
            {
                tax = checked(tax + trade.Tax);
                if (trade.TrainerId >= 0 && trade.Channel == "Station")
                { stationGross = checked(stationGross + trade.Gross); stationNet = checked(stationNet + trade.NetToSeller); }
                if (trade.TrainerId >= 0 && trade.Channel == "Merchant")
                { merchantGross = checked(merchantGross + trade.Gross); merchantNet = checked(merchantNet + trade.NetToSeller); }
            }
            else if (e is DonationReceived donation) donations = checked(donations + donation.Gold);
            else if (e is ServiceUsed service) servicesPaid = checked(servicesPaid + service.Paid);
            else if (e is TrainerStateChanged state)
            {
                if (state.To == TrainerState.WaitingForMarket) marketWaitSince[state.TrainerId] = state.Minute;
                if (state.From == TrainerState.WaitingForMarket && marketWaitSince.TryGetValue(state.TrainerId, out int entered))
                {
                    long waited = state.Minute - entered;
                    totalMarketWaitMinutes += waited;
                    maximumMarketWaitMinutes = Math.Max(maximumMarketWaitMinutes, waited);
                    marketWaitEpisodes++;
                    marketWaitSince.Remove(state.TrainerId);
                }
            }
            else if (e is TreasuryChanged treasuryChange &&
                     (treasuryChange.Reason == "TradeTax" || treasuryChange.Reason == "MerchantPurchase" ||
                      treasuryChange.Reason == "ProductionCost"))
                tradeTreasuryEvents = checked(tradeTreasuryEvents + treasuryChange.Delta);
            else if (e is MerchantStateChanged merchant && merchant.State == Game.Domain.Supply.MerchantState.Bankrupt)
                bankruptcies++;
            else if (e is ProductionJobChanged job)
            {
                if (job.State == "Running") jobStarts[job.JobId] = e.Minute;
                else if (job.State == "Completed")
                {
                    completedJobs++;
                    if (jobStarts.TryGetValue(job.JobId, out int start)) totalJobLeadMinutes += e.Minute - start;
                    var recipe = MaterialCatalog.Default.Recipes.FirstOrDefault(x => x.Id.Value == job.RecipeId);
                    if (recipe != null)
                    {
                        foreach (var input in recipe.Inputs)
                        {
                            string id = input.Material.HasValue ? "material:" + input.Material.Value.Value : "product:" + input.Product.Value.Value;
                            productionInput[id] = productionInput.TryGetValue(id, out long amount) ? amount + input.Quantity : input.Quantity;
                        }
                    }
                }
                if (job.State == "Running")
                {
                    var recipe = MaterialCatalog.Default.Recipes.FirstOrDefault(x => x.Id.Value == job.RecipeId);
                    if (recipe != null)
                    foreach (var input in recipe.Inputs)
                    {
                        string id = input.Material.HasValue ? "material:" + input.Material.Value.Value : "product:" + input.Product.Value.Value;
                        if (closedDemandSinceJob.TryGetValue(id, out int since))
                        {
                            demandToJobMinutes += e.Minute - since;
                            dependentJobsAfterRestock++;
                            closedDemandSinceJob.Remove(id);
                        }
                    }
                }
            }
            else if (e is ProductionRestockDemandChanged demand)
            {
                if (demand.Quantity > 0)
                {
                    if (!demandSince.ContainsKey(demand.ItemId)) demandSince[demand.ItemId] = e.Minute;
                }
                else if (demandSince.TryGetValue(demand.ItemId, out int since))
                {
                    restockDemandMinutes += e.Minute - since;
                    closedDemandSinceJob[demand.ItemId] = since;
                    demandSince.Remove(demand.ItemId);
                }
            }
            else if (e is SupplyStockChanged stock && stock.ItemId.StartsWith("product:", StringComparison.Ordinal))
            {
                int current = stock.Balance.Available;
                priorStock.TryGetValue(stock.ItemId, out int previous);
                if (current > previous)
                    productionOutput[stock.ItemId] = productionOutput.TryGetValue(stock.ItemId, out long amount)
                        ? amount + current - previous : current - previous;
                priorStock[stock.ItemId] = current;
            }
            else if (e is PaydayResolved payday) wagesPaid = checked(wagesPaid + payday.Outcome.TotalPaid);
        }

        public void SampleStockoutDay(HubWorld world)
        {
            foreach (var request in world.BuyRequests.Where(x => x.Enabled && x.TargetStock > 0))
            {
                string id = "material:" + request.MaterialId;
                var stock = world.SupplyStocks.FirstOrDefault(x => x.ItemId == id);
                long covered = stock == null ? 0 : (long)stock.Available + stock.Reserved + stock.InProduction;
                if (covered < request.TargetStock) stockoutMaterialDays++;
            }
            if (world.ProductionRestockDemands.Count > 0) productionInputShortageDays++;
        }

        public void Print(HubWorld world, SimConfig config, string name, long? measuredRuntimeMs = null)
        {
            long finalTrainerGold = world.Trainers.Sum(x => x.Gold);
            long initialGold = trainerStartGold.Values.Sum();
            long trainerSalesNet = checked(stationNet + merchantNet);
            // Wallet reconciliation isolates the Gold created by farm and Patron from internal transfers.
            long farmGold = checked(finalTrainerGold - initialGold - donations - trainerSalesNet - wagesPaid + servicesPaid);
            long externalGold = checked(farmGold + donations);
            long treasuryMovementFromLedger = 0;
            foreach (var tx in world.SupplyTransactions)
            {
                if (tx.Payer == "hub:treasury") treasuryMovementFromLedger = checked(treasuryMovementFromLedger - tx.Gross);
                if (tx.Payee == "hub:treasury") treasuryMovementFromLedger = checked(treasuryMovementFromLedger + tx.Gross - tx.Tax);
                if (tx.TaxAccount == "hub:treasury") treasuryMovementFromLedger = checked(treasuryMovementFromLedger + tx.Tax);
            }
            long treasuryDifference = tradeTreasuryEvents - treasuryMovementFromLedger;
            long available = world.SupplyStocks.Sum(x => (long)x.Available);
            long reserved = world.SupplyStocks.Sum(x => (long)x.Reserved);
            long inProduction = world.SupplyStocks.Sum(x => (long)x.InProduction);
            long inputs = productionInput.Values.Sum();
            long outputs = productionOutput.Values.Sum();
            long runtime = measuredRuntimeMs ?? 0;
            long outstandingRestockMinutes = demandSince.Values.Sum(since => Math.Max(0, world.Now.TotalMinutes - since));
            long outstandingMarketWait = marketWaitSince.Values.Sum(since => Math.Max(0, world.Now.TotalMinutes - since));
            maximumMarketWaitMinutes = Math.Max(maximumMarketWaitMinutes,
                marketWaitSince.Values.Select(since => (long)Math.Max(0, world.Now.TotalMinutes - since)).DefaultIfEmpty(0).Max());

            Console.WriteLine($"\n## {name}: {world.Trainers.Count} Trainer, ngày {world.Now.Day}");
            Console.WriteLine($"Gold ngoài vào: {externalGold} (farm suy ra từ đối soát {farmGold}; donate {donations}); bán Trainer gross/net: Trạm {stationGross}/{stationNet}, Merchant {merchantGross}/{merchantNet}; thuế supply {tax}.");
            int backpackUnits = world.Trainers.Sum(x => x.BackpackUnits);
            Console.WriteLine($"Kho cuối: available {available}, reserved {reserved}, in-production {inProduction}; balo còn {backpackUnits} units; {stockoutMaterialDays} ngày-nguyên-liệu có tồn dưới target (proxy, không khẳng định quầy tiêu hao hết); ngày có demand thiếu đầu vào {productionInputShortageDays}, tổng demand-item thiếu {restockDemandMinutes + outstandingRestockMinutes} phút.");
            Console.WriteLine($"Merchant cuối: cash {world.Merchant?.Cash ?? 0}, hàng {world.Merchant?.LoadUnits ?? 0}/{world.Merchant?.CapacityUnits ?? 0}, phá sản {bankruptcies}; jobs xong {completedJobs}, input tiêu thụ {inputs}, output tăng ròng qua tồn kho {outputs}.");
            Console.WriteLine($"WaitingForMarket: {marketWaitEpisodes} lượt kết thúc, {marketWaitSince.Count} lượt còn chờ cuối kỳ, tổng {totalMarketWaitMinutes + outstandingMarketWait} Trainer-phút, tối đa quan sát {maximumMarketWaitMinutes} phút; luật chờ có giới hạn từ MerchantConfig (giá trị Prototype/TBD). Job start→finish TB: {(completedJobs == 0 ? "n/a" : (totalJobLeadMinutes / (double)completedJobs).ToString("F1") + " phút")}; demand→job bắt đầu TB: {(dependentJobsAfterRestock == 0 ? "n/a" : (demandToJobMinutes / (double)dependentJobsAfterRestock).ToString("F1") + " phút")} ({dependentJobsAfterRestock} đầu vào); đây là proxy, chưa có hàng đợi job riêng.");
            Console.WriteLine($"Supply treasury reconciliation: event delta {tradeTreasuryEvents}, ledger-derived delta {treasuryMovementFromLedger}, chênh {treasuryDifference}; runtime {runtime} ms.");
            if (measuredRuntimeMs.HasValue)
                Console.WriteLine($"CSV,{name},{world.Trainers.Count},{world.Now.Day},{externalGold},{stationGross},{stationNet},{merchantGross},{merchantNet},{tax},{available},{reserved},{inProduction},{world.Merchant?.Cash ?? 0},{bankruptcies},{inputs},{outputs},{stockoutMaterialDays},{productionInputShortageDays},{(completedJobs == 0 ? -1 : totalJobLeadMinutes / (double)completedJobs):F1},{treasuryDifference},{runtime}");
        }
    }

    static void Main(string[] args)
    {
        string mode = args.Length > 0 ? args[0] : "core";
        if (mode == "monster" || mode == "expedition") { MonsterScenarios.Run(mode, Console.Out); return; }
        if (mode == "finance") { FinanceScenarios.Run(Console.Out); return; }
        if (mode == "progression") { ProgressionEventScenarios.Run(Console.Out); return; }
        if (mode == "gear") { GearScenarios.Run(Console.Out); return; }
        if (mode == "core") { Core(); return; }
        if (mode == "market") { Market(); return; }
        if (mode == "ladders") { Ladders(); return; }
        if (mode == "stock") { Stock(); return; }
        Console.WriteLine("Dùng: dotnet run --project tools/Game.Sim [core|market|ladders|stock|monster|expedition|gear|finance|progression]");
    }
}
