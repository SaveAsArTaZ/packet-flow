// SimulationUnitTests.cs
// Unit tests using StubNativeInterop — run WITHOUT ns-3 native DLL.

using Xunit;
using PacketFlow.Ns3Adapter;
using PacketFlow.Ns3Adapter.Interop;

namespace PacketFlow.Ns3Adapter.Tests.Unit;

public class SimulationUnitTests
{
    private static (Simulation Sim, StubNativeInterop Stub) Create()
    {
        var stub = new StubNativeInterop();
        var sim = new Simulation(stub, ownsNative: false);
        return (sim, stub);
    }

    // ========================================================================
    // Lifecycle
    // ========================================================================

    [Fact]
    public void Constructor_CreatesSim()
    {
        var (sim, _) = Create();
        Assert.NotNull(sim);
    }

    [Fact]
    public void Constructor_SimCreateFails_ThrowsNs3Exception()
    {
        var stub = new StubNativeInterop { SimCreateResult = NativeMethods.Ns3Status.Error };
        Assert.Throws<Ns3Exception>(() => new Simulation(stub, ownsNative: false));
    }

    [Fact]
    public void Constructor_NullInterop_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new Simulation(null!, ownsNative: false));
    }

    [Fact]
    public void SetSeed_PassesCorrectValue()
    {
        var (sim, stub) = Create();
        sim.SetSeed(42);
        Assert.Equal(42u, stub.CapturedSeed);
    }

    [Fact]
    public void SetSeed_NativeFails_Throws()
    {
        var (sim, stub) = Create();
        stub.SimSetSeedResult = NativeMethods.Ns3Status.Error;
        Assert.Throws<Ns3Exception>(() => sim.SetSeed(1));
    }

    [Fact]
    public void SetSeed_AfterDispose_Throws()
    {
        var (sim, _) = Create();
        sim.Dispose();
        Assert.Throws<ObjectDisposedException>(() => sim.SetSeed(1));
    }

    [Fact]
    public void Run_FailsAfterDispose()
    {
        var (sim, _) = Create();
        sim.Dispose();
        Assert.Throws<ObjectDisposedException>(() => sim.Run());
    }

    [Fact]
    public void Run_NativeFails_Throws()
    {
        var (sim, stub) = Create();
        stub.SimRunResult = NativeMethods.Ns3Status.Error;
        Assert.Throws<Ns3Exception>(() => sim.Run());
    }

    [Fact]
    public void Stop_PassesCorrectTime()
    {
        var (sim, stub) = Create();
        sim.Stop(TimeSpan.FromSeconds(5.5));
        Assert.Equal(5.5, stub.CapturedStopTime);
    }

    [Fact]
    public void Stop_NativeFails_Throws()
    {
        var (sim, stub) = Create();
        stub.SimStopResult = NativeMethods.Ns3Status.Error;
        Assert.Throws<Ns3Exception>(() => sim.Stop(TimeSpan.FromSeconds(1.0)));
    }

    [Fact]
    public void IsRunning_ReturnsTrue_WhenNativeSaysYes()
    {
        var (sim, stub) = Create();
        stub.IsRunningValue = 1;
        Assert.True(sim.IsRunning);
    }

    [Fact]
    public void IsRunning_ReturnsFalse_WhenNativeSaysNo()
    {
        var (sim, stub) = Create();
        stub.IsRunningValue = 0;
        Assert.False(sim.IsRunning);
    }

    [Fact]
    public void Now_ReturnsCorrectTimeSpan()
    {
        var (sim, stub) = Create();
        stub.NowValue = 3.14159;
        Assert.Equal(3.14159, sim.Now.TotalSeconds, 5);
    }

    [Fact]
    public void Dispose_MultipleCalls_NoException()
    {
        var (sim, _) = Create();
        sim.Dispose();
        sim.Dispose();
        sim.Dispose();
    }

    // ========================================================================
    // Nodes
    // ========================================================================

    [Fact]
    public void CreateNodes_ReturnsCorrectCount()
    {
        var (sim, _) = Create();
        var nodes = sim.CreateNodes(5);
        Assert.Equal(5, nodes.Length);
        Assert.All(nodes, n => Assert.NotNull(n));
    }

    [Fact]
    public void CreateNodes_HasSimReference()
    {
        var (sim, _) = Create();
        var nodes = sim.CreateNodes(2);
        Assert.Same(sim, nodes[0].Simulation);
    }

    [Fact]
    public void CreateNodes_Zero_Throws()
    {
        var (sim, _) = Create();
        Assert.Throws<ArgumentOutOfRangeException>(() => sim.CreateNodes(0));
    }

    [Fact]
    public void CreateNodes_Negative_Throws()
    {
        var (sim, _) = Create();
        Assert.Throws<ArgumentOutOfRangeException>(() => sim.CreateNodes(-5));
    }

    [Fact]
    public void SetPosition_PassesCorrectCoordinates()
    {
        var (sim, stub) = Create();
        var node = sim.CreateNodes(1)[0];
        node.SetPosition(1.0, 2.0, 3.0);
        Assert.Equal((1.0, 2.0, 3.0), stub.LastPosition);
    }

    // ========================================================================
    // Internet & Routing
    // ========================================================================

    [Fact]
    public void InstallInternetStack_Empty_Throws()
    {
        var (sim, _) = Create();
        Assert.Throws<ArgumentException>(() => sim.InstallInternetStack());
    }

    [Fact]
    public void InstallInternetStack_Null_Throws()
    {
        var (sim, _) = Create();
        Assert.Throws<ArgumentException>(() => sim.InstallInternetStack(null!));
    }

    [Fact]
    public void AssignIpv4_Empty_Throws()
    {
        var (sim, _) = Create();
        Assert.Throws<ArgumentException>(() => sim.AssignIpv4Addresses(Array.Empty<Device>(), "10.0.0.0", "255.0.0.0"));
    }

    [Fact]
    public void AssignIpv4_Null_Throws()
    {
        var (sim, _) = Create();
        Assert.Throws<ArgumentException>(() => sim.AssignIpv4Addresses(null!, "10.0.0.0", "255.0.0.0"));
    }

    // ========================================================================
    // Schedule
    // ========================================================================

    [Fact]
    public void Schedule_NullCallback_Throws()
    {
        var (sim, _) = Create();
        Assert.Throws<ArgumentNullException>(() => sim.Schedule(TimeSpan.FromSeconds(1.0), null!));
    }

    [Fact]
    public void Schedule_NativeFails_Throws()
    {
        var (sim, stub) = Create();
        stub.SimScheduleResult = NativeMethods.Ns3Status.Error;
        Assert.Throws<Ns3Exception>(() => sim.Schedule(TimeSpan.FromSeconds(1.0), () => { }));
    }

    [Fact]
    public void Schedule_CallbackFires()
    {
        var (sim, stub) = Create();
        var fired = false;
        stub.OnSimSchedule = (s, sec, cb, user) => cb(user);

        sim.Schedule(TimeSpan.FromSeconds(1.0), () => fired = true);
        Assert.True(fired);
    }

    [Fact]
    public void Schedule_UnfiredCleanedUpOnDispose()
    {
        var (sim, _) = Create();
        sim.Schedule(TimeSpan.FromSeconds(10.0), () => { });
        sim.Dispose(); // Must not throw
    }

    // ========================================================================
    // Device
    // ========================================================================

    [Fact]
    public void Device_EnablePcap_PassesPrefix()
    {
        var (sim, stub) = Create();
        var nodes = sim.CreateNodes(2);
        var (dev0, _) = PointToPoint.Install(sim, nodes[0], nodes[1], "5Mbps", "2ms");
        dev0.EnablePcap("my-capture");
        Assert.Equal("my-capture", stub.LastPcapPrefix);
    }

    [Fact]
    public void Device_SubscribeToPacketEvents_RegistersCallbacks()
    {
        var (sim, stub) = Create();
        var nodes = sim.CreateNodes(2);
        var (dev0, _) = PointToPoint.Install(sim, nodes[0], nodes[1], "5Mbps", "2ms");

        dev0.SubscribeToPacketEvents(onTx: _ => { }, onRx: _ => { });

        Assert.NotNull(stub.LastTxCallback);
        Assert.NotNull(stub.LastRxCallback);
    }

    [Fact]
    public void Device_SubscribeToPacketEvents_OnlyTx_Works()
    {
        var (sim, stub) = Create();
        var nodes = sim.CreateNodes(2);
        var (dev0, _) = PointToPoint.Install(sim, nodes[0], nodes[1], "5Mbps", "2ms");

        dev0.SubscribeToPacketEvents(onTx: _ => { }, onRx: null);

        Assert.NotNull(stub.LastTxCallback);
        Assert.Null(stub.LastRxCallback);
    }

    [Fact]
    public void Device_SubscribeToPacketEvents_OnlyRx_Works()
    {
        var (sim, stub) = Create();
        var nodes = sim.CreateNodes(2);
        var (dev0, _) = PointToPoint.Install(sim, nodes[0], nodes[1], "5Mbps", "2ms");

        dev0.SubscribeToPacketEvents(onTx: null, onRx: _ => { });

        Assert.Null(stub.LastTxCallback);
        Assert.NotNull(stub.LastRxCallback);
    }

    [Fact]
    public void Device_SubscribeToPacketEvents_NativeFails_FreesHandles()
    {
        var (sim, stub) = Create();
        var nodes = sim.CreateNodes(2);
        var (dev0, _) = PointToPoint.Install(sim, nodes[0], nodes[1], "5Mbps", "2ms");
        stub.TraceSubscribePacketEventsResult = NativeMethods.Ns3Status.Error;

        Assert.Throws<Ns3Exception>(() =>
            dev0.SubscribeToPacketEvents(onTx: _ => { }, onRx: _ => { }));
    }

    // ========================================================================
    // PopulateRoutingTables
    // ========================================================================

    [Fact]
    public void PopulateRoutingTables_DoesNotThrow()
    {
        var (sim, _) = Create();
        sim.PopulateRoutingTables(); // Should succeed with stub
    }
}
