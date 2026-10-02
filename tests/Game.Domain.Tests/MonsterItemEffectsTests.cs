using Game.Domain.Combat;
using Game.Domain.Materials;
using Game.Domain.Monsters;
using Xunit;

namespace Game.Domain.Tests
{
    public sealed class MonsterItemEffectsTests
    {
        static Monster Make() => Monster.Create(new MonsterId("item-test"),
            new MonsterDefinition("item-test-species", "Item Test", MonsterElement.Grass, MonsterRole.Dps,
                new MonsterStats(100, 20, 10, 1, .1), new MonsterStats(0, 0, 0, 0, 0)),
            Rarity.Common, MonsterIvGrade.B, 1, 7);

        [Fact]
        public void PotionHealsOnlyWhenInjuredAndCapsAtMaximumHp()
        {
            var monster = Make();
            Assert.False(MonsterItemEffects.Apply(new ProductId("potion"), monster, MonsterItemConfig.Prototype).Applied);
            monster.SetCurrentHp(30);
            var result = MonsterItemEffects.Apply(new ProductId("potion"), monster, MonsterItemConfig.Prototype);
            Assert.True(result.Applied); Assert.Equal(1, result.ConsumedUnits); Assert.Equal(70, result.HpRestored);
            Assert.Equal(100, monster.CurrentHp);
        }

        [Fact]
        public void BuffBottleExpiresAtConfiguredMinute()
        {
            var monster = Make(); var config = new MonsterItemConfig(buffDurationMinutes: 10);
            var result = MonsterItemEffects.Apply(new ProductId("monster_buff_bottle"), monster, config, currentMinute: 5,
                managementScore: 0, leadershipScore: 0, hasEligibleReserve: false, hasCommunicationLock: false);
            Assert.True(result.Applied); Assert.Equal(15, result.ExpiresAtMinute);
            var during = MonsterSnapshot.FromMonster(monster, new[] { "grass_strike" }, totalMinutes: 14);
            var after = MonsterSnapshot.FromMonster(monster, new[] { "grass_strike" }, totalMinutes: 15);
            Assert.Equal(25, during.Stats.Attack); Assert.Equal(12, during.Stats.Defense); Assert.Equal(.15, during.Stats.CriticalChance, 8);
            Assert.Equal(20, after.Stats.Attack); Assert.Equal(10, after.Stats.Defense); Assert.Equal(.1, after.Stats.CriticalChance, 8);
        }

        [Fact]
        public void RewardCakeRequiresRebellionAndTemporarilyReducesManagementScore()
        {
            var monster = Make(); var item = new ProductId("reward_cake");
            Assert.False(MonsterItemEffects.Apply(item, monster, MonsterItemConfig.Prototype, 0, 20, 20, false, false).Applied);
            var result = MonsterItemEffects.Apply(item, monster, MonsterItemConfig.Prototype, 0, 30, 20, false, false);
            Assert.True(result.Applied); Assert.Equal(1, result.ConsumedUnits);
            Assert.Equal(10, MonsterSnapshot.FromMonster(monster, new[] { "grass_strike" }, totalMinutes: 1).ManagementScoreReduction);
            Assert.Equal(0, MonsterSnapshot.FromMonster(monster, new[] { "grass_strike" }, totalMinutes: result.ExpiresAtMinute).ManagementScoreReduction);
        }

        [Fact]
        public void TacticsBookRequiresEligibleReserveAndCommunicationLockAddsLeadership()
        {
            var monster = Make();
            Assert.False(MonsterItemEffects.Apply(new ProductId("tactics_book"), monster, MonsterItemConfig.Prototype, 0, 0, 0, false, false).Applied);
            var tactics = MonsterItemEffects.Apply(new ProductId("tactics_book"), monster, MonsterItemConfig.Prototype, 0, 0, 0, true, false);
            Assert.True(tactics.BagSynergyEnabled); Assert.Equal(1, tactics.ConsumedUnits);
            var lockResult = MonsterItemEffects.Apply(new ProductId("pet_communication_lock"), monster, MonsterItemConfig.Prototype, 0, 0, 0, false, false);
            Assert.Equal(10, lockResult.LeadershipBonus);
            Assert.False(MonsterItemEffects.Apply(new ProductId("pet_communication_lock"), monster, MonsterItemConfig.Prototype, 0, 0, 0, false, true).Applied);
        }
    }
}
