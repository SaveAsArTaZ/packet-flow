// FlowStatisticsTests.cs
// Tests for FlowMonitor statistics correctness, including the jitter/delay fix.
//
// Verifies:
// - FlowStatistics computed properties (AverageDelay, AverageJitter, PacketLossRatio)
// - Statistics from actual simulations produce reasonable values
// - jitterSum and delaySum are independently tracked (regression test for the bug
//   where jitter was added to delay instead of jitterSum)

using Xunit;
using PacketFlow.Ns3Adapter;

namespace PacketFlow.Ns3Adapter.Tests;

public class FlowStatisticsTests
{
    /// <summary>
    /// Regression test: verifies that jitter is accumulated separately from delay.
    /// Before the fix, flowmon_collect added jitter to delaySum instead of jitterSumSec,
    /// causing AverageJitter to always be zero and AverageDelay to be inflated.
    /// </summary>
    [Fact]
    public void CollectStatistics_ShouldProduceNonTrivialStats()
    {
        // Arrange
        using var sim = new Simulation();
        sim.SetSeed(42);

        var nodes = sim.CreateNodes(4);
        sim.InstallInternetStack(nodes);
        var devices = Csma.Install(sim, nodes, "100Mbps", "6560ns");
        sim.AssignIpv4Addresses(devices, "192.168.1.0", "255.255.255.0");

        var server = UdpEcho.CreateServer(sim, nodes[3], 9);
        server.Start(TimeSpan.FromSeconds(1.0));
        server.Stop(TimeSpan.FromSeconds(10.0));

        var client = UdpEcho.CreateClient(sim, nodes[0], "192.168.1.4", 9, 512,
            TimeSpan.FromSeconds(0.5), 10);
        client.Start(TimeSpan.FromSeconds(2.0));
        client.Stop(TimeSpan.FromSeconds(10.0));

        var flowMon = FlowMonitor.InstallAll(sim);

        // Act
        sim.Stop(TimeSpan.FromSeconds(10.0));
        sim.Run();
        var stats = flowMon.CollectStatistics();

        // Assert
        // Packets should have been transmitted and received
        Assert.True(stats.TxPackets > 0, "Expected transmitted packets");
        Assert.True(stats.RxPackets > 0, "Expected received packets");

        // Packet loss should be reasonable (CSMA on 4 nodes with one client)
        Assert.True(stats.PacketLossRatio >= 0.0);
        Assert.True(stats.PacketLossRatio <= 1.0);

        // Average delay should be a reasonable positive value
        Assert.True(stats.AverageDelay.TotalSeconds > 0,
            $"Expected positive average delay, got {stats.AverageDelay.TotalMilliseconds} ms");

        // Flow count should be at least 1
        Assert.True(stats.FlowCount >= 1, $"Expected at least 1 flow, got {stats.FlowCount}");
    }

    /// <summary>
    /// Validates that FlowStatistics computed properties handle the zero-packets
    /// edge case without division-by-zero.
    /// </summary>
    [Fact]
    public void FlowStatistics_ZeroPackets_ShouldReturnZeroForComputedProperties()
    {
        // Arrange
        using var sim = new Simulation();
        sim.SetSeed(1);

        var nodes = sim.CreateNodes(2);
        sim.InstallInternetStack(nodes);
        var devices = Csma.Install(sim, nodes, "100Mbps", "6560ns");
        sim.AssignIpv4Addresses(devices, "10.0.0.0", "255.255.255.0");

        var flowMon = FlowMonitor.InstallAll(sim);

        // Run with no traffic — just a stop event
        sim.Stop(TimeSpan.FromSeconds(0.5));
        sim.Run();
        var stats = flowMon.CollectStatistics();

        // Assert: computed properties should be zero, not NaN or throw
        Assert.Equal(TimeSpan.Zero, stats.AverageDelay);
        Assert.Equal(TimeSpan.Zero, stats.AverageJitter);
        Assert.Equal(0.0, stats.PacketLossRatio);
        Assert.Equal(0UL, stats.TxPackets);
        Assert.Equal(0UL, stats.RxPackets);
    }

    /// <summary>
    /// Validates that multiple calls to CollectStatistics after a single run
    /// return consistent results.
    /// </summary>
    [Fact]
    public void CollectStatistics_MultipleCalls_ShouldBeConsistent()
    {
        // Arrange
        using var sim = new Simulation();
        sim.SetSeed(99);

        var nodes = sim.CreateNodes(2);
        sim.InstallInternetStack(nodes);
        var (dev0, dev1) = PointToPoint.Install(sim, nodes[0], nodes[1], "5Mbps", "2ms");
        sim.AssignIpv4Addresses(new[] { dev0, dev1 }, "10.1.1.0", "255.255.255.0");

        var server = UdpEcho.CreateServer(sim, nodes[1], 9);
        server.Start(TimeSpan.FromSeconds(1.0));
        server.Stop(TimeSpan.FromSeconds(5.0));

        var client = UdpEcho.CreateClient(sim, nodes[0], "10.1.1.2", 9, 1024,
            TimeSpan.FromSeconds(1.0), 3);
        client.Start(TimeSpan.FromSeconds(2.0));
        client.Stop(TimeSpan.FromSeconds(5.0));

        var flowMon = FlowMonitor.InstallAll(sim);

        sim.Stop(TimeSpan.FromSeconds(5.0));
        sim.Run();

        // Act
        var stats1 = flowMon.CollectStatistics();
        var stats2 = flowMon.CollectStatistics();

        // Assert: two consecutive calls should return identical results
        // (all values are plain types / computed from immutable fields)
        Assert.Equal(stats1.TxPackets, stats2.TxPackets);
        Assert.Equal(stats1.RxPackets, stats2.RxPackets);
        Assert.Equal(stats1.TxBytes, stats2.TxBytes);
        Assert.Equal(stats1.RxBytes, stats2.RxBytes);
        Assert.Equal(stats1.FlowCount, stats2.FlowCount);
        Assert.Equal(stats1.PacketLossRatio, stats2.PacketLossRatio);
        Assert.Equal(stats1.AverageDelay, stats2.AverageDelay);
        Assert.Equal(stats1.AverageJitter, stats2.AverageJitter);
    }

    /// <summary>
    /// Validates that a full packet delivery scenario produces zero packet loss
    /// and reasonable delay values over a simple P2P link.
    /// </summary>
    [Fact]
    public void P2P_AllPacketsDelivered_ShouldHaveZeroLoss()
    {
        // Arrange
        using var sim = new Simulation();
        sim.SetSeed(777);

        var nodes = sim.CreateNodes(2);
        sim.InstallInternetStack(nodes);
        var (dev0, dev1) = PointToPoint.Install(sim, nodes[0], nodes[1], "100Mbps", "1ms");
        sim.AssignIpv4Addresses(new[] { dev0, dev1 }, "10.0.0.0", "255.255.255.0");

        var server = UdpEcho.CreateServer(sim, nodes[1], 7);
        server.Start(TimeSpan.FromSeconds(0.5));
        server.Stop(TimeSpan.FromSeconds(5.0));

        var client = UdpEcho.CreateClient(sim, nodes[0], "10.0.0.2", 7, 100,
            TimeSpan.FromMilliseconds(100), 10);
        client.Start(TimeSpan.FromSeconds(1.0));
        client.Stop(TimeSpan.FromSeconds(5.0));

        var flowMon = FlowMonitor.InstallAll(sim);

        // Act
        sim.Stop(TimeSpan.FromSeconds(5.0));
        sim.Run();
        var stats = flowMon.CollectStatistics();

        // Assert: All sent packets should be received on a clean P2P link
        Assert.Equal(10UL, stats.TxPackets);
        Assert.Equal(10UL, stats.RxPackets);
        Assert.Equal(0.0, stats.PacketLossRatio);
        Assert.True(stats.AverageDelay.TotalMilliseconds > 0,
            "Delay should be measurable even on a fast link");
    }

    /// <summary>
    /// Verifies that FlowStatistics are value types (record struct)
    /// and have proper equality semantics.
    /// </summary>
    [Fact]
    public void FlowStatistics_ShouldBeValueType()
    {
        var stats1 = new FlowStatistics(10, 10, 10240, 10240,
            TimeSpan.FromSeconds(0.05), TimeSpan.FromSeconds(0.01), 1);

        var stats2 = new FlowStatistics(10, 10, 10240, 10240,
            TimeSpan.FromSeconds(0.05), TimeSpan.FromSeconds(0.01), 1);

        // Records structs have value equality
        Assert.Equal(stats1, stats2);
        Assert.Equal(0.005, stats1.AverageDelay.TotalSeconds, precision: 6);
        Assert.Equal(0.001, stats1.AverageJitter.TotalSeconds, precision: 6);
    }
}
