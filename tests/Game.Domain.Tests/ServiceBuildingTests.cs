using System.Linq;
using Xunit;
using Game.Domain;

public class ServiceBuildingTests
{
    static ServiceBuilding Make(BuildingKind kind, int level)
        => new ServiceBuilding(SimConfig.Default.Buildings.First(b => b.Kind == kind), level, 50);

    [Theory]
    [InlineData(1, 5)]
    [InlineData(5, 9)]
    [InlineData(10, 13)]
    [InlineData(25, 25)]
    public void SlotsAreFivePlusEightyPercentOfLevel(int level, int expected)
        => Assert.Equal(expected, Make(BuildingKind.Inn, level).Slots);

    [Fact]
    public void MaintenanceLossHalvesSlots()
    {
        var b = Make(BuildingKind.Inn, 5);
        b.Maintained = false;
        Assert.Equal(4, b.Slots);   // 9 / 2
    }

    [Fact]
    public void WaitingTrainersAreSeatedInFifoOrder()
    {
        var b = Make(BuildingKind.Inn, 1);   // 5 chỗ
        for (int id = 0; id < 7; id++) b.Enqueue(id);
        Assert.Equal(new[] { 0, 1, 2, 3, 4 }, b.PromoteWaiting());
        Assert.Equal(2, b.QueueLength);
        Assert.Equal(2, b.MaxQueueLength);

        b.Leave(0);
        Assert.Equal(new[] { 5 }, b.PromoteWaiting());
        b.Leave(1);
        Assert.Equal(new[] { 6 }, b.PromoteWaiting());
        Assert.Equal(0, b.QueueLength);
        Assert.Empty(b.PromoteWaiting());
    }

    [Fact]
    public void CapitalistGetsTenPercentDiscountRoundedUp()
    {
        var b = Make(BuildingKind.Inn, 5);   // giá 45
        Assert.Equal(41, b.PriceFor(PersonalityProfile.Of(Personality.Capitalist), 0));   // ceil(40.5)
        Assert.Equal(45, b.PriceFor(PersonalityProfile.Of(Personality.Timid), 0));
    }

    [Fact]
    public void HospitalPriceIsPerTenHpRoundedUp()
    {
        var b = Make(BuildingKind.Hospital, 5);   // 14 Gold mỗi 10 HP
        var timid = PersonalityProfile.Of(Personality.Timid);
        Assert.Equal(0, b.PriceFor(timid, 0));
        Assert.Equal(14, b.PriceFor(timid, 1));
        Assert.Equal(14, b.PriceFor(timid, 10));
        Assert.Equal(42, b.PriceFor(timid, 25));
    }

    [Fact]
    public void InnSleepsUntilDawnAtNightWithMinimumHalfHour()
    {
        var inn = Make(BuildingKind.Inn, 5);
        Assert.Equal(360, inn.ServiceMinutesFor(600));            // ban ngày: đủ 360
        Assert.Equal(360, inn.ServiceMinutesFor(1080));           // 18:00: còn 720 phút tới sáng, chặn ở 360
        Assert.Equal(180, inn.ServiceMinutesFor(1440 + 180));     // 03:00: còn 180 phút
        Assert.Equal(30, inn.ServiceMinutesFor(1440 + 350));      // 05:50: còn 10 phút, tối thiểu 30
    }

    [Fact]
    public void OtherBuildingsUseBaseServiceMinutes()
    {
        Assert.Equal(30, Make(BuildingKind.Restaurant, 5).ServiceMinutesFor(1100));
        Assert.Equal(60, Make(BuildingKind.Bar, 5).ServiceMinutesFor(1100));
    }

    [Fact]
    public void PriceStartsAtFairPrice()
    {
        var b = Make(BuildingKind.Bar, 5);
        Assert.Equal(800, b.FairPrice);
        Assert.Equal(800, b.Price);
    }
}
