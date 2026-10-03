using System;
using System.Collections.Generic;
using Game.Domain.Combat;
using Game.Domain.Monsters;
using Xunit;

namespace Game.Domain.Tests
{
    public sealed class ElementChartTests
    {
        public static IEnumerable<object[]> GddMatchups()
        {
            // Kỳ vọng chép độc lập từ GDD 04 §2; hàng tấn công, cột phòng thủ.
            var elements = new[] { MonsterElement.Fire, MonsterElement.Water, MonsterElement.Grass,
                MonsterElement.Electric, MonsterElement.Ice, MonsterElement.Poison,
                MonsterElement.Ground, MonsterElement.Light, MonsterElement.Dark };
            var rows = new[]
            {
                new[] { 0.5, 0.5, 2, 1, 2, 1, 1, 1, 1 },
                new[] { 2, 0.5, 0.5, 1, 1, 1, 2, 1, 1 },
                new[] { 0.5, 2, 0.5, 1, 1, 0.5, 2, 1, 1 },
                new[] { 1, 2, 0.5, 0.5, 1, 1, 0.5, 1, 1 },
                new[] { 0.5, 0.5, 2, 1, 0.5, 1, 2, 1, 1 },
                new[] { 1, 1, 2, 1, 1, 0.5, 0.5, 1, 1 },
                new[] { 2, 1, 0.5, 2, 1, 2, 1, 1, 1 },
                new[] { 1, 1, 1, 1, 1, 1, 1, 0.5, 2 },
                new[] { 1, 1, 1, 1, 1, 1, 1, 2, 0.5 }
            };
            for (int attack = 0; attack < elements.Length; attack++)
                for (int defense = 0; defense < elements.Length; defense++)
                    yield return new object[] { elements[attack], elements[defense], rows[attack][defense] };
        }

        [Theory]
        [MemberData(nameof(GddMatchups))]
        public void MultiplierMatchesEveryGddCell(MonsterElement attack, MonsterElement defense, double expected)
        {
            Assert.Equal(expected, ElementChart.Multiplier(attack, defense));
        }

        [Fact]
        public void LightAndDarkAreReciprocallyEffective()
        {
            Assert.Equal(2, ElementChart.Multiplier(MonsterElement.Light, MonsterElement.Dark));
            Assert.Equal(2, ElementChart.Multiplier(MonsterElement.Dark, MonsterElement.Light));
            Assert.Equal(0.5, ElementChart.Multiplier(MonsterElement.Light, MonsterElement.Light));
            Assert.Equal(0.5, ElementChart.Multiplier(MonsterElement.Dark, MonsterElement.Dark));
        }

        [Fact]
        public void EveryElementPairHasNonzeroAllowedMultiplier()
        {
            var elements = Enum.GetValues<MonsterElement>();
            Assert.Equal(9, elements.Length);
            foreach (var attack in elements)
                foreach (var defense in elements)
                    Assert.Contains(ElementChart.Multiplier(attack, defense), new[] { 0.5, 1.0, 2.0 });
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(9)]
        [InlineData(int.MinValue)]
        [InlineData(int.MaxValue)]
        public void InvalidAttackElementIsRejected(int value)
        {
            var error = Assert.Throws<ArgumentOutOfRangeException>(() =>
                ElementChart.Multiplier((MonsterElement)value, MonsterElement.Fire));
            Assert.Equal("attack", error.ParamName);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(9)]
        [InlineData(int.MinValue)]
        [InlineData(int.MaxValue)]
        public void InvalidDefenseElementIsRejected(int value)
        {
            var error = Assert.Throws<ArgumentOutOfRangeException>(() =>
                ElementChart.Multiplier(MonsterElement.Fire, (MonsterElement)value));
            Assert.Equal("defense", error.ParamName);
        }
    }
}
