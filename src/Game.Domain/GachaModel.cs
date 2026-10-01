using System;

namespace Game.Domain
{
    /// <summary>Thu Moi Hoang Gia: ky vong so luot toi Rarity muc tieu, co hard pity.</summary>
    public static class GachaModel
    {
        /// <summary>E[so luot] de nhan >=1 muc tieu voi xac suat p moi luot, hard pity sau pity luot.</summary>
        public static double ExpectedPulls(double p, int pity)
        {
            if (p <= 0 || p > 1) throw new ArgumentOutOfRangeException(nameof(p));
            // E = sum_{k=0}^{pity-1} (1-p)^k
            double e = 0, q = 1;
            for (int k = 0; k < pity; k++) { e += q; q *= 1 - p; }
            return e;
        }
    }
}
