namespace Game.Domain
{
    /// <summary>
    /// Bộ sinh số ngẫu nhiên tất định (SplitMix64). Tự cài đặt thay vì dùng System.Random
    /// để kết quả giống hệt giữa các nền tảng và lưu/khôi phục được trạng thái vào save.
    /// </summary>
    public sealed class SimRandom
    {
        ulong state;

        public SimRandom(int seed)
        {
            state = (ulong)(uint)seed * 0x9E3779B97F4A7C15UL + 0x1234567UL;
        }

        /// <summary>Trạng thái nội bộ, lưu vào save để chạy tiếp đúng chuỗi.</summary>
        public ulong State { get => state; set => state = value; }

        ulong NextULong()
        {
            state += 0x9E3779B97F4A7C15UL;
            ulong z = state;
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
            return z ^ (z >> 31);
        }

        /// <summary>Số thực trong [0, 1).</summary>
        public double NextDouble() => (NextULong() >> 11) * (1.0 / 9007199254740992.0);

        /// <summary>Số nguyên trong [0, maxExclusive).</summary>
        public int NextInt(int maxExclusive) => (int)(NextDouble() * maxExclusive);
    }
}
