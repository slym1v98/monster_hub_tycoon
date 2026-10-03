using System;
using Game.Domain.Gear;
using Game.Domain;
using Xunit;

namespace Game.Domain.Tests
{
    public sealed class GearMarketTests
    {
        static GearCatalog Cat => GearCatalog.Default;
        static GearConfig Cfg => Cat.Config;

        static GearItem Item(string slot, int tier, int enh = 0, int stars = 1, GearRefineGrade refine = GearRefineGrade.Normal, int dur = -1)
            => new GearItem("m:" + slot + ":" + tier + ":" + enh + ":" + stars + ":" + (int)refine, Cat.GetSlot(slot), tier, Cat.MaxDurabilityFor(Cat.GetSlot(slot).Group), null, enh, stars, refine, dur);

        [Fact]
        public void TrainerAcceptsWhenScoreHigherAndAffordable()
        {
            var current = Item("monster.weapon", 1);
            var offered = Item("monster.weapon", 4);
            long money = 100000;
            long price = 500;
            double currentScore = GearScore.Score(current, Cat);
            double offeredScore = GearScore.Score(offered, Cat);
            var r = GearMarket.WouldAccept(current, offered, money, price, Cfg);
            Assert.True(offeredScore > currentScore);
            Assert.True(r);
        }

        [Fact]
        public void TrainerRejectsUnaffordableOffer()
        {
            var current = Item("monster.weapon", 1);
            var offered = Item("monster.weapon", 4);
            var r = GearMarket.WouldAccept(current, offered, money: 100, price: 500, Cfg);
            Assert.False(r);
        }

        [Fact]
        public void TrainerRejectsLowerScoreOffer()
        {
            var current = Item("monster.weapon", 5);
            var offered = Item("monster.weapon", 1);
            var r = GearMarket.WouldAccept(current, offered, money: 100000, price: 500, Cfg);
            Assert.False(r);
        }

        [Fact]
        public void GoldFractionGuardPreventsOverspend()
        {
            var current = Item("monster.weapon", 1);
            var offered = Item("monster.weapon", 4);
            // giá bằng 90% số tiền, vượt ngưỡng chấp nhận 50%
            long money = 10000;
            long price = (long)(money * 0.9);
            var r = GearMarket.WouldAccept(current, offered, money, price, Cfg);
            Assert.False(r);
        }

        [Fact]
        public void BuybackPriceUsesFraction()
        {
            var item = Item("monster.weapon", 3);
            long price = 1000;
            long buyback = GearMarket.BuybackPrice(item, price, Cfg);
            Assert.Equal((long)(price * Cfg.BuybackFraction), buyback);
        }

        [Fact]
        public void BrokenItemScoreZeroIsNeverBetter()
        {
            var current = Item("monster.weapon", 1);
            var offered = Item("monster.weapon", 4, dur: 0); // broken -> score 0
            var r = GearMarket.WouldAccept(current, offered, money: 100000, price: 1, Cfg);
            Assert.False(r);
        }
    }
}
