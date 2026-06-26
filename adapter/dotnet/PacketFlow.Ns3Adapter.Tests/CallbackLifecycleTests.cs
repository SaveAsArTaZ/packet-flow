// CallbackLifecycleTests.cs
// Tests for scheduled and trace callback lifecycle: fire, leak prevention,
// and cleanup on dispose.
//
// Verifies:
// - Scheduled callbacks fire exactly once
// - Unfired scheduled callbacks are cleaned up on Dispose (no GCHandle leak)
// - Multiple sequential schedules work correctly
// - Callbacks fire in correct time order

using Xunit;
using PacketFlow.Ns3Adapter;

namespace PacketFlow.Ns3Adapter.Tests;

public class CallbackLifecycleTests
{
    /// <summary>
    /// Verifies that scheduled callbacks fire exactly the expected number of times.
    /// </summary>
    [Fact]
    public void Schedule_AllCallbacksFire_ShouldIncrementCorrectly()
    {
        // Arrange
        using var sim = new Simulation();
        var callbackCount = 0;
        var expectedCallbacks = 5;

        // Schedule callbacks at different times
        for (int i = 0; i < expectedCallbacks; i++)
        {
            sim.Schedule(TimeSpan.FromSeconds(i * 0.1), () =>
            {
                Interlocked.Increment(ref callbackCount);
            });
        }

        // Act
        sim.Stop(TimeSpan.FromSeconds(1.0));
        sim.Run();

        // Assert
        Assert.Equal(expectedCallbacks, callbackCount);
    }

    /// <summary>
    /// Verifies that scheduled callbacks fire in the correct time order.
    /// </summary>
    [Fact]
    public void Schedule_CallbacksFireInOrder()
    {
        // Arrange
        using var sim = new Simulation();
        var order = new List<int>();

        sim.Schedule(TimeSpan.FromSeconds(0.3), () => order.Add(3));
        sim.Schedule(TimeSpan.FromSeconds(0.1), () => order.Add(1));
        sim.Schedule(TimeSpan.FromSeconds(0.2), () => order.Add(2));

        // Act
        sim.Stop(TimeSpan.FromSeconds(1.0));
        sim.Run();

        // Assert: callbacks should fire in time order (0.1 → 0.2 → 0.3)
        Assert.Equal(3, order.Count);
        Assert.Equal(new[] { 1, 2, 3 }, order);
    }

    /// <summary>
    /// Verifies that callbacks scheduled after the stop time never fire,
    /// and that Dispose cleans up their GCHandles without error.
    /// </summary>
    [Fact]
    public void Schedule_UnfiredCallbacks_CleanedUpOnDispose()
    {
        // Arrange
        var sim = new Simulation();
        var firedCount = 0;
        var unfiredCount = 0;

        // This one fires
        sim.Schedule(TimeSpan.FromSeconds(0.1), () => Interlocked.Increment(ref firedCount));

        // These won't fire — sim stops at 0.5s
        sim.Schedule(TimeSpan.FromSeconds(1.0), () => Interlocked.Increment(ref unfiredCount));
        sim.Schedule(TimeSpan.FromSeconds(2.0), () => Interlocked.Increment(ref unfiredCount));
        sim.Schedule(TimeSpan.FromSeconds(3.0), () => Interlocked.Increment(ref unfiredCount));

        // Act
        sim.Stop(TimeSpan.FromSeconds(0.5));
        sim.Run();

        // Dispose should clean up the 3 unfired GCHandles without exception
        sim.Dispose();

        // Assert
        Assert.Equal(1, firedCount);
        Assert.Equal(0, unfiredCount);
    }

    /// <summary>
    /// Verifies that Dispose can be called multiple times without error
    /// (idempotent dispose with handle tracking).
    /// </summary>
    [Fact]
    public void Dispose_MultipleTimes_WithHandles_ShouldNotThrow()
    {
        var sim = new Simulation();
        sim.Schedule(TimeSpan.FromSeconds(0.1), () => { });
        sim.Stop(TimeSpan.FromSeconds(1.0));
        sim.Run();

        // Act & Assert — multiple Dispose calls should not throw
        sim.Dispose();
        sim.Dispose();
        sim.Dispose();
    }

    /// <summary>
    /// Verifies that a disposed simulation cannot schedule new callbacks.
    /// </summary>
    [Fact]
    public void Schedule_AfterDispose_ShouldThrow()
    {
        var sim = new Simulation();
        sim.Stop(TimeSpan.FromSeconds(0.1));
        sim.Run();
        sim.Dispose();

        Assert.Throws<ObjectDisposedException>(() =>
            sim.Schedule(TimeSpan.FromSeconds(1.0), () => { }));
    }

    /// <summary>
    /// Verifies that scheduling with a null callback throws ArgumentNullException.
    /// </summary>
    [Fact]
    public void Schedule_NullCallback_ShouldThrowArgumentNullException()
    {
        using var sim = new Simulation();
        Assert.Throws<ArgumentNullException>(() => sim.Schedule(TimeSpan.FromSeconds(1.0), null!));
    }

    /// <summary>
    /// Verifies that many scheduled callbacks all fire without issue.
    /// </summary>
    [Fact]
    public void Schedule_ManyCallbacks_AllFire()
    {
        using var sim = new Simulation();
        var count = 0;
        const int total = 50;

        var rng = new Random(12345);
        for (int i = 0; i < total; i++)
        {
            var delay = rng.NextDouble() * 0.9; // 0..0.9 seconds
            sim.Schedule(TimeSpan.FromSeconds(delay), () => Interlocked.Increment(ref count));
        }

        sim.Stop(TimeSpan.FromSeconds(1.0));
        sim.Run();

        Assert.Equal(total, count);
    }

    /// <summary>
    /// Verifies that the simulation time advances correctly as callbacks fire.
    /// </summary>
    [Fact]
    public void Schedule_TimeAdvancesDuringCallbacks()
    {
        using var sim = new Simulation();
        var times = new List<double>();

        sim.Schedule(TimeSpan.FromSeconds(0.1), () => times.Add(sim.Now.TotalSeconds));
        sim.Schedule(TimeSpan.FromSeconds(0.5), () => times.Add(sim.Now.TotalSeconds));
        sim.Schedule(TimeSpan.FromSeconds(0.9), () => times.Add(sim.Now.TotalSeconds));

        sim.Stop(TimeSpan.FromSeconds(1.0));
        sim.Run();

        Assert.Equal(3, times.Count);
        Assert.Equal(0.1, times[0], precision: 3);
        Assert.Equal(0.5, times[1], precision: 3);
        Assert.Equal(0.9, times[2], precision: 3);
    }
}
