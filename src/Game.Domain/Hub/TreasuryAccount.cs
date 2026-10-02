using System;

namespace Game.Domain
{
    /// <summary>Kho bạc của Giám đốc. Số dư không bao giờ âm.</summary>
    public sealed class TreasuryAccount
    {
        public long Balance { get; private set; }

        public TreasuryAccount(long initial)
        {
            if (initial < 0) throw new ArgumentOutOfRangeException(nameof(initial));
            Balance = initial;
        }

        public void Add(long amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            Balance = checked(Balance + amount);
        }

        /// <summary>Trừ tiền nếu đủ; trả về false và giữ nguyên số dư nếu không đủ.</summary>
        public bool TrySpend(long amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            if (amount > Balance) return false;
            Balance -= amount;
            return true;
        }
    }
}
