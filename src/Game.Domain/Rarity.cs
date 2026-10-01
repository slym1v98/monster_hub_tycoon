using System;

namespace Game.Domain
{
    public enum Rarity { Common = 0, Rare = 1, Epic = 2, Legendary = 3, Ultimate = 4 }

    public static class RarityScale
    {
        /// <summary>He so quy mo thu nhap/chi tieu theo bac Rarity (khoi diem, xem docs/designs/13).</summary>
        public static double Of(Rarity r, double growth) => Math.Pow(growth, (int)r);
    }
}
