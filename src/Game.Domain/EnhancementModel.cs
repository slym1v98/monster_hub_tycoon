using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Domain
{
    /// <summary>Mo hinh Cuong hoa +1..+20. Thuan C#, khong phu thuoc Unity.</summary>
    public sealed class EnhancementModel
    {
        public double CostBase = 100;       // Gold cho lan thu +1
        public double CostGrowth = 1.30;    // chi phi nhan len moi cap
        public double ScrollPrice = 0;      // Gold-tuong-duong cua 1 Bua Bao Ho (HUB ban cho AI)
        public int BreakFrom = 11;          // tu cap nay that bai co the vo do
        public double BreakChance = 0.30;   // xac suat vo khi that bai (khong co Bua)

        /// <summary>Tham số Cường hóa có mã ổn định, trạng thái và nguồn để đối chiếu workbook.</summary>
        public IReadOnlyList<BalanceParameter> BalanceParameters
        {
            get
            {
                const string source = "docs/designs/13 §4: enhancement success/cost/break starting values; GDD 05 locks +1..+20 and break threshold +11.";
                var values = new List<BalanceParameter>
                {
                    new BalanceParameter("gear.enhance_cost_base", CostBase, "Gold", "Prototype", source),
                    new BalanceParameter("gear.enhance_cost_growth", CostGrowth, "multiplier/level", "Prototype", source),
                    new BalanceParameter("gear.enhance_scroll_price", ScrollPrice, "Gold-equivalent", "TBD", "GDD requires Protection Charm at break-risk levels but gives no price; this field is legacy expected-cost analysis only."),
                    new BalanceParameter("gear.enhance_break_from", BreakFrom, "target level", "Locked", source),
                    new BalanceParameter("gear.enhance_break_chance", BreakChance, "probability on failure", "Prototype", source)
                };
                for (int level = 1; level <= 20; level++)
                    values.Add(new BalanceParameter("gear.enhance_success_" + level, Success(level), "probability", "Prototype", source));
                return Array.AsReadOnly(values.ToArray());
            }
        }

        /// <summary>Ty le thanh cong de len tu level-1 len level (1..20).</summary>
        public double Success(int level)
        {
            if (level < 1 || level > 20) throw new ArgumentOutOfRangeException(nameof(level));
            if (level <= 10) return 1.0 - 0.05 * (level - 1);   // 100% ... 55%
            return 0.50 - 0.035 * (level - 11);                  // 50% ... 18.5%
        }

        public double AttemptCost(int level) => CostBase * Math.Pow(CostGrowth, level - 1);

        /// <summary>
        /// Chi phi ky vong de len muc target (tinh ca chi phi thay do khi vo, neu khong dung Bua).
        /// Thay do = replacementCost (Gold). useScroll: dung Bua moi lan thu tu BreakFrom tro len.
        /// </summary>
        public double ExpectedCost(int target, bool useScroll, double replacementCost)
        {
            // e[lv] = chi phi ky vong tu +0 den lv
            double[] e = new double[target + 1];
            for (int lv = 1; lv <= target; lv++)
            {
                double p = Success(lv);
                double c = AttemptCost(lv) + ((useScroll && lv >= BreakFrom) ? ScrollPrice : 0);
                double pBreak = (!useScroll && lv >= BreakFrom) ? BreakChance : 0;
                // E_lv = (c + (1-p)*pBreak*(replacement + E_lv_restart)) / p ...
                // that bai khong vo: o lai level-1 va thu lai; that bai co vo: ve +0 (mua do moi roi len lai).
                // Goi A = E[len lv | dang o lv-1]. A = c + (1-p)*((1-pBreak)*A + pBreak*(replacementCost + e[lv-1] + A))
                // => A*(1 - (1-p)) = c + (1-p)*pBreak*(replacementCost + e[lv-1])
                double a = (c + (1 - p) * pBreak * (replacementCost + e[lv - 1])) / p;
                e[lv] = e[lv - 1] + a;
            }
            return e[target];
        }
    }
}
