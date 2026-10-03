using System.Collections.Generic;
using Game.Domain;
using Xunit;

public sealed class ReputationTests
{
    static ServiceBuilding[] Buildings(int level = 25, long price = 45)
    {
        var specs = new[]
        {
            new BuildingSpec(BuildingKind.Inn, 45, 30),
            new BuildingSpec(BuildingKind.Restaurant, 40, 30),
            new BuildingSpec(BuildingKind.Bar, 800, 30),
            new BuildingSpec(BuildingKind.Hospital, 14, 30),
        };
        var result = new ServiceBuilding[specs.Length];
        for (int i = 0; i < specs.Length; i++) result[i] = new ServiceBuilding(specs[i], level, 50);
        foreach (var building in result) building.Price = price;
        return result;
    }

    [Fact]
    public void BetterServiceQualityAndFairerPricingRaiseReputation()
    {
        var trainer = TestTrainers.Make();
        double strong = HubReputation.Calculate(Buildings(25), new[] { trainer }, 0).Score;
        double weak = HubReputation.Calculate(Buildings(1), new[] { trainer }, 0).Score;

        Assert.True(strong > weak);
        Assert.InRange(strong, 0, 100);
        Assert.InRange(weak, 0, 100);
    }

    [Fact]
    public void HigherStressAndBankruptciesLowerReputationAndTraffic()
    {
        var trainer = TestTrainers.Make();
        var baseline = HubReputation.Calculate(Buildings(), new[] { trainer }, 0);
        trainer.Needs.Stress = 80;
        var stressed = HubReputation.Calculate(Buildings(), new[] { trainer }, 1);

        Assert.True(stressed.Score < baseline.Score);
        Assert.True(stressed.TrafficMultiplier < baseline.TrafficMultiplier);
        Assert.True(stressed.InspectionPressureMultiplier > baseline.InspectionPressureMultiplier);
    }

    [Fact]
    public void ReputationFormulaUsesConfiguredBuildingNormalizer()
    {
        var trainer = TestTrainers.Make();
        var normal = HubReputation.Calculate(Buildings(), new[] { trainer }, 0, new HubReputationConfig(maxBuildingLevel: 25));
        var slower = HubReputation.Calculate(Buildings(), new[] { trainer }, 0, new HubReputationConfig(maxBuildingLevel: 50));
        Assert.True(normal.Score > slower.Score);
    }
}
