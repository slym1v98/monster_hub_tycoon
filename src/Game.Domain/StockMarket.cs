using System;

namespace Game.Domain
{
    /// <summary>Gia co phieu HUB Street: bien dong ngau nhien moi ngay + tac dong cua Traffic. Sau thoi gian thu thap goc 100.</summary>
    public sealed class StockMarket
    {
        public double Price = 100;
        public double DailyVolatility = 0.03;
        public double TrafficSensitivity = 0.5;   // gia doi 0.5% cho moi 1% Traffic doi
        readonly Random rng;

        public StockMarket(int seed) { rng = new Random(seed); }

        double Gaussian()
        {
            double u1 = 1.0 - rng.NextDouble(), u2 = rng.NextDouble();
            return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
        }

        /// <summary>trafficChange: ty le thay doi Traffic so voi ngay truoc (0.1 = +10%).</summary>
        public double StepDay(double trafficChange = 0)
        {
            double change = DailyVolatility * Gaussian() + TrafficSensitivity * trafficChange;
            Price = Math.Max(1, Price * (1 + change));
            return Price;
        }
    }
}
