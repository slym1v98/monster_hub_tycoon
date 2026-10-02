using System;
using System.Collections.Generic;

namespace Game.Domain
{
    public sealed partial class HubWorld
    {
        /// <summary>
        /// Giám đốc donate Gold cho một Trainer (không hoàn lại). Lỗi của người chơi trả về Rejected.
        /// Nếu Trainer đang kẹt vì hết tiền thì được xử lý lại ngay.
        /// </summary>
        public CommandResult Donate(int trainerId, long gold)
        {
            CommandResult check = CheckTransfer(trainerId, gold);
            if (!check.Ok) return check;

            Trainer t = trainers[trainerId];
            treasury.TrySpend(gold);
            t.Gold += gold;
            Raise(new TreasuryChanged(now, -gold, treasury.Balance, "Donate"));
            Raise(new DonationReceived(now, trainerId, gold, "Director"));
            WakeIfWaitingForMoney(t);
            return CommandResult.Success();
        }

        /// <summary>Ứng lương: Giám đốc đưa Gold trước, khoản này được trừ vào lương Payday kế tiếp.</summary>
        public CommandResult AdvanceWage(int trainerId, long gold)
        {
            CommandResult check = CheckTransfer(trainerId, gold);
            if (!check.Ok) return check;

            Trainer t = trainers[trainerId];
            treasury.TrySpend(gold);
            t.Gold += gold;
            t.WageAdvance += gold;
            Raise(new TreasuryChanged(now, -gold, treasury.Balance, "AdvanceWage"));
            WakeIfWaitingForMoney(t);
            return CommandResult.Success();
        }

        /// <summary>Đặt giá bán của một công trình dịch vụ (Bệnh Viện: giá trên mỗi 10 HP). Giá phải &gt; 0.</summary>
        public CommandResult SetPrice(BuildingKind building, long price)
        {
            if (price <= 0) return CommandResult.Rejected("Giá phải lớn hơn 0.");
            buildings[(int)building].Price = price;
            return CommandResult.Success();
        }

        CommandResult CheckTransfer(int trainerId, long gold)
        {
            if (trainerId < 0 || trainerId >= trainers.Count) return CommandResult.Rejected("Trainer không tồn tại.");
            if (gold <= 0) return CommandResult.Rejected("Số Gold phải lớn hơn 0.");
            if (gold > treasury.Balance) return CommandResult.Rejected("Kho bạc không đủ Gold.");
            return CommandResult.Success();
        }

        /// <summary>
        /// Trainer đang kẹt vì hết tiền được xử lý lại ngay sau khi nhận tiền (hủy sự kiện chờ cũ bằng token).
        /// Nếu số tiền nhận được vẫn chưa đủ thì giữ nguyên lượt chờ (không đặt lại đồng hồ 24 giờ).
        /// </summary>
        void WakeIfWaitingForMoney(Trainer t)
        {
            if (t.State != TrainerState.WaitingForMoney) return;
            long needed = buildings[(int)t.PendingService].PriceFor(PersonalityProfile.Of(t.Personality), t.Roster.TotalMissingHp);
            if (t.Gold < needed) return;
            Settle(t);
            LeaveWait(t);
            t.Token++;
            SetState(t, TrainerState.AtHub, "Funded");
            OnDecide(t);
        }

        /// <summary>
        /// Kiểm tra các bất biến của mô phỏng, ném InvalidOperationException nếu vi phạm:
        /// Kho bạc không âm; thanh nhu cầu trong 0-100; mỗi Trainer không ở hai chỗ cùng lúc;
        /// Trainer đang xếp hàng (<see cref="TrainerState.Queued"/>) không có sự kiện cá nhân nào còn hiệu lực,
        /// mọi Trainer khác có đúng một; mỗi Trainer xếp hàng nằm trong đúng một hàng đợi dịch vụ.
        /// </summary>
        public void ValidateInvariants()
        {
            if (treasury.Balance < 0) throw new InvalidOperationException("Kho bạc âm.");

            var seated = new HashSet<int>();
            var waitingIds = new HashSet<int>();
            foreach (ServiceBuilding b in buildings)
            {
                foreach (int id in b.Occupants)
                    if (!seated.Add(id)) throw new InvalidOperationException($"Trainer {id} đang ở hai chỗ cùng lúc.");
                foreach (int id in b.Waiting)
                    if (!waitingIds.Add(id)) throw new InvalidOperationException($"Trainer {id} đang xếp hàng ở nhiều chỗ.");
            }
            foreach (int id in waitingIds)
                if (seated.Contains(id)) throw new InvalidOperationException($"Trainer {id} vừa xếp hàng vừa đang được phục vụ.");

            foreach (Trainer t in trainers)
            {
                Needs n = t.Needs;
                if (n.Stamina < 0 || n.Stamina > 100 || n.Satiety < 0 || n.Satiety > 100
                    || n.Hydration < 0 || n.Hydration > 100 || n.Stress < 0 || n.Stress > 100)
                    throw new InvalidOperationException($"Trainer {t.Id} có thanh nhu cầu ngoài khoảng 0-100.");
            }

            var pending = new int[trainers.Count];
            foreach (SimEvent e in queue.Snapshot)
                if (e.TrainerId >= 0 && trainers[e.TrainerId].Token == e.Token) pending[e.TrainerId]++;
            for (int i = 0; i < pending.Length; i++)
            {
                bool queued = trainers[i].State == TrainerState.Queued;
                int expected = queued ? 0 : 1;   // người xếp hàng chờ SeatWaiting gọi; mọi trạng thái khác luôn có đúng một sự kiện kế tiếp
                if (pending[i] != expected)
                    throw new InvalidOperationException($"Trainer {i} ({trainers[i].State}) có {pending[i]} sự kiện đang chờ, cần đúng {expected}.");
                if (queued != waitingIds.Contains(i))
                    throw new InvalidOperationException($"Trainer {i} ({trainers[i].State}) không khớp với hàng đợi dịch vụ.");
            }
        }
    }
}
