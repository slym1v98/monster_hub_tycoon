using System;
using System.Linq;
using Game.Domain.Materials;
using Game.Domain.Monsters;
using Xunit;

namespace Game.Domain.Tests
{
    public sealed class EvolutionTests
    {
        static MonsterDefinition Def(string id, MonsterElement element, MonsterRole role, long hp)
            => new MonsterDefinition(id, id, element, role, new MonsterStats(hp, 10, 5, 1, 0.05),
                new MonsterStats(2, 1, 1, 0, 0));
        static (Trainer trainer, Monster monster, EvolutionCatalog catalog) Setup(double chance = 1)
        {
            var trainer = new Trainer { Id = 2, Rank = 1, Level = 100, Rarity = Rarity.Epic,
                Personality = Personality.Warlike, Gold = 1000 };
            var first = Def("form_a", MonsterElement.Grass, MonsterRole.Support, 100);
            var second = Def("form_b", MonsterElement.Water, MonsterRole.Tank, 130);
            var third = Def("form_c", MonsterElement.Ice, MonsterRole.Dps, 160);
            var monster = Monster.Create(new MonsterId("evolver"), first, Rarity.Epic, MonsterIvGrade.S,
                12, 1234);
            trainer.Roster.Add(monster);
            var catalog = new EvolutionCatalog(new[] {
                new EvolutionDefinition("form_a", "branch_b", second, minimumLevel: 10, successChance: chance,
                    geneFragments: 3, productCost: new ProductId("evolution_stone"), productCostUnits: 1,
                    skillIds: new[] { "water_guard" }),
                new EvolutionDefinition("form_a", "branch_c", Def("form_d", MonsterElement.Dark, MonsterRole.Dps, 120),
                    minimumLevel: 10, successChance: chance, geneFragments: 5, productCost: new ProductId("evolution_stone"),
                    productCostUnits: 2, skillIds: new[] { "dark_burst" }),
                new EvolutionDefinition("form_b", "branch_final", third, minimumLevel: 20, successChance: chance,
                    geneFragments: 6, productCost: new ProductId("mutation_core"), productCostUnits: 1,
                    skillIds: new[] { "ice_charge" })
            });
            trainer.Inventory.Add(new ProductId("gene_fragment"), 50);
            trainer.Inventory.Add(new ProductId("evolution_stone"), 10);
            trainer.Inventory.Add(new ProductId("mutation_core"), 2);
            return (trainer, monster, catalog);
        }

        [Fact]
        public void CatalogAllowsZeroOneOrTwoStepsAndRequiresExplicitBranch()
        {
            var setup = Setup();
            Assert.Equal(2, setup.catalog.BranchesFrom("form_a").Count);
            Assert.Single(setup.catalog.BranchesFrom("form_b"));
            Assert.Empty(setup.catalog.BranchesFrom("form_c"));
            Assert.False(new GeneticLab(setup.trainer, evolutionCatalog: setup.catalog)
                .Evolve(setup.monster.Id, "unknown", default, new SimRandom(1)).Eligible);
            var first = new GeneticLab(setup.trainer, evolutionCatalog: setup.catalog)
                .Evolve(setup.monster.Id, "branch_b", default, new SimRandom(1));
            Assert.True(first.Success);
            Assert.Equal("form_b", setup.monster.SpeciesId);
            var second = new GeneticLab(setup.trainer, evolutionCatalog: setup.catalog)
                .Evolve(setup.monster.Id, "branch_final", default, new SimRandom(2));
            Assert.True(second.Success);
            Assert.Equal("form_c", setup.monster.SpeciesId);
            Assert.Empty(setup.catalog.BranchesFrom("form_c"));
        }

        [Fact]
        public void EvolutionBranchPreservesIdentityLevelRarityIvGenesAndHpWhileChangingFormAndRole()
        {
            var setup = Setup();
            var id = setup.monster.Id;
            var level = setup.monster.Level;
            var rarity = setup.monster.Rarity;
            var iv = setup.monster.Iv;
            var genes = setup.monster.Genes.ToArray();
            setup.monster.SetCurrentHp(setup.monster.MaxHp / 2);
            var hpFraction = (double)setup.monster.CurrentHp / setup.monster.MaxHp;
            var result = new GeneticLab(setup.trainer, evolutionCatalog: setup.catalog)
                .Evolve(id, "branch_b", default, new SimRandom(1));
            Assert.True(result.Success);
            Assert.Equal(id, setup.monster.Id);
            Assert.Equal(level, setup.monster.Level);
            Assert.Equal(rarity, setup.monster.Rarity);
            Assert.Equal(iv, setup.monster.Iv);
            Assert.Equal(genes, setup.monster.Genes);
            Assert.Equal(MonsterElement.Water, setup.monster.Element);
            Assert.Equal(MonsterRole.Tank, setup.monster.Role);
            Assert.Equal(new[] { "water_guard" }, setup.monster.CombatSkillIds);
            Assert.InRange(Math.Abs(hpFraction - (double)setup.monster.CurrentHp / setup.monster.MaxHp), 0, 0.02);
        }

        [Fact]
        public void EvolutionFailureWithCharmPreservesCostsAndMonsterState()
        {
            var setup = Setup(chance: 0);
            var charm = new ProductId("protection_charm");
            setup.trainer.Inventory.Add(charm, 1);
            int fragments = setup.trainer.Inventory.Count(new ProductId("gene_fragment"));
            int stones = setup.trainer.Inventory.Count(new ProductId("evolution_stone"));
            var result = new GeneticLab(setup.trainer, evolutionCatalog: setup.catalog)
                .Evolve(setup.monster.Id, "branch_b", charm, new SimRandom(1));
            Assert.False(result.Success);
            Assert.True(result.ProtectionApplied);
            Assert.Equal(fragments, setup.trainer.Inventory.Count(new ProductId("gene_fragment")));
            Assert.Equal(stones, setup.trainer.Inventory.Count(new ProductId("evolution_stone")));
            Assert.Equal(0, setup.trainer.Inventory.Count(charm));
            Assert.Equal("form_a", setup.monster.SpeciesId);
        }

        [Fact]
        public void EvolutionFailureWithoutCharmConsumesConfiguredCosts()
        {
            var setup = Setup(chance: 0);
            var result = new GeneticLab(setup.trainer, evolutionCatalog: setup.catalog)
                .Evolve(setup.monster.Id, "branch_b", default, new SimRandom(1));
            Assert.False(result.Success);
            Assert.False(result.ProtectionApplied);
            Assert.Equal(47, setup.trainer.Inventory.Count(new ProductId("gene_fragment")));
            Assert.Equal(9, setup.trainer.Inventory.Count(new ProductId("evolution_stone")));
            Assert.Equal("form_a", setup.monster.SpeciesId);
        }

        [Fact]
        public void MissingEvolutionCostsRejectBeforeRandomRollOrMonsterMutation()
        {
            var setup = Setup();
            setup.trainer.Inventory.TryConsume(new ProductId("evolution_stone"), 10);
            var result = new GeneticLab(setup.trainer, evolutionCatalog: setup.catalog)
                .Evolve(setup.monster.Id, "branch_b", default, new SimRandom(1));
            Assert.False(result.Eligible);
            Assert.Equal("form_a", setup.monster.SpeciesId);
            Assert.Equal(50, setup.trainer.Inventory.Count(new ProductId("gene_fragment")));
        }

        [Fact]
        public void CatalogRejectsEvolutionChainsLongerThanTwoAndCycles()
        {
            var a = Def("a", MonsterElement.Grass, MonsterRole.Support, 100);
            var b = Def("b", MonsterElement.Water, MonsterRole.Tank, 100);
            var c = Def("c", MonsterElement.Fire, MonsterRole.Dps, 100);
            var d = Def("d", MonsterElement.Ice, MonsterRole.Dps, 100);
            EvolutionDefinition Step(string from, MonsterDefinition to) => new EvolutionDefinition(from, "next", to,
                1, 0.5, 0, new ProductId("evolution_stone"), 1, new[] { "skill" });
            Assert.Throws<System.ArgumentException>(() => new EvolutionCatalog(new[] {
                Step("a", b), Step("b", c), Step("c", d)
            }));
            Assert.Throws<System.ArgumentException>(() => new EvolutionCatalog(new[] {
                Step("a", b), Step("b", a)
            }));
        }
    }
}
