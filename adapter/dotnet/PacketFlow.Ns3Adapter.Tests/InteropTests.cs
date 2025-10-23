// InteropTests.cs
// Tests for P/Invoke interop and basic ns-3 functionality

using Xunit;
using PacketFlow.Ns3Adapter;

namespace PacketFlow.Ns3Adapter.Tests;

public class InteropTests
{
    [Fact]
    public void CreateNodes_ShouldReturnCorrectCount()
    {
        // Arrange
        using var sim = new Simulation();
        
        // Act
        var nodes = sim.CreateNodes(5);
        
        // Assert
        Assert.Equal(5, nodes.Length);
        Assert.All(nodes, node => Assert.NotNull(node));
    }

    [Fact]
    public void InstallInternetStack_ShouldNotThrow()
    {
        // Arrange
        using var sim = new Simulation();
        var nodes = sim.CreateNodes(2);
        
        // Act & Assert
        sim.InstallInternetStack(nodes);
    }

    [Fact]
    public void P2PInstall_ShouldReturnDevices()
    {
        // Arrange
        using var sim = new Simulation();
        var nodes = sim.CreateNodes(2);
        
        // Act
        var (dev0, dev1) = PointToPoint.Install(sim, nodes[0], nodes[1], "5Mbps", "2ms");
        
        // Assert
        Assert.NotNull(dev0);
        Assert.NotNull(dev1);
    }

    [Fact]
    public void CsmaInstall_ShouldReturnCorrectDeviceCount()
    {
        // Arrange
        using var sim = new Simulation();
        var nodes = sim.CreateNodes(4);
        
        // Act
        var devices = Csma.Install(sim, nodes, "100Mbps", "6560ns");
        
        // Assert
        Assert.Equal(4, devices.Length);
        Assert.All(devices, dev => Assert.NotNull(dev));
    }

    [Fact]
    public void AssignIpv4Addresses_ShouldNotThrow()
    {
        // Arrange
        using var sim = new Simulation();
        var nodes = sim.CreateNodes(2);
        sim.InstallInternetStack(nodes);
        var (dev0, dev1) = PointToPoint.Install(sim, nodes[0], nodes[1], "5Mbps", "2ms");
        
        // Act & Assert
        sim.AssignIpv4Addresses(new[] { dev0, dev1 }, "10.1.1.0", "255.255.255.0");
    }

    [Fact]
    public void PopulateRoutingTables_ShouldNotThrow()
    {
        // Arrange
        using var sim = new Simulation();
        var nodes = sim.CreateNodes(2);
        sim.InstallInternetStack(nodes);
        var (dev0, dev1) = PointToPoint.Install(sim, nodes[0], nodes[1], "5Mbps", "2ms");
        sim.AssignIpv4Addresses(new[] { dev0, dev1 }, "10.1.1.0", "255.255.255.0");
        
        // Act & Assert
        sim.PopulateRoutingTables();
    }

    [Fact]
    public void SetPosition_ShouldNotThrow()
    {
        // Arrange
        using var sim = new Simulation();
        var nodes = sim.CreateNodes(1);
        
        // Act & Assert
        nodes[0].SetPosition(10.0, 20.0, 0.0);
    }

    [Fact]
    public void UdpEchoServer_ShouldBeCreated()
    {
        // Arrange
        using var sim = new Simulation();
        var nodes = sim.CreateNodes(1);
        sim.InstallInternetStack(nodes);
        
        // Act
        var server = UdpEcho.CreateServer(sim, nodes[0], 9);
        
        // Assert
        Assert.NotNull(server);
    }

    [Fact]
    public void UdpEchoClient_ShouldBeCreated()
    {
        // Arrange
        using var sim = new Simulation();
        var nodes = sim.CreateNodes(1);
        sim.InstallInternetStack(nodes);
        
        // Act
        var client = UdpEcho.CreateClient(sim, nodes[0], "10.1.1.2", 9, 1024, TimeSpan.FromSeconds(1.0), 5);
        
        // Assert
        Assert.NotNull(client);
    }

    [Fact]
    public void Application_StartStop_ShouldNotThrow()
    {
        // Arrange
        using var sim = new Simulation();
        var nodes = sim.CreateNodes(1);
        sim.InstallInternetStack(nodes);
        var app = UdpEcho.CreateServer(sim, nodes[0], 9);
        
        // Act & Assert
        app.Start(TimeSpan.FromSeconds(1.0));
        app.Stop(TimeSpan.FromSeconds(5.0));
    }

    [Fact]
    public void FlowMonitor_ShouldBeInstalled()
    {
        // Arrange
        using var sim = new Simulation();
        var nodes = sim.CreateNodes(2);
        sim.InstallInternetStack(nodes);
        
        // Act
        var flowMon = FlowMonitor.InstallAll(sim);
        
        // Assert
        Assert.NotNull(flowMon);
    }

    [Fact]
    public void FlowMonitor_CollectStatistics_ShouldReturnValidData()
    {
        // Arrange
        using var sim = new Simulation();
        var nodes = sim.CreateNodes(2);
        sim.InstallInternetStack(nodes);
        var flowMon = FlowMonitor.InstallAll(sim);
        sim.Stop(TimeSpan.FromSeconds(1.0));
        sim.Run();
        
        // Act
        var stats = flowMon.CollectStatistics();
        
        // Assert - just verify we got something back
        Assert.True(stats.FlowCount >= 0);
    }
}

