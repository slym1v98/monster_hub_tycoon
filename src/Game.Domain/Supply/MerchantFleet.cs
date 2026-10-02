using System;
using System.Collections.Generic;

namespace Game.Domain.Supply
{
    /// <summary>Quản lý Merchant hiện tại và lịch thay thế sau phá sản.</summary>
    public sealed class MerchantFleet
    {
        private readonly long startingCash;
        private readonly int replacementDelayMinutes;
        private readonly MerchantConfig config;
        private readonly MoneyLedger ledger;
        private readonly List<Merchant> formerMerchants = new List<Merchant>();
        private int nextId;
        private int? replacementMinute;
        public Merchant Current { get; private set; }
        public int? ReplacementMinute => replacementMinute;
        public IReadOnlyList<Merchant> FormerMerchants => formerMerchants.AsReadOnly();

        public MerchantFleet(long startingCash, int replacementDelayMinutes, MerchantConfig config, MoneyLedger ledger)
        {
            if (startingCash < 0) throw new ArgumentOutOfRangeException(nameof(startingCash));
            if (replacementDelayMinutes < 0) throw new ArgumentOutOfRangeException(nameof(replacementDelayMinutes));
            this.startingCash = startingCash;
            this.replacementDelayMinutes = replacementDelayMinutes;
            this.config = config ?? throw new ArgumentNullException(nameof(config));
            this.ledger = ledger ?? throw new ArgumentNullException(nameof(ledger));
            Current = NewMerchant();
        }

        public MerchantFleet(MerchantConfig config, MoneyLedger ledger)
            : this((config ?? throw new ArgumentNullException(nameof(config))).StartingCash,
                  config.ReplacementDelayMinutes, config, ledger)
        { }

        /// <summary>Phát hiện phá sản, lên lịch rồi đưa Merchant mới tới khi đến hạn.</summary>
        public bool AdvanceTo(int minute)
        {
            if (minute < 0) throw new ArgumentOutOfRangeException(nameof(minute));
            if (!Current.IsBankrupt) return false;
            if (!replacementMinute.HasValue) replacementMinute = checked(minute + replacementDelayMinutes);
            if (minute < replacementMinute.Value) return false;
            formerMerchants.Add(Current);
            Current = NewMerchant();
            replacementMinute = null;
            return true;
        }

        private Merchant NewMerchant() => new Merchant("route_merchant_" + (++nextId), startingCash, config, ledger);
    }
}
