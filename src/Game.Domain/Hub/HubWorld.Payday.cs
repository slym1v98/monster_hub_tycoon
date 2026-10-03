using System;
using System.Linq;

namespace Game.Domain
{
    public sealed partial class HubWorld
    {
        /// <summary>Dự báo Payday: số ngày còn lại, quỹ lương cần trả, Kho bạc hiện có.</summary>
        public PaydayForecast Forecast
        {
            get
            {
                int daysLeft = paydayPending
                    ? 0
                    : (SimClock.PaydayMinute(paydayIndex) - now + SimClock.MinutesPerDay) / SimClock.MinutesPerDay;
                return new PaydayForecast(daysLeft, payroll.TotalDue(trainers), treasury.Balance);
            }
        }

        void OnPaydayDue()
        {
            paydayPending = true;
            Raise(new PaydayDue(now, Forecast));
        }

        /// <summary>
        /// Người chơi xử lý Payday: trả lương hợp đồng, nếu thiếu thì chia theo tỉ lệ và toàn bộ Trainer đình công.
        /// Chỉ gọi được khi <see cref="RunFor"/> đã dừng vì PaydayDue; nếu không, ném InvalidOperationException (lỗi lập trình).
        /// </summary>
        public PaydayOutcome ResolvePayday()
        {
            if (!paydayPending) throw new InvalidOperationException("Payday chưa tới, chưa thể xử lý.");

            var balancesBeforePayroll = trainers.ToDictionary(t => t.Id, t => t.Gold);
            PaydayOutcome outcome = payroll.Resolve(trainers, treasury, cfg);
            paydayPending = false;
            paydayIndex++;
            queue.Schedule(SimClock.PaydayMinute(paydayIndex), SimEventKind.PaydayDue);
            if (outcome.TotalPaid > 0) Raise(new TreasuryChanged(now, -outcome.TotalPaid, treasury.Balance, "Payday"));
            Raise(new PaydayResolved(now, outcome));
            foreach (var trainer in trainers)
            {
                long wagesReceived = Math.Max(0, trainer.Gold - balancesBeforePayroll[trainer.Id]);
                ApplyTrainerLoanPayday(trainer, wagesReceived);
                ApplyReverseLoanPayday(trainer);
                var fee = geneBank.CalculatePaydayFee(trainer.Id);
                if (fee.Amount > 0)
                {
                    Raise(new GeneBankFeeAssessed(now, trainer.Id, fee.MonsterCount, fee.Days, fee.Amount));
                    long paid = Math.Min(Math.Max(0, trainer.Gold), fee.Amount);
                    trainer.Gold -= paid;
                    AddTreasury(paid, "GeneBankFee");
                    long unpaid = fee.Amount - paid;
                    string confiscatedId = "";
                    if (unpaid > 0)
                    {
                        var confiscated = geneBank.ConfiscateForUnpaidFee(trainer.Id);
                        if (confiscated != null)
                        {
                            confiscatedId = confiscated.Id.Value;
                            Raise(new MonsterConfiscated(now, trainer.Id, confiscatedId));
                            Raise(new GeneBankOwnershipChanged(now, trainer.Id, confiscatedId,
                                Game.Domain.Monsters.MonsterCustody.GeneBank, Game.Domain.Monsters.MonsterCustody.Hub));
                        }
                    }
                    Raise(new GeneBankFeeSettled(now, trainer.Id, fee.Amount, paid, unpaid, confiscatedId));
                }
            }

            if (outcome.StrikeStarted)   // đình công: Trainer đang ở ngoài phải về HUB
            {
                foreach (Trainer t in trainers)
                {
                    bool outside = t.State == TrainerState.Traveling || t.State == TrainerState.Farming;
                    if (outside) SendHome(t, ReturnReason.Strike);
                }
            }
            return outcome;
        }
    }
}
