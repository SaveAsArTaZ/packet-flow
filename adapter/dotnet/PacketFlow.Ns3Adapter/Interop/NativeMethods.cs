// NativeMethods.cs
// P/Invoke declarations for ns3shim C ABI
// 
// All imports use:
// - CallingConvention.Cdecl (C default)
// - ExactSpelling=true (no A/W suffix probing)
// - BestFitMapping=false, ThrowOnUnmappableChar=true (strict UTF-8)

using System.Runtime.InteropServices;

namespace PacketFlow.Ns3Adapter.Interop;

/// <summary>
/// Native library name (platform-specific resolution handled by NativeLibrary)
/// </summary>
internal static class LibraryName
{
    public const string Ns3Shim = "ns3shim";
}

/// <summary>
/// P/Invoke declarations for ns3shim native library
/// </summary>
internal static unsafe class NativeMethods
{
    // ========================================================================
    // Delegates for Callbacks
    // ========================================================================

    /// <summary>
    /// Generic void callback delegate
    /// </summary>
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void VoidCallback(nint user);

    /// <summary>
    /// Packet trace callback delegate
    /// </summary>
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void PacketCallback(nint user, ulong deviceId, double timeSec, uint bytes);

    // ========================================================================
    // Enums
    // ========================================================================

    internal enum Ns3Status : int
    {
        Ok = 0,
        Error = -1
    }

    internal enum Ns3AttrKind : int
    {
        Bool = 0,
        UInt = 1,
        Double = 2,
        String = 3
    }

    // ========================================================================
    // Structures
    // ========================================================================

    [StructLayout(LayoutKind.Explicit)]
    internal struct Ns3Attr
    {
        [FieldOffset(0)]
        public Ns3AttrKind Kind;

        [FieldOffset(8)]
        public ulong U;

        [FieldOffset(8)]
        public double D;

        [FieldOffset(8)]
        public nint S; // const char*

        [FieldOffset(8)]
        public int B;

        public static Ns3Attr FromBool(bool value) => new() { Kind = Ns3AttrKind.Bool, B = value ? 1 : 0 };
        public static Ns3Attr FromUInt(ulong value) => new() { Kind = Ns3AttrKind.UInt, U = value };
        public static Ns3Attr FromDouble(double value) => new() { Kind = Ns3AttrKind.Double, D = value };
        public static Ns3Attr FromString(nint value) => new() { Kind = Ns3AttrKind.String, S = value };
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct Ns3FlowStats
    {
        public ulong TxPackets;
        public ulong RxPackets;
        public ulong TxBytes;
        public ulong RxBytes;
        public double DelaySumSec;
        public double JitterSumSec;
        public uint FlowCount;
    }

    // ========================================================================
    // Error Handling
    // ========================================================================

    [DllImport(LibraryName.Ns3Shim, CallingConvention = CallingConvention.Cdecl, 
               ExactSpelling = true, BestFitMapping = false, ThrowOnUnmappableChar = true)]
    internal static extern Ns3Status ns3_last_error(nint sim, byte* buf, nuint len);

    // ========================================================================
    // Simulation Lifecycle
    // ========================================================================

    [DllImport(LibraryName.Ns3Shim, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    internal static extern Ns3Status sim_create(out nint outSim);

    [DllImport(LibraryName.Ns3Shim, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    internal static extern Ns3Status sim_set_seed(nint sim, uint seed);

    [DllImport(LibraryName.Ns3Shim, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    internal static extern Ns3Status sim_run(nint sim);

    [DllImport(LibraryName.Ns3Shim, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    internal static extern Ns3Status sim_stop(nint sim, double atTimeSec);

    [DllImport(LibraryName.Ns3Shim, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    internal static extern Ns3Status sim_is_running(nint sim, out int outIsRunning);

    [DllImport(LibraryName.Ns3Shim, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    internal static extern Ns3Status sim_now(nint sim, out double outTimeSec);

    [DllImport(LibraryName.Ns3Shim, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    internal static extern Ns3Status sim_schedule(nint sim, double inSeconds, VoidCallback cb, nint user);

    [DllImport(LibraryName.Ns3Shim, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    internal static extern Ns3Status sim_destroy(nint sim);

    // ========================================================================
    // Nodes & Topology
    // ========================================================================

    [DllImport(LibraryName.Ns3Shim, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    internal static extern Ns3Status nodes_create(nint sim, uint count, nint* outArray);

    [DllImport(LibraryName.Ns3Shim, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    internal static extern Ns3Status internet_install(nint sim, nint* nodes, uint count);

    // ========================================================================
    // Network Devices & Links
    // ========================================================================

    [DllImport(LibraryName.Ns3Shim, CallingConvention = CallingConvention.Cdecl, 
               ExactSpelling = true, BestFitMapping = false, ThrowOnUnmappableChar = true, CharSet = CharSet.Ansi)]
    internal static extern Ns3Status p2p_install(nint sim, nint a, nint b,
                                                 [MarshalAs(UnmanagedType.LPStr)] string dataRate,
                                                 [MarshalAs(UnmanagedType.LPStr)] string delay,
                                                 uint mtu,
                                                 out nint outDevA, out nint outDevB);

    [DllImport(LibraryName.Ns3Shim, CallingConvention = CallingConvention.Cdecl,
               ExactSpelling = true, BestFitMapping = false, ThrowOnUnmappableChar = true, CharSet = CharSet.Ansi)]
    internal static extern Ns3Status csma_install(nint sim, nint* nodes, uint count,
                                                  [MarshalAs(UnmanagedType.LPStr)] string dataRate,
                                                  [MarshalAs(UnmanagedType.LPStr)] string delay,
                                                  nint* outDevices);

    [DllImport(LibraryName.Ns3Shim, CallingConvention = CallingConvention.Cdecl,
               ExactSpelling = true, BestFitMapping = false, ThrowOnUnmappableChar = true, CharSet = CharSet.Ansi)]
    internal static extern Ns3Status wifi_install_sta_ap(nint sim, nint* stas, uint staCount, nint ap,
                                                         int phyStandard,
                                                         [MarshalAs(UnmanagedType.LPStr)] string dataRate,
                                                         int channelNumber,
                                                         nint* outStaDevices, out nint outApDevice);

    // ========================================================================
    // Mobility
    // ========================================================================

    [DllImport(LibraryName.Ns3Shim, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    internal static extern Ns3Status mobility_set_constant_position(nint sim, nint node, double x, double y, double z);

    // ========================================================================
    // IP Addressing & Routing
    // ========================================================================

    [DllImport(LibraryName.Ns3Shim, CallingConvention = CallingConvention.Cdecl,
               ExactSpelling = true, BestFitMapping = false, ThrowOnUnmappableChar = true, CharSet = CharSet.Ansi)]
    internal static extern Ns3Status ipv4_assign(nint sim, nint* devices, uint count,
                                                 [MarshalAs(UnmanagedType.LPStr)] string networkBase,
                                                 [MarshalAs(UnmanagedType.LPStr)] string mask);

    [DllImport(LibraryName.Ns3Shim, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    internal static extern Ns3Status ipv4_populate_routing_tables(nint sim);

    // ========================================================================
    // Applications
    // ========================================================================

    [DllImport(LibraryName.Ns3Shim, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    internal static extern Ns3Status app_udpecho_server(nint sim, nint node, ushort port, out nint outApp);

    [DllImport(LibraryName.Ns3Shim, CallingConvention = CallingConvention.Cdecl,
               ExactSpelling = true, BestFitMapping = false, ThrowOnUnmappableChar = true, CharSet = CharSet.Ansi)]
    internal static extern Ns3Status app_udpecho_client(nint sim, nint node,
                                                        [MarshalAs(UnmanagedType.LPStr)] string dstIp,
                                                        ushort port,
                                                        uint packetSize, double intervalSec, uint maxPackets,
                                                        out nint outApp);

    [DllImport(LibraryName.Ns3Shim, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    internal static extern Ns3Status app_start(nint sim, nint app, double atTimeSec);

    [DllImport(LibraryName.Ns3Shim, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    internal static extern Ns3Status app_stop(nint sim, nint app, double atTimeSec);

    // ========================================================================
    // Tracing & Statistics
    // ========================================================================

    [DllImport(LibraryName.Ns3Shim, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    internal static extern Ns3Status trace_subscribe_packet_events(nint sim, nint dev,
                                                                   PacketCallback? onTx,
                                                                   PacketCallback? onRx,
                                                                   nint user);

    [DllImport(LibraryName.Ns3Shim, CallingConvention = CallingConvention.Cdecl,
               ExactSpelling = true, BestFitMapping = false, ThrowOnUnmappableChar = true, CharSet = CharSet.Ansi)]
    internal static extern Ns3Status pcap_enable(nint sim, nint dev,
                                                 [MarshalAs(UnmanagedType.LPStr)] string filePrefix);

    [DllImport(LibraryName.Ns3Shim, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    internal static extern Ns3Status flowmon_install_all(nint sim, out nint outFlowMon);

    [DllImport(LibraryName.Ns3Shim, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    internal static extern Ns3Status flowmon_collect(nint sim, nint fm, out Ns3FlowStats outStats);

    // ========================================================================
    // Configuration
    // ========================================================================

    [DllImport(LibraryName.Ns3Shim, CallingConvention = CallingConvention.Cdecl,
               ExactSpelling = true, BestFitMapping = false, ThrowOnUnmappableChar = true, CharSet = CharSet.Ansi)]
    internal static extern Ns3Status config_set(nint sim,
                                                [MarshalAs(UnmanagedType.LPStr)] string path,
                                                [MarshalAs(UnmanagedType.LPStr)] string attrName,
                                                Ns3Attr value);
}

