// SafeHandles.cs
// Safe handle wrappers for native ns-3 resources
//
// Each handle type:
// - Derives from SafeHandleZeroOrMinusOneIsInvalid
// - Implements deterministic cleanup via ReleaseHandle()
// - Is thread-safe and finalizer-safe
// - Prevents handle recycling attacks

using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace PacketFlow.Ns3Adapter.Interop;

/// <summary>
/// Safe handle for ns3_sim (simulation context)
/// </summary>
internal sealed class SimHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    /// <summary>
    /// Creates an invalid handle
    /// </summary>
    public SimHandle() : base(ownsHandle: true)
    {
    }

    /// <summary>
    /// Creates a handle with the specified value
    /// </summary>
    /// <param name="handle">Native handle value</param>
    public SimHandle(nint handle) : base(ownsHandle: true)
    {
        SetHandle(handle);
    }

    /// <summary>
    /// Releases the native simulation handle
    /// </summary>
    protected override bool ReleaseHandle()
    {
        if (IsInvalid)
            return true;

        try
        {
            // Call sim_destroy (idempotent and NULL-safe)
            _ = NativeMethods.sim_destroy(handle);
            return true;
        }
        catch
        {
            // Best-effort cleanup
            return false;
        }
    }
}

/// <summary>
/// Safe handle for ns3_node
/// </summary>
/// <remarks>
/// Node handles are managed by the simulation context and don't require explicit cleanup.
/// This wrapper provides type safety and prevents misuse.
/// </remarks>
internal sealed class NodeHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    public NodeHandle() : base(ownsHandle: false)
    {
    }

    public NodeHandle(nint handle) : base(ownsHandle: false)
    {
        SetHandle(handle);
    }

    protected override bool ReleaseHandle()
    {
        // Nodes are owned by simulation context
        return true;
    }
}

/// <summary>
/// Safe handle for ns3_device
/// </summary>
/// <remarks>
/// Device handles are managed by the simulation context and don't require explicit cleanup.
/// </remarks>
internal sealed class DeviceHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    public DeviceHandle() : base(ownsHandle: false)
    {
    }

    public DeviceHandle(nint handle) : base(ownsHandle: false)
    {
        SetHandle(handle);
    }

    protected override bool ReleaseHandle()
    {
        // Devices are owned by simulation context
        return true;
    }
}

/// <summary>
/// Safe handle for ns3_app
/// </summary>
/// <remarks>
/// Application handles are managed by the simulation context and don't require explicit cleanup.
/// </remarks>
internal sealed class AppHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    public AppHandle() : base(ownsHandle: false)
    {
    }

    public AppHandle(nint handle) : base(ownsHandle: false)
    {
        SetHandle(handle);
    }

    protected override bool ReleaseHandle()
    {
        // Applications are owned by simulation context
        return true;
    }
}

/// <summary>
/// Safe handle for ns3_flowmon
/// </summary>
/// <remarks>
/// FlowMonitor handles are managed by the simulation context and don't require explicit cleanup.
/// </remarks>
internal sealed class FlowMonHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    public FlowMonHandle() : base(ownsHandle: false)
    {
    }

    public FlowMonHandle(nint handle) : base(ownsHandle: false)
    {
        SetHandle(handle);
    }

    protected override bool ReleaseHandle()
    {
        // FlowMonitor is owned by simulation context
        return true;
    }
}

