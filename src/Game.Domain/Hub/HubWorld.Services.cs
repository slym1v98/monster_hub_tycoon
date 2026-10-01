using System;

namespace Game.Domain
{
    public sealed partial class HubWorld
    {
        /// <summary>Trainer xin một dịch vụ: hết tiền thì chờ tiền, ngược lại vào hàng đợi FIFO.</summary>
        void RequestService(Trainer t, BuildingKind kind)
        {
            ServiceBuilding b = buildings[(int)kind];
            PersonalityProfile profile = PersonalityProfile.Of(t.Personality);
            long price = b.PriceFor(profile, t.TeamHpMax - t.TeamHp);
            bool freeInDebtMode = payroll.DebtMode && kind == BuildingKind.Restaurant && t.WageOwed > 0;
            if (!freeInDebtMode && t.Gold < price) { BeginWaitForMoney(t, kind); return; }

            SetState(t, TrainerState.Queued, kind.ToString());
            b.Enqueue(t.Id);
            SeatWaiting(b);
        }

        /// <summary>Xếp người đang chờ vào các chỗ trống và bắt đầu phục vụ họ.</summary>
        void SeatWaiting(ServiceBuilding b)
        {
            System.Collections.Generic.List<int> seated;
            while ((seated = b.PromoteWaiting()).Count > 0)
                foreach (int id in seated) StartService(trainers[id], b);
            Raise(new QueueChanged(now, b.Kind, b.QueueLength, b.Occupied));
        }

        /// <summary>
        /// Bắt đầu một lượt phục vụ: tính giá tại thời điểm này, thu tiền (hoặc cấn nợ ở chế độ vỡ nợ),
        /// cộng Stress nếu giá cao hơn giá hợp lý, rồi hẹn giờ kết thúc.
        /// </summary>
        void StartService(Trainer t, ServiceBuilding b)
        {
            Settle(t);   // cộng nốt Stress xếp hàng
            PersonalityProfile profile = PersonalityProfile.Of(t.Personality);
            long normalPrice = b.PriceFor(profile, t.TeamHpMax - t.TeamHp);
            bool free = payroll.DebtMode && b.Kind == BuildingKind.Restaurant && t.WageOwed > 0;
            long paid = free ? 0 : normalPrice;

            if (t.Gold < paid)   // giá bị Giám đốc đẩy lên trong lúc chờ
            {
                b.Leave(t.Id);
                BeginWaitForMoney(t, b.Kind);
                return;
            }

            if (free)
            {
                t.WageOwed = Math.Max(0, t.WageOwed - normalPrice);   // giá trị dịch vụ trừ vào nợ lương
            }
            else
            {
                t.Gold -= paid;
                long cogs = (long)Math.Round(paid * cfg.ServiceCogs);
                AddTreasury(paid - cogs, "Service");
            }

            double priceRatio = (double)b.Price / b.FairPrice;
            double stressAdded = cfg.PriceStressFactor * Math.Max(0.0, priceRatio - 1.0) * profile.PriceSensitivity;
            t.Needs.Stress += stressAdded;
            t.Needs.Clamp();

            SetState(t, TrainerState.InService, b.Kind.ToString());
            queue.Schedule(now + b.ServiceMinutesFor(now), SimEventKind.ServiceDone, t.Id, t.Token, (int)b.Kind);
            Raise(new ServiceUsed(now, t.Id, b.Kind, paid, b.Price, b.FairPrice, stressAdded));
        }

        /// <summary>Dùng xong dịch vụ: hồi nhu cầu tương ứng, nhả chỗ, gọi người kế tiếp.</summary>
        void OnServiceDone(Trainer t, ServiceBuilding b)
        {
            Settle(t);
            switch (b.Kind)
            {
                case BuildingKind.Inn: t.Needs.Stamina = 100; break;
                case BuildingKind.Restaurant: t.Needs.Satiety = 100; t.Needs.Hydration = 100; break;
                case BuildingKind.Bar: t.Needs.Stress = cfg.BarStressTarget; break;
                case BuildingKind.Hospital: t.TeamHp = t.TeamHpMax; break;
            }
            b.Leave(t.Id);
            SeatWaiting(b);
            SetState(t, TrainerState.AtHub, "Served");
            queue.Schedule(now, SimEventKind.TrainerDecide, t.Id, t.Token);
        }

        // ---- Hết tiền ----
        void BeginWaitForMoney(Trainer t, BuildingKind kind)
        {
            SetState(t, TrainerState.WaitingForMoney, kind.ToString());
            t.WaitingSinceMinute = now;
            t.PendingService = kind;
            queue.Schedule(now + 60, SimEventKind.WaitTick, t.Id, t.Token);
        }

        /// <summary>Kết thúc trạng thái chờ tiền và ghi nhận thời gian chờ.</summary>
        void LeaveWait(Trainer t)
        {
            int waited = now - t.WaitingSinceMinute;
            if (waited > MaxMoneyWaitMinutes) MaxMoneyWaitMinutes = waited;
        }

        /// <summary>
        /// Mỗi giờ chờ tiền: nếu đã đủ tiền (giá giảm) thì xử lý lại ngay; nếu không thì thử gọi Tổng tài
        /// (xác suất mỗi giờ, chắc chắn sau PatronGuaranteedAfterHours giờ). Tổng tài donate gấp đôi giá cần.
        /// </summary>
        void OnWaitTick(Trainer t)
        {
            Settle(t);
            ServiceBuilding b = buildings[(int)t.PendingService];
            long needed = b.PriceFor(PersonalityProfile.Of(t.Personality), t.TeamHpMax - t.TeamHp);

            if (t.Gold >= needed)
            {
                LeaveWait(t);
                SetState(t, TrainerState.AtHub, "Funded");
                OnDecide(t);
                return;
            }

            int hoursWaited = (now - t.WaitingSinceMinute) / 60;
            bool patronAppears = hoursWaited >= cfg.PatronGuaranteedAfterHours || rng.NextDouble() < cfg.PatronChancePerHour;
            if (patronAppears)
            {
                long donation = Math.Max(needed * 2, 1);
                t.Gold += donation;   // tiền từ ngoài vào
                Raise(new DonationReceived(now, t.Id, donation, "Patron"));
                LeaveWait(t);
                SetState(t, TrainerState.AtHub, "Funded");
                OnDecide(t);
                return;
            }
            queue.Schedule(now + 60, SimEventKind.WaitTick, t.Id, t.Token);
        }
    }
}
