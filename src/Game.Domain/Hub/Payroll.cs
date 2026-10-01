using System;
using System.Collections.Generic;

namespace Game.Domain
{
    /// <summary>Kết quả một kỳ Payday.</summary>
    public sealed record PaydayOutcome(long TotalDue, long TotalPaid, double PaidRatio, bool StrikeStarted, int UnpaidStreak);

    /// <summary>
    /// Lương hợp đồng và thang vỡ nợ.
    /// Đủ tiền: trả đủ. Thiếu tiền: chia theo tỉ lệ, TOÀN BỘ Trainer đình công, phần thiếu thành nợ lương.
    /// Vỡ nợ lần 2: mọi Trainer +Stress. Từ lần 3: chế độ cấn nợ (<see cref="DebtMode"/>).
    /// </summary>
    public sealed class Payroll
    {
        /// <summary>Số Payday liên tiếp không trả đủ lương.</summary>
        public int UnpaidStreak { get; private set; }

        /// <summary>Từ lần vỡ nợ thứ 3: farm giảm, Nhà Hàng miễn phí để cấn nợ lương.</summary>
        public bool DebtMode => UnpaidStreak >= 3;

        /// <summary>Số tiền HUB phải trả một Trainer ở kỳ này: lương hợp đồng + nợ cũ - phần đã ứng.</summary>
        public static long DueOf(Trainer t) => Math.Max(0, t.ContractWage + t.WageOwed - t.WageAdvance);

        public long TotalDue(IReadOnlyList<Trainer> trainers)
        {
            long total = 0;
            for (int i = 0; i < trainers.Count; i++) total += DueOf(trainers[i]);
            return total;
        }

        public PaydayOutcome Resolve(IReadOnlyList<Trainer> trainers, TreasuryAccount treasury, SimConfig cfg)
        {
            long total = TotalDue(trainers);
            if (total <= treasury.Balance)
            {
                foreach (Trainer t in trainers)
                {
                    t.Gold += DueOf(t);
                    t.WageOwed = 0;
                    t.WageAdvance = 0;
                }
                treasury.TrySpend(total);
                UnpaidStreak = 0;
                return new PaydayOutcome(total, total, 1.0, false, 0);
            }

            // Thiếu tiền: trả mỗi người floor(due x balance / total), rồi chia phần dư cho các Trainer đầu danh sách.
            long balance = treasury.Balance;
            int n = trainers.Count;
            var paid = new long[n];
            long sum = 0;
            for (int i = 0; i < n; i++)
            {
                paid[i] = DueOf(trainers[i]) * balance / total;
                sum += paid[i];
            }
            long remainder = balance - sum;
            bool progressed = true;
            while (remainder > 0 && progressed)
            {
                progressed = false;
                for (int i = 0; i < n && remainder > 0; i++)
                {
                    if (paid[i] < DueOf(trainers[i])) { paid[i]++; remainder--; progressed = true; }
                }
            }

            long totalPaid = 0;
            for (int i = 0; i < n; i++)
            {
                Trainer t = trainers[i];
                long due = DueOf(t);
                t.Gold += paid[i];
                t.WageOwed = due - paid[i];
                t.WageAdvance = 0;
                t.StrikeDaysLeft = cfg.StrikeDays;
                totalPaid += paid[i];
            }
            treasury.TrySpend(totalPaid);

            UnpaidStreak++;
            if (UnpaidStreak == 2)
            {
                foreach (Trainer t in trainers)
                {
                    t.Needs.Stress += cfg.StressOnSecondMiss;
                    t.Needs.Clamp();
                }
            }
            return new PaydayOutcome(total, totalPaid, (double)totalPaid / total, true, UnpaidStreak);
        }
    }
}
