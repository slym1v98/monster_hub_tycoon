using System;
namespace Game.Domain
{
    /// <summary>Kết quả bán nguyên liệu: Trainer nhận <c>GrossToTrainer - Tax</c>, Kho bạc nhận <c>Tax</c>.</summary>
    public sealed record SaleResult(long GrossToTrainer, long Tax);

    /// <summary>Điểm cắm cho Trạm Giao Thương + Thương nhân (sub-project 2). Chỉ tính toán, không đổi trạng thái.</summary>
    public interface IMaterialMarket
    {
        SaleResult Quote(Trainer seller, int units);
    }

    /// <summary>
    /// Chợ tạm: một người mua bên ngoài (đóng vai Thương nhân) trả giá cố định cho mỗi đơn vị
    /// (Gold từ ngoài vào), HUB chỉ thu thuế giao dịch. HUB không mua, không bán nguyên liệu.
    /// </summary>
    public sealed class FixedPriceMarket : IMaterialMarket
    {
        readonly SimConfig cfg;
        public FixedPriceMarket(SimConfig config) { cfg = config; }

        public SaleResult Quote(Trainer seller, int units)
        {
            long gross = units * cfg.MaterialPrice;
            long tax = (long)Math.Round(gross * cfg.TaxRate);
            return new SaleResult(gross, tax);
        }
    }
}
