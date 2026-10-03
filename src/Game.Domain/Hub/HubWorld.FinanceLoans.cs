using System;

namespace Game.Domain
{
    public sealed partial class HubWorld
    {
        double currentTrainerLoanRate;
        TrainerLoanConfig LoanSettings => cfg.TrainerLoanSettings ?? TrainerLoanConfig.Prototype;
        ReverseLoanConfig ReverseLoanTerms => cfg.ReverseLoanSettings ?? ReverseLoanConfig.Prototype;

        public long TrainerLoanLimit(int trainerId)
        {
            if (trainerId < 0 || trainerId >= trainers.Count) return 0;
            return checked((long)Math.Ceiling(trainers[trainerId].ContractWage * LoanSettings.MonthlyWageLimitMultiplier));
        }

        /// <summary>Director policy control from GDD 02: adjustable interest from 5% to 40% per Payday.</summary>
        public CommandResult SetTrainerLoanInterestRate(double rate)
        {
            if (double.IsNaN(rate) || double.IsInfinity(rate) || rate < 0.05 || rate > 0.40)
                return CommandResult.Rejected("Lãi suất phải nằm trong khoảng 5-40% mỗi Payday.");
            double previous = currentTrainerLoanRate;
            currentTrainerLoanRate = rate;
            Raise(new TrainerLoanPolicyChanged(now, previous, rate));
            return CommandResult.Success();
        }

        /// <summary>Director borrows from a Rank V Trainer; overdue debt is repaid through free services.</summary>
        public CommandResult BorrowFromTrainer(int lenderTrainerId, long amount)
        {
            if (lenderTrainerId < 0 || lenderTrainerId >= trainers.Count) return CommandResult.Rejected("Trainer không tồn tại.");
            var lender = trainers[lenderTrainerId];
            if (lender.Rank < 5) return CommandResult.Rejected("Chỉ Trainer Rank V có thể cho HUB vay.");
            if (lender.ReverseLoanBalance > 0) return CommandResult.Rejected("Khoản vay trước của Trainer chưa được tất toán.");
            if (amount <= 0) return CommandResult.Rejected("Số Gold phải lớn hơn 0.");
            long cap = (long)Math.Floor(Math.Max(0, lender.Gold) * ReverseLoanTerms.CashLimitFraction);
            if (amount > cap) return CommandResult.Rejected("Khoản vay vượt giới hạn tiền mặt của Trainer.");
            if (treasury.Balance > long.MaxValue - amount) return CommandResult.Rejected("Kho bạc vượt giới hạn số dư.");
            long oldBalance = lender.ReverseLoanBalance;
            lender.Gold -= amount;
            treasury.Add(amount);
            lender.ReverseLoanBalance = amount;
            lender.ReverseLoanPaydaysRemaining = ReverseLoanTerms.TermPaydays;
            lender.ReverseLoanOverdue = false;
            Raise(new TreasuryChanged(now, amount, treasury.Balance, "ReverseLoanDisbursement"));
            Raise(new ReverseLoanBalanceChanged(now, lenderTrainerId, oldBalance, amount, amount, "BorrowedFromTrainer"));
            return CommandResult.Success();
        }

        /// <summary>Director command: lend Gold to a Trainer within the configured credit line.</summary>
        public CommandResult LendToTrainer(int trainerId, long amount)
        {
            if (trainerId < 0 || trainerId >= trainers.Count) return CommandResult.Rejected("Trainer không tồn tại.");
            if (amount <= 0) return CommandResult.Rejected("Số Gold phải lớn hơn 0.");
            var trainer = trainers[trainerId];
            long limit = TrainerLoanLimit(trainerId);
            long available = Math.Max(0, limit - trainer.HubLoanBalance);
            if (amount > available) return CommandResult.Rejected("Khoản vay vượt hạn mức tín dụng.");
            if (amount > treasury.Balance) return CommandResult.Rejected("Kho bạc không đủ Gold.");
            long oldBalance = trainer.HubLoanBalance;
            treasury.TrySpend(amount);
            trainer.HubLoanBalance = checked(oldBalance + amount);
            trainer.Gold = checked(trainer.Gold + amount);
            Raise(new TreasuryChanged(now, -amount, treasury.Balance, "TrainerLoanDisbursement"));
            Raise(new TrainerLoanBalanceChanged(now, trainerId, oldBalance, trainer.HubLoanBalance, amount, "Disbursement"));
            WakeIfWaitingForMoney(trainer);
            return CommandResult.Success();
        }

        bool EnsureTrainerCanPayFromLoan(Trainer trainer, long cost)
        {
            if (trainer.Gold >= cost) return true;
            long shortfall = cost - Math.Max(0, trainer.Gold);
            return LendToTrainer(trainer.Id, shortfall).Ok && trainer.Gold >= cost;
        }

        void ReceiveTrainerIncome(Trainer trainer, long income, string source)
        {
            if (income < 0) throw new ArgumentOutOfRangeException(nameof(income));
            long repayment = Math.Min(trainer.HubLoanBalance,
                (long)Math.Floor(income * LoanSettings.IncomeRepaymentFraction));
            long old = trainer.HubLoanBalance;
            trainer.HubLoanBalance -= repayment;
            trainer.Gold = checked(trainer.Gold + income - repayment);
            if (repayment > 0)
            {
                AddTreasury(repayment, "TrainerLoanRepayment");
                Raise(new TrainerLoanBalanceChanged(now, trainer.Id, old, trainer.HubLoanBalance, repayment, "IncomeRepayment"));
                Raise(new TrainerLoanRepaid(now, trainer.Id, repayment, source));
            }
        }

        void ApplyTrainerLoanPayday(Trainer trainer, long wagesReceived)
        {
            long old = trainer.HubLoanBalance;
            if (old > 0)
            {
                long interest = checked((long)Math.Ceiling(old * currentTrainerLoanRate));
                trainer.HubLoanBalance = checked(old + interest);
                if (interest > 0) Raise(new TrainerLoanBalanceChanged(now, trainer.Id, old, trainer.HubLoanBalance, interest, "PaydayInterest"));
            }
            if (wagesReceived > 0)
            {
                long repayment = Math.Min(trainer.HubLoanBalance,
                    (long)Math.Floor(wagesReceived * LoanSettings.IncomeRepaymentFraction));
                if (repayment > 0)
                {
                    long before = trainer.HubLoanBalance;
                    trainer.HubLoanBalance -= repayment;
                    trainer.Gold -= repayment;
                    AddTreasury(repayment, "TrainerLoanRepayment");
                    Raise(new TrainerLoanBalanceChanged(now, trainer.Id, before, trainer.HubLoanBalance, repayment, "PaydayWageRepayment"));
                    Raise(new TrainerLoanRepaid(now, trainer.Id, repayment, "PaydayWages"));
                }
            }
            if (trainer.HubLoanBalance > TrainerLoanLimit(trainer.Id)) trainer.HubLoanOverLimitPaydays++;
            else trainer.HubLoanOverLimitPaydays = 0;
            if (trainer.HubLoanOverLimitPaydays >= LoanSettings.OverLimitPaydaysToStrike && trainer.StrikeDaysLeft == 0)
            {
                trainer.StrikeDaysLeft = cfg.StrikeDays;
                Raise(new TrainerLoanOverdueStrike(now, trainer.Id, trainer.HubLoanBalance, TrainerLoanLimit(trainer.Id), trainer.HubLoanOverLimitPaydays));
                if (trainer.State == TrainerState.Traveling || trainer.State == TrainerState.Farming)
                    SendHome(trainer, ReturnReason.Strike);
            }
        }

        void ApplyReverseLoanPayday(Trainer trainer)
        {
            if (trainer.ReverseLoanBalance <= 0) return;
            long old = trainer.ReverseLoanBalance;
            long interest = checked((long)Math.Ceiling(old * ReverseLoanTerms.InterestPerPayday));
            trainer.ReverseLoanBalance = checked(old + interest);
            if (interest > 0)
                Raise(new ReverseLoanBalanceChanged(now, trainer.Id, old, trainer.ReverseLoanBalance, interest, "PaydayInterest"));
            if (!trainer.ReverseLoanOverdue && trainer.ReverseLoanPaydaysRemaining > 0)
            {
                trainer.ReverseLoanPaydaysRemaining--;
                if (trainer.ReverseLoanPaydaysRemaining == 0)
                {
                    trainer.ReverseLoanOverdue = true;
                    Raise(new ReverseLoanBalanceChanged(now, trainer.Id, trainer.ReverseLoanBalance,
                        trainer.ReverseLoanBalance, 0, "Overdue; service offset enabled"));
                }
            }
        }
    }
}
