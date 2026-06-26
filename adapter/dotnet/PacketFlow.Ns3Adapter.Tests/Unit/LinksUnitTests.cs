// LinksUnitTests.cs — unit tests using StubNativeInterop (no Moq, no native DLL).

using Xunit;
using PacketFlow.Ns3Adapter;
using PacketFlow.Ns3Adapter.Interop;

namespace PacketFlow.Ns3Adapter.Tests.Unit;

public class LinksUnitTests
{
    private static (Simulation Sim, StubNativeInterop Stub) Create()
    {
        var stub = new StubNativeInterop();
        return (new Simulation(stub, ownsNative: false), stub);
    }

    // ========================================================================
    // PointToPoint
    // ========================================================================

    [Fact]
    public void P2P_Install_PassesCorrectParams()
    {
        var (sim, stub) = Create();
        var nodes = sim.CreateNodes(2);

        var (dev0, dev1) = PointToPoint.Install(sim, nodes[0], nodes[1], "5Mbps", "2ms", 9000);

        Assert.NotNull(dev0);
        Assert.NotNull(dev1);
        Assert.Same(sim, dev0.Simulation);
        Assert.Equal(("5Mbps", "2ms", 9000u), stub.LastP2PArgs);
    }

    [Fact]
    public void P2P_Install_DefaultMtu_Is1500()
    {
        var (sim, stub) = Create();
        var nodes = sim.CreateNodes(2);
        PointToPoint.Install(sim, nodes[0], nodes[1], "5Mbps", "2ms");
        Assert.Equal(1500u, stub.LastP2PArgs!.Value.mtu);
    }

    [Fact]
    public void P2P_Install_NullSim_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            PointToPoint.Install(null!, null!, null!, "5Mbps", "2ms"));
    }

    [Fact]
    public void P2P_Install_NullNodeA_Throws()
    {
        var (sim, _) = Create();
        var nodes = sim.CreateNodes(2);
        Assert.Throws<ArgumentNullException>(() =>
            PointToPoint.Install(sim, null!, nodes[1], "5Mbps", "2ms"));
    }

    [Fact]
    public void P2P_Install_NullNodeB_Throws()
    {
        var (sim, _) = Create();
        var nodes = sim.CreateNodes(2);
        Assert.Throws<ArgumentNullException>(() =>
            PointToPoint.Install(sim, nodes[0], null!, "5Mbps", "2ms"));
    }

    [Fact]
    public void P2P_Install_EmptyDataRate_Throws()
    {
        var (sim, _) = Create();
        var nodes = sim.CreateNodes(2);
        Assert.Throws<ArgumentException>(() =>
            PointToPoint.Install(sim, nodes[0], nodes[1], "", "2ms"));
    }

    [Fact]
    public void P2P_Install_EmptyDelay_Throws()
    {
        var (sim, _) = Create();
        var nodes = sim.CreateNodes(2);
        Assert.Throws<ArgumentException>(() =>
            PointToPoint.Install(sim, nodes[0], nodes[1], "5Mbps", ""));
    }

    [Fact]
    public void P2P_Install_NativeFails_Throws()
    {
        var (sim, stub) = Create();
        var nodes = sim.CreateNodes(2);
        stub.P2PResult = NativeMethods.Ns3Status.Error;
        Assert.Throws<Ns3Exception>(() =>
            PointToPoint.Install(sim, nodes[0], nodes[1], "5Mbps", "2ms"));
    }

    // ========================================================================
    // Csma
    // ========================================================================

    [Fact]
    public void Csma_Install_ReturnsCorrectCount()
    {
        var (sim, _) = Create();
        var nodes = sim.CreateNodes(4);
        var devices = Csma.Install(sim, nodes, "100Mbps", "6560ns");
        Assert.Equal(4, devices.Length);
        Assert.All(devices, d => Assert.NotNull(d));
    }

    [Fact]
    public void Csma_Install_NullSim_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            Csma.Install(null!, Array.Empty<Node>(), "100Mbps", "6560ns"));
    }

    [Fact]
    public void Csma_Install_EmptyNodes_Throws()
    {
        var (sim, _) = Create();
        Assert.Throws<ArgumentException>(() =>
            Csma.Install(sim, Array.Empty<Node>(), "100Mbps", "6560ns"));
    }

    // ========================================================================
    // WiFi
    // ========================================================================

    [Fact]
    public void WiFi_InstallStationAp_ReturnsCorrectStructure()
    {
        var (sim, _) = Create();
        var nodes = sim.CreateNodes(4);
        var stas = new[] { nodes[0], nodes[1], nodes[2] };

        var (staDevs, apDev) = WiFi.InstallStationAp(sim, stas, nodes[3],
            WiFiStandard.Std_80211n_2_4GHz, "HtMcs7", 1);

        Assert.Equal(3, staDevs.Length);
        Assert.NotNull(apDev);
    }

    [Fact]
    public void WiFi_InstallStationAp_NullSim_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            WiFi.InstallStationAp(null!, Array.Empty<Node>(), null!, WiFiStandard.Std_80211n_2_4GHz, "HtMcs7", 1));
    }

    [Fact]
    public void WiFi_InstallStationAp_EmptyStations_Throws()
    {
        var (sim, _) = Create();
        var nodes = sim.CreateNodes(2);
        Assert.Throws<ArgumentException>(() =>
            WiFi.InstallStationAp(sim, Array.Empty<Node>(), nodes[0], WiFiStandard.Std_80211n_2_4GHz, "HtMcs7", 1));
    }

    [Fact]
    public void WiFi_InstallStationAp_NullAp_Throws()
    {
        var (sim, _) = Create();
        var nodes = sim.CreateNodes(3);
        Assert.Throws<ArgumentNullException>(() =>
            WiFi.InstallStationAp(sim, new[] { nodes[0] }, null!, WiFiStandard.Std_80211n_2_4GHz, "HtMcs7", 1));
    }

    [Fact]
    public void WiFi_InstallStationAp_EmptyDataRate_Throws()
    {
        var (sim, _) = Create();
        var nodes = sim.CreateNodes(3);
        Assert.Throws<ArgumentException>(() =>
            WiFi.InstallStationAp(sim, new[] { nodes[0] }, nodes[1], WiFiStandard.Std_80211n_2_4GHz, "", 1));
    }

    // ========================================================================
    // WiFiStandard Enum
    // ========================================================================

    [Theory]
    [InlineData(WiFiStandard.Std_80211a, 0)]
    [InlineData(WiFiStandard.Std_80211b, 1)]
    [InlineData(WiFiStandard.Std_80211g, 2)]
    [InlineData(WiFiStandard.Std_80211n_2_4GHz, 3)]
    [InlineData(WiFiStandard.Std_80211n_5GHz, 4)]
    [InlineData(WiFiStandard.Std_80211ac, 5)]
    public void WiFiStandard_EnumMatchesNative(WiFiStandard s, int v)
    {
        Assert.Equal(v, (int)s);
    }
}
