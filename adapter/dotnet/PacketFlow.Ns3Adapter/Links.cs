// Links.cs
// High-level API for network links and topologies

using PacketFlow.Ns3Adapter.Interop;

namespace PacketFlow.Ns3Adapter;

/// <summary>
/// Helper class for creating point-to-point links
/// </summary>
public static class PointToPoint
{
    /// <summary>
    /// Creates a point-to-point link between two nodes
    /// </summary>
    /// <param name="simulation">Simulation context</param>
    /// <param name="nodeA">First node</param>
    /// <param name="nodeB">Second node</param>
    /// <param name="dataRate">Data rate (e.g., "5Mbps", "1Gbps")</param>
    /// <param name="delay">Propagation delay (e.g., "2ms", "10us")</param>
    /// <param name="mtu">Maximum transmission unit in bytes (default: 1500)</param>
    /// <returns>Tuple of devices on both nodes</returns>
    public static (Device DeviceA, Device DeviceB) Install(
        Simulation simulation,
        Node nodeA,
        Node nodeB,
        string dataRate,
        string delay,
        uint mtu = 1500)
    {
        if (simulation == null)
            throw new ArgumentNullException(nameof(simulation));
        if (nodeA == null)
            throw new ArgumentNullException(nameof(nodeA));
        if (nodeB == null)
            throw new ArgumentNullException(nameof(nodeB));
        if (string.IsNullOrEmpty(dataRate))
            throw new ArgumentException("Data rate cannot be empty", nameof(dataRate));
        if (string.IsNullOrEmpty(delay))
            throw new ArgumentException("Delay cannot be empty", nameof(delay));

        var status = NativeMethods.p2p_install(
            simulation.Handle,
            nodeA.NativeHandle,
            nodeB.NativeHandle,
            dataRate,
            delay,
            mtu,
            out nint devA,
            out nint devB);

        Ns3Exception.ThrowIfError(status, simulation.Handle, nameof(Install));

        return (
            new Device(simulation, new DeviceHandle(devA)),
            new Device(simulation, new DeviceHandle(devB))
        );
    }
}

/// <summary>
/// Helper class for creating CSMA (Ethernet) networks
/// </summary>
public static class Csma
{
    /// <summary>
    /// Creates a CSMA bus connecting multiple nodes
    /// </summary>
    /// <param name="simulation">Simulation context</param>
    /// <param name="nodes">Nodes to connect on the bus</param>
    /// <param name="dataRate">Data rate (e.g., "100Mbps")</param>
    /// <param name="delay">Propagation delay (e.g., "6560ns")</param>
    /// <returns>Array of devices (one per node)</returns>
    public static unsafe Device[] Install(
        Simulation simulation,
        Node[] nodes,
        string dataRate,
        string delay)
    {
        if (simulation == null)
            throw new ArgumentNullException(nameof(simulation));
        if (nodes == null || nodes.Length == 0)
            throw new ArgumentException("At least one node required", nameof(nodes));
        if (string.IsNullOrEmpty(dataRate))
            throw new ArgumentException("Data rate cannot be empty", nameof(dataRate));
        if (string.IsNullOrEmpty(delay))
            throw new ArgumentException("Delay cannot be empty", nameof(delay));

        var nodeHandles = new nint[nodes.Length];
        for (int i = 0; i < nodes.Length; i++)
        {
            nodeHandles[i] = nodes[i].NativeHandle;
        }

        var deviceHandles = new nint[nodes.Length];

        fixed (nint* nodePtr = nodeHandles)
        fixed (nint* devPtr = deviceHandles)
        {
            var status = NativeMethods.csma_install(
                simulation.Handle,
                nodePtr,
                (uint)nodes.Length,
                dataRate,
                delay,
                devPtr);

            Ns3Exception.ThrowIfError(status, simulation.Handle, nameof(Install));
        }

        var devices = new Device[nodes.Length];
        for (int i = 0; i < nodes.Length; i++)
        {
            devices[i] = new Device(simulation, new DeviceHandle(deviceHandles[i]));
        }

        return devices;
    }
}

/// <summary>
/// Wi-Fi PHY standard enumeration
/// </summary>
public enum WiFiStandard
{
    /// <summary>802.11a (5 GHz)</summary>
    Std_80211a = 0,
    /// <summary>802.11b (2.4 GHz)</summary>
    Std_80211b = 1,
    /// <summary>802.11g (2.4 GHz)</summary>
    Std_80211g = 2,
    /// <summary>802.11n (2.4 GHz)</summary>
    Std_80211n_2_4GHz = 3,
    /// <summary>802.11n (5 GHz)</summary>
    Std_80211n_5GHz = 4,
    /// <summary>802.11ac (5 GHz)</summary>
    Std_80211ac = 5
}

/// <summary>
/// Helper class for creating Wi-Fi networks
/// </summary>
public static class WiFi
{
    /// <summary>
    /// Creates a Wi-Fi network with stations and an access point
    /// </summary>
    /// <param name="simulation">Simulation context</param>
    /// <param name="stations">Station nodes</param>
    /// <param name="accessPoint">Access point node</param>
    /// <param name="standard">Wi-Fi PHY standard</param>
    /// <param name="dataRate">Data rate (e.g., "54Mbps")</param>
    /// <param name="channel">Wi-Fi channel number</param>
    /// <returns>Tuple of station devices and AP device</returns>
    public static unsafe (Device[] StationDevices, Device AccessPointDevice) InstallStationAp(
        Simulation simulation,
        Node[] stations,
        Node accessPoint,
        WiFiStandard standard,
        string dataRate,
        int channel)
    {
        if (simulation == null)
            throw new ArgumentNullException(nameof(simulation));
        if (stations == null || stations.Length == 0)
            throw new ArgumentException("At least one station required", nameof(stations));
        if (accessPoint == null)
            throw new ArgumentNullException(nameof(accessPoint));
        if (string.IsNullOrEmpty(dataRate))
            throw new ArgumentException("Data rate cannot be empty", nameof(dataRate));

        var staHandles = new nint[stations.Length];
        for (int i = 0; i < stations.Length; i++)
        {
            staHandles[i] = stations[i].NativeHandle;
        }

        var staDevHandles = new nint[stations.Length];

        fixed (nint* staPtr = staHandles)
        fixed (nint* devPtr = staDevHandles)
        {
            var status = NativeMethods.wifi_install_sta_ap(
                simulation.Handle,
                staPtr,
                (uint)stations.Length,
                accessPoint.NativeHandle,
                (int)standard,
                dataRate,
                channel,
                devPtr,
                out nint apDev);

            Ns3Exception.ThrowIfError(status, simulation.Handle, nameof(InstallStationAp));

            var staDevices = new Device[stations.Length];
            for (int i = 0; i < stations.Length; i++)
            {
                staDevices[i] = new Device(simulation, new DeviceHandle(staDevHandles[i]));
            }

            return (staDevices, new Device(simulation, new DeviceHandle(apDev)));
        }
    }
}

