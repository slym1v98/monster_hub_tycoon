using System;
using Xunit;
using Game.Domain;

public class EventQueueTests
{
    [Fact]
    public void DequeuesInTimeOrder()
    {
        var q = new EventQueue();
        q.Schedule(50, SimEventKind.DayStart);
        q.Schedule(10, SimEventKind.Dawn);
        q.Schedule(30, SimEventKind.Dusk);
        Assert.Equal(10, q.PeekTime);
        Assert.Equal(10, q.Dequeue().Time);
        Assert.Equal(30, q.Dequeue().Time);
        Assert.Equal(50, q.Dequeue().Time);
        Assert.Equal(0, q.Count);
    }

    [Fact]
    public void SameTimeKeepsInsertionOrder()
    {
        var q = new EventQueue();
        q.Schedule(10, SimEventKind.Dawn);
        q.Schedule(10, SimEventKind.Dusk);
        q.Schedule(10, SimEventKind.DayStart);
        Assert.Equal(SimEventKind.Dawn, q.Dequeue().Kind);
        Assert.Equal(SimEventKind.Dusk, q.Dequeue().Kind);
        Assert.Equal(SimEventKind.DayStart, q.Dequeue().Kind);
    }

    [Fact]
    public void ManyRandomEventsComeOutSorted()
    {
        var rng = new SimRandom(5);
        var q = new EventQueue();
        for (int i = 0; i < 500; i++) q.Schedule(rng.NextInt(1000), SimEventKind.FarmChunk, i);
        int prev = -1;
        while (q.Count > 0) { int t = q.Dequeue().Time; Assert.True(t >= prev); prev = t; }
    }

    [Fact]
    public void DequeueOnEmptyThrows()
        => Assert.Throws<InvalidOperationException>(() => new EventQueue().Dequeue());

    [Fact]
    public void EventCarriesTrainerTokenAndArg()
    {
        var q = new EventQueue();
        q.Schedule(7, SimEventKind.ServiceDone, trainerId: 3, token: 9, arg: 2);
        var e = q.Dequeue();
        Assert.Equal(3, e.TrainerId); Assert.Equal(9, e.Token); Assert.Equal(2, e.Arg);
    }
}
