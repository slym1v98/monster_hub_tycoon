using Game.Domain.Combat;
using Game.Domain.Materials;
using Game.Domain.Monsters;
using Xunit;

namespace Game.Domain.Tests
{
    public sealed class CaptureResolverTests
    {
        static MonsterDefinition Definition(string id, long hp, double attack) => new MonsterDefinition(id, id,
            MonsterElement.Fire, MonsterRole.Dps, new MonsterStats(hp, attack, 5, 1, 0.05),
            new MonsterStats(0, 0, 0, 0, 0));

        static MonsterSnapshot Target(long hp, long maxHp) => new MonsterSnapshot(new MonsterId("wild"),
            MonsterElement.Fire, new MonsterStats(maxHp, 10, 5, 1, 0.05), hp, new[] { "fire_strike" });

        static CaptureInputs Inputs(int balls = 1, int traps = 1, bool replacementEligible = true,
            double dexterity = 10, TrainerClass trainerClass = TrainerClass.None, int ballTier = 1, int trapTier = 1)
            => new CaptureInputs("wild_species", Rarity.Rare, 5, balls, traps, replacementEligible,
                dexterity, trainerClass, ballTier, trapTier);

        [Fact]
        public void ShouldAttemptRequiresBallWeakTargetAndGddOrReplacementRule()
        {
            var roster = new MonsterRoster();
            roster.Add(Monster.Create(new MonsterId("team"), Definition("team", 100, 10), Rarity.Rare,
                MonsterIvGrade.B, 5, 1));
            var inventory = new TrainerInventory();
            var trainer = new TrainerAttributes(10, 0, 0, 0);
            var config = CaptureConfig.Prototype;

            Assert.False(CaptureResolver.ShouldAttempt(Definition("strong", 50, 8), Rarity.Epic, 5, 50, 50,
                roster, trainer, inventory, config));
            inventory.Add(new ProductId("capture_ball"), 1);
            Assert.False(CaptureResolver.ShouldAttempt(Definition("strong", 50, 8), Rarity.Epic, 5, 40, 50,
                roster, trainer, inventory, config)); // still above weak HP threshold
            Assert.True(CaptureResolver.ShouldAttempt(Definition("rare", 40, 6), Rarity.Epic, 5, 10, 50,
                roster, trainer, inventory, config)); // better rarity, even with lower power
            Assert.True(CaptureResolver.ShouldAttempt(Definition("strong", 200, 20), Rarity.Common, 5, 10, 50,
                roster, trainer, inventory, config)); // stronger power, even with lower rarity
            Assert.False(CaptureResolver.ShouldAttempt(Definition("weak", 1, 1), Rarity.Common, 5, 10, 50,
                roster, trainer, inventory, config));
        }

        [Fact]
        public void ResolveRequiresBallAndEligibleWeakReplacementAndDoesNotConsumeWhenSkipped()
        {
            var skipped = CaptureResolver.Resolve(Target(50, 100), Inputs(balls: 0), CaptureConfig.Prototype, new SimRandom(3));
            Assert.False(skipped.Attempted);
            Assert.Equal(0, skipped.ConsumedBallCount);
            Assert.Null(skipped.Roll);
            var notWeak = CaptureResolver.Resolve(Target(90, 100), Inputs(), CaptureConfig.Prototype, new SimRandom(3));
            Assert.False(notWeak.Attempted);
            var noReplacement = CaptureResolver.Resolve(Target(10, 100), Inputs(replacementEligible: false), CaptureConfig.Prototype, new SimRandom(3));
            Assert.False(noReplacement.Attempted);
        }

        [Fact]
        public void ResolveReportsChanceInputsDeterministicRollAndConsumesItemsByConfiguredOutcome()
        {
            var config = new CaptureConfig(baseChance: 0.1, hpDepletionBonus: 0.4, ballTierBonus: 0.03,
                trapTierBonus: 0.05, dexterityScale: 0.005, trapperBonus: 0.1, rarityPenalty: 0.01,
                minimumChance: 0.02, maximumChance: 0.95, weakHpFraction: 0.3, consumeTrapOnFailure: false);
            var inputs = Inputs(trainerClass: TrainerClass.Trapper, dexterity: 20, ballTier: 2, trapTier: 1);
            var a = CaptureResolver.Resolve(Target(20, 100), inputs, config, new SimRandom(42));
            var b = CaptureResolver.Resolve(Target(20, 100), inputs, config, new SimRandom(42));
            Assert.True(a.Attempted);
            Assert.Equal(a.Chance, b.Chance);
            Assert.Equal(a.Roll, b.Roll);
            Assert.Equal(a.Success, b.Success);
            Assert.Equal(a.Generation?.Seed, b.Generation?.Seed);
            Assert.Equal(a.Generation?.Iv, b.Generation?.Iv);
            Assert.Equal(1, a.ConsumedBallCount);
            Assert.Equal(a.Success ? 1 : 0, a.ConsumedTrapCount);
            Assert.Equal(a.Success, a.Generation != null);
            if (a.Generation != null)
            {
                Assert.Equal("wild_species", a.Generation.SpeciesId);
                Assert.Equal(Rarity.Rare, a.Generation.Rarity);
                Assert.InRange((int)a.Generation.Iv, 0, 6);
            }
        }

        [Fact]
        public void BetterBallDexterityTrapperAndTrapImproveOdds()
        {
            var target = Target(20, 100);
            var baseline = CaptureResolver.CalculateChance(target, Inputs(dexterity: 0, traps: 0, ballTier: 0, trapTier: 0), CaptureConfig.Prototype);
            Assert.True(CaptureResolver.CalculateChance(target, Inputs(dexterity: 20, traps: 0, ballTier: 0, trapTier: 0), CaptureConfig.Prototype) > baseline);
            Assert.True(CaptureResolver.CalculateChance(target, Inputs(dexterity: 0, traps: 0, ballTier: 2, trapTier: 0), CaptureConfig.Prototype) > baseline);
            Assert.True(CaptureResolver.CalculateChance(target, Inputs(dexterity: 0, traps: 1, ballTier: 0, trapTier: 2), CaptureConfig.Prototype) > baseline);
            Assert.True(CaptureResolver.CalculateChance(target, Inputs(dexterity: 0, traps: 0, ballTier: 0, trapTier: 0,
                trainerClass: TrainerClass.Trapper), CaptureConfig.Prototype) > baseline);
            var higherRarity = new CaptureInputs("wild", Rarity.Legendary, 5, 1, 0, true, 0, TrainerClass.None);
            Assert.True(CaptureResolver.CalculateChance(target, higherRarity, CaptureConfig.Prototype) < baseline);
        }

        [Fact]
        public void FailedCaptureConsumesBallAndUsesConfiguredTrapFailurePolicy()
        {
            var config = new CaptureConfig(baseChance: 0, hpDepletionBonus: 0, ballTierBonus: 0,
                trapTierBonus: 0, dexterityScale: 0, trapperBonus: 0, rarityPenalty: 0,
                minimumChance: 0, maximumChance: 0, weakHpFraction: 0.3, consumeTrapOnFailure: true);
            var result = CaptureResolver.Resolve(Target(20, 100), Inputs(), config, new SimRandom(1));
            Assert.True(result.Attempted);
            Assert.False(result.Success);
            Assert.Equal(1, result.ConsumedBallCount);
            Assert.Equal(1, result.ConsumedTrapCount);
            Assert.Null(result.Generation);
        }
    }
}
