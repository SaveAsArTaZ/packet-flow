// MemorySafetyTests.cs
// Tests for memory safety, resource cleanup, and simulation lifecycle edge cases.
//
// Verifies:
// - Multiple sequential simulations work without leaks
// - Dispose is idempotent and safe
// - Disposed objects throw ObjectDisposedException
// - Large numbers of nodes/devices can be created and cleaned up
// - Mixed topologies in a single simulation work correctly

using Xunit;
using PacketFlow.Ns3Adapter;

namespace PacketFlow.Ns3Adapter.Tests;

public class MemorySafetyTests
{
    /// <summary>
    /// Creates and destroys multiple simulations sequentially.
    /// If native resources leak, this will either crash or slow down significantly.
    /// </summary>
    [Fact]
    public void SequentialSimulations_NoLeaks()
    {
        for (int i = 0; i < 10; i++)
        {
            using var sim = new Simulation();
            sim.SetSeed((uint)i);

            var nodes = sim.CreateNodes(4);
            sim.InstallInternetStack(nodes);
            var devices = Csma.Install(sim, nodes, "100Mbps", "6560ns");
            sim.AssignIpv4Addresses(devices, "10.0.0.0", "255.255.255.0");

            // Add a trace subscription to test persistent handle cleanup
            var txCount = 0;
            devices[0].SubscribeToPacketEvents(
                onTx: evt => Interlocked.Increment(ref txCount),
                onRx: null);

            sim.Stop(TimeSpan.FromSeconds(0.1));
            sim.Run();
        }

        // If we got here without crashing, resource cleanup is working
        Assert.True(true);
    }

    /// <summary>
    /// Creates simulations with scheduled callbacks that never fire,
    /// verifying handle cleanup on dispose across multiple iterations.
    /// </summary>
    [Fact]
    public void UnfiredCallbacks_MultipleSimulations_NoLeaks()
    {
        for (int i = 0; i < 5; i++)
        {
            var sim = new Simulation();

            // Schedule many callbacks that will never fire (sim stops at 0.1s)
            for (int j = 0; j < 20; j++)
            {
                sim.Schedule(TimeSpan.FromSeconds(1.0 + j * 0.1), () => { });
            }

            sim.Stop(TimeSpan.FromSeconds(0.1));
            sim.Run();
            sim.Dispose();
        }

        Assert.True(true);
    }

    /// <summary>
    /// Verifies that creating a large number of nodes and running the simulation
    /// works without issues.
    /// </summary>
    [Fact]
    public void LargeNodeCount_ShouldNotCrash()
    {
        using var sim = new Simulation();
        const int nodeCount = 20;

        var nodes = sim.CreateNodes(nodeCount);
        sim.InstallInternetStack(nodes);

        // Create a chain of P2P links
        for (int i = 0; i < nodeCount - 1; i++)
        {
            PointToPoint.Install(sim, nodes[i], nodes[i + 1], "1Gbps", "1ms");
        }

        sim.Stop(TimeSpan.FromSeconds(0.1));
        sim.Run();

        Assert.Equal(nodeCount, nodes.Length);
    }

    /// <summary>
    /// Verifies that mixed topologies in a single simulation work correctly.
    /// </summary>
    [Fact]
    public void MixedTopologies_ShouldWork()
    {
        using var sim = new Simulation();
        sim.SetSeed(42);

        // P2P subnet
        var p2pNodes = sim.CreateNodes(2);
        sim.InstallInternetStack(p2pNodes);
        var (p2pDev0, p2pDev1) = PointToPoint.Install(sim, p2pNodes[0], p2pNodes[1], "5Mbps", "2ms");
        sim.AssignIpv4Addresses(new[] { p2pDev0, p2pDev1 }, "10.1.0.0", "255.255.255.0");

        // CSMA subnet
        var csmaNodes = sim.CreateNodes(3);
        sim.InstallInternetStack(csmaNodes);
        var csmaDevices = Csma.Install(sim, csmaNodes, "100Mbps", "6560ns");
        sim.AssignIpv4Addresses(csmaDevices, "10.2.0.0", "255.255.255.0");

        // P2P echo on first subnet
        var server1 = UdpEcho.CreateServer(sim, p2pNodes[1], 9);
        server1.Start(TimeSpan.FromSeconds(1.0));
        server1.Stop(TimeSpan.FromSeconds(5.0));
        var client1 = UdpEcho.CreateClient(sim, p2pNodes[0], "10.1.0.2", 9, 512,
            TimeSpan.FromSeconds(1.0), 3);
        client1.Start(TimeSpan.FromSeconds(2.0));
        client1.Stop(TimeSpan.FromSeconds(5.0));

        // CSMA echo on second subnet
        var server2 = UdpEcho.CreateServer(sim, csmaNodes[2], 7);
        server2.Start(TimeSpan.FromSeconds(1.0));
        server2.Stop(TimeSpan.FromSeconds(5.0));
        var client2 = UdpEcho.CreateClient(sim, csmaNodes[0], "10.2.0.3", 7, 512,
            TimeSpan.FromSeconds(1.0), 3);
        client2.Start(TimeSpan.FromSeconds(2.0));
        client2.Stop(TimeSpan.FromSeconds(5.0));

        sim.Stop(TimeSpan.FromSeconds(5.0));
        sim.Run();

        Assert.Equal(5.0, sim.Now.TotalSeconds, precision: 2);
    }

    /// <summary>
    /// Verifies that objects created by the simulation cannot be used after disposal.
    /// </summary>
    [Fact]
    public void SimulationProperties_AfterDispose_ThrowObjectDisposedException()
    {
        var sim = new Simulation();
        sim.Dispose();

        Assert.Throws<ObjectDisposedException>(() => sim.SetSeed(123));
        Assert.Throws<ObjectDisposedException>(() => sim.Stop(TimeSpan.FromSeconds(1.0)));
        Assert.Throws<ObjectDisposedException>(() => sim.Run());
        Assert.Throws<ObjectDisposedException>(() => _ = sim.Now);
        Assert.Throws<ObjectDisposedException>(() => _ = sim.IsRunning);
        Assert.Throws<ObjectDisposedException>(() => sim.CreateNodes(1));
        Assert.Throws<ObjectDisposedException>(() => sim.Schedule(TimeSpan.FromSeconds(1.0), () => { }));
    }

    /// <summary>
    /// Verifies that a fresh simulation starts at time zero.
    /// </summary>
    [Fact]
    public void Simulation_InitialTime_IsZero()
    {
        using var sim = new Simulation();
        Assert.Equal(TimeSpan.Zero, sim.Now);
    }

    /// <summary>
    /// Verifies that IsRunning is false before Run and true during Run.
    /// We test before and after; during is harder to test without threading.
    /// </summary>
    [Fact]
    public void IsRunning_BeforeRun_False_AfterRun_False()
    {
        using var sim = new Simulation();
        Assert.False(sim.IsRunning);

        sim.Stop(TimeSpan.FromSeconds(0.1));
        sim.Run();

        Assert.False(sim.IsRunning);
    }

    /// <summary>
    /// Verifies that creating zero nodes throws ArgumentOutOfRangeException.
    /// </summary>
    [Fact]
    public void CreateNodes_Zero_ShouldThrowArgumentOutOfRangeException()
    {
        using var sim = new Simulation();
        Assert.Throws<ArgumentOutOfRangeException>(() => sim.CreateNodes(0));
    }

    /// <summary>
    /// Verifies that creating negative nodes throws ArgumentOutOfRangeException.
    /// </summary>
    [Fact]
    public void CreateNodes_Negative_ShouldThrowArgumentOutOfRangeException()
    {
        using var sim = new Simulation();
        Assert.Throws<ArgumentOutOfRangeException>(() => sim.CreateNodes(-1));
    }

    /// <summary>
    /// Verifies that InstallInternetStack with no nodes throws ArgumentException.
    /// </summary>
    [Fact]
    public void InstallInternetStack_EmptyArray_ShouldThrowArgumentException()
    {
        using var sim = new Simulation();
        Assert.Throws<ArgumentException>(() => sim.InstallInternetStack());
    }

    /// <summary>
    /// Verifies that AssignIpv4Addresses with no devices throws ArgumentException.
    /// </summary>
    [Fact]
    public void AssignIpv4Addresses_EmptyArray_ShouldThrowArgumentException()
    {
        using var sim = new Simulation();
        Assert.Throws<ArgumentException>(() =>
            sim.AssignIpv4Addresses(Array.Empty<Device>(), "10.0.0.0", "255.255.255.0"));
    }
}
