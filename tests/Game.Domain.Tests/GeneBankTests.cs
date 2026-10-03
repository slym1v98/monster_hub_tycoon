using Game.Domain.Monsters;
using Xunit;

namespace Game.Domain.Tests
{
    public sealed class GeneBankTests
    {
        static MonsterDefinition Def(string id, long hp, double attack) => new MonsterDefinition(id, id,
            MonsterElement.Grass, MonsterRole.Support, new MonsterStats(hp, attack, 5, 1, 0.05),
            new MonsterStats(0, 0, 0, 0, 0));
        static Trainer MakeTrainer(int id, long gold = 1000)
        {
            var t = new Trainer { Id = id, Rarity = Rarity.Common, Personality = Personality.Capitalist, Gold = gold };
            for (int i = 0; i < 3; i++)
                t.Roster.Add(Monster.Create(new MonsterId("t" + id + "_" + i), Def("s" + i, 100 + 20 * i, 10 + i),
                    Rarity.Common, MonsterIvGrade.B, 1, i));
            return t;
        }

        [Fact]
        public void StoreWithdrawAndOwnershipTransitionsNeverDuplicateMonster()
        {
            var trainer = MakeTrainer(0);
            var reserve = Monster.Create(new MonsterId("reserve"), Def("reserve_species", 80, 8), Rarity.Common,
                MonsterIvGrade.B, 1, 9);
            trainer.Roster.AddToStorage(reserve);
            var bank = new GeneBank(new[] { trainer });
            var id = reserve.Id;
            var monster = reserve;
            Assert.True(bank.Store(0, id));
            Assert.Equal(MonsterCustody.GeneBank, monster.Custody);
            Assert.DoesNotContain(trainer.Roster.Members, x => x.Id == id);
            Assert.False(bank.Store(0, id));
            Assert.True(bank.Withdraw(0, id));
            Assert.Equal(MonsterCustody.Trainer, monster.Custody);
            Assert.Contains(trainer.Roster.Storage, x => x.Id == id); // full battle team; restore ownership into storage
            Assert.DoesNotContain(bank.StoredMonsters, x => x.Id == id);
        }

        [Fact]
        public void PaydayFeeUsesTwentyGoldPerMonsterForThirtyDaysAndSupportsCustomPeriod()
        {
            var trainer = MakeTrainer(0);
            var bank = new GeneBank(new[] { trainer });
            Assert.True(bank.Store(0, trainer.Roster.Members[0].Id));
            Assert.True(bank.Store(0, trainer.Roster.Members[0].Id));
            Assert.Equal(1200, bank.CalculatePaydayFee(0).Amount);
            var assessment = bank.CalculatePaydayFee(0, 7);
            Assert.Equal(2, assessment.MonsterCount);
            Assert.Equal(280, assessment.Amount);
        }

        [Fact]
        public void UnpaidFeeConfiscatesOnlyWeakestOneAndHubCanResellItToAnotherTrainer()
        {
            var owner = MakeTrainer(0);
            var buyer = MakeTrainer(1);
            var bank = new GeneBank(new[] { owner, buyer }, new GeneBankConfig(resalePrice: 75));
            var weakest = owner.Roster.Members[0];
            Assert.True(bank.Store(0, weakest.Id));
            Assert.True(bank.Store(0, owner.Roster.Members[0].Id));
            var seized = bank.ConfiscateForUnpaidFee(0);
            Assert.Equal(weakest.Id, seized.Id);
            Assert.Equal(MonsterCustody.Hub, seized.Custody);
            Assert.Equal(1, bank.Count);
            Assert.Equal(1, bank.ConfiscatedCount);
            long gold = buyer.Gold;
            Assert.True(bank.ResellToTrainer(1, seized.Id));
            Assert.Equal(gold - 75, buyer.Gold);
            Assert.Equal(MonsterCustody.Trainer, seized.Custody);
            Assert.Contains(buyer.Roster.Storage, x => x.Id == seized.Id);
            Assert.Equal(0, bank.ConfiscatedCount);
            var secondSeized = bank.ConfiscateForUnpaidFee(0);
            Assert.NotNull(secondSeized); // one item per fee-triggered call
            Assert.NotEqual(seized.Id, secondSeized.Id);
            Assert.Null(bank.ConfiscateForUnpaidFee(0));
        }

        [Fact]
        public void BankCapacityRejectsNewDepositWithoutChangingTrainerOwnership()
        {
            var trainer = MakeTrainer(0);
            var bank = new GeneBank(new[] { trainer }, new GeneBankConfig(capacity: 1));
            var first = trainer.Roster.Members[0].Id;
            var second = trainer.Roster.Members[1].Id;
            Assert.True(bank.Store(0, first));
            var rejected = trainer.Roster.Members[1];
            Assert.False(bank.Store(0, second));
            Assert.Equal(MonsterCustody.Trainer, rejected.Custody);
            Assert.Contains(trainer.Roster.Members, x => x.Id == second);
        }
    }
}
