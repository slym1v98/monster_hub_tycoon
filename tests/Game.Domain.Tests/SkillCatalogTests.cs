using System;
using System.Collections.Generic;
using System.Linq;
using Game.Domain.Combat;
using Game.Domain.Monsters;
using Xunit;

namespace Game.Domain.Tests
{
    public sealed class SkillCatalogTests
    {
        static SkillDefinition Definition(string id = "test_strike", MonsterElement element = MonsterElement.Fire,
            double power = 10, int cooldownActions = 0, SkillTargetRule targetRule = SkillTargetRule.SingleEnemy,
            string effectId = null, string balanceStatus = "Prototype") =>
            new SkillDefinition(id, element, power, cooldownActions, targetRule, effectId, balanceStatus);

        [Fact]
        public void CatalogRejectsDuplicateSkillIds()
        {
            Assert.Throws<ArgumentException>(() => new SkillCatalog(new[] { Definition(), Definition(power: 20) }));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" \t")]
        public void CatalogRejectsMissingSkillIds(string id)
        {
            Assert.Throws<ArgumentException>(() => new SkillCatalog(new[] { Definition(id) }));
        }

        [Fact]
        public void CatalogRejectsNullEmptyAndNullEntryDefinitions()
        {
            Assert.Throws<ArgumentNullException>(() => new SkillCatalog(null));
            Assert.Throws<ArgumentException>(() => new SkillCatalog(Array.Empty<SkillDefinition>()));
            Assert.Throws<ArgumentException>(() => new SkillCatalog(new SkillDefinition[] { null }));
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(9)]
        [InlineData(int.MaxValue)]
        public void CatalogRejectsUnknownElements(int element)
        {
            Assert.Throws<ArgumentException>(() => new SkillCatalog(new[] { Definition(element: (MonsterElement)element) }));
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(int.MaxValue)]
        public void CatalogRejectsUnknownTargetRules(int rule)
        {
            Assert.Throws<ArgumentException>(() => new SkillCatalog(new[] { Definition(targetRule: (SkillTargetRule)rule) }));
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(double.NaN)]
        [InlineData(double.PositiveInfinity)]
        [InlineData(double.NegativeInfinity)]
        public void CatalogRejectsNegativeAndNonfinitePower(double power)
        {
            Assert.Throws<ArgumentException>(() => new SkillCatalog(new[] { Definition(power: power) }));
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(int.MinValue)]
        public void CatalogRejectsNegativeCooldownActions(int cooldown)
        {
            Assert.Throws<ArgumentException>(() => new SkillCatalog(new[] { Definition(cooldownActions: cooldown) }));
        }

        [Theory]
        [InlineData(SkillTargetRule.Self)]
        [InlineData(SkillTargetRule.SingleEnemy)]
        [InlineData(SkillTargetRule.AllEnemies)]
        [InlineData(SkillTargetRule.SingleAlly)]
        [InlineData(SkillTargetRule.AllAllies)]
        public void CatalogPreservesConfiguredSkillData(SkillTargetRule targetRule)
        {
            var catalog = new SkillCatalog(new[] { Definition("support", MonsterElement.Light, 0, 3,
                targetRule, "regen", "TBD") }, new[] { "regen" });
            var skill = catalog.Get("support");
            Assert.Equal(MonsterElement.Light, skill.Element);
            Assert.Equal(0, skill.Power);
            Assert.Equal(3, skill.CooldownActions);
            Assert.Equal(targetRule, skill.TargetRule);
            Assert.Equal("regen", skill.EffectId);
            Assert.Equal("TBD", skill.BalanceStatus);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("Locked")]
        [InlineData("prototype")]
        public void SkillBalanceValuesRequirePrototypeOrTbdStatus(string status)
        {
            Assert.Throws<ArgumentException>(() => new SkillCatalog(new[] { Definition(balanceStatus: status) }));
        }

        [Fact]
        public void CatalogRejectsUnknownEffectReferences()
        {
            Assert.Throws<ArgumentException>(() => new SkillCatalog(new[] { Definition(effectId: "regen") }));
            Assert.Throws<ArgumentException>(() => new SkillCatalog(new[] { Definition(effectId: "Regen") }, new[] { "regen" }));
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        public void CatalogRejectsBlankOptionalEffectReferences(string effectId)
        {
            Assert.Throws<ArgumentException>(() => new SkillCatalog(new[] { Definition(effectId: effectId) }));
        }

        [Fact]
        public void CatalogAcceptsSkillsWithoutEffects()
        {
            var catalog = new SkillCatalog(new[] { Definition() });
            Assert.Null(catalog.Get("test_strike").EffectId);
            Assert.Empty(catalog.EffectIds);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public void CatalogRejectsInvalidEffectRegistryIds(string id)
        {
            Assert.Throws<ArgumentException>(() => new SkillCatalog(new[] { Definition() }, new[] { id }));
        }

        [Fact]
        public void CatalogRejectsDuplicateEffectRegistryIds()
        {
            Assert.Throws<ArgumentException>(() => new SkillCatalog(new[] { Definition() }, new[] { "regen", "regen" }));
        }

        [Fact]
        public void CatalogCopiesInputsAndOrdersIdsOrdinally()
        {
            var definitions = new List<SkillDefinition> { Definition("z"), Definition("a"), Definition("A") };
            var effects = new List<string> { "z", "a", "A" };
            var catalog = new SkillCatalog(definitions, effects);
            definitions.Clear();
            effects.Clear();
            Assert.Equal(new[] { "A", "a", "z" }, catalog.Definitions.Select(x => x.Id));
            Assert.Equal(new[] { "A", "a", "z" }, catalog.EffectIds);
            Assert.Equal("A", catalog.Get("A").Id);
            Assert.Equal("a", catalog.Get("a").Id);
            Assert.Throws<NotSupportedException>(() => ((IList<SkillDefinition>)catalog.Definitions).Clear());
            Assert.Throws<NotSupportedException>(() => ((IList<string>)catalog.EffectIds).Clear());
        }

        [Fact]
        public void CatalogLookupRejectsUnknownSkillReferencesWithoutFallback()
        {
            var catalog = new SkillCatalog(new[] { Definition() });
            Assert.Throws<KeyNotFoundException>(() => catalog.Get("missing"));
            Assert.Throws<KeyNotFoundException>(() => catalog.Get("TEST_STRIKE"));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public void CatalogLookupRejectsInvalidSkillReferences(string id)
        {
            var catalog = new SkillCatalog(new[] { Definition() });
            Assert.Throws<ArgumentException>(() => catalog.Get(id));
        }

        [Fact]
        public void ResolveLoadoutRejectsUnknownSkillReferences()
        {
            var catalog = new SkillCatalog(new[] { Definition() });
            Assert.Throws<KeyNotFoundException>(() => catalog.ResolveLoadout(new[] { "test_strike", "missing" }));
            Assert.Throws<ArgumentNullException>(() => catalog.ResolveLoadout(null));
        }

        [Fact]
        public void ResolveLoadoutCopiesReferencesAndPreservesConfiguredOrder()
        {
            var catalog = new SkillCatalog(new[] { Definition("a"), Definition("z") });
            var ids = new List<string> { "z", "a" };
            var loadout = catalog.ResolveLoadout(ids);
            ids.Clear();
            Assert.Equal(new[] { "z", "a" }, loadout.Select(x => x.Id));
            Assert.Throws<NotSupportedException>(() => ((IList<SkillDefinition>)loadout).Clear());
        }

        [Fact]
        public void PrototypeCatalogProvidesStableAttackIdsForAllNineElements()
        {
            var catalog = SkillCatalog.Prototype;
            Assert.Equal(new[] { "dark_strike", "electric_strike", "fire_strike", "grass_strike", "ground_strike",
                "ice_strike", "light_strike", "poison_strike", "water_strike" }, catalog.Definitions.Select(x => x.Id));
            Assert.Equal(Enum.GetValues<MonsterElement>().OrderBy(x => x), catalog.Definitions.Select(x => x.Element).OrderBy(x => x));
            Assert.All(catalog.Definitions, skill =>
            {
                Assert.Equal("Prototype", skill.BalanceStatus);
                Assert.True(skill.Power > 0);
                Assert.True(skill.CooldownActions >= 0);
                Assert.Equal(SkillTargetRule.SingleEnemy, skill.TargetRule);
                Assert.Null(skill.EffectId);
            });
        }

        [Fact]
        public void CombatConfigUsesValidatedCatalogData()
        {
            var custom = new SkillCatalog(new[] { Definition(power: 42, cooldownActions: 7) });
            var config = new CombatConfig(custom);
            Assert.Same(custom, config.Skills);
            Assert.Equal(42, config.Skills.Get("test_strike").Power);
            Assert.Equal(7, config.Skills.Get("test_strike").CooldownActions);
            Assert.Same(SkillCatalog.Prototype, CombatConfig.Prototype.Skills);
            Assert.Equal("Prototype", CombatConfig.BalanceStatus);
            Assert.Throws<ArgumentNullException>(() => new CombatConfig(null));
        }
    }
}
