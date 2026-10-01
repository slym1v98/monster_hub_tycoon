using Xunit;
using Game.Domain;

public class SimClockTests
{
    [Theory]
    [InlineData(359, true)]
    [InlineData(360, false)]
    [InlineData(1079, false)]
    [InlineData(1080, true)]
    [InlineData(1440 + 100, true)]
    public void IsNightFollowsDawnAndDuskBoundaries(int minute, bool expected)
        => Assert.Equal(expected, SimClock.IsNight(minute));

    [Fact]
    public void NextMinuteOfDayIsStrictlyLater()
    {
        Assert.Equal(1800, SimClock.NextMinuteOfDay(360, SimClock.DawnMinute));   // đúng mốc thì lấy ngày hôm sau
        Assert.Equal(1080, SimClock.NextMinuteOfDay(360, SimClock.DuskMinute));
        Assert.Equal(1440, SimClock.NextMinuteOfDay(360, 0));
    }

    [Fact]
    public void PaydayIsLastMinuteOfDayThirty()
    {
        Assert.Equal(43199, SimClock.PaydayMinute(0));
        Assert.Equal(86399, SimClock.PaydayMinute(1));
    }

    [Fact]
    public void SimTimeSplitsDayAndMinute()
    {
        var t = new SimTime(1440 + 75);
        Assert.Equal(1, t.Day);
        Assert.Equal(75, t.MinuteOfDay);
        Assert.Equal(2, t.DayOfMonth);
        Assert.True(t.IsNight);
        Assert.Equal(1, new SimTime(30 * 1440).DayOfMonth);   // ngày đầu tháng sau
    }
}
