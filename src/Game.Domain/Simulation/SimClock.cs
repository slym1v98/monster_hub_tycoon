namespace Game.Domain
{
    /// <summary>Hằng số và hàm hỗ trợ về thời gian mô phỏng. Đơn vị là phút in-game (1 ngày = 15 phút thực).</summary>
    public static class SimClock
    {
        public const int MinutesPerDay = 1440;
        public const int DaysPerMonth = 30;
        public const int MinutesPerMonth = MinutesPerDay * DaysPerMonth;
        public const int DawnMinute = 6 * 60;    // 06:00, bắt đầu ban ngày
        public const int DuskMinute = 18 * 60;   // 18:00, bắt đầu ban đêm

        /// <summary>Ban đêm là 18:00 đến 06:00.</summary>
        public static bool IsNight(int totalMinutes)
        {
            int m = totalMinutes % MinutesPerDay;
            return m < DawnMinute || m >= DuskMinute;
        }

        /// <summary>Thời điểm nhỏ nhất lớn hơn hẳn <paramref name="fromMinute"/> mà phút-trong-ngày bằng <paramref name="minuteOfDay"/>.</summary>
        public static int NextMinuteOfDay(int fromMinute, int minuteOfDay)
        {
            int t = (fromMinute / MinutesPerDay) * MinutesPerDay + minuteOfDay;
            if (t <= fromMinute) t += MinutesPerDay;
            return t;
        }

        /// <summary>Phút xảy ra Payday thứ <paramref name="paydayIndex"/> (bắt đầu từ 0): phút cuối cùng của ngày 30.</summary>
        public static int PaydayMinute(int paydayIndex) => (paydayIndex + 1) * MinutesPerMonth - 1;
    }

    /// <summary>Thời điểm mô phỏng ở dạng chỉ đọc, dành cho UI.</summary>
    public readonly struct SimTime
    {
        public readonly int TotalMinutes;
        public SimTime(int totalMinutes) { TotalMinutes = totalMinutes; }

        /// <summary>Số ngày đã qua, bắt đầu từ 0.</summary>
        public int Day => TotalMinutes / SimClock.MinutesPerDay;
        public int MinuteOfDay => TotalMinutes % SimClock.MinutesPerDay;
        /// <summary>Ngày trong tháng, từ 1 đến 30.</summary>
        public int DayOfMonth => Day % SimClock.DaysPerMonth + 1;
        public bool IsNight => SimClock.IsNight(TotalMinutes);
    }
}
