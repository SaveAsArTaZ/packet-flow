// SimulationLifecycleTests.cs
// Unit tests for simulation lifecycle management

using Xunit;
using PacketFlow.Ns3Adapter;

namespace PacketFlow.Ns3Adapter.Tests;

public class SimulationLifecycleTests
{
    [Fact]
    public void CreateAndDestroy_ShouldNotThrow()
    {
        // Arrange & Act
        using var sim = new Simulation();
        
        // Assert - just verifying no exception
        Assert.NotNull(sim);
    }

    [Fact]
    public void SetSeed_ShouldNotThrow()
    {
        // Arrange
        using var sim = new Simulation();
        
        // Act & Assert
        sim.SetSeed(12345);
    }

    [Fact]
    public void Now_InitialTime_ShouldBeZero()
    {
        // Arrange
        using var sim = new Simulation();
        
        // Act
        var now = sim.Now;
        
        // Assert
        Assert.Equal(TimeSpan.Zero, now);
    }

    [Fact]
    public void IsRunning_BeforeRun_ShouldBeFalse()
    {
        // Arrange
        using var sim = new Simulation();
        
        // Act
        var isRunning = sim.IsRunning;
        
        // Assert
        Assert.False(isRunning);
    }

    [Fact]
    public void Run_WithStopTime_ShouldComplete()
    {
        // Arrange
        using var sim = new Simulation();
        sim.Stop(TimeSpan.FromSeconds(1.0));
        
        // Act
        sim.Run();
        
        // Assert
        var finalTime = sim.Now;
        Assert.Equal(1.0, finalTime.TotalSeconds, precision: 3);
    }

    [Fact]
    public void Schedule_Callback_ShouldBeInvoked()
    {
        // Arrange
        using var sim = new Simulation();
        var callbackInvoked = false;
        
        // Act
        sim.Schedule(TimeSpan.FromSeconds(0.5), () => callbackInvoked = true);
        sim.Stop(TimeSpan.FromSeconds(1.0));
        sim.Run();
        
        // Assert
        Assert.True(callbackInvoked);
    }

    [Fact]
    public void Dispose_MultipleTimes_ShouldBeIdempotent()
    {
        // Arrange
        var sim = new Simulation();
        
        // Act & Assert - should not throw
        sim.Dispose();
        sim.Dispose();
        sim.Dispose();
    }

    [Fact]
    public void DisposedSimulation_ShouldThrowObjectDisposedException()
    {
        // Arrange
        var sim = new Simulation();
        sim.Dispose();
        
        // Act & Assert
        Assert.Throws<ObjectDisposedException>(() => sim.SetSeed(123));
    }
}

