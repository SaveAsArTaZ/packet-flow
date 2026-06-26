// StubNativeInterop.cs
// Manual test double for INativeInterop — avoids Moq expression-tree CLR issues.

using System.Runtime.InteropServices;
using PacketFlow.Ns3Adapter.Interop;

namespace PacketFlow.Ns3Adapter.Tests.Unit;

/// <summary>
/// Configurable stub for INativeInterop. Defaults to returning Ok for every call.
/// Override specific methods via the Action/Function properties for test scenarios.
/// </summary>
internal class StubNativeInterop : INativeInterop
{
    // ---- Configurable return values and callbacks ----

    public NativeMethods.Ns3Status SimCreateResult { get; set; } = NativeMethods.Ns3Status.Ok;
    public nint SimCreateHandle { get; set; } = (nint)0x100;
    public Action? OnSimCreate { get; set; }

    public NativeMethods.Ns3Status SimSetSeedResult { get; set; } = NativeMethods.Ns3Status.Ok;
    public uint? CapturedSeed { get; private set; }

    public NativeMethods.Ns3Status SimRunResult { get; set; } = NativeMethods.Ns3Status.Ok;

    public NativeMethods.Ns3Status SimStopResult { get; set; } = NativeMethods.Ns3Status.Ok;
    public double? CapturedStopTime { get; private set; }

    public NativeMethods.Ns3Status SimDestroyResult { get; set; } = NativeMethods.Ns3Status.Ok;

    public NativeMethods.Ns3Status SimIsRunningResult { get; set; } = NativeMethods.Ns3Status.Ok;
    public int IsRunningValue { get; set; }

    public NativeMethods.Ns3Status SimNowResult { get; set; } = NativeMethods.Ns3Status.Ok;
    public double NowValue { get; set; }

    public NativeMethods.Ns3Status SimScheduleResult { get; set; } = NativeMethods.Ns3Status.Ok;
    public Action<nint, double, NativeMethods.VoidCallback, nint>? OnSimSchedule { get; set; }
    public (double secs, nint user)? LastScheduleArgs { get; private set; }

    public NativeMethods.Ns3Status MobilityResult { get; set; } = NativeMethods.Ns3Status.Ok;
    public (double x, double y, double z)? LastPosition { get; private set; }

    // Return values for multi-output methods
    public NativeMethods.Ns3FlowStats FlowStatsResult { get; set; }
    public NativeMethods.Ns3Status FlowMonCollectResult { get; set; } = NativeMethods.Ns3Status.Ok;

    public nint AppHandle { get; set; } = (nint)0x400;
    public NativeMethods.Ns3Status AppResult { get; set; } = NativeMethods.Ns3Status.Ok;

    public (nint a, nint b) P2PDevices { get; set; } = ((nint)0x300, (nint)0x308);
    public NativeMethods.Ns3Status P2PResult { get; set; } = NativeMethods.Ns3Status.Ok;
    public (string dr, string delay, uint mtu)? LastP2PArgs { get; private set; }

    // Captured callback delegates for test verification
    public NativeMethods.VoidCallback? LastVoidCallback { get; private set; }
    public NativeMethods.PacketCallback? LastTxCallback { get; private set; }
    public NativeMethods.PacketCallback? LastRxCallback { get; private set; }
    public nint LastUserPtr { get; private set; }
    public string? LastPcapPrefix { get; private set; }

    // ========================================================================
    // INativeInterop Implementation
    // ========================================================================

    public unsafe NativeMethods.Ns3Status Ns3LastError(nint sim, Span<byte> buf)
    {
        System.Text.Encoding.UTF8.GetBytes("no error", buf);
        return NativeMethods.Ns3Status.Ok;
    }

    public NativeMethods.Ns3Status SimCreate(out nint outSim)
    {
        OnSimCreate?.Invoke();
        outSim = SimCreateHandle;
        return SimCreateResult;
    }

    public NativeMethods.Ns3Status SimSetSeed(nint sim, uint seed)
    {
        CapturedSeed = seed;
        return SimSetSeedResult;
    }

    public NativeMethods.Ns3Status SimRun(nint sim) => SimRunResult;

    public NativeMethods.Ns3Status SimStop(nint sim, double atTimeSec)
    {
        CapturedStopTime = atTimeSec;
        return SimStopResult;
    }

    public NativeMethods.Ns3Status SimDestroy(nint sim) => SimDestroyResult;

    public NativeMethods.Ns3Status SimIsRunning(nint sim, out int outIsRunning)
    {
        outIsRunning = IsRunningValue;
        return SimIsRunningResult;
    }

    public NativeMethods.Ns3Status SimNow(nint sim, out double outTimeSec)
    {
        outTimeSec = NowValue;
        return SimNowResult;
    }

    public NativeMethods.Ns3Status SimSchedule(nint sim, double inSeconds, NativeMethods.VoidCallback cb, nint user)
    {
        LastVoidCallback = cb;
        LastUserPtr = user;
        LastScheduleArgs = (inSeconds, user);
        OnSimSchedule?.Invoke(sim, inSeconds, cb, user);
        return SimScheduleResult;
    }

    public unsafe NativeMethods.Ns3Status NodesCreate(nint sim, uint count, nint* outArray)
    {
        for (int i = 0; i < count; i++)
            outArray[i] = (nint)(0x200 + i * 8);
        return NativeMethods.Ns3Status.Ok;
    }

    public unsafe NativeMethods.Ns3Status InternetInstall(nint sim, nint* nodes, uint count) =>
        NativeMethods.Ns3Status.Ok;

    public NativeMethods.Ns3Status P2PInstall(nint sim, nint a, nint b, string dataRate, string delay, uint mtu,
        out nint outDevA, out nint outDevB)
    {
        LastP2PArgs = (dataRate, delay, mtu);
        outDevA = P2PDevices.a;
        outDevB = P2PDevices.b;
        return P2PResult;
    }

    public unsafe NativeMethods.Ns3Status CsmaInstall(nint sim, nint* nodes, uint count,
        string dataRate, string delay, nint* outDevices)
    {
        for (int i = 0; i < count; i++)
            outDevices[i] = (nint)(0x300 + i * 8);
        return NativeMethods.Ns3Status.Ok;
    }

    public unsafe NativeMethods.Ns3Status WiFiInstallStaAp(nint sim, nint* stas, uint staCount, nint ap,
        int phyStandard, string dataRate, int channelNumber, nint* outStaDevices, out nint outApDevice)
    {
        for (int i = 0; i < staCount; i++)
            outStaDevices[i] = (nint)(0x300 + i * 8);
        outApDevice = (nint)(0x300 + staCount * 8);
        return NativeMethods.Ns3Status.Ok;
    }

    public NativeMethods.Ns3Status MobilitySetConstantPosition(nint sim, nint node, double x, double y, double z)
    {
        LastPosition = (x, y, z);
        return MobilityResult;
    }

    public unsafe NativeMethods.Ns3Status Ipv4Assign(nint sim, nint* devices, uint count,
        string networkBase, string mask) => NativeMethods.Ns3Status.Ok;

    public NativeMethods.Ns3Status Ipv4PopulateRoutingTables(nint sim) => NativeMethods.Ns3Status.Ok;

    public NativeMethods.Ns3Status AppUdpEchoServer(nint sim, nint node, ushort port, out nint outApp)
    {
        outApp = AppHandle;
        return AppResult;
    }

    public NativeMethods.Ns3Status AppUdpEchoClient(nint sim, nint node, string dstIp, ushort port,
        uint packetSize, double intervalSec, uint maxPackets, out nint outApp)
    {
        outApp = AppHandle;
        return AppResult;
    }

    public NativeMethods.Ns3Status AppStart(nint sim, nint app, double atTimeSec) =>
        NativeMethods.Ns3Status.Ok;

    public NativeMethods.Ns3Status AppStop(nint sim, nint app, double atTimeSec) =>
        NativeMethods.Ns3Status.Ok;

    public NativeMethods.Ns3Status TraceSubscribePacketEventsResult { get; set; } = NativeMethods.Ns3Status.Ok;

    public NativeMethods.Ns3Status TraceSubscribePacketEvents(nint sim, nint dev,
        NativeMethods.PacketCallback? onTx, NativeMethods.PacketCallback? onRx, nint user)
    {
        LastTxCallback = onTx;
        LastRxCallback = onRx;
        LastUserPtr = user;
        return TraceSubscribePacketEventsResult;
    }

    public NativeMethods.Ns3Status PcapEnable(nint sim, nint dev, string filePrefix)
    {
        LastPcapPrefix = filePrefix;
        return NativeMethods.Ns3Status.Ok;
    }

    public NativeMethods.Ns3Status FlowMonInstallAll(nint sim, out nint outFlowMon)
    {
        outFlowMon = (nint)0x500;
        return NativeMethods.Ns3Status.Ok;
    }

    public NativeMethods.Ns3Status FlowMonCollect(nint sim, nint fm, out NativeMethods.Ns3FlowStats outStats)
    {
        outStats = FlowStatsResult;
        return FlowMonCollectResult;
    }

    public NativeMethods.Ns3Status ConfigSet(nint sim, string path, string attrName, NativeMethods.Ns3Attr value) =>
        NativeMethods.Ns3Status.Ok;
}
