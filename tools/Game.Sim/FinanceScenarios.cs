using System;
using System.Collections.Generic;
using System.Linq;
using Game.Domain;

/// <summary>Seeded 360-day finance integration run. Rates and rules come from the configured domain models.</summary>
public static class FinanceScenarios
{
    public static void Run(System.IO.TextWriter output)
    {
        const int seed = 20261003, days = 360;
        var config = new SimConfig { TrainerCount = 2, StartMinute = SimClock.DawnMinute, StartBuildingLevel = 5,
            StartTrainerGold = 100000, StartTreasury = 1000000, StartingTrainerRanks = new[] { 1, 5 } };
        var world = new HubWorld(config, seed);
        var events = new List<IDomainEvent>();
        world.EventRaised += events.Add;
        long trainerGoldStart = world.Trainers.Sum(t => t.Gold), treasuryStart = world.Treasury;
        world.DepositMonster(0, "trainer_0_starter");
        world.LendToTrainer(0, Math.Min(100, world.TrainerLoanLimit(0)));
        world.BorrowFromTrainer(1, 1000);
        world.SetStockExchangeLevel(5);
        var listed = false;

        output.WriteLine($"# finance: seed {seed}; {config.TrainerCount} Trainers; {days} days");
        foreach (var p in config.TrainerLoanSettings.BalanceParameters.Concat(config.ReverseLoanSettings.BalanceParameters)
            .Concat(config.StockExchangeSettings.BalanceParameters).Concat(config.GeneBankSettings.BalanceParameters)
            .OrderBy(p => p.Id, StringComparer.Ordinal))
            output.WriteLine(FormattableString.Invariant($"Balance: {p.Id}={p.Value} {p.Unit} [{p.Status}; source: {p.Source}]"));

        for (int day = 1; day <= days; day++)
        {
            var advanced = world.RunFor(SimClock.MinutesPerDay);
            int remaining = advanced.RemainingMinutes;
            while (advanced.Stop == StopReason.PaydayDue)
            {
                world.ResolvePayday();
                if (remaining <= 0) break;
                advanced = world.RunFor(remaining);
                remaining = advanced.RemainingMinutes;
            }
            if (remaining > 0) throw new InvalidOperationException("Finance scenario stopped before completing its day.");
            if (!listed && day == config.StockExchangeSettings.DividendPeriodDays)
            {
                var ipo = world.IpoBuildingStock(BuildingKind.Restaurant);
                if (ipo.Ok) listed = true;
            }
            if (listed && day == config.StockExchangeSettings.DividendPeriodDays + 1)
            {
                var listing = world.StockCompanies.First(x => x.CompanyId == BuildingKind.Restaurant.ToString());
                long shares = Math.Min(100, listing.AvailableFloatShares);
                if (shares > 0) world.BuyIpoShares(0, listing.CompanyId, shares);
            }
            if (listed && day == config.StockExchangeSettings.DividendPeriodDays + 2)
            {
                var listing = world.StockCompanies.First(x => x.CompanyId == BuildingKind.Restaurant.ToString());
                if (world.StockHoldingsForTrainer(0).Any(x => x.CompanyId == listing.CompanyId))
                    world.TradeStockShares(0, 1, listing.CompanyId, 1);
            }
        }
        world.ValidateInvariants();
        long trades = events.OfType<StockTradeSettled>().LongCount();
        long feesAndTaxes = events.OfType<StockTradeSettled>().Sum(x => x.Fee + x.Tax);
        long confiscations = events.OfType<GeneBankFeeSettled>().LongCount(x => !string.IsNullOrEmpty(x.ConfiscatedMonsterId));
        long loanInterest = events.OfType<TrainerLoanBalanceChanged>().Where(x => x.Reason == "PaydayInterest").Sum(x => x.Amount);
        long geneFeesPaid = events.OfType<GeneBankFeeSettled>().Sum(x => x.Paid);
        long dividends = events.OfType<StockDividendPaid>().Where(x => x.Paid).Sum(x => x.Gross);
        long internalTreasuryDelta = world.Treasury - treasuryStart;
        long treasuryEventDelta = events.OfType<TreasuryChanged>().Sum(x => x.Delta);
        long trainerDelta = world.Trainers.Sum(t => t.Gold) - trainerGoldStart;
        if (internalTreasuryDelta != treasuryEventDelta)
            throw new InvalidOperationException($"Treasury ledger mismatch: balance delta {internalTreasuryDelta}, events {treasuryEventDelta}.");
        output.WriteLine($"Result: loan interest {loanInterest}; Gene Bank paid {geneFeesPaid}; confiscations {confiscations}; stock listed {listed}; trades {trades}; dividends paid {dividends}; stock fees/tax {feesAndTaxes}.");
        output.WriteLine($"Balances: treasury {world.Treasury}; Trainer Gold {world.Trainers.Sum(t => t.Gold)}; HUB loans {world.Trainers.Sum(t => t.HubLoanBalance)}; Trainer loans {world.Trainers.Sum(t => t.ReverseLoanBalance)}.");
        output.WriteLine($"Cash deltas: Treasury {internalTreasuryDelta} (event ledger {treasuryEventDelta}, difference {internalTreasuryDelta - treasuryEventDelta}); Trainer Gold {trainerDelta}; external farm Gold is excluded from this summary.");
    }
}
