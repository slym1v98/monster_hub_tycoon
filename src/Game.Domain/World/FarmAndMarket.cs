using System;

namespace Game.Domain
{
    /// <summary>Kết quả một khúc farm.</summary>
    public sealed record FarmResult(int MaterialUnits, long Gold, long HpLost);

    /// <summary>Điểm cắm cho chiến đấu thật (sub-project 3): tính kết quả farm của một Trainer trong <paramref name="minutes"/> phút.</summary>
    public interface IFarmResolver
    {
        FarmResult Resolve(Trainer trainer, int minutes);
    }

    /// <summary>Bộ giải farm tạm: công thức đơn giản theo tính cách và Rarity, có nhiễu ±20%.</summary>
    public sealed class SimpleFarmResolver : IFarmResolver
    {
        readonly SimConfig cfg;
        readonly SimRandom rng;

        public SimpleFarmResolver(SimConfig config, SimRandom random)
        {
            cfg = config; rng = random;
        }

        public FarmResult Resolve(Trainer trainer, int minutes)
        {
            PersonalityProfile p = PersonalityProfile.Of(trainer.Personality);
            double rarityScale = Math.Pow(cfg.RarityGrowth, (int)trainer.Rarity);
            double chunks = minutes / (double)cfg.FarmChunkMinutes;
            double noise = 0.8 + 0.4 * rng.NextDouble();   // một lần bốc ngẫu nhiên cho cả khúc

            int units = (int)Math.Round(cfg.FarmMaterialsPerChunk * chunks * p.MaterialPickRate * p.LootMult * rarityScale * noise);
            long gold = (long)Math.Round(cfg.FarmGoldPerChunk * chunks * p.LootMult * rarityScale * noise);
            long hpLost = (long)Math.Round(cfg.FarmHpLostPerChunk * chunks * p.HpLossMult * rarityScale * noise);
            return new FarmResult(units, gold, hpLost);
        }
    }

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
