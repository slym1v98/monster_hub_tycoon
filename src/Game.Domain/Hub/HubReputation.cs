using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Domain
{
    public sealed class HubReputationConfig
    {
        public static HubReputationConfig Prototype { get; } = new HubReputationConfig();
        public int MaxBuildingLevel { get; }
        public double BankruptcyPenaltyPoints { get; }
        public double TrafficBase { get; }
        public double TrafficPerReputationPoint { get; }
        public double ApplicantRarityPerReputationPoint { get; }
        public double InspectionBase { get; }
        public double InspectionPerReputationPoint { get; }
        public IReadOnlyList<BalanceParameter> BalanceParameters { get; }
        public HubReputationConfig(int maxBuildingLevel = 25, double bankruptcyPenaltyPoints = 20,
            double trafficBase = 0.5, double trafficPerReputationPoint = 0.01,
            double applicantRarityPerReputationPoint = 0.02, double inspectionBase = 2,
            double inspectionPerReputationPoint = -0.01)
        {
            if (maxBuildingLevel <= 0 || bankruptcyPenaltyPoints < 0 || trafficBase < 0 ||
                trafficPerReputationPoint < 0 || applicantRarityPerReputationPoint < 0 ||
                inspectionBase < 0 || double.IsNaN(inspectionPerReputationPoint)) throw new ArgumentOutOfRangeException(nameof(maxBuildingLevel));
            MaxBuildingLevel = maxBuildingLevel; BankruptcyPenaltyPoints = bankruptcyPenaltyPoints;
            TrafficBase = trafficBase; TrafficPerReputationPoint = trafficPerReputationPoint;
            ApplicantRarityPerReputationPoint = applicantRarityPerReputationPoint;
            InspectionBase = inspectionBase; InspectionPerReputationPoint = inspectionPerReputationPoint;
            string source = "docs/designs/02_HUB_Economy_Infrastructure.md §2.2: relationships specified; coefficients are not.";
            BalanceParameters = Array.AsReadOnly(new[] {
                new BalanceParameter("reputation.service_level_normalizer", maxBuildingLevel, "building levels", "Prototype", source),
                new BalanceParameter("reputation.bankruptcy_penalty", bankruptcyPenaltyPoints, "points/bankruptcy", "Prototype", source),
                new BalanceParameter("reputation.traffic_base", trafficBase, "multiplier", "Prototype", source),
                new BalanceParameter("reputation.traffic_per_point", trafficPerReputationPoint, "multiplier/point", "Prototype", source),
                new BalanceParameter("reputation.applicant_rarity_per_point", applicantRarityPerReputationPoint, "chance modifier/point", "Prototype", source),
                new BalanceParameter("reputation.inspection_base", inspectionBase, "multiplier", "Prototype", source),
                new BalanceParameter("reputation.inspection_per_point", inspectionPerReputationPoint, "multiplier/point", "Prototype", source)
            });
        }
    }

    /// <summary>Prototype mapping from the GDD reputation inputs to one deterministic bounded score.</summary>
    public static class HubReputation
    {
        public static HubReputationView Calculate(IReadOnlyList<ServiceBuilding> buildings,
            IReadOnlyList<Trainer> trainers, int? bankruptcies, HubReputationConfig config = null)
        {
            config = config ?? HubReputationConfig.Prototype;
            if (buildings == null || buildings.Count == 0) throw new ArgumentException("At least one building is required.", nameof(buildings));
            if (trainers == null) throw new ArgumentNullException(nameof(trainers));
            if (bankruptcies < 0) throw new ArgumentOutOfRangeException(nameof(bankruptcies));
            double quality = buildings.Average(b => Math.Max(0, Math.Min(1, b.Level / (double)config.MaxBuildingLevel * b.QualityMultiplier))) * 100;
            double fairPrice = buildings.Average(b => Math.Max(0, 100 - Math.Abs((double)b.Price / b.FairPrice - 1) * 100));
            double stress = trainers.Count == 0 ? 50 : 100 - trainers.Average(t => t.Needs.Stress);
            double bankruptcy = bankruptcies.HasValue ? Math.Max(0, 100 - bankruptcies.Value * config.BankruptcyPenaltyPoints) : 50;
            double score = Math.Max(0, Math.Min(100, (quality + fairPrice + stress + bankruptcy) / 4));
            return new HubReputationView(score, quality, fairPrice,
                trainers.Count == 0 ? null : trainers.Average(t => t.Needs.Stress), bankruptcies,
                config.TrafficBase + score * config.TrafficPerReputationPoint,
                (score - 50) * config.ApplicantRarityPerReputationPoint,
                config.InspectionBase + score * config.InspectionPerReputationPoint);
        }
    }

    public sealed record HubReputationView(double Score, double ServiceQualityScore, double FairPriceScore,
        double? AverageTrainerStress, int? Bankruptcies, double TrafficMultiplier,
        double RareApplicantChanceModifier, double InspectionPressureMultiplier);
}
