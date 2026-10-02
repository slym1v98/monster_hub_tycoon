using System;
using Game.Domain.Materials;

namespace Game.Domain.Supply
{
    public enum MerchantState { AtTrainerRoute, TravelingToStation, AtStation, TravelingToTrainers, Bankrupt }

    /// <summary>Cấu hình Merchant. Chi phí chuyến và vốn khởi điểm là prototype để cân bằng sau.</summary>
    public sealed class MerchantConfig
    {
        public int CapacityUnits { get; }
        public double MinimumSpread { get; }
        public double MaximumSpread { get; }
        public int LotSizeForMaximumSpread { get; }
        public long OperatingCostPerTrip { get; }
        public long StartingCash { get; }
        public int RouteCycleMinutes { get; }
        public int ReplacementDelayMinutes { get; }
        public int MaximumWaitMinutes { get; }

        public MerchantConfig(int capacityUnits, double minimumSpread, double maximumSpread,
            int lotSizeForMaximumSpread, long operatingCostPerTrip = 25, long startingCash = 10000,
            int routeCycleMinutes = 180, int replacementDelayMinutes = 120, int maximumWaitMinutes = 180)
        {
            if (capacityUnits <= 0) throw new ArgumentOutOfRangeException(nameof(capacityUnits));
            if (double.IsNaN(minimumSpread) || minimumSpread < 0 || minimumSpread > 1) throw new ArgumentOutOfRangeException(nameof(minimumSpread));
            if (double.IsNaN(maximumSpread) || maximumSpread < minimumSpread || maximumSpread > 1) throw new ArgumentOutOfRangeException(nameof(maximumSpread));
            if (lotSizeForMaximumSpread < 1) throw new ArgumentOutOfRangeException(nameof(lotSizeForMaximumSpread));
            if (operatingCostPerTrip < 0) throw new ArgumentOutOfRangeException(nameof(operatingCostPerTrip));
            if (startingCash < 0) throw new ArgumentOutOfRangeException(nameof(startingCash));
            if (routeCycleMinutes <= 0) throw new ArgumentOutOfRangeException(nameof(routeCycleMinutes));
            if (replacementDelayMinutes < 0) throw new ArgumentOutOfRangeException(nameof(replacementDelayMinutes));
            if (maximumWaitMinutes <= 0) throw new ArgumentOutOfRangeException(nameof(maximumWaitMinutes));
            CapacityUnits = capacityUnits; MinimumSpread = minimumSpread; MaximumSpread = maximumSpread;
            LotSizeForMaximumSpread = lotSizeForMaximumSpread; OperatingCostPerTrip = operatingCostPerTrip;
            StartingCash = startingCash; RouteCycleMinutes = routeCycleMinutes;
            ReplacementDelayMinutes = replacementDelayMinutes; MaximumWaitMinutes = maximumWaitMinutes;
        }

        public static MerchantConfig Prototype => new MerchantConfig(1000, 0.05, 0.10, 100);
    }

    /// <summary>Merchant lưu động có vốn và sức chứa hữu hạn; giao dịch chỉ diễn ra tại đúng điểm tuyến.</summary>
    public sealed class Merchant
    {
        public string Id { get; }
        public long Cash { get; private set; }
        public MerchantState State { get; private set; }
        public Inventory Goods { get; } = new Inventory();
        public MerchantConfig Config { get; }
        public int LoadUnits => Goods.TotalAvailableUnits;
        public bool IsBankrupt => State == MerchantState.Bankrupt;
        private readonly MoneyLedger ledger;

        public Merchant(string id, long startingCash, MerchantConfig config, MoneyLedger ledger)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Thiếu mã Merchant.", nameof(id));
            if (startingCash < 0) throw new ArgumentOutOfRangeException(nameof(startingCash));
            Id = id; Cash = startingCash; Config = config ?? throw new ArgumentNullException(nameof(config));
            this.ledger = ledger ?? throw new ArgumentNullException(nameof(ledger));
            State = MerchantState.AtTrainerRoute;
        }

        public SaleBreakdown BuyFromTrainer(string trainerAccount, MaterialId material, int offeredUnits,
            long stationReferencePrice, double taxRate)
        {
            EnsureState(MerchantState.AtTrainerRoute);
            if (string.IsNullOrWhiteSpace(trainerAccount)) throw new ArgumentException("Thiếu tài khoản Trainer.", nameof(trainerAccount));
            if (offeredUnits < 0) throw new ArgumentOutOfRangeException(nameof(offeredUnits));
            if (stationReferencePrice < 0) throw new ArgumentOutOfRangeException(nameof(stationReferencePrice));
            ValidateTax(taxRate);
            if (offeredUnits == 0) return new SaleBreakdown(0, 0, 0, 0, 0, 0);
            var space = Config.CapacityUnits - LoadUnits;
            var quantity = MaxAffordablePurchase(Math.Min(offeredUnits, space), stationReferencePrice);
            if (quantity <= 0) return new SaleBreakdown(0, 0, offeredUnits, 0, 0, 0);
            var spread = BuyDiscountForLot(quantity);
            var unitPrice = QuoteUnitPrice(stationReferencePrice, spread, buying: true);
            var unsold = offeredUnits - quantity;
            if (quantity == 0) return new SaleBreakdown(0, 0, unsold, 0, 0, 0);
            var gross = checked(unitPrice * quantity);
            var tax = CalculateTax(gross, taxRate);
            var net = checked(gross - tax);
            var item = new InventoryItem(material);
            if (Goods.Get(item).Available > int.MaxValue - quantity) throw new OverflowException("Kho Merchant vượt giới hạn.");
            var newCash = checked(Cash - gross);
            ledger.Record(Account, trainerAccount, "hub:treasury", gross, tax, "merchant material purchase");
            Goods.Add(item, quantity);
            Cash = newCash;
            return new SaleBreakdown(0, quantity, unsold, gross, tax, net);
        }

        public SaleBreakdown SellToStation(Station station, MaterialId material, BuyRequest request)
        {
            EnsureState(MerchantState.AtStation);
            if (station == null) throw new ArgumentNullException(nameof(station));
            if (request == null) throw new ArgumentNullException(nameof(request));
            var item = new InventoryItem(material);
            var available = Goods.Get(item).Available;
            if (available == 0) return new SaleBreakdown(0, 0, 0, 0, 0, 0);
            var stationDeficit = request.Deficit(station.Stock.Get(item).Available);
            var offered = MaxAffordableSale(Math.Min(available, stationDeficit), request.BidPrice, station.Treasury, long.MaxValue - Cash);
            if (offered == 0) return new SaleBreakdown(0, 0, available, 0, 0, 0);
            var spread = SellMarkupForLot(offered);
            var result = station.BuyFromMerchant(Account, material, offered, request, spread);
            if (result.StationUnits > 0)
            {
                Goods.Remove(item, result.StationUnits);
                Cash = checked(Cash + result.Gross);
            }
            return new SaleBreakdown(result.StationUnits, 0, available - result.StationUnits,
                result.Gross, result.Tax, result.NetToSeller);
        }

        public void DepartForStation()
        { EnsureState(MerchantState.AtTrainerRoute); State = MerchantState.TravelingToStation; }

        public bool ArriveAtStation()
        { EnsureState(MerchantState.TravelingToStation); State = MerchantState.AtStation; return ChargeTripCost(); }

        public void DepartForTrainers()
        { EnsureState(MerchantState.AtStation); State = MerchantState.TravelingToTrainers; }

        public bool ArriveAtTrainerRoute()
        { EnsureState(MerchantState.TravelingToTrainers); State = MerchantState.AtTrainerRoute; return ChargeTripCost(); }

        private bool ChargeTripCost()
        {
            var cost = Config.OperatingCostPerTrip;
            if (Cash < cost)
            {
                if (Cash > 0) ledger.Record(Account, "world:merchant-operating-cost", "world:tax-sink", Cash, 0, "merchant final operating payment");
                Cash = 0; State = MerchantState.Bankrupt; return false;
            }
            if (cost > 0)
            {
                ledger.Record(Account, "world:merchant-operating-cost", "world:tax-sink", cost, 0, "merchant route cost");
                Cash -= cost;
            }
            return true;
        }

        private double Spread(int quantity)
        {
            if (Config.LotSizeForMaximumSpread <= 1) return Config.MaximumSpread;
            var progress = Math.Min(1.0, Math.Max(0.0, (quantity - 1.0) / (Config.LotSizeForMaximumSpread - 1.0)));
            return Config.MinimumSpread + (Config.MaximumSpread - Config.MinimumSpread) * progress;
        }

        public double BuyDiscountForLot(int quantity)
        { if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity)); return Spread(quantity); }

        public double SellMarkupForLot(int quantity)
        { if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity)); return Spread(quantity); }

        private int MaxAffordablePurchase(int maximum, long referencePrice)
        {
            var low = 0;
            var high = maximum;
            while (low < high)
            {
                var candidate = low + (high - low + 1) / 2;
                var unitPrice = QuoteUnitPrice(referencePrice, BuyDiscountForLot(candidate), buying: true);
                if ((decimal)unitPrice * candidate <= Cash) low = candidate;
                else high = candidate - 1;
            }
            return low;
        }

        private int MaxAffordableSale(int maximum, long referencePrice, long stationCash, long cashHeadroom)
        {
            var affordable = Math.Min(stationCash, cashHeadroom);
            var low = 0;
            var high = maximum;
            while (low < high)
            {
                var candidate = low + (high - low + 1) / 2;
                var unitPrice = QuoteUnitPrice(referencePrice, SellMarkupForLot(candidate), buying: false);
                if ((decimal)unitPrice * candidate <= affordable) low = candidate;
                else high = candidate - 1;
            }
            return low;
        }

        private static long QuoteUnitPrice(long referencePrice, double spread, bool buying)
        {
            var factor = buying ? 1m - (decimal)spread : 1m + (decimal)spread;
            return decimal.ToInt64(decimal.Floor((decimal)referencePrice * factor));
        }

        private static long CalculateTax(long gross, double taxRate)
            => decimal.ToInt64(decimal.Round((decimal)gross * (decimal)taxRate, 0, MidpointRounding.AwayFromZero));
        private static void ValidateTax(double rate)
        { if (double.IsNaN(rate) || rate < 0 || rate > 1) throw new ArgumentOutOfRangeException(nameof(rate)); }
        private string Account => "merchant:" + Id;
        private void EnsureState(MerchantState expected)
        { if (State != expected) throw new InvalidOperationException($"Merchant phải ở trạng thái {expected}."); }
    }
}
