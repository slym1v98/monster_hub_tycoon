using System;
using System.Collections.Generic;

namespace Game.Domain
{
    /// <summary>
    /// Công trình dịch vụ: số chỗ cố định, hàng đợi FIFO, giá do Giám đốc đặt.
    /// Lớp này chỉ giữ chỗ và hàng đợi; việc thanh toán và hồi nhu cầu do HubWorld xử lý.
    /// </summary>
    public sealed class ServiceBuilding
    {
        readonly List<int> occupants = new List<int>();
        readonly Queue<int> waiting = new Queue<int>();

        public readonly BuildingKind Kind;
        public int Level;
        public readonly long FairPrice;
        /// <summary>Giá Giám đốc đặt. Bệnh Viện: giá trên mỗi 10 HP.</summary>
        public long Price;
        public readonly int BaseServiceMinutes;
        public readonly long UpkeepPerDay;
        /// <summary>false khi thiếu tiền vận hành (Thiếu bảo trì): số chỗ chia đôi.</summary>
        public bool Maintained = true;

        public ServiceBuilding(BuildingSpec spec, int level, long upkeepPerDay)
        {
            if (spec == null) throw new ArgumentNullException(nameof(spec));
            if (level < 1) throw new ArgumentOutOfRangeException(nameof(level));
            Kind = spec.Kind; Level = level;
            FairPrice = spec.FairPrice; Price = spec.FairPrice;
            BaseServiceMinutes = spec.ServiceMinutes; UpkeepPerDay = upkeepPerDay;
        }

        /// <summary>Số chỗ khi vận hành bình thường: 5 + floor(0.8 x cấp), tính bằng số nguyên.</summary>
        public int FullSlots => 5 + (Level * 4) / 5;
        public int Slots => Maintained ? FullSlots : FullSlots / 2;
        public int Occupied => occupants.Count;
        public int QueueLength => waiting.Count;
        /// <summary>Độ dài hàng đợi lớn nhất từng gặp (dùng cho báo cáo Game.Sim).</summary>
        public int MaxQueueLength { get; private set; }
        public IReadOnlyList<int> Occupants => occupants;

        /// <summary>Đưa Trainer vào hàng đợi. Chưa xếp chỗ; gọi <see cref="PromoteWaiting"/> sau đó.</summary>
        public void Enqueue(int trainerId)
        {
            waiting.Enqueue(trainerId);
        }

        /// <summary>Xếp người đang chờ vào các chỗ trống theo thứ tự vào hàng; trả về các id vừa được xếp chỗ.</summary>
        public List<int> PromoteWaiting()
        {
            var seated = new List<int>();
            while (waiting.Count > 0 && occupants.Count < Slots)
            {
                int id = waiting.Dequeue();
                occupants.Add(id);
                seated.Add(id);
            }
            if (waiting.Count > MaxQueueLength) MaxQueueLength = waiting.Count;
            return seated;
        }

        /// <summary>Nhả chỗ của một Trainer. Không tự gọi người kế tiếp.</summary>
        public void Leave(int trainerId) => occupants.Remove(trainerId);

        /// <summary>
        /// Giá một lượt cho Trainer này, đã trừ giảm giá tính cách (làm tròn lên).
        /// Bệnh Viện tính theo mỗi 10 HP còn thiếu (làm tròn lên số đơn vị).
        /// </summary>
        public long PriceFor(PersonalityProfile profile, long missingHp)
        {
            double raw = Price;
            if (Kind == BuildingKind.Hospital)
            {
                if (missingHp <= 0) return 0;
                raw = Price * ((missingHp + 9) / 10);
            }
            return (long)Math.Ceiling(raw * (1.0 - profile.PriceDiscount));
        }

        /// <summary>
        /// Thời gian phục vụ một lượt. Nhà Trọ ban đêm ngủ tới sáng (tối đa 360 phút, tối thiểu 30 phút).
        /// </summary>
        public int ServiceMinutesFor(int nowMinute)
        {
            if (Kind == BuildingKind.Inn && SimClock.IsNight(nowMinute))
            {
                int untilDawn = SimClock.NextMinuteOfDay(nowMinute, SimClock.DawnMinute) - nowMinute;
                return Math.Max(30, Math.Min(BaseServiceMinutes, untilDawn));
            }
            return BaseServiceMinutes;
        }
    }
}
