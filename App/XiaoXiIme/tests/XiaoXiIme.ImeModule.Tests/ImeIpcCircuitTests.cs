namespace XiaoXiIme.ImeModule.Tests;

public class ImeIpcCircuitTests
{
    [Theory]
    [InlineData(4, true)]
    [InlineData(5, false)]
    public void ConsecutiveTimeoutsOpenAtFive(int count, bool expected)
    {
        var circuit = new ImeIpcCircuit(new TestClock());
        circuit.TryEnter(out var generation);
        for (var i = 0; i < count; i++)
        {
            circuit.Complete(generation, timedOut: true);
        }

        Assert.Equal(expected, circuit.TryEnter(out _));
    }

    [Fact]
    public void SuccessResetsConsecutiveTimeouts()
    {
        var circuit = new ImeIpcCircuit(new TestClock());
        circuit.TryEnter(out var generation);
        for (var i = 0; i < 4; i++)
        {
            circuit.Complete(generation, timedOut: true);
        }

        circuit.Complete(generation, timedOut: false);
        circuit.Complete(generation, timedOut: true);

        Assert.True(circuit.TryEnter(out _));
    }

    [Theory]
    [InlineData(59, false)]
    [InlineData(60, true)]
    public void FallbackLastsOneMinuteDespiteLateSuccess(int seconds, bool expected)
    {
        var clock = new TestClock();
        var circuit = new ImeIpcCircuit(clock);
        circuit.TryEnter(out var generation);
        for (var i = 0; i < 5; i++)
        {
            circuit.Complete(generation, timedOut: true);
        }

        clock.Timestamp = seconds;
        circuit.Complete(generation, timedOut: false);

        Assert.Equal(expected, circuit.TryEnter(out _));
    }

    private sealed class TestClock : TimeProvider
    {
        internal long Timestamp { get; set; }
        public override long TimestampFrequency => 1;
        public override long GetTimestamp() => Timestamp;
    }
}
