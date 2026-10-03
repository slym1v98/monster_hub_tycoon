using Game.Domain.Materials;
using Game.Domain.Monsters;
using Xunit;

namespace Game.Domain.Tests
{
    public sealed class GeneticLabTests
    {
        static MonsterDefinition Def(string id = "test") => new MonsterDefinition(id, id, MonsterElement.Grass,
            MonsterRole.Support, new MonsterStats(100, 10, 5, 1, 0.05), new MonsterStats(2, 1, 1, 0, 0));
        static Trainer Make(int level, MonsterIvGrade iv, int rank = 1)
        {
            var trainer = new Trainer { Id = 1, Rank = rank, Level = level, Rarity = Rarity.Common,
                Personality = Personality.Warlike, Gold = 1000 };
            trainer.Roster.Add(Monster.Create(new MonsterId("specimen"), Def(), Rarity.Rare, iv, 1, 15));
            return trainer;
        }

        [Fact]
        public void AppraisalRevealsExistingIvOnceAndChargesOnlyFirstTime()
        {
            var trainer = Make(1, MonsterIvGrade.S);
            var lab = new GeneticLab(trainer);
            var monster = trainer.Roster.Active;
            Assert.False(monster.IsIvAppraised);
            Assert.Null(monster.KnownIv);
            var first = lab.Appraise(monster.Id);
            Assert.True(first.Applied);
            Assert.Equal(MonsterIvGrade.S, first.RevealedIv);
            Assert.Equal(975, trainer.Gold);
            var second = lab.Appraise(monster.Id);
            Assert.False(second.Applied);
            Assert.Equal(975, trainer.Gold);
            Assert.Equal(MonsterIvGrade.S, monster.KnownIv);
        }

        [Theory]
        [InlineData(MonsterIvGrade.D, 5)]
        [InlineData(MonsterIvGrade.C, 5)]
        public void DAndCIvSpecimensDismantleForConfiguredFragments(MonsterIvGrade iv, int expectedFragments)
        {
            var trainer = Make(1, iv);
            var result = new GeneticLab(trainer).Dismantle(new MonsterId("specimen"));
            Assert.True(result.Applied);
            Assert.Equal(expectedFragments, result.GeneFragments);
            Assert.Equal(expectedFragments, trainer.Inventory.Count(new ProductId("gene_fragment")));
            Assert.Empty(trainer.Roster.Members);
        }

        [Fact]
        public void BetterIvCannotBeDismantledAndLeavesRosterAndInventoryUntouched()
        {
            var trainer = Make(1, MonsterIvGrade.B);
            var result = new GeneticLab(trainer).Dismantle(new MonsterId("specimen"));
            Assert.False(result.Applied);
            Assert.Single(trainer.Roster.Members);
            Assert.Equal(0, trainer.Inventory.Count(new ProductId("gene_fragment")));
        }

        [Fact]
        public void BankedDAndCMonsterCanBeDismantledOnlyByItsDepositor()
        {
            var trainer = Make(1, MonsterIvGrade.D);
            var other = new Trainer { Id = 9, Rarity = Rarity.Common, Personality = Personality.Warlike };
            var monster = trainer.Roster.Active;
            var bank = new GeneBank(new[] { trainer, other });
            Assert.True(bank.Store(trainer.Id, monster.Id));
            Assert.False(new GeneticLab(other, bank).Dismantle(monster.Id).Applied);
            var result = new GeneticLab(trainer, bank).Dismantle(monster.Id);
            Assert.True(result.Applied);
            Assert.Equal(MonsterCustody.Consumed, monster.Custody);
            Assert.Equal(0, bank.Count);
            Assert.Equal(5, trainer.Inventory.Count(new ProductId("gene_fragment")));
        }

        [Fact]
        public void RarityUpgradeRequiresLevelFortyAndUsesConfiguredFourStepRates()
        {
            var config = UpgradeConfig.Prototype;
            Assert.Equal(new[] { 0.60, 0.40, 0.25, 0.10 }, config.SuccessChances);
            var below = Make(level: 95, iv: MonsterIvGrade.A, rank: 2); // Trainer maps to Monster Lv39
            below.Inventory.Add(new ProductId("enhancement_stone"), 10);
            below.Inventory.Add(new ProductId("gene_fragment"), 100);
            Assert.False(new GeneticLab(below).UpgradeRarity(new MonsterId("specimen"), default, new SimRandom(1)).Eligible);
            Assert.Equal(Rarity.Rare, below.Roster.Active.Rarity);
            var eligible = Make(level: 100, iv: MonsterIvGrade.A, rank: 2); // Trainer maps to Monster Lv40
            eligible.Inventory.Add(new ProductId("enhancement_stone"), 10);
            eligible.Inventory.Add(new ProductId("gene_fragment"), 100);
            Assert.True(new GeneticLab(eligible).UpgradeRarity(new MonsterId("specimen"), default, new SimRandom(1)).Eligible);
        }

        [Fact]
        public void FailedUpgradeWithProtectionCharmPreservesUpgradeInputs()
        {
            var trainer = Make(100, MonsterIvGrade.A, rank: 2);
            var stone = new ProductId("enhancement_stone");
            var fragments = new ProductId("gene_fragment");
            var charm = new ProductId("protection_charm");
            trainer.Inventory.Add(stone, 10);
            trainer.Inventory.Add(fragments, 100);
            trainer.Inventory.Add(charm, 1);
            var forcedFailure = new UpgradeConfig(successChances: new[] { 0d, 0d, 0d, 0d });
            var lab = new GeneticLab(trainer, upgradeConfig: forcedFailure);
            var result = lab.UpgradeRarity(new MonsterId("specimen"), charm, new SimRandom(2));
            Assert.True(result.Eligible);
            Assert.False(result.Success);
            Assert.True(result.ProtectionApplied);
            Assert.Equal(10, trainer.Inventory.Count(stone));
            Assert.Equal(100, trainer.Inventory.Count(fragments));
            Assert.Equal(0, trainer.Inventory.Count(charm));
            Assert.Equal(Rarity.Rare, trainer.Roster.Active.Rarity);
        }

        [Fact]
        public void FailedUnprotectedUpgradeConsumesCostsButDoesNotChangeRarity()
        {
            var trainer = Make(100, MonsterIvGrade.A, rank: 2);
            var stone = new ProductId("enhancement_stone");
            var fragments = new ProductId("gene_fragment");
            trainer.Inventory.Add(stone, 10);
            trainer.Inventory.Add(fragments, 100);
            var lab = new GeneticLab(trainer, upgradeConfig: new UpgradeConfig(successChances: new[] { 0d, 0d, 0d, 0d }));
            var result = lab.UpgradeRarity(new MonsterId("specimen"), default, new SimRandom(2));
            Assert.True(result.Eligible);
            Assert.False(result.Success);
            Assert.Equal(8, trainer.Inventory.Count(stone));
            Assert.Equal(80, trainer.Inventory.Count(fragments));
            Assert.Equal(Rarity.Rare, trainer.Roster.Active.Rarity);
        }

        [Fact]
        public void UpgradeWithoutInputsRejectsWithoutChangingMonsterOrInventory()
        {
            var trainer = Make(100, MonsterIvGrade.A, rank: 2);
            var result = new GeneticLab(trainer).UpgradeRarity(new MonsterId("specimen"), default, new SimRandom(2));
            Assert.False(result.Eligible);
            Assert.Equal(Rarity.Rare, trainer.Roster.Active.Rarity);
            Assert.Empty(trainer.Inventory.Products);
        }
    }
}
