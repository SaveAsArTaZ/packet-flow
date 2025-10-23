// Applications.cs
// High-level API for ns-3 applications

using PacketFlow.Ns3Adapter.Interop;

namespace PacketFlow.Ns3Adapter;

/// <summary>
/// Represents an ns-3 application
/// </summary>
public sealed class Application
{
    private readonly Simulation _simulation;
    private readonly AppHandle _handle;

    internal Application(Simulation simulation, AppHandle handle)
    {
        _simulation = simulation ?? throw new ArgumentNullException(nameof(simulation));
        _handle = handle ?? throw new ArgumentNullException(nameof(handle));
    }

    /// <summary>
    /// Gets the native application handle
    /// </summary>
    internal nint NativeHandle => _handle.DangerousGetHandle();

    /// <summary>
    /// Gets the simulation this application belongs to
    /// </summary>
    public Simulation Simulation => _simulation;

    /// <summary>
    /// Schedules the application to start at the specified time
    /// </summary>
    /// <param name="startTime">Time to start the application</param>
    public void Start(TimeSpan startTime)
    {
        var status = NativeMethods.app_start(_simulation.Handle, NativeHandle, startTime.TotalSeconds);
        Ns3Exception.ThrowIfError(status, _simulation.Handle, nameof(Start));
    }

    /// <summary>
    /// Schedules the application to stop at the specified time
    /// </summary>
    /// <param name="stopTime">Time to stop the application</param>
    public void Stop(TimeSpan stopTime)
    {
        var status = NativeMethods.app_stop(_simulation.Handle, NativeHandle, stopTime.TotalSeconds);
        Ns3Exception.ThrowIfError(status, _simulation.Handle, nameof(Stop));
    }
}

/// <summary>
/// Helper class for creating UDP Echo applications
/// </summary>
public static class UdpEcho
{
    /// <summary>
    /// Creates a UDP Echo server application
    /// </summary>
    /// <param name="simulation">Simulation context</param>
    /// <param name="node">Node to host the server</param>
    /// <param name="port">UDP port number</param>
    /// <returns>UDP Echo server application</returns>
    public static Application CreateServer(Simulation simulation, Node node, ushort port)
    {
        if (simulation == null)
            throw new ArgumentNullException(nameof(simulation));
        if (node == null)
            throw new ArgumentNullException(nameof(node));

        var status = NativeMethods.app_udpecho_server(
            simulation.Handle,
            node.NativeHandle,
            port,
            out nint appHandle);

        Ns3Exception.ThrowIfError(status, simulation.Handle, nameof(CreateServer));

        return new Application(simulation, new AppHandle(appHandle));
    }

    /// <summary>
    /// Creates a UDP Echo client application
    /// </summary>
    /// <param name="simulation">Simulation context</param>
    /// <param name="node">Node to host the client</param>
    /// <param name="destinationIp">Destination IP address</param>
    /// <param name="port">Destination UDP port</param>
    /// <param name="packetSize">Size of each packet in bytes</param>
    /// <param name="interval">Interval between packets</param>
    /// <param name="maxPackets">Maximum number of packets to send</param>
    /// <returns>UDP Echo client application</returns>
    public static Application CreateClient(
        Simulation simulation,
        Node node,
        string destinationIp,
        ushort port,
        uint packetSize,
        TimeSpan interval,
        uint maxPackets)
    {
        if (simulation == null)
            throw new ArgumentNullException(nameof(simulation));
        if (node == null)
            throw new ArgumentNullException(nameof(node));
        if (string.IsNullOrEmpty(destinationIp))
            throw new ArgumentException("Destination IP cannot be empty", nameof(destinationIp));

        var status = NativeMethods.app_udpecho_client(
            simulation.Handle,
            node.NativeHandle,
            destinationIp,
            port,
            packetSize,
            interval.TotalSeconds,
            maxPackets,
            out nint appHandle);

        Ns3Exception.ThrowIfError(status, simulation.Handle, nameof(CreateClient));

        return new Application(simulation, new AppHandle(appHandle));
    }
}

/// <summary>
/// Flow monitor statistics
/// </summary>
public readonly record struct FlowStatistics(
    ulong TxPackets,
    ulong RxPackets,
    ulong TxBytes,
    ulong RxBytes,
    TimeSpan DelaySum,
    TimeSpan JitterSum,
    uint FlowCount)
{
    /// <summary>
    /// Average delay per packet
    /// </summary>
    public TimeSpan AverageDelay => RxPackets > 0 
        ? TimeSpan.FromSeconds(DelaySum.TotalSeconds / RxPackets) 
        : TimeSpan.Zero;

    /// <summary>
    /// Average jitter
    /// </summary>
    public TimeSpan AverageJitter => RxPackets > 0 
        ? TimeSpan.FromSeconds(JitterSum.TotalSeconds / RxPackets) 
        : TimeSpan.Zero;

    /// <summary>
    /// Packet loss ratio (0.0 to 1.0)
    /// </summary>
    public double PacketLossRatio => TxPackets > 0 
        ? 1.0 - ((double)RxPackets / TxPackets) 
        : 0.0;
}

/// <summary>
/// Flow monitor for collecting network statistics
/// </summary>
public sealed class FlowMonitor
{
    private readonly Simulation _simulation;
    private readonly FlowMonHandle _handle;

    internal FlowMonitor(Simulation simulation, FlowMonHandle handle)
    {
        _simulation = simulation ?? throw new ArgumentNullException(nameof(simulation));
        _handle = handle ?? throw new ArgumentNullException(nameof(handle));
    }

    /// <summary>
    /// Gets the native flow monitor handle
    /// </summary>
    internal nint NativeHandle => _handle.DangerousGetHandle();

    /// <summary>
    /// Installs flow monitor on all nodes in the simulation
    /// </summary>
    /// <param name="simulation">Simulation context</param>
    /// <returns>Flow monitor instance</returns>
    public static FlowMonitor InstallAll(Simulation simulation)
    {
        if (simulation == null)
            throw new ArgumentNullException(nameof(simulation));

        var status = NativeMethods.flowmon_install_all(simulation.Handle, out nint handle);
        Ns3Exception.ThrowIfError(status, simulation.Handle, nameof(InstallAll));

        return new FlowMonitor(simulation, new FlowMonHandle(handle));
    }

    /// <summary>
    /// Collects flow statistics
    /// </summary>
    /// <returns>Aggregated flow statistics</returns>
    public FlowStatistics CollectStatistics()
    {
        var status = NativeMethods.flowmon_collect(_simulation.Handle, NativeHandle, out var stats);
        Ns3Exception.ThrowIfError(status, _simulation.Handle, nameof(CollectStatistics));

        return new FlowStatistics(
            stats.TxPackets,
            stats.RxPackets,
            stats.TxBytes,
            stats.RxBytes,
            TimeSpan.FromSeconds(stats.DelaySumSec),
            TimeSpan.FromSeconds(stats.JitterSumSec),
            stats.FlowCount
        );
    }
}

