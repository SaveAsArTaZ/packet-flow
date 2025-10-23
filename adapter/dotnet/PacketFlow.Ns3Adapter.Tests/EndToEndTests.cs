// EndToEndTests.cs
// End-to-end integration tests

using Xunit;
using PacketFlow.Ns3Adapter;

namespace PacketFlow.Ns3Adapter.Tests;

public class EndToEndTests
{
    [Fact]
    public void P2PEcho_EndToEnd_ShouldTransmitPackets()
    {
        // Arrange
        using var sim = new Simulation();
        sim.SetSeed(12345);
        
        var nodes = sim.CreateNodes(2);
        sim.InstallInternetStack(nodes);
        
        var (dev0, dev1) = PointToPoint.Install(sim, nodes[0], nodes[1], "5Mbps", "2ms");
        sim.AssignIpv4Addresses(new[] { dev0, dev1 }, "10.1.1.0", "255.255.255.0");
        
        var server = UdpEcho.CreateServer(sim, nodes[1], 9);
        server.Start(TimeSpan.FromSeconds(1.0));
        server.Stop(TimeSpan.FromSeconds(10.0));
        
        var client = UdpEcho.CreateClient(sim, nodes[0], "10.1.1.2", 9, 1024, TimeSpan.FromSeconds(1.0), 3);
        client.Start(TimeSpan.FromSeconds(2.0));
        client.Stop(TimeSpan.FromSeconds(10.0));
        
        int rxCount = 0;
        dev0.SubscribeToPacketEvents(
            onTx: null,
            onRx: evt => rxCount++
        );
        
        // Act
        sim.Stop(TimeSpan.FromSeconds(10.0));
        sim.Run();
        
        // Assert
        Assert.True(rxCount > 0, "Expected to receive at least one packet");
        Assert.Equal(10.0, sim.Now.TotalSeconds, precision: 2);
    }

    [Fact]
    public void CallbackMarshalling_ShouldWorkCorrectly()
    {
        // Arrange
        using var sim = new Simulation();
        var callbackCount = 0;
        var expectedCallbacks = 5;
        
        // Schedule multiple callbacks
        for (int i = 0; i < expectedCallbacks; i++)
        {
            sim.Schedule(TimeSpan.FromSeconds(i * 0.1), () => callbackCount++);
        }
        
        // Act
        sim.Stop(TimeSpan.FromSeconds(1.0));
        sim.Run();
        
        // Assert
        Assert.Equal(expectedCallbacks, callbackCount);
    }

    [Fact]
    public void PacketTracing_ShouldCaptureEvents()
    {
        // Arrange
        using var sim = new Simulation();
        var nodes = sim.CreateNodes(2);
        sim.InstallInternetStack(nodes);
        
        var (dev0, dev1) = PointToPoint.Install(sim, nodes[0], nodes[1], "1Gbps", "1ms");
        sim.AssignIpv4Addresses(new[] { dev0, dev1 }, "192.168.1.0", "255.255.255.0");
        
        var server = UdpEcho.CreateServer(sim, nodes[1], 9);
        server.Start(TimeSpan.Zero);
        server.Stop(TimeSpan.FromSeconds(5.0));
        
        var client = UdpEcho.CreateClient(sim, nodes[0], "192.168.1.2", 9, 512, TimeSpan.FromSeconds(0.5), 5);
        client.Start(TimeSpan.FromSeconds(1.0));
        client.Stop(TimeSpan.FromSeconds(5.0));
        
        var txEvents = new List<PacketEvent>();
        var rxEvents = new List<PacketEvent>();
        
        dev0.SubscribeToPacketEvents(
            onTx: evt => txEvents.Add(evt),
            onRx: evt => rxEvents.Add(evt)
        );
        
        // Act
        sim.Stop(TimeSpan.FromSeconds(5.0));
        sim.Run();
        
        // Assert
        Assert.NotEmpty(txEvents);
        Assert.NotEmpty(rxEvents);
        Assert.All(txEvents, evt => Assert.True(evt.Bytes > 0));
        Assert.All(rxEvents, evt => Assert.True(evt.Bytes > 0));
    }

    [Fact]
    public void MultipleSimulations_Sequential_ShouldWork()
    {
        // Test that we can create and destroy multiple simulations sequentially
        for (int i = 0; i < 3; i++)
        {
            using var sim = new Simulation();
            sim.SetSeed((uint)i);
            
            var nodes = sim.CreateNodes(2);
            sim.InstallInternetStack(nodes);
            
            sim.Stop(TimeSpan.FromSeconds(0.1));
            sim.Run();
            
            Assert.Equal(0.1, sim.Now.TotalSeconds, precision: 3);
        }
    }
}

