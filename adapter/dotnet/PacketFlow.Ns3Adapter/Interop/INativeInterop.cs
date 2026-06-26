// INativeInterop.cs
// Abstract interface for all ns-3 native operations.
// Enables unit testing via mocking without requiring ns3shim.dll.
// Internal — test projects access via InternalsVisibleTo.

using System.Runtime.InteropServices;

namespace PacketFlow.Ns3Adapter.Interop;

/// <summary>
/// Abstracts all P/Invoke calls to the ns3shim native library.
/// The real implementation delegates to NativeMethods; tests use a mock.
/// </summary>
internal interface INativeInterop
{
    // Error Handling
    NativeMethods.Ns3Status Ns3LastError(nint sim, Span<byte> buf);

    // Simulation Lifecycle
    NativeMethods.Ns3Status SimCreate(out nint outSim);
    NativeMethods.Ns3Status SimSetSeed(nint sim, uint seed);
    NativeMethods.Ns3Status SimRun(nint sim);
    NativeMethods.Ns3Status SimStop(nint sim, double atTimeSec);
    NativeMethods.Ns3Status SimIsRunning(nint sim, out int outIsRunning);
    NativeMethods.Ns3Status SimNow(nint sim, out double outTimeSec);
    NativeMethods.Ns3Status SimSchedule(nint sim, double inSeconds, NativeMethods.VoidCallback cb, nint user);
    NativeMethods.Ns3Status SimDestroy(nint sim);

    // Nodes & Topology
    unsafe NativeMethods.Ns3Status NodesCreate(nint sim, uint count, nint* outArray);
    unsafe NativeMethods.Ns3Status InternetInstall(nint sim, nint* nodes, uint count);

    // Network Devices
    NativeMethods.Ns3Status P2PInstall(nint sim, nint a, nint b, string dataRate, string delay, uint mtu, out nint outDevA, out nint outDevB);
    unsafe NativeMethods.Ns3Status CsmaInstall(nint sim, nint* nodes, uint count, string dataRate, string delay, nint* outDevices);
    unsafe NativeMethods.Ns3Status WiFiInstallStaAp(nint sim, nint* stas, uint staCount, nint ap, int phyStandard, string dataRate, int channelNumber, nint* outStaDevices, out nint outApDevice);

    // Mobility
    NativeMethods.Ns3Status MobilitySetConstantPosition(nint sim, nint node, double x, double y, double z);

    // IP Addressing & Routing
    unsafe NativeMethods.Ns3Status Ipv4Assign(nint sim, nint* devices, uint count, string networkBase, string mask);
    NativeMethods.Ns3Status Ipv4PopulateRoutingTables(nint sim);

    // Applications
    NativeMethods.Ns3Status AppUdpEchoServer(nint sim, nint node, ushort port, out nint outApp);
    NativeMethods.Ns3Status AppUdpEchoClient(nint sim, nint node, string dstIp, ushort port, uint packetSize, double intervalSec, uint maxPackets, out nint outApp);
    NativeMethods.Ns3Status AppStart(nint sim, nint app, double atTimeSec);
    NativeMethods.Ns3Status AppStop(nint sim, nint app, double atTimeSec);

    // Tracing & Statistics
    NativeMethods.Ns3Status TraceSubscribePacketEvents(nint sim, nint dev, NativeMethods.PacketCallback? onTx, NativeMethods.PacketCallback? onRx, nint user);
    NativeMethods.Ns3Status PcapEnable(nint sim, nint dev, string filePrefix);
    NativeMethods.Ns3Status FlowMonInstallAll(nint sim, out nint outFlowMon);
    NativeMethods.Ns3Status FlowMonCollect(nint sim, nint fm, out NativeMethods.Ns3FlowStats outStats);

    // Configuration
    NativeMethods.Ns3Status ConfigSet(nint sim, string path, string attrName, NativeMethods.Ns3Attr value);
}
