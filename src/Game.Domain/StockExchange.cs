using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Domain
{
    /// <summary>Balance settings for HUB Street stock exchange (GDD 02 §3 and GDD 13 §§3, 12).</summary>
    public sealed class StockExchangeConfig
    {
        public static StockExchangeConfig Prototype { get; } = new StockExchangeConfig();
        public double DailyVolatility { get; }
        public double TrafficSensitivity { get; }
        public double OrderImpactCoefficient { get; }
        public double DividendRate { get; }
        public int DividendPeriodDays { get; }
        public double TargetAnnualYield { get; }
        public double RealizedProfitTaxRate { get; }
        public double TransactionFeeRate { get; }
        public double OpeningSharePrice { get; }
        public double HubLockedShareFraction { get; }
        public int MinimumIpoFacilityLevel { get; }
        public int ListingsPerExchangeLevel { get; }
        public double CommonAiBuyFraction { get; }
        public double UltimateAiBuyFraction { get; }
        public double CapitalistAiTradeFraction { get; }
        public double TimidAiPanicDrop3Day { get; }
        public IReadOnlyList<BalanceParameter> BalanceParameters { get; }

        public StockExchangeConfig(double dailyVolatility = 0.03, double trafficSensitivity = 0.5,
            double orderImpactCoefficient = 0.05, double dividendRate = 0.10, int dividendPeriodDays = 15,
            double targetAnnualYield = 0.08, double realizedProfitTaxRate = 0.30,
            double transactionFeeRate = 0.01, double openingSharePrice = 100,
            double hubLockedShareFraction = 0.51, int minimumIpoFacilityLevel = 5,
            int listingsPerExchangeLevel = 1, double commonAiBuyFraction = 1,
            double ultimateAiBuyFraction = 1, double capitalistAiTradeFraction = 0.5,
            double timidAiPanicDrop3Day = 0.10)
        {
            UnitInterval(dailyVolatility, nameof(dailyVolatility));
            NonNegative(trafficSensitivity, nameof(trafficSensitivity));
            NonNegative(orderImpactCoefficient, nameof(orderImpactCoefficient));
            UnitInterval(dividendRate, nameof(dividendRate));
            if (dividendPeriodDays <= 0) throw new ArgumentOutOfRangeException(nameof(dividendPeriodDays));
            Positive(targetAnnualYield, nameof(targetAnnualYield));
            UnitInterval(realizedProfitTaxRate, nameof(realizedProfitTaxRate));
            UnitInterval(transactionFeeRate, nameof(transactionFeeRate));
            Positive(openingSharePrice, nameof(openingSharePrice));
            if (hubLockedShareFraction <= 0 || hubLockedShareFraction >= 1) throw new ArgumentOutOfRangeException(nameof(hubLockedShareFraction));
            if (minimumIpoFacilityLevel <= 0 || listingsPerExchangeLevel <= 0) throw new ArgumentOutOfRangeException(nameof(minimumIpoFacilityLevel));
            UnitInterval(commonAiBuyFraction, nameof(commonAiBuyFraction));
            UnitInterval(ultimateAiBuyFraction, nameof(ultimateAiBuyFraction));
            UnitInterval(capitalistAiTradeFraction, nameof(capitalistAiTradeFraction));
            UnitInterval(timidAiPanicDrop3Day, nameof(timidAiPanicDrop3Day));
            DailyVolatility = dailyVolatility; TrafficSensitivity = trafficSensitivity;
            OrderImpactCoefficient = orderImpactCoefficient; DividendRate = dividendRate;
            DividendPeriodDays = dividendPeriodDays; TargetAnnualYield = targetAnnualYield;
            RealizedProfitTaxRate = realizedProfitTaxRate; TransactionFeeRate = transactionFeeRate;
            OpeningSharePrice = openingSharePrice; HubLockedShareFraction = hubLockedShareFraction;
            MinimumIpoFacilityLevel = minimumIpoFacilityLevel; ListingsPerExchangeLevel = listingsPerExchangeLevel;
            CommonAiBuyFraction = commonAiBuyFraction; UltimateAiBuyFraction = ultimateAiBuyFraction;
            CapitalistAiTradeFraction = capitalistAiTradeFraction; TimidAiPanicDrop3Day = timidAiPanicDrop3Day;
            BalanceParameters = Array.AsReadOnly(new[] {
                P("stock.daily_volatility", dailyVolatility, "fraction/day", "docs/designs/02_HUB_Economy_Infrastructure.md §3: random ±3%/day; docs/designs/13_Balance_Parameters.md §3"),
                P("stock.traffic_sensitivity", trafficSensitivity, "fraction per traffic fraction", "docs/designs/02_HUB_Economy_Infrastructure.md §3: 0.5 × Traffic change"),
                P("stock.order_impact_coefficient", orderImpactCoefficient, "fraction per net shares/outstanding", "docs/designs/02_HUB_Economy_Infrastructure.md §3: coefficient k is unspecified; Prototype value"),
                P("stock.dividend_rate", dividendRate, "fraction of 15-day revenue", "docs/designs/13_Balance_Parameters.md §3: 10% every 15 days"),
                P("stock.dividend_period_days", dividendPeriodDays, "days", "docs/designs/13_Balance_Parameters.md §3: every 15 days"),
                P("stock.target_annual_yield", targetAnnualYield, "fraction/year", "docs/designs/13_Balance_Parameters.md §3: IPO target yield 8%/year"),
                P("stock.realized_profit_tax_rate", realizedProfitTaxRate, "fraction of realized gains", "docs/designs/13_Balance_Parameters.md §3: 30%; losses are untaxed"),
                P("stock.transaction_fee_rate", transactionFeeRate, "fraction of trade value", "docs/designs/02_HUB_Economy_Infrastructure.md §3 says each Trainer transaction pays an exchange fee; rate TBD, Prototype value"),
                P("stock.opening_share_price", openingSharePrice, "Gold/share", "Prototype base quote for initial IPO pricing"),
                new BalanceParameter("stock.hub_locked_share_fraction", hubLockedShareFraction, "fraction of issued shares", "Locked", "docs/designs/02_HUB_Economy_Infrastructure.md §3 and docs/designs/13_Balance_Parameters.md §3: HUB holds locked 51%"),
                P("stock.minimum_ipo_facility_level", minimumIpoFacilityLevel, "facility levels", "docs/designs/02_HUB_Economy_Infrastructure.md §3 requires an eligible building level; threshold not specified, Prototype"),
                P("stock.listings_per_exchange_level", listingsPerExchangeLevel, "listings/Exchange level", "docs/designs/02_HUB_Economy_Infrastructure.md §3: Exchange level controls listings; rate not specified, Prototype"),
                P("stock.ai_common_buy_fraction", commonAiBuyFraction, "available float fraction/day", "docs/designs/02_HUB_Economy_Infrastructure.md §3: Common tends to go all-in/borrow; exact fraction is Prototype"),
                P("stock.ai_ultimate_buy_fraction", ultimateAiBuyFraction, "available float fraction/day", "docs/designs/02_HUB_Economy_Infrastructure.md §3: Ultimate prefers blue-chip; exact fraction is Prototype"),
                P("stock.ai_capitalist_trade_fraction", capitalistAiTradeFraction, "held shares fraction/day", "docs/designs/02_HUB_Economy_Infrastructure.md §3: Capitalist day-trades; exact fraction is Prototype"),
                P("stock.ai_timid_panic_drop_3day", timidAiPanicDrop3Day, "fraction decline/3 days", "docs/designs/02_HUB_Economy_Infrastructure.md §3: Timid panic-sells; threshold Prototype")
            });
        }
        static BalanceParameter P(string id, double value, string unit, string source)
            => new BalanceParameter(id, value, unit, "Prototype", source);
        static void UnitInterval(double value, string name)
        { if (double.IsNaN(value) || double.IsInfinity(value) || value < 0 || value > 1) throw new ArgumentOutOfRangeException(name); }
        static void NonNegative(double value, string name)
        { if (double.IsNaN(value) || double.IsInfinity(value) || value < 0) throw new ArgumentOutOfRangeException(name); }
        static void Positive(double value, string name)
        { if (double.IsNaN(value) || double.IsInfinity(value) || value <= 0) throw new ArgumentOutOfRangeException(name); }
    }

    public sealed record StockCompanyView(string CompanyId, double Price, long TotalShares, long HubLockedShares,
        long DirectorFloatShares, long AvailableFloatShares, long Revenue15Days, double Traffic, double ThreeDayReturn);
    public sealed record StockHoldingView(string CompanyId, int ShareholderId, long Shares, double AverageCost);
    public sealed record StockTradeQuote(long Gross, long Fee, long RealizedProfit, long Tax, long SellerNet, long BuyerTotal);
    public sealed record StockDailyClose(string CompanyId, double OldPrice, double NewPrice, double TrafficChange,
        double NetOrderFraction, long Revenue15Days, long DividendPool);
    public sealed record StockDividend(int TrainerId, string CompanyId, long Shares, long Gross);

    /// <summary>
    /// Deterministic share registry, order impact, listing history and 15-day dividend calculation.
    /// Cash transfer and events are committed by HubWorld only after validation.
    /// </summary>
    public sealed class StockExchange
    {
        public const int HubLockedShareholderId = -1;
        sealed class Position { public long Shares; public double Cost; }
        sealed class Company
        {
            public string Id;
            public double Price;
            public long TotalShares, LockedShares, AvailableFloat, DirectorFloat, NetOrders, DailyRevenue;
            public double PreviousTraffic = 1;
            public readonly Queue<long> RevenueHistory = new Queue<long>();
            public readonly Queue<double> PriceHistory = new Queue<double>();
            public readonly Dictionary<int, Position> Positions = new Dictionary<int, Position>();
        }
        readonly SortedDictionary<string, Company> companies = new SortedDictionary<string, Company>(StringComparer.Ordinal);
        readonly SimRandom random;
        readonly StockExchangeConfig config;
        public StockExchangeConfig Config => config;
        public int ExchangeLevel { get; private set; } = 1;
        public IReadOnlyList<StockCompanyView> Companies => Array.AsReadOnly(companies.Values.Where(c => c.TotalShares > 0).Select(View).ToArray());

        public StockExchange(int seed, StockExchangeConfig config = null)
        { random = new SimRandom(seed); this.config = config ?? StockExchangeConfig.Prototype; }

        public bool SetExchangeLevel(int level)
        {
            if (level < 1 || level > 10 || level < ExchangeLevel) return false;
            ExchangeLevel = level; return true;
        }

        /// <summary>Accumulate paid facility revenue and retain 15 daily samples for Traffic and payouts.</summary>
        public void RecordRevenue(string companyId, long grossRevenue)
        {
            if (string.IsNullOrWhiteSpace(companyId)) throw new ArgumentException("Company ID is required.", nameof(companyId));
            if (grossRevenue < 0) throw new ArgumentOutOfRangeException(nameof(grossRevenue));
            Company company = GetOrCreate(companyId);
            company.DailyRevenue = checked(company.DailyRevenue + grossRevenue);
        }

        public bool TryIpo(string companyId, int facilityLevel, out StockCompanyView listing)
        {
            listing = null;
            if (string.IsNullOrWhiteSpace(companyId) || facilityLevel < config.MinimumIpoFacilityLevel) return false;
            Company company = GetOrCreate(companyId);
            if (company.TotalShares > 0 || ListedCount >= ExchangeLevel * config.ListingsPerExchangeLevel) return false;
            if (company.RevenueHistory.Count < config.DividendPeriodDays) return false;
            long revenue15Days = RevenueHistoryTotal(company);
            if (revenue15Days <= 0) return false;
            double rawShares = FinanceModel.IpoShares(revenue15Days, config.TargetAnnualYield, config.OpeningSharePrice);
            if (double.IsNaN(rawShares) || double.IsInfinity(rawShares) || rawShares > long.MaxValue) return false;
            long totalShares = Math.Max(2, checked((long)Math.Ceiling(rawShares)));
            long locked = Math.Clamp(checked((long)Math.Ceiling(totalShares * config.HubLockedShareFraction)), 1, totalShares - 1);
            company.TotalShares = totalShares; company.LockedShares = locked;
            company.AvailableFloat = totalShares - locked; company.Price = config.OpeningSharePrice;
            listing = View(company); return true;
        }

        public int ListedCount => companies.Values.Count(c => c.TotalShares > 0);
        public bool IsListed(string companyId) => companies.TryGetValue(companyId ?? "", out var c) && c.TotalShares > 0;
        public StockCompanyView GetCompany(string companyId) => companies.TryGetValue(companyId ?? "", out var c) && c.TotalShares > 0 ? View(c) : null;

        public StockTradeQuote QuoteIpoPurchase(string companyId, long shares)
        {
            var c = FindListed(companyId);
            if (shares <= 0 || shares > c.AvailableFloat) return null;
            long gross = Value(c.Price, shares);
            long fee = Fee(gross);
            return new StockTradeQuote(gross, fee, 0, 0, 0, checked(gross + fee));
        }

        public bool BuyIpoShares(int trainerId, string companyId, long shares, out StockTradeQuote quote)
        {
            quote = QuoteIpoPurchase(companyId, shares);
            if (quote == null || trainerId < 0) return false;
            var c = FindListed(companyId);
            c.AvailableFloat -= shares;
            AddShares(c, trainerId, shares, (double)quote.BuyerTotal / shares);
            c.NetOrders = checked(c.NetOrders + shares);
            return true;
        }

        public StockTradeQuote QuoteTransfer(string companyId, int sellerId, int buyerId, long shares)
        {
            var c = FindListed(companyId);
            if (sellerId == buyerId || shares <= 0 || !c.Positions.TryGetValue(sellerId, out var seller) || seller.Shares < shares) return null;
            long gross = Value(c.Price, shares);
            long fee = Fee(gross);
            long profit = checked((long)Math.Floor(gross - seller.Cost * shares));
            long tax = profit > 0 ? checked((long)Math.Ceiling(profit * config.RealizedProfitTaxRate)) : 0;
            long sellerNet = Math.Max(0, gross - tax);
            return new StockTradeQuote(gross, fee, profit, tax, sellerNet, checked(gross + fee));
        }

        public bool TransferShares(string companyId, int sellerId, int buyerId, long shares, out StockTradeQuote quote)
        {
            quote = QuoteTransfer(companyId, sellerId, buyerId, shares);
            if (quote == null) return false;
            var c = FindListed(companyId);
            RemoveShares(c, sellerId, shares);
            AddShares(c, buyerId, shares, (double)quote.BuyerTotal / shares);
            c.NetOrders = checked(c.NetOrders - shares);
            return true;
        }

        public StockTradeQuote QuoteDirectorSale(string companyId, int buyerId, long shares)
        {
            var c = FindListed(companyId);
            if (buyerId < 0 || shares <= 0 || shares > c.DirectorFloat) return null;
            long gross = Value(c.Price, shares);
            long fee = Fee(gross);
            return new StockTradeQuote(gross, fee, 0, 0, 0, checked(gross + fee));
        }

        public bool DirectorSellsFloat(string companyId, int buyerId, long shares, out StockTradeQuote quote)
        {
            quote = QuoteDirectorSale(companyId, buyerId, shares);
            if (quote == null) return false;
            var c = FindListed(companyId);
            c.DirectorFloat -= shares;
            AddShares(c, buyerId, shares, (double)quote.BuyerTotal / shares);
            c.NetOrders = checked(c.NetOrders - shares);
            return true;
        }

        public StockTradeQuote QuoteDirectorPurchase(string companyId, int sellerId, long shares)
        {
            var c = FindListed(companyId);
            if (sellerId < 0 || shares <= 0 || !c.Positions.TryGetValue(sellerId, out var seller) || seller.Shares < shares) return null;
            long gross = Value(c.Price, shares);
            long profit = checked((long)Math.Floor(gross - seller.Cost * shares));
            long tax = profit > 0 ? checked((long)Math.Ceiling(profit * config.RealizedProfitTaxRate)) : 0;
            long fee = Fee(gross);
            return new StockTradeQuote(gross, fee, profit, tax, Math.Max(0, gross - tax - fee), 0);
        }

        public bool DirectorBuysFloat(string companyId, int sellerId, long shares, out StockTradeQuote quote)
        {
            quote = QuoteDirectorPurchase(companyId, sellerId, shares);
            if (quote == null) return false;
            var c = FindListed(companyId);
            RemoveShares(c, sellerId, shares);
            c.DirectorFloat = checked(c.DirectorFloat + shares);
            c.NetOrders = checked(c.NetOrders + shares);
            return true;
        }

        public IReadOnlyList<StockHoldingView> HoldingsFor(int shareholderId)
            => Array.AsReadOnly(companies.Values.Where(c => c.TotalShares > 0 && c.Positions.ContainsKey(shareholderId))
                .Select(c => new StockHoldingView(c.Id, shareholderId, c.Positions[shareholderId].Shares, c.Positions[shareholderId].Cost)).ToArray());

        public void ValidateInvariants()
        {
            foreach (var c in companies.Values)
            {
                if (c.TotalShares == 0)
                {
                    if (c.LockedShares != 0 || c.AvailableFloat != 0 || c.DirectorFloat != 0 || c.Positions.Count != 0)
                        throw new InvalidOperationException($"Unlisted company {c.Id} has share balances.");
                    continue;
                }
                if (c.TotalShares <= 0 || c.LockedShares <= 0 || c.LockedShares >= c.TotalShares ||
                    c.AvailableFloat < 0 || c.DirectorFloat < 0 || c.Price <= 0)
                    throw new InvalidOperationException($"Invalid share balance for {c.Id}.");
                long issued = checked(c.LockedShares + c.AvailableFloat + c.DirectorFloat + c.Positions.Values.Sum(p => p.Shares));
                if (issued != c.TotalShares || c.Positions.Any(p => p.Key < 0 || p.Value.Shares <= 0 || double.IsNaN(p.Value.Cost) || p.Value.Cost <= 0))
                    throw new InvalidOperationException($"Share ownership does not reconcile for {c.Id}: {issued}/{c.TotalShares}.");
            }
        }

        public IReadOnlyList<StockDailyClose> CloseDay(int dayNumber)
        {
            var closes = new List<StockDailyClose>();
            foreach (var c in companies.Values)
            {
                double average = c.RevenueHistory.Count == 0 ? 0 : c.RevenueHistory.Average();
                double traffic = average <= 0 ? (c.DailyRevenue > 0 ? 1 : 0) : c.DailyRevenue / average;
                double trafficChange = traffic - c.PreviousTraffic;
                c.RevenueHistory.Enqueue(c.DailyRevenue);
                while (c.RevenueHistory.Count > config.DividendPeriodDays) c.RevenueHistory.Dequeue();
                long revenue15 = RevenueHistoryTotal(c);
                c.DailyRevenue = 0;
                if (c.TotalShares > 0)
                {
                    double orderFraction = (double)c.NetOrders / c.TotalShares;
                    double old = c.Price;
                    double delta = (random.NextDouble() * 2 - 1) * config.DailyVolatility
                        + config.TrafficSensitivity * trafficChange + config.OrderImpactCoefficient * orderFraction;
                    c.Price = Math.Max(1, c.Price * (1 + delta));
                    c.PriceHistory.Enqueue(c.Price);
                    while (c.PriceHistory.Count > 4) c.PriceHistory.Dequeue();
                    c.PreviousTraffic = traffic;
                    long pool = dayNumber > 0 && dayNumber % config.DividendPeriodDays == 0
                        ? checked((long)Math.Floor(revenue15 * config.DividendRate)) : 0;
                    closes.Add(new StockDailyClose(c.Id, old, c.Price, trafficChange, orderFraction, revenue15, pool));
                }
                c.NetOrders = 0;
            }
            return Array.AsReadOnly(closes.ToArray());
        }

        public IReadOnlyList<StockDividend> Dividends(StockDailyClose close)
        {
            if (close == null || close.DividendPool <= 0) return Array.Empty<StockDividend>();
            var c = FindListed(close.CompanyId);
            return Array.AsReadOnly(c.Positions.Where(p => p.Key >= 0 && p.Value.Shares > 0)
                .OrderBy(p => p.Key).Select(p => new StockDividend(p.Key, c.Id, p.Value.Shares,
                    checked((long)Math.Floor((double)close.DividendPool * p.Value.Shares / c.TotalShares)))).Where(x => x.Gross > 0).ToArray());
        }

        static StockCompanyView View(Company c)
        {
            double threeDayReturn = c.PriceHistory.Count >= 4
                ? c.Price / c.PriceHistory.Peek() - 1 : 0;
            return new StockCompanyView(c.Id, c.Price, c.TotalShares, c.LockedShares, c.DirectorFloat,
                c.AvailableFloat, RevenueHistoryTotal(c), c.PreviousTraffic, threeDayReturn);
        }
        static long RevenueHistoryTotal(Company c) => c.RevenueHistory.Aggregate(0L, (sum, value) => checked(sum + value));
        static long Value(double price, long shares) => checked((long)Math.Ceiling(price * shares));
        long Fee(long gross) => checked((long)Math.Ceiling(gross * config.TransactionFeeRate));
        Company GetOrCreate(string id)
        {
            if (!companies.TryGetValue(id, out var c)) companies.Add(id, c = new Company { Id = id });
            return c;
        }
        Company FindListed(string id)
        {
            if (!companies.TryGetValue(id ?? "", out var c) || c.TotalShares == 0) throw new ArgumentException("Company is not listed.", nameof(id));
            return c;
        }
        static void AddShares(Company c, int shareholderId, long shares, double unitCost)
        {
            if (shareholderId < HubLockedShareholderId || shares <= 0) throw new ArgumentOutOfRangeException(nameof(shares));
            if (!c.Positions.TryGetValue(shareholderId, out var p)) c.Positions.Add(shareholderId, p = new Position());
            long total = checked(p.Shares + shares);
            p.Cost = (p.Cost * p.Shares + unitCost * shares) / total;
            p.Shares = total;
        }
        static void RemoveShares(Company c, int shareholderId, long shares)
        {
            var p = c.Positions[shareholderId];
            p.Shares -= shares;
            if (p.Shares == 0) c.Positions.Remove(shareholderId);
        }
    }
}
