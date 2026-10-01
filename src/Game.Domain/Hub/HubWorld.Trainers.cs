using System;

namespace Game.Domain
{
    public sealed partial class HubWorld
    {
        /// <summary>
        /// Cộng trừ nhu cầu cho khoảng thời gian trôi kể từ lần cập nhật trước, theo trạng thái ĐANG có.
        /// Luôn gọi trước khi đổi trạng thái của Trainer.
        /// </summary>
        void Settle(Trainer t)
        {
            int minutes = now - t.LastSettleMinute;
            t.LastSettleMinute = now;
            if (minutes <= 0) return;

            double hours = minutes / 60.0;
            PersonalityProfile p = PersonalityProfile.Of(t.Personality);
            Needs n = t.Needs;
            bool outside = t.State == TrainerState.Traveling || t.State == TrainerState.Farming || t.State == TrainerState.Returning;
            if (outside)
            {
                n.Stamina -= cfg.FieldStaminaPerHour * hours;
                n.Satiety -= cfg.FieldSatietyPerHour * p.SatietyDecayMult * hours;
                n.Hydration -= cfg.FieldHydrationPerHour * hours;
                n.Stress += cfg.FieldStressPerHour * hours;
            }
            else if (t.State != TrainerState.InService)   // đang được phục vụ thì không tụt
            {
                n.Stamina -= cfg.HubStaminaPerHour * hours;
                n.Satiety -= cfg.HubSatietyPerHour * p.SatietyDecayMult * hours;
                n.Hydration -= cfg.HubHydrationPerHour * hours;
                if (t.State == TrainerState.Queued) n.Stress += cfg.QueueStressPerHour * hours;
                if (t.State == TrainerState.WaitingForMoney) n.Stress += cfg.WaitStressPerHour * hours;
            }
            n.Clamp();
        }

        void SetState(Trainer t, TrainerState state, string reason)
        {
            if (t.State == state && t.StateReason == reason) return;
            TrainerState from = t.State;
            t.State = state;
            t.StateReason = reason;
            Raise(new TrainerStateChanged(now, t.Id, from, state, reason));
        }

        /// <summary>
        /// Trainer chọn việc tiếp theo ở HUB: dịch vụ cần dùng > nghỉ (đình công, ban đêm không kính) > đi farm.
        /// </summary>
        void OnDecide(Trainer t)
        {
            Settle(t);
            bool isNight = SimClock.IsNight(now);

            BuildingKind? need = TrainerBrain.PickService(t, cfg, isNight);
            if (need.HasValue) { RequestService(t, need.Value); return; }

            if (t.IsOnStrike)
            {
                SetState(t, TrainerState.AtHub, "Strike");
                queue.Schedule(now + 60, SimEventKind.TrainerDecide, t.Id, t.Token);   // đình công: nghỉ 1 giờ rồi xét lại
                return;
            }
            if (isNight && !t.HasNightVision)
            {
                SetState(t, TrainerState.AtHub, "NightRest");
                queue.Schedule(SimClock.NextMinuteOfDay(now, SimClock.DawnMinute), SimEventKind.TrainerDecide, t.Id, t.Token);
                return;
            }
            SetState(t, TrainerState.Traveling, "Farm");
            queue.Schedule(now + cfg.ZoneTravelMinutes, SimEventKind.TrainerArriveZone, t.Id, t.Token);
        }

        void OnArriveZone(Trainer t)
        {
            Settle(t);
            SetState(t, TrainerState.Farming, "Farm");
            queue.Schedule(now + cfg.FarmChunkMinutes, SimEventKind.FarmChunk, t.Id, t.Token);
        }

        /// <summary>Hết một khúc farm: nhận kết quả, rồi quyết định ở lại hay về HUB.</summary>
        void OnFarmChunk(Trainer t)
        {
            Settle(t);
            FarmResult r = farm.Resolve(t, cfg.FarmChunkMinutes);
            if (payroll.DebtMode)   // chế độ cấn nợ: farm ít hơn
                r = new FarmResult((int)(r.MaterialUnits * cfg.DebtModeFarmMultiplier), (long)(r.Gold * cfg.DebtModeFarmMultiplier), r.HpLost);

            t.BackpackUnits = Math.Min(t.BackpackCapacity, t.BackpackUnits + r.MaterialUnits);
            t.Gold += r.Gold;                                  // Gold quái rơi là nguồn tiền từ ngoài vào
            t.TeamHp = Math.Max(0, t.TeamHp - r.HpLost);

            ReturnReason reason = TrainerBrain.ShouldReturn(t, SimClock.IsNight(now));
            if (reason != ReturnReason.None) StartReturn(t, reason);
            else queue.Schedule(now + cfg.FarmChunkMinutes, SimEventKind.FarmChunk, t.Id, t.Token);
        }

        /// <summary>Bắt đầu đường về HUB. Gọi sau <see cref="Settle"/>.</summary>
        void StartReturn(Trainer t, ReturnReason reason)
        {
            SetState(t, TrainerState.Returning, reason.ToString());
            queue.Schedule(now + cfg.ZoneTravelMinutes, SimEventKind.TrainerArriveHub, t.Id, t.Token);
        }

        /// <summary>Ngắt việc đang làm của Trainer (hủy sự kiện cũ bằng token) và cho về HUB.</summary>
        void SendHome(Trainer t, ReturnReason reason)
        {
            t.Token++;
            Settle(t);
            StartReturn(t, reason);
        }

        /// <summary>Về tới HUB: bán nguyên liệu (HUB thu thuế), rồi chọn việc tiếp theo.</summary>
        void OnArriveHub(Trainer t)
        {
            Settle(t);
            SetState(t, TrainerState.AtHub, "Arrived");
            if (t.BackpackUnits > 0)
            {
                SaleResult sale = market.Quote(t, t.BackpackUnits);
                t.Gold += sale.GrossToTrainer - sale.Tax;
                AddTreasury(sale.Tax, "TradeTax");
                t.BackpackUnits = 0;
            }
            OnDecide(t);
        }
    }
}
