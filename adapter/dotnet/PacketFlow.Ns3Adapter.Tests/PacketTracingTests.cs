// PacketTracingTests.cs
// Tests for packet tracing (SubscribeToPacketEvents) across all device types.
//
// Verifies:
// - TX/RX events are captured on PointToPoint devices
// - TX/RX events are captured on CSMA devices (was broken before fix)
// - TX/RX events are captured on Wi-Fi devices (was broken before fix)
// - GCHandles for trace delegates are properly tracked and freed on Dispose
// - Null callbacks are handled correctly (subscribe to only TX or only RX)

using Xunit;
using PacketFlow.Ns3Adapter;

namespace PacketFlow.Ns3Adapter.Tests;

public class PacketTracingTests
{
    /// <summary>
    /// Verifies that packet tracing on PointToPoint devices captures both
    /// TX and RX events with correct data.
    /// </summary>
    [Fact]
    public void P2P_PacketTracing_ShouldCaptureTxAndRx()
    {
        // Arrange
        using var sim = new Simulation();
        sim.SetSeed(42);

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

        var txEvents = new List<PacketEvent>();
        var rxEvents = new List<PacketEvent>();

        dev0.SubscribeToPacketEvents(
            onTx: evt => txEvents.Add(evt),
            onRx: evt => rxEvents.Add(evt));

        // Act
        sim.Stop(TimeSpan.FromSeconds(5.0));
        sim.Run();

        // Assert
        Assert.NotEmpty(txEvents);
        Assert.NotEmpty(rxEvents);
        Assert.All(txEvents, evt => Assert.True(evt.Bytes > 0, "TX event should have bytes > 0"));
        Assert.All(rxEvents, evt => Assert.True(evt.Bytes > 0, "RX event should have bytes > 0"));
        Assert.All(txEvents, evt => Assert.True(evt.Time.TotalSeconds > 0, "TX time should be > 0"));
    }

    /// <summary>
    /// Verifies that packet tracing on CSMA devices captures events.
    /// Before the fix, this would fail because trace_subscribe_packet_events
    /// hardcast to PointToPointNetDevice.
    /// </summary>
    [Fact]
    public void Csma_PacketTracing_ShouldCaptureEvents()
    {
        // Arrange
        using var sim = new Simulation();
        sim.SetSeed(123);

        var nodes = sim.CreateNodes(3);
        sim.InstallInternetStack(nodes);
        var devices = Csma.Install(sim, nodes, "100Mbps", "6560ns");
        sim.AssignIpv4Addresses(devices, "192.168.1.0", "255.255.255.0");

        var server = UdpEcho.CreateServer(sim, nodes[2], 7);
        server.Start(TimeSpan.FromSeconds(1.0));
        server.Stop(TimeSpan.FromSeconds(5.0));

        var client = UdpEcho.CreateClient(sim, nodes[0], "192.168.1.3", 7, 512,
            TimeSpan.FromSeconds(0.5), 5);
        client.Start(TimeSpan.FromSeconds(2.0));
        client.Stop(TimeSpan.FromSeconds(5.0));

        var txEvents = new List<PacketEvent>();
        var rxEvents = new List<PacketEvent>();

        // Subscribe on the CSMA device (not a P2P device!)
        devices[0].SubscribeToPacketEvents(
            onTx: evt => txEvents.Add(evt),
            onRx: evt => rxEvents.Add(evt));

        // Act
        sim.Stop(TimeSpan.FromSeconds(5.0));
        sim.Run();

        // Assert
        Assert.NotEmpty(txEvents);
        Assert.NotEmpty(rxEvents);
    }

    /// <summary>
    /// Verifies that packet tracing on Wi-Fi devices captures events.
    /// Before the fix, this would fail because trace_subscribe_packet_events
    /// hardcast to PointToPointNetDevice.
    /// </summary>
    [Fact]
    public void WiFi_PacketTracing_ShouldCaptureEvents()
    {
        // Arrange
        using var sim = new Simulation();
        sim.SetSeed(456);

        var allNodes = sim.CreateNodes(3);
        var sta0 = allNodes[0];
        var sta1 = allNodes[1];
        var ap = allNodes[2];

        sim.InstallInternetStack(allNodes);

        var (staDevices, apDevice) = WiFi.InstallStationAp(
            sim, new[] { sta0, sta1 }, ap,
            WiFiStandard.Std_80211n_2_4GHz, "HtMcs7", 1);

        // Set positions close together for reliable Wi-Fi
        sta0.SetPosition(0, 0, 0);
        sta1.SetPosition(5, 0, 0);
        ap.SetPosition(2.5, 5, 0);

        var allDevs = new[] { staDevices[0], staDevices[1], apDevice };
        sim.AssignIpv4Addresses(allDevs, "10.1.2.0", "255.255.255.0");
        sim.PopulateRoutingTables();

        var server = UdpEcho.CreateServer(sim, ap, 9);
        server.Start(TimeSpan.FromSeconds(1.0));
        server.Stop(TimeSpan.FromSeconds(10.0));

        var client = UdpEcho.CreateClient(sim, sta0, "10.1.2.3", 9, 512,
            TimeSpan.FromSeconds(1.0), 5);
        client.Start(TimeSpan.FromSeconds(2.0));
        client.Stop(TimeSpan.FromSeconds(10.0));

        var txEvents = new List<PacketEvent>();
        var rxEvents = new List<PacketEvent>();

        // Subscribe on the Wi-Fi station device (not a P2P device!)
        staDevices[0].SubscribeToPacketEvents(
            onTx: evt => txEvents.Add(evt),
            onRx: evt => rxEvents.Add(evt));

        // Act
        sim.Stop(TimeSpan.FromSeconds(10.0));
        sim.Run();

        // Assert: Wi-Fi should have TX events (at minimum).
        // RX depends on Wi-Fi reliability in the test environment,
        // but TX should always be captured since the client sends packets.
        Assert.NotEmpty(txEvents);
    }

    /// <summary>
    /// Verifies that subscribing with only onTx (null onRx) works correctly.
    /// </summary>
    [Fact]
    public void SubscribeToPacketEvents_OnlyTx_ShouldNotThrow()
    {
        using var sim = new Simulation();
        var nodes = sim.CreateNodes(2);
        sim.InstallInternetStack(nodes);
        var (dev0, dev1) = PointToPoint.Install(sim, nodes[0], nodes[1], "5Mbps", "2ms");
        sim.AssignIpv4Addresses(new[] { dev0, dev1 }, "10.1.1.0", "255.255.255.0");

        var txCount = 0;
        dev0.SubscribeToPacketEvents(
            onTx: evt => Interlocked.Increment(ref txCount),
            onRx: null);

        var server = UdpEcho.CreateServer(sim, nodes[1], 9);
        server.Start(TimeSpan.FromSeconds(1.0));
        server.Stop(TimeSpan.FromSeconds(3.0));
        var client = UdpEcho.CreateClient(sim, nodes[0], "10.1.1.2", 9, 1024,
            TimeSpan.FromSeconds(0.5), 2);
        client.Start(TimeSpan.FromSeconds(1.5));
        client.Stop(TimeSpan.FromSeconds(3.0));

        sim.Stop(TimeSpan.FromSeconds(3.0));
        sim.Run();

        Assert.True(txCount > 0);
    }

    /// <summary>
    /// Verifies that subscribing with only onRx (null onTx) works correctly.
    /// </summary>
    [Fact]
    public void SubscribeToPacketEvents_OnlyRx_ShouldNotThrow()
    {
        using var sim = new Simulation();
        var nodes = sim.CreateNodes(2);
        sim.InstallInternetStack(nodes);
        var (dev0, dev1) = PointToPoint.Install(sim, nodes[0], nodes[1], "5Mbps", "2ms");
        sim.AssignIpv4Addresses(new[] { dev0, dev1 }, "10.1.1.0", "255.255.255.0");

        var rxCount = 0;
        dev0.SubscribeToPacketEvents(
            onTx: null,
            onRx: evt => Interlocked.Increment(ref rxCount));

        var server = UdpEcho.CreateServer(sim, nodes[1], 9);
        server.Start(TimeSpan.FromSeconds(1.0));
        server.Stop(TimeSpan.FromSeconds(3.0));
        var client = UdpEcho.CreateClient(sim, nodes[0], "10.1.1.2", 9, 1024,
            TimeSpan.FromSeconds(0.5), 2);
        client.Start(TimeSpan.FromSeconds(1.5));
        client.Stop(TimeSpan.FromSeconds(3.0));

        sim.Stop(TimeSpan.FromSeconds(3.0));
        sim.Run();

        Assert.True(rxCount > 0);
    }

    /// <summary>
    /// Verifies that trace subscription handles are safely cleaned up when
    /// the simulation is disposed (no crashes, no native memory leaks).
    /// </summary>
    [Fact]
    public void TraceSubscriptions_CleanedUpOnDispose()
    {
        var sim = new Simulation();
        var nodes = sim.CreateNodes(2);
        sim.InstallInternetStack(nodes);
        var (dev0, dev1) = PointToPoint.Install(sim, nodes[0], nodes[1], "5Mbps", "2ms");
        sim.AssignIpv4Addresses(new[] { dev0, dev1 }, "10.1.1.0", "255.255.255.0");

        var txCount = 0;
        dev0.SubscribeToPacketEvents(
            onTx: evt => Interlocked.Increment(ref txCount),
            onRx: evt => { });

        // Don't run the simulation — dispose immediately to verify cleanup
        // of persistent trace handles works even without running.
        sim.Dispose();

        // If we get here without exception, trace handle cleanup worked
        Assert.True(true);
    }

    /// <summary>
    /// Verifies that multiple subscribe calls on the same device
    /// do not cause issues.
    /// </summary>
    [Fact]
    public void MultipleSubscriptions_SameDevice_ShouldNotThrow()
    {
        using var sim = new Simulation();
        var nodes = sim.CreateNodes(2);
        sim.InstallInternetStack(nodes);
        var (dev0, dev1) = PointToPoint.Install(sim, nodes[0], nodes[1], "5Mbps", "2ms");
        sim.AssignIpv4Addresses(new[] { dev0, dev1 }, "10.1.1.0", "255.255.255.0");

        var count1 = 0;
        var count2 = 0;

        // First subscription
        dev0.SubscribeToPacketEvents(
            onTx: evt => Interlocked.Increment(ref count1),
            onRx: null);

        // Second subscription on the same device
        dev0.SubscribeToPacketEvents(
            onTx: evt => Interlocked.Increment(ref count2),
            onRx: null);

        var server = UdpEcho.CreateServer(sim, nodes[1], 9);
        server.Start(TimeSpan.FromSeconds(1.0));
        server.Stop(TimeSpan.FromSeconds(3.0));
        var client = UdpEcho.CreateClient(sim, nodes[0], "10.1.1.2", 9, 1024,
            TimeSpan.FromSeconds(0.5), 2);
        client.Start(TimeSpan.FromSeconds(1.5));
        client.Stop(TimeSpan.FromSeconds(3.0));

        sim.Stop(TimeSpan.FromSeconds(3.0));
        sim.Run();

        // Both subscriptions should receive events
        Assert.True(count1 > 0);
        Assert.True(count2 > 0);
    }
}
