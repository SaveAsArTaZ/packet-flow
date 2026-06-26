// NativeInterop.cs
// Real implementation of INativeInterop that delegates to NativeMethods (P/Invoke).
// This is the default implementation used at runtime; tests substitute a mock.

using System.Runtime.InteropServices;

namespace PacketFlow.Ns3Adapter.Interop;

/// <summary>
/// Production implementation — thin pass-through to NativeMethods P/Invoke calls.
/// </summary>
internal sealed class NativeInterop : INativeInterop
{
    public static readonly NativeInterop Instance = new();

    private NativeInterop() { }

    public unsafe NativeMethods.Ns3Status Ns3LastError(nint sim, Span<byte> buf)
    {
        fixed (byte* ptr = buf)
        {
            return NativeMethods.ns3_last_error(sim, ptr, (nuint)buf.Length);
        }
    }

    public NativeMethods.Ns3Status SimCreate(out nint outSim) =>
        NativeMethods.sim_create(out outSim);

    public NativeMethods.Ns3Status SimSetSeed(nint sim, uint seed) =>
        NativeMethods.sim_set_seed(sim, seed);

    public NativeMethods.Ns3Status SimRun(nint sim) =>
        NativeMethods.sim_run(sim);

    public NativeMethods.Ns3Status SimStop(nint sim, double atTimeSec) =>
        NativeMethods.sim_stop(sim, atTimeSec);

    public NativeMethods.Ns3Status SimIsRunning(nint sim, out int outIsRunning) =>
        NativeMethods.sim_is_running(sim, out outIsRunning);

    public NativeMethods.Ns3Status SimNow(nint sim, out double outTimeSec) =>
        NativeMethods.sim_now(sim, out outTimeSec);

    public NativeMethods.Ns3Status SimSchedule(nint sim, double inSeconds, NativeMethods.VoidCallback cb, nint user) =>
        NativeMethods.sim_schedule(sim, inSeconds, cb, user);

    public NativeMethods.Ns3Status SimDestroy(nint sim) =>
        NativeMethods.sim_destroy(sim);

    public unsafe NativeMethods.Ns3Status NodesCreate(nint sim, uint count, nint* outArray) =>
        NativeMethods.nodes_create(sim, count, outArray);

    public unsafe NativeMethods.Ns3Status InternetInstall(nint sim, nint* nodes, uint count) =>
        NativeMethods.internet_install(sim, nodes, count);

    public NativeMethods.Ns3Status P2PInstall(nint sim, nint a, nint b, string dataRate, string delay, uint mtu, out nint outDevA, out nint outDevB) =>
        NativeMethods.p2p_install(sim, a, b, dataRate, delay, mtu, out outDevA, out outDevB);

    public unsafe NativeMethods.Ns3Status CsmaInstall(nint sim, nint* nodes, uint count, string dataRate, string delay, nint* outDevices) =>
        NativeMethods.csma_install(sim, nodes, count, dataRate, delay, outDevices);

    public unsafe NativeMethods.Ns3Status WiFiInstallStaAp(nint sim, nint* stas, uint staCount, nint ap, int phyStandard, string dataRate, int channelNumber, nint* outStaDevices, out nint outApDevice) =>
        NativeMethods.wifi_install_sta_ap(sim, stas, staCount, ap, phyStandard, dataRate, channelNumber, outStaDevices, out outApDevice);

    public NativeMethods.Ns3Status MobilitySetConstantPosition(nint sim, nint node, double x, double y, double z) =>
        NativeMethods.mobility_set_constant_position(sim, node, x, y, z);

    public unsafe NativeMethods.Ns3Status Ipv4Assign(nint sim, nint* devices, uint count, string networkBase, string mask) =>
        NativeMethods.ipv4_assign(sim, devices, count, networkBase, mask);

    public NativeMethods.Ns3Status Ipv4PopulateRoutingTables(nint sim) =>
        NativeMethods.ipv4_populate_routing_tables(sim);

    public NativeMethods.Ns3Status AppUdpEchoServer(nint sim, nint node, ushort port, out nint outApp) =>
        NativeMethods.app_udpecho_server(sim, node, port, out outApp);

    public NativeMethods.Ns3Status AppUdpEchoClient(nint sim, nint node, string dstIp, ushort port, uint packetSize, double intervalSec, uint maxPackets, out nint outApp) =>
        NativeMethods.app_udpecho_client(sim, node, dstIp, port, packetSize, intervalSec, maxPackets, out outApp);

    public NativeMethods.Ns3Status AppStart(nint sim, nint app, double atTimeSec) =>
        NativeMethods.app_start(sim, app, atTimeSec);

    public NativeMethods.Ns3Status AppStop(nint sim, nint app, double atTimeSec) =>
        NativeMethods.app_stop(sim, app, atTimeSec);

    public NativeMethods.Ns3Status TraceSubscribePacketEvents(nint sim, nint dev, NativeMethods.PacketCallback? onTx, NativeMethods.PacketCallback? onRx, nint user) =>
        NativeMethods.trace_subscribe_packet_events(sim, dev, onTx, onRx, user);

    public NativeMethods.Ns3Status PcapEnable(nint sim, nint dev, string filePrefix) =>
        NativeMethods.pcap_enable(sim, dev, filePrefix);

    public NativeMethods.Ns3Status FlowMonInstallAll(nint sim, out nint outFlowMon) =>
        NativeMethods.flowmon_install_all(sim, out outFlowMon);

    public NativeMethods.Ns3Status FlowMonCollect(nint sim, nint fm, out NativeMethods.Ns3FlowStats outStats) =>
        NativeMethods.flowmon_collect(sim, fm, out outStats);

    public NativeMethods.Ns3Status ConfigSet(nint sim, string path, string attrName, NativeMethods.Ns3Attr value) =>
        NativeMethods.config_set(sim, path, attrName, value);
}
