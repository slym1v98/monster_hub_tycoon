using Game.Domain;
using Game.Domain.Combat;
using Game.Domain.Monsters;
using Xunit;

namespace Game.Domain.Tests
{
    public sealed class EncounterGeneratorTests
    {
        [Fact]
        public void WeightedEncounterIsSeedDeterministicAndNightDoesNotDoubleRewardsHere()
        {
            var zone = ZoneCatalog.Default.Definitions[0];
            var generator = new EncounterGenerator();
            var day = generator.Generate(zone, new SimTime(SimClock.DawnMinute), new SimRandom(17));
            var replay = generator.Generate(zone, new SimTime(SimClock.DawnMinute), new SimRandom(17));
            var night = generator.Generate(zone, new SimTime(SimClock.DuskMinute), new SimRandom(17));
            Assert.Equal(day.Element, replay.Element);
            Assert.Equal(day.GoldReward, night.GoldReward);
            Assert.Equal(day.ExperienceReward, night.ExperienceReward);
            Assert.Equal(day.MaterialUnits, night.MaterialUnits);
            Assert.Contains(day.Element, zone.EncounterProfile.ElementWeights.Keys);
        }
    }
}
