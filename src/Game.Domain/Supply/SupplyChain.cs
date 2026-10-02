using System;
using Game.Domain.Production;

namespace Game.Domain.Supply
{
    /// <summary>Thành phần kinh tế dùng chung trong HubWorld: kho, Trạm, Merchant, sản xuất và sổ giao dịch.</summary>
    public sealed class SupplyChain
    {
        public MoneyLedger Ledger { get; }
        public Station Station { get; }
        public MerchantFleet Merchants { get; }
        public ProductionController Production { get; }

        public SupplyChain(MoneyLedger ledger, Station station, MerchantFleet merchants, ProductionController production)
        {
            Ledger = ledger ?? throw new ArgumentNullException(nameof(ledger));
            Station = station ?? throw new ArgumentNullException(nameof(station));
            Merchants = merchants ?? throw new ArgumentNullException(nameof(merchants));
            Production = production ?? throw new ArgumentNullException(nameof(production));
            if (!ReferenceEquals(Station.Stock, Production.Inventory))
                throw new ArgumentException("Trạm và sản xuất phải dùng chung một kho.", nameof(production));
        }
    }
}
