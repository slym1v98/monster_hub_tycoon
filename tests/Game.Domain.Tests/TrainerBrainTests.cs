using Xunit;
using Game.Domain;

public class TrainerBrainTests
{
    static readonly SimConfig Cfg = SimConfig.Default;

    // ---- ShouldReturn ----
    [Fact]
    public void TimidReturnsWhenStaminaBelowFifty()
    {
        var t = TestTrainers.Make(personality: Personality.Timid);
        t.Needs.Stamina = 49;
        Assert.Equal(ReturnReason.Tired, TrainerBrain.ShouldReturn(t, false));
        t.Needs.Stamina = 50;
        Assert.Equal(ReturnReason.None, TrainerBrain.ShouldReturn(t, false));
    }

    [Fact]
    public void WarlikeStaysOutUntilStaminaBelowFifteen()
    {
        var t = TestTrainers.Make(personality: Personality.Warlike);
        t.Needs.Stamina = 49;
        Assert.Equal(ReturnReason.None, TrainerBrain.ShouldReturn(t, false));
        t.Needs.Stamina = 14;
        Assert.Equal(ReturnReason.Tired, TrainerBrain.ShouldReturn(t, false));
    }

    [Fact]
    public void GluttonReturnsHungryBelowFiftySatiety()
    {
        var t = TestTrainers.Make(personality: Personality.Glutton);
        t.Needs.Satiety = 49;
        Assert.Equal(ReturnReason.Hungry, TrainerBrain.ShouldReturn(t, false));
        t.Needs.Satiety = 51;
        Assert.Equal(ReturnReason.None, TrainerBrain.ShouldReturn(t, false));
    }

    [Fact]
    public void CapitalistReturnsThirstyBelowTwentyFive()
    {
        var t = TestTrainers.Make(personality: Personality.Capitalist);
        t.Needs.Hydration = 24;
        Assert.Equal(ReturnReason.Thirsty, TrainerBrain.ShouldReturn(t, false));
        t.Needs.Hydration = 26;
        Assert.Equal(ReturnReason.None, TrainerBrain.ShouldReturn(t, false));
    }

    [Fact]
    public void ReturnsWhenBackpackFullOrTeamDown()
    {
        var t = TestTrainers.Make();
        t.BackpackUnits = 30;
        Assert.Equal(ReturnReason.BackpackFull, TrainerBrain.ShouldReturn(t, false));
        t.BackpackUnits = 0; t.TeamHp = 0;
        Assert.Equal(ReturnReason.TeamDown, TrainerBrain.ShouldReturn(t, false));
    }

    [Fact]
    public void NightForcesReturnOnlyWithoutNightVision()
    {
        var t = TestTrainers.Make();
        Assert.Equal(ReturnReason.Night, TrainerBrain.ShouldReturn(t, true));
        t.HasNightVision = true;
        Assert.Equal(ReturnReason.None, TrainerBrain.ShouldReturn(t, true));
    }

    [Fact]
    public void StrikeForcesReturn()
    {
        var t = TestTrainers.Make();
        t.StrikeDaysLeft = 2;
        Assert.Equal(ReturnReason.Strike, TrainerBrain.ShouldReturn(t, false));
    }

    // ---- PickService ----
    [Fact]
    public void FullStressGoesToBarBeforeAnythingElse()
    {
        var t = TestTrainers.Make();
        t.Needs.Stress = 100; t.TeamHp = 10; t.Needs.Satiety = 5;
        Assert.Equal(BuildingKind.Bar, TrainerBrain.PickService(t, Cfg, false, true));
    }

    [Fact]
    public void MissingHpGoesToHospital()
    {
        var t = TestTrainers.Make();
        t.TeamHp = 299; t.Needs.Satiety = 10;
        Assert.Equal(BuildingKind.Hospital, TrainerBrain.PickService(t, Cfg, false, true));
    }

    [Fact]
    public void NightWithoutVisionSleepsOnlyIfTired()
    {
        var t = TestTrainers.Make();
        t.Needs.Stamina = 80;
        Assert.Equal(BuildingKind.Inn, TrainerBrain.PickService(t, Cfg, true, true));
        t.Needs.Stamina = 95;
        Assert.Null(TrainerBrain.PickService(t, Cfg, true, true));
    }

    [Fact]
    public void LowestNeedBelowSufficientPicksItsBuilding()
    {
        var t = TestTrainers.Make();
        t.Needs.Satiety = 50;
        Assert.Equal(BuildingKind.Restaurant, TrainerBrain.PickService(t, Cfg, false, true));

        t = TestTrainers.Make(); t.Needs.Stamina = 40; t.Needs.Satiety = 55;
        Assert.Equal(BuildingKind.Inn, TrainerBrain.PickService(t, Cfg, false, true));

        t = TestTrainers.Make(); t.Needs.Hydration = 30; t.Needs.Stamina = 50;
        Assert.Equal(BuildingKind.Restaurant, TrainerBrain.PickService(t, Cfg, false, true));
    }

    [Fact]
    public void HighStressGoesToBarWhenNothingElseNeeded()
    {
        var t = TestTrainers.Make();
        t.Needs.Stress = 75;
        Assert.Equal(BuildingKind.Bar, TrainerBrain.PickService(t, Cfg, false, true));
        t.Needs.Stress = 69;
        Assert.Null(TrainerBrain.PickService(t, Cfg, false, true));
    }

    [Fact]
    public void StrikerWithMissingHpSkipsTheHospital()
    {
        var t = TestTrainers.Make();
        t.TeamHp = 100; t.Needs.Satiety = 40;
        Assert.Equal(BuildingKind.Hospital, TrainerBrain.PickService(t, Cfg, false, true));
        Assert.Equal(BuildingKind.Restaurant, TrainerBrain.PickService(t, Cfg, false, false));   // rơi xuống luật kế tiếp

        t = TestTrainers.Make(); t.TeamHp = 100;
        Assert.Null(TrainerBrain.PickService(t, Cfg, false, false));
    }
}
