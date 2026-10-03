using System;
using System.Linq;

namespace Game.Domain
{
    public sealed partial class HubWorld
    {
        bool StockMarketOpen
        {
            get
            {
                int minute = ((now % SimClock.MinutesPerDay) + SimClock.MinutesPerDay) % SimClock.MinutesPerDay;
                return minute >= SimClock.DawnMinute && minute < SimClock.DuskMinute;
            }
        }

        public CommandResult SetStockExchangeLevel(int level)
        {
            int old = stockExchange.ExchangeLevel;
            if (!stockExchange.SetExchangeLevel(level)) return CommandResult.Rejected("Cấp Sàn Chứng Khoán chỉ tăng từ 1 đến 10.");
            if (old != level) Raise(new StockExchangeLevelChanged(now, old, level));
            return CommandResult.Success();
        }

        public CommandResult IpoBuildingStock(BuildingKind building)
        {
            if (!StockMarketOpen) return CommandResult.Rejected("Sàn Chứng Khoán chỉ mở cửa ban ngày.");
            if ((int)building < 0 || (int)building >= buildings.Length) return CommandResult.Rejected("Công trình không hợp lệ.");
            if (!stockExchange.TryIpo(building.ToString(), buildings[(int)building].Level, out var listing))
                return CommandResult.Rejected("Công trình chưa đủ cấp/doanh thu 15 ngày hoặc Sàn đã đạt giới hạn niêm yết.");
            Raise(new StockCompanyIpo(now, listing.CompanyId, listing.Price, listing.TotalShares,
                listing.HubLockedShares, listing.TotalShares - listing.HubLockedShares));
            return CommandResult.Success();
        }

        public CommandResult BuyIpoShares(int trainerId, string companyId, long shares)
        {
            if (!ValidStockTrainer(trainerId) || !StockMarketOpen) return CommandResult.Rejected("Trainer không hợp lệ hoặc Sàn đang đóng cửa.");
            StockTradeQuote quote;
            try { quote = stockExchange.QuoteIpoPurchase(companyId, shares); }
            catch (Exception ex) when (ex is ArgumentException || ex is OverflowException) { return CommandResult.Rejected(ex.Message); }
            if (quote == null) return CommandResult.Rejected("Số cổ phiếu chào bán không đủ.");
            var trainer = trainers[trainerId];
            if (quote.BuyerTotal > long.MaxValue - treasury.Balance) return CommandResult.Rejected("Kho bạc vượt giới hạn số dư.");
            if (!EnsureTrainerCanPayFromLoan(trainer, quote.BuyerTotal) || trainer.Gold < quote.BuyerTotal)
                return CommandResult.Rejected("Trainer không đủ tiền hoặc hạn mức vay.");
            if (!stockExchange.BuyIpoShares(trainerId, companyId, shares, out quote)) return CommandResult.Rejected("Không thể khớp lệnh IPO.");
            trainer.Gold -= quote.BuyerTotal;
            AddTreasury(quote.BuyerTotal, "StockIpoPurchase");
            Raise(new StockTradeSettled(now, companyId, trainerId, StockExchange.HubLockedShareholderId,
                shares, quote.Gross, quote.Fee, 0, 0, quote.BuyerTotal, 0, "IpoPurchase"));
            return CommandResult.Success();
        }

        /// <summary>Immediate trainer-to-trainer order; the buyer pays the exchange fee and seller gains are taxed.</summary>
        public CommandResult TradeStockShares(int sellerTrainerId, int buyerTrainerId, string companyId, long shares)
        {
            if (!ValidStockTrainer(sellerTrainerId) || !ValidStockTrainer(buyerTrainerId) || !StockMarketOpen)
                return CommandResult.Rejected("Trainer không hợp lệ hoặc Sàn đang đóng cửa.");
            StockTradeQuote quote;
            try { quote = stockExchange.QuoteTransfer(companyId, sellerTrainerId, buyerTrainerId, shares); }
            catch (Exception ex) when (ex is ArgumentException || ex is OverflowException) { return CommandResult.Rejected(ex.Message); }
            if (quote == null) return CommandResult.Rejected("Người bán không sở hữu đủ cổ phiếu hoặc lệnh không hợp lệ.");
            var buyer = trainers[buyerTrainerId]; var seller = trainers[sellerTrainerId];
            long treasuryCredit = checked(quote.Fee + quote.Tax);
            if (treasuryCredit > long.MaxValue - treasury.Balance ||
                quote.SellerNet > long.MaxValue - seller.Gold)
                return CommandResult.Rejected("Giá trị giao dịch vượt giới hạn số dư.");
            if (!EnsureTrainerCanPayFromLoan(buyer, quote.BuyerTotal) || buyer.Gold < quote.BuyerTotal)
                return CommandResult.Rejected("Người mua không đủ tiền hoặc hạn mức vay.");
            if (!stockExchange.TransferShares(companyId, sellerTrainerId, buyerTrainerId, shares, out quote))
                return CommandResult.Rejected("Không thể khớp lệnh cổ phiếu.");
            buyer.Gold -= quote.BuyerTotal;
            ReceiveTrainerIncome(seller, quote.SellerNet, "StockSale");
            AddTreasury(treasuryCredit, "StockFeeAndTax");
            Raise(new StockTradeSettled(now, companyId, buyerTrainerId, sellerTrainerId, shares,
                quote.Gross, quote.Fee, quote.RealizedProfit, quote.Tax, quote.BuyerTotal, quote.SellerNet, "TrainerTrade"));
            return CommandResult.Success();
        }

        /// <summary>Director sells Treasury-held public float; only shares purchased from Trainers can be resold.</summary>
        public CommandResult DirectorSellStock(string companyId, int buyerTrainerId, long shares)
        {
            if (!ValidStockTrainer(buyerTrainerId) || !StockMarketOpen) return CommandResult.Rejected("Trainer không hợp lệ hoặc Sàn đang đóng cửa.");
            StockTradeQuote quote;
            try { quote = stockExchange.QuoteDirectorSale(companyId, buyerTrainerId, shares); }
            catch (Exception ex) when (ex is ArgumentException || ex is OverflowException) { return CommandResult.Rejected(ex.Message); }
            if (quote == null) return CommandResult.Rejected("Kho cổ phiếu lưu hành của HUB không đủ.");
            var trainer = trainers[buyerTrainerId];
            if (quote.BuyerTotal > long.MaxValue - treasury.Balance) return CommandResult.Rejected("Kho bạc vượt giới hạn số dư.");
            if (!EnsureTrainerCanPayFromLoan(trainer, quote.BuyerTotal) || trainer.Gold < quote.BuyerTotal)
                return CommandResult.Rejected("Trainer không đủ tiền hoặc hạn mức vay.");
            if (!stockExchange.DirectorSellsFloat(companyId, buyerTrainerId, shares, out quote)) return CommandResult.Rejected("Không thể khớp lệnh.");
            trainer.Gold -= quote.BuyerTotal;
            AddTreasury(quote.BuyerTotal, "StockDirectorSale");
            Raise(new StockTradeSettled(now, companyId, buyerTrainerId, StockExchange.HubLockedShareholderId,
                shares, quote.Gross, quote.Fee, 0, 0, quote.BuyerTotal, 0, "DirectorSale"));
            return CommandResult.Success();
        }

        /// <summary>Director buys public float from a Trainer and pays realized-profit tax and the exchange fee to HUB.</summary>
        public CommandResult DirectorBuyStock(string companyId, int sellerTrainerId, long shares)
        {
            if (!ValidStockTrainer(sellerTrainerId) || !StockMarketOpen) return CommandResult.Rejected("Trainer không hợp lệ hoặc Sàn đang đóng cửa.");
            StockTradeQuote quote;
            try { quote = stockExchange.QuoteDirectorPurchase(companyId, sellerTrainerId, shares); }
            catch (Exception ex) when (ex is ArgumentException || ex is OverflowException) { return CommandResult.Rejected(ex.Message); }
            if (quote == null) return CommandResult.Rejected("Trainer không sở hữu đủ cổ phiếu.");
            var seller = trainers[sellerTrainerId];
            long credits = checked(quote.Fee + quote.Tax);
            if (quote.Gross > treasury.Balance || credits > long.MaxValue - (treasury.Balance - quote.Gross) ||
                quote.SellerNet > long.MaxValue - seller.Gold) return CommandResult.Rejected("Không đủ Gold để khớp lệnh.");
            if (!stockExchange.DirectorBuysFloat(companyId, sellerTrainerId, shares, out quote)) return CommandResult.Rejected("Không thể khớp lệnh.");
            treasury.TrySpend(quote.Gross);
            Raise(new TreasuryChanged(now, -quote.Gross, treasury.Balance, "DirectorStockPurchase"));
            ReceiveTrainerIncome(seller, quote.SellerNet, "DirectorStockPurchase");
            AddTreasury(credits, "StockFeeAndTax");
            Raise(new StockTradeSettled(now, companyId, StockExchange.HubLockedShareholderId, sellerTrainerId,
                shares, quote.Gross, quote.Fee, quote.RealizedProfit, quote.Tax, 0, quote.SellerNet, "DirectorPurchase"));
            return CommandResult.Success();
        }

        void CloseStockMarketDay()
        {
            int day = now / SimClock.MinutesPerDay;
            foreach (var close in stockExchange.CloseDay(day))
            {
                Raise(new StockDailyRevenue(now, close.CompanyId, close.Revenue15Days, day));
                Raise(new StockPriceChanged(now, close.CompanyId, close.OldPrice, close.NewPrice,
                    close.TrafficChange, close.NetOrderFraction));
                foreach (var dividend in stockExchange.Dividends(close))
                {
                    bool paid = treasury.TrySpend(dividend.Gross);
                    if (paid)
                    {
                        Raise(new TreasuryChanged(now, -dividend.Gross, treasury.Balance, "StockDividend"));
                        ReceiveTrainerIncome(trainers[dividend.TrainerId], dividend.Gross, "StockDividend");
                    }
                    Raise(new StockDividendPaid(now, close.CompanyId, dividend.TrainerId, dividend.Shares, dividend.Gross, paid));
                }
            }
        }

        /// <summary>Daily seeded-market AI orders follow the rarity and personality tendencies in GDD 02 §3.</summary>
        void ExecuteStockAiOrders()
        {
            var listed = stockExchange.Companies.OrderByDescending(c => c.Revenue15Days)
                .ThenBy(c => c.CompanyId, StringComparer.Ordinal).ToArray();
            if (listed.Length == 0) return;
            foreach (var trainer in trainers.OrderBy(t => t.Id))
            {
                var holdings = stockExchange.HoldingsFor(trainer.Id);
                if (trainer.Personality == Personality.Timid)
                {
                    foreach (var holding in holdings)
                    {
                        var company = stockExchange.GetCompany(holding.CompanyId);
                        if (company != null && company.ThreeDayReturn <= -stockExchange.Config.TimidAiPanicDrop3Day)
                            TradeStockShares(trainer.Id, ChooseBuyer(trainer.Id), holding.CompanyId, holding.Shares);
                    }
                    continue;
                }
                if (trainer.Personality == Personality.Capitalist)
                {
                    var rising = holdings.Select(h => (Holding: h, Company: stockExchange.GetCompany(h.CompanyId)))
                        .Where(x => x.Company != null && x.Company.ThreeDayReturn > 0)
                        .OrderByDescending(x => x.Company.ThreeDayReturn).FirstOrDefault();
                    if (rising.Holding != null)
                    {
                        long sell = Math.Max(1, (long)Math.Ceiling(rising.Holding.Shares * stockExchange.Config.CapitalistAiTradeFraction));
                        var buyerId = ChooseBuyer(trainer.Id);
                        if (buyerId >= 0) TradeStockShares(trainer.Id, buyerId, rising.Holding.CompanyId, Math.Min(sell, rising.Holding.Shares));
                        continue;
                    }
                }
                StockCompanyView target = trainer.Rarity == Rarity.Common
                    ? listed[0]
                    : trainer.Rarity == Rarity.Ultimate ? listed[0] : null;
                if (trainer.Personality == Personality.Capitalist && target == null)
                    target = listed[0];
                if (target == null) continue;
                double fraction = trainer.Personality == Personality.Capitalist
                    ? stockExchange.Config.CapitalistAiTradeFraction
                    : trainer.Rarity == Rarity.Ultimate
                        ? stockExchange.Config.UltimateAiBuyFraction : stockExchange.Config.CommonAiBuyFraction;
                long maxShares = Math.Min(target.AvailableFloatShares,
                    Math.Max(0, (long)Math.Floor(target.AvailableFloatShares * fraction)));
                long budget = Math.Max(0, trainer.Gold);
                if (trainer.Rarity == Rarity.Common)
                    budget = checked(budget + Math.Max(0, TrainerLoanLimit(trainer.Id) - trainer.HubLoanBalance));
                long shares = AffordableShares(target.CompanyId, maxShares, budget);
                if (shares > 0) BuyIpoShares(trainer.Id, target.CompanyId, shares);
            }
        }

        long AffordableShares(string companyId, long maximum, long budget)
        {
            long lo = 0, hi = maximum;
            while (lo < hi)
            {
                long mid = lo + (hi - lo + 1) / 2;
                var quote = stockExchange.QuoteIpoPurchase(companyId, mid);
                if (quote != null && quote.BuyerTotal <= budget) lo = mid;
                else hi = mid - 1;
            }
            return lo;
        }

        int ChooseBuyer(int sellerId) => trainers.Where(t => t.Id != sellerId)
            .OrderByDescending(t => t.Gold).Select(t => t.Id).DefaultIfEmpty(-1).First();

        bool ValidStockTrainer(int trainerId) => trainerId >= 0 && trainerId < trainers.Count;
    }
}
