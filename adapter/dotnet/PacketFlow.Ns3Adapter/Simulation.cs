// Simulation.cs
// High-level API for ns-3 simulation lifecycle
//
// Provides ergonomic, type-safe access to ns-3 functionality:
// - RAII-style resource management
// - Event callback marshalling
// - Thread-safe error handling

using System.Runtime.InteropServices;
using System.Threading.Channels;
using PacketFlow.Ns3Adapter.Interop;

namespace PacketFlow.Ns3Adapter;

/// <summary>
/// Represents an ns-3 simulation context
/// </summary>
public sealed class Simulation : IDisposable
{
    private readonly SimHandle _handle;
    private bool _disposed;

    static Simulation()
    {
        // Initialize native library resolver
        NativeLibraryResolver.Initialize();
    }

    /// <summary>
    /// Creates a new simulation context
    /// </summary>
    public Simulation()
    {
        var status = NativeMethods.sim_create(out nint handle);
        Ns3Exception.ThrowIfError(status, nint.Zero, nameof(NativeMethods.sim_create));

        _handle = new SimHandle(handle);
    }

    /// <summary>
    /// Gets the native simulation handle
    /// </summary>
    internal nint Handle => _handle.DangerousGetHandle();

    /// <summary>
    /// Sets the random number generator seed
    /// </summary>
    /// <param name="seed">RNG seed value</param>
    public void SetSeed(uint seed)
    {
        ThrowIfDisposed();
        var status = NativeMethods.sim_set_seed(Handle, seed);
        Ns3Exception.ThrowIfError(status, Handle, nameof(SetSeed));
    }

    /// <summary>
    /// Runs the simulation (blocks until stopped or no events remain)
    /// </summary>
    public void Run()
    {
        ThrowIfDisposed();
        var status = NativeMethods.sim_run(Handle);
        Ns3Exception.ThrowIfError(status, Handle, nameof(Run));
    }

    /// <summary>
    /// Schedules a simulation stop at the specified time
    /// </summary>
    /// <param name="atTime">Time to stop the simulation</param>
    public void Stop(TimeSpan atTime)
    {
        ThrowIfDisposed();
        var status = NativeMethods.sim_stop(Handle, atTime.TotalSeconds);
        Ns3Exception.ThrowIfError(status, Handle, nameof(Stop));
    }

    /// <summary>
    /// Checks if the simulation is currently running
    /// </summary>
    public bool IsRunning
    {
        get
        {
            ThrowIfDisposed();
            var status = NativeMethods.sim_is_running(Handle, out int isRunning);
            Ns3Exception.ThrowIfError(status, Handle, nameof(IsRunning));
            return isRunning != 0;
        }
    }

    /// <summary>
    /// Gets the current simulation time
    /// </summary>
    public TimeSpan Now
    {
        get
        {
            ThrowIfDisposed();
            var status = NativeMethods.sim_now(Handle, out double timeSec);
            Ns3Exception.ThrowIfError(status, Handle, nameof(Now));
            return TimeSpan.FromSeconds(timeSec);
        }
    }

    /// <summary>
    /// Schedules a callback to be invoked at a future time
    /// </summary>
    /// <param name="delay">Delay from current time</param>
    /// <param name="callback">Action to invoke</param>
    public void Schedule(TimeSpan delay, Action callback)
    {
        ThrowIfDisposed();
        if (callback == null)
            throw new ArgumentNullException(nameof(callback));

        // Create a GC handle to keep the callback alive
        var gcHandle = GCHandle.Alloc(callback);
        
        NativeMethods.VoidCallback nativeCallback = (user) =>
        {
            try
            {
                var handle = GCHandle.FromIntPtr(user);
                var action = (Action)handle.Target!;
                action();
            }
            finally
            {
                // Free the GC handle after invocation
                GCHandle.FromIntPtr(user).Free();
            }
        };

        var status = NativeMethods.sim_schedule(Handle, delay.TotalSeconds, nativeCallback, GCHandle.ToIntPtr(gcHandle));
        
        if (status != NativeMethods.Ns3Status.Ok)
        {
            gcHandle.Free();
            Ns3Exception.ThrowIfError(status, Handle, nameof(Schedule));
        }
    }

    /// <summary>
    /// Creates network nodes
    /// </summary>
    /// <param name="count">Number of nodes to create</param>
    /// <returns>Array of node handles</returns>
    public unsafe Node[] CreateNodes(int count)
    {
        ThrowIfDisposed();
        if (count <= 0)
            throw new ArgumentOutOfRangeException(nameof(count), "Count must be positive");

        var handles = new nint[count];
        fixed (nint* ptr = handles)
        {
            var status = NativeMethods.nodes_create(Handle, (uint)count, ptr);
            Ns3Exception.ThrowIfError(status, Handle, nameof(CreateNodes));
        }

        var nodes = new Node[count];
        for (int i = 0; i < count; i++)
        {
            nodes[i] = new Node(this, new NodeHandle(handles[i]));
        }

        return nodes;
    }

    /// <summary>
    /// Installs the Internet stack (IPv4, TCP, UDP) on the specified nodes
    /// </summary>
    /// <param name="nodes">Nodes to install Internet stack on</param>
    public unsafe void InstallInternetStack(params Node[] nodes)
    {
        ThrowIfDisposed();
        if (nodes == null || nodes.Length == 0)
            throw new ArgumentException("At least one node required", nameof(nodes));

        var handles = new nint[nodes.Length];
        for (int i = 0; i < nodes.Length; i++)
        {
            handles[i] = nodes[i].NativeHandle;
        }

        fixed (nint* ptr = handles)
        {
            var status = NativeMethods.internet_install(Handle, ptr, (uint)nodes.Length);
            Ns3Exception.ThrowIfError(status, Handle, nameof(InstallInternetStack));
        }
    }

    /// <summary>
    /// Populates global IPv4 routing tables
    /// </summary>
    public void PopulateRoutingTables()
    {
        ThrowIfDisposed();
        var status = NativeMethods.ipv4_populate_routing_tables(Handle);
        Ns3Exception.ThrowIfError(status, Handle, nameof(PopulateRoutingTables));
    }

    /// <summary>
    /// Assigns IPv4 addresses to devices
    /// </summary>
    /// <param name="devices">Devices to assign addresses to</param>
    /// <param name="networkBase">Network base address (e.g., "10.1.1.0")</param>
    /// <param name="mask">Network mask (e.g., "255.255.255.0")</param>
    public unsafe void AssignIpv4Addresses(Device[] devices, string networkBase, string mask)
    {
        ThrowIfDisposed();
        if (devices == null || devices.Length == 0)
            throw new ArgumentException("At least one device required", nameof(devices));

        var handles = new nint[devices.Length];
        for (int i = 0; i < devices.Length; i++)
        {
            handles[i] = devices[i].NativeHandle;
        }

        fixed (nint* ptr = handles)
        {
            var status = NativeMethods.ipv4_assign(Handle, ptr, (uint)devices.Length, networkBase, mask);
            Ns3Exception.ThrowIfError(status, Handle, nameof(AssignIpv4Addresses));
        }
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(Simulation));
    }

    /// <summary>
    /// Disposes the simulation and frees all resources
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        _handle?.Dispose();
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// Represents a network node
/// </summary>
public sealed class Node
{
    private readonly Simulation _simulation;
    private readonly NodeHandle _handle;

    internal Node(Simulation simulation, NodeHandle handle)
    {
        _simulation = simulation ?? throw new ArgumentNullException(nameof(simulation));
        _handle = handle ?? throw new ArgumentNullException(nameof(handle));
    }

    /// <summary>
    /// Gets the native node handle
    /// </summary>
    internal nint NativeHandle => _handle.DangerousGetHandle();

    /// <summary>
    /// Gets the simulation this node belongs to
    /// </summary>
    public Simulation Simulation => _simulation;

    /// <summary>
    /// Sets a constant (static) position for this node
    /// </summary>
    /// <param name="x">X coordinate (meters)</param>
    /// <param name="y">Y coordinate (meters)</param>
    /// <param name="z">Z coordinate (meters)</param>
    public void SetPosition(double x, double y, double z)
    {
        var status = NativeMethods.mobility_set_constant_position(_simulation.Handle, NativeHandle, x, y, z);
        Ns3Exception.ThrowIfError(status, _simulation.Handle, nameof(SetPosition));
    }
}

/// <summary>
/// Represents a network device
/// </summary>
public sealed class Device
{
    private readonly Simulation _simulation;
    private readonly DeviceHandle _handle;

    internal Device(Simulation simulation, DeviceHandle handle)
    {
        _simulation = simulation ?? throw new ArgumentNullException(nameof(simulation));
        _handle = handle ?? throw new ArgumentNullException(nameof(handle));
    }

    /// <summary>
    /// Gets the native device handle
    /// </summary>
    internal nint NativeHandle => _handle.DangerousGetHandle();

    /// <summary>
    /// Gets the simulation this device belongs to
    /// </summary>
    public Simulation Simulation => _simulation;

    /// <summary>
    /// Enables PCAP tracing on this device
    /// </summary>
    /// <param name="filePrefix">Prefix for PCAP file name</param>
    public void EnablePcap(string filePrefix)
    {
        var status = NativeMethods.pcap_enable(_simulation.Handle, NativeHandle, filePrefix);
        Ns3Exception.ThrowIfError(status, _simulation.Handle, nameof(EnablePcap));
    }

    /// <summary>
    /// Subscribes to packet TX/RX events
    /// </summary>
    /// <param name="onTx">Callback for transmitted packets (may be null)</param>
    /// <param name="onRx">Callback for received packets (may be null)</param>
    public void SubscribeToPacketEvents(Action<PacketEvent>? onTx, Action<PacketEvent>? onRx)
    {
        NativeMethods.PacketCallback? nativeTx = null;
        NativeMethods.PacketCallback? nativeRx = null;

        GCHandle? txHandle = null;
        GCHandle? rxHandle = null;

        if (onTx != null)
        {
            txHandle = GCHandle.Alloc(onTx);
            nativeTx = (user, deviceId, timeSec, bytes) =>
            {
                var handle = GCHandle.FromIntPtr(user);
                var callback = (Action<PacketEvent>)handle.Target!;
                callback(new PacketEvent(deviceId, TimeSpan.FromSeconds(timeSec), bytes));
            };
        }

        if (onRx != null)
        {
            rxHandle = GCHandle.Alloc(onRx);
            nativeRx = (user, deviceId, timeSec, bytes) =>
            {
                var handle = GCHandle.FromIntPtr(user);
                var callback = (Action<PacketEvent>)handle.Target!;
                callback(new PacketEvent(deviceId, TimeSpan.FromSeconds(timeSec), bytes));
            };
        }

        var userPtr = txHandle.HasValue ? GCHandle.ToIntPtr(txHandle.Value) : 
                      rxHandle.HasValue ? GCHandle.ToIntPtr(rxHandle.Value) : nint.Zero;
        var status = NativeMethods.trace_subscribe_packet_events(_simulation.Handle, NativeHandle, nativeTx, nativeRx, userPtr);
        
        if (status != NativeMethods.Ns3Status.Ok)
        {
            txHandle?.Free();
            rxHandle?.Free();
            Ns3Exception.ThrowIfError(status, _simulation.Handle, nameof(SubscribeToPacketEvents));
        }
    }
}

/// <summary>
/// Represents a packet event (TX or RX)
/// </summary>
public readonly record struct PacketEvent(ulong DeviceId, TimeSpan Time, uint Bytes);

