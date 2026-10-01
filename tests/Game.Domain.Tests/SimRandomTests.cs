using Xunit;
using Game.Domain;

public class SimRandomTests
{
    [Fact]
    public void SameSeedSameSequence()
    {
        var a = new SimRandom(42); var b = new SimRandom(42);
        for (int i = 0; i < 100; i++) Assert.Equal(a.NextDouble(), b.NextDouble());
    }

    [Fact]
    public void NextDoubleIsInUnitInterval()
    {
        var r = new SimRandom(1);
        for (int i = 0; i < 10_000; i++) Assert.InRange(r.NextDouble(), 0.0, 0.9999999999999999);
    }

    [Fact]
    public void NextIntStaysBelowBound()
    {
        var r = new SimRandom(2);
        for (int i = 0; i < 10_000; i++) Assert.InRange(r.NextInt(4), 0, 3);
    }

    [Fact]
    public void RestoringStateReplaysSequence()
    {
        var a = new SimRandom(3);
        a.NextDouble();
        ulong saved = a.State;
        double next = a.NextDouble();
        var b = new SimRandom(999) { State = saved };
        Assert.Equal(next, b.NextDouble());
    }
}
