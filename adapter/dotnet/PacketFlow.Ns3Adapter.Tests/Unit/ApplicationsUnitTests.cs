// ApplicationsUnitTests.cs — unit tests using StubNativeInterop (no native DLL).

using Xunit;
using PacketFlow.Ns3Adapter;
using PacketFlow.Ns3Adapter.Interop;

namespace PacketFlow.Ns3Adapter.Tests.Unit;

public class ApplicationsUnitTests
{
    private static (Simulation Sim, StubNativeInterop Stub) Create()
    {
        var stub = new StubNativeInterop();
        return (new Simulation(stub, ownsNative: false), stub);
    }

    // ========================================================================
    // UdpEcho Server
    // ========================================================================

    [Fact]
    public void UdpEcho_CreateServer_ReturnsApp()
    {
        var (sim, _) = Create();
        var node = sim.CreateNodes(1)[0];
        var app = UdpEcho.CreateServer(sim, node, 9);
        Assert.NotNull(app);
        Assert.Same(sim, app.Simulation);
    }

    [Fact]
    public void UdpEcho_CreateServer_NullSim_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => UdpEcho.CreateServer(null!, null!, 9));
    }

    [Fact]
    public void UdpEcho_CreateServer_NullNode_Throws()
    {
        var (sim, _) = Create();
        Assert.Throws<ArgumentNullException>(() => UdpEcho.CreateServer(sim, null!, 9));
    }

    [Fact]
    public void UdpEcho_CreateServer_NativeFails_Throws()
    {
        var (sim, stub) = Create();
        var node = sim.CreateNodes(1)[0];
        stub.AppResult = NativeMethods.Ns3Status.Error;
        Assert.Throws<Ns3Exception>(() => UdpEcho.CreateServer(sim, node, 9));
    }

    // ========================================================================
    // UdpEcho Client
    // ========================================================================

    [Fact]
    public void UdpEcho_CreateClient_ReturnsApp()
    {
        var (sim, _) = Create();
        var node = sim.CreateNodes(1)[0];
        var app = UdpEcho.CreateClient(sim, node, "10.1.1.2", 9, 1024, TimeSpan.FromSeconds(1.5), 5);
        Assert.NotNull(app);
        Assert.Same(sim, app.Simulation);
    }

    [Fact]
    public void UdpEcho_CreateClient_NullSim_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            UdpEcho.CreateClient(null!, null!, "10.0.0.1", 9, 1024, TimeSpan.FromSeconds(1), 5));
    }

    [Fact]
    public void UdpEcho_CreateClient_NullNode_Throws()
    {
        var (sim, _) = Create();
        Assert.Throws<ArgumentNullException>(() =>
            UdpEcho.CreateClient(sim, null!, "10.0.0.1", 9, 1024, TimeSpan.FromSeconds(1), 5));
    }

    [Fact]
    public void UdpEcho_CreateClient_EmptyIp_Throws()
    {
        var (sim, _) = Create();
        var node = sim.CreateNodes(1)[0];
        Assert.Throws<ArgumentException>(() =>
            UdpEcho.CreateClient(sim, node, "", 9, 1024, TimeSpan.FromSeconds(1), 5));
    }

    [Fact]
    public void UdpEcho_CreateClient_NativeFails_Throws()
    {
        var (sim, stub) = Create();
        var node = sim.CreateNodes(1)[0];
        stub.AppResult = NativeMethods.Ns3Status.Error;
        Assert.Throws<Ns3Exception>(() =>
            UdpEcho.CreateClient(sim, node, "10.0.0.1", 9, 1024, TimeSpan.FromSeconds(1), 5));
    }

    // ========================================================================
    // Application Start/Stop
    // ========================================================================

    [Fact]
    public void Application_Start_DoesNotThrow()
    {
        var (sim, _) = Create();
        var node = sim.CreateNodes(1)[0];
        var app = UdpEcho.CreateServer(sim, node, 9);
        app.Start(TimeSpan.FromSeconds(2.5));
    }

    [Fact]
    public void Application_Stop_DoesNotThrow()
    {
        var (sim, _) = Create();
        var node = sim.CreateNodes(1)[0];
        var app = UdpEcho.CreateServer(sim, node, 9);
        app.Stop(TimeSpan.FromSeconds(10.0));
    }

    // ========================================================================
    // FlowMonitor
    // ========================================================================

    [Fact]
    public void FlowMonitor_InstallAll_ReturnsInstance()
    {
        var (sim, _) = Create();
        var fm = FlowMonitor.InstallAll(sim);
        Assert.NotNull(fm);
    }

    [Fact]
    public void FlowMonitor_InstallAll_NullSim_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => FlowMonitor.InstallAll(null!));
    }

    [Fact]
    public void FlowMonitor_CollectStatistics_ReturnsCorrectValues()
    {
        var (sim, stub) = Create();
        stub.FlowStatsResult = new NativeMethods.Ns3FlowStats
        {
            TxPackets = 100, RxPackets = 95,
            TxBytes = 102400, RxBytes = 97280,
            DelaySumSec = 0.5, JitterSumSec = 0.05,
            FlowCount = 3
        };

        var fm = FlowMonitor.InstallAll(sim);
        var stats = fm.CollectStatistics();

        Assert.Equal(100UL, stats.TxPackets);
        Assert.Equal(95UL, stats.RxPackets);
        Assert.Equal(3U, stats.FlowCount);
        Assert.Equal(0.05, stats.PacketLossRatio, 3);
    }

    [Fact]
    public void FlowMonitor_CollectStatistics_NativeFails_Throws()
    {
        var (sim, stub) = Create();
        stub.FlowMonCollectResult = NativeMethods.Ns3Status.Error;
        var fm = FlowMonitor.InstallAll(sim);
        Assert.Throws<Ns3Exception>(() => fm.CollectStatistics());
    }

    // ========================================================================
    // FlowStatistics Computed Properties (pure C# logic, no interop)
    // ========================================================================

    [Fact]
    public void FlowStatistics_AllDelivered_ZeroLoss() =>
        Assert.Equal(0.0, new FlowStatistics(10, 10, 10240, 10240, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1), 1).PacketLossRatio);

    [Fact]
    public void FlowStatistics_HalfLost_50Percent() =>
        Assert.Equal(0.5, new FlowStatistics(10, 5, 10240, 5120, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1), 1).PacketLossRatio, 6);

    [Fact]
    public void FlowStatistics_AllLost_FullLoss()
    {
        var s = new FlowStatistics(10, 0, 10240, 0, TimeSpan.Zero, TimeSpan.Zero, 1);
        Assert.Equal(1.0, s.PacketLossRatio);
        Assert.Equal(TimeSpan.Zero, s.AverageDelay);
    }

    [Fact]
    public void FlowStatistics_ZeroTx_AllZero()
    {
        var s = new FlowStatistics(0, 0, 0, 0, TimeSpan.Zero, TimeSpan.Zero, 0);
        Assert.Equal(0.0, s.PacketLossRatio);
        Assert.Equal(TimeSpan.Zero, s.AverageDelay);
    }

    [Fact]
    public void FlowStatistics_AverageDelay_ComputedCorrectly() =>
        Assert.Equal(0.1, new FlowStatistics(5, 5, 5000, 5000, TimeSpan.FromSeconds(0.5), TimeSpan.Zero, 2).AverageDelay.TotalSeconds, 6);

    [Fact]
    public void FlowStatistics_AverageJitter_ComputedCorrectly() =>
        Assert.Equal(0.01, new FlowStatistics(4, 4, 4000, 4000, TimeSpan.Zero, TimeSpan.FromSeconds(0.04), 1).AverageJitter.TotalSeconds, 6);

    [Fact]
    public void FlowStatistics_ValueEquality()
    {
        var s1 = new FlowStatistics(1, 1, 100, 100, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1), 1);
        var s2 = new FlowStatistics(1, 1, 100, 100, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1), 1);
        var s3 = new FlowStatistics(2, 2, 200, 200, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(2), 2);
        Assert.Equal(s1, s2);
        Assert.NotEqual(s1, s3);
    }

    [Fact]
    public void FlowStatistics_DelayAndJitter_Independent()
    {
        // Regression: verify delay and jitter stay separate in the C# record struct
        var highDelay = new FlowStatistics(10, 10, 1000, 1000, TimeSpan.FromSeconds(5.0), TimeSpan.FromSeconds(0.001), 1);
        var highJitter = new FlowStatistics(10, 10, 1000, 1000, TimeSpan.FromSeconds(0.001), TimeSpan.FromSeconds(5.0), 1);

        Assert.Equal(0.5, highDelay.AverageDelay.TotalSeconds, 4);
        Assert.Equal(0.0001, highDelay.AverageJitter.TotalSeconds, 4);
        Assert.Equal(0.0001, highJitter.AverageDelay.TotalSeconds, 4);
        Assert.Equal(0.5, highJitter.AverageJitter.TotalSeconds, 4);
    }
}
