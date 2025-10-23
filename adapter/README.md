# PacketFlow ns-3 Adapter

**Production-quality .NET 8 ↔ ns-3 interoperability adapter**

This adapter provides a complete, type-safe C# interface to the [ns-3 network simulator](https://www.nsnam.org/), enabling .NET applications to configure, run, and monitor ns-3 simulations with full access to the most commonly used ns-3 APIs.

## Features

✅ **Complete C ABI Layer** - Clean C interface wrapping ns-3's C++ API  
✅ **Type-Safe .NET SDK** - Modern C# 12 API with `SafeHandle` resource management  
✅ **Cross-Platform** - Windows 11 and Ubuntu 22.04 support  
✅ **Event Marshalling** - Callback-based packet tracing with automatic GC protection  
✅ **Flow Statistics** - Built-in FlowMonitor integration for network metrics  
✅ **Multiple Topologies** - Point-to-Point, CSMA, and Wi-Fi helpers  
✅ **Production Ready** - Comprehensive error handling, thread-safe, leak-free  

## Supported ns-3 Version

**ns-3.41** (tested) - Should work with ns-3.40+ with minor adjustments

## Architecture

```
┌─────────────────────────────────────┐
│  .NET Application (C#)              │
│  - High-level API (Simulation, etc) │
└─────────────────┬───────────────────┘
                  │ P/Invoke
┌─────────────────▼───────────────────┐
│  PacketFlow.Ns3Adapter (C#)         │
│  - SafeHandles                      │
│  - Callback Marshalling             │
└─────────────────┬───────────────────┘
                  │ DllImport
┌─────────────────▼───────────────────┐
│  ns3shim (C ABI)                    │
│  - Opaque handles                   │
│  - POD structs                      │
└─────────────────┬───────────────────┘
                  │ C++ Wrapper
┌─────────────────▼───────────────────┐
│  ns-3 Network Simulator (C++)       │
│  - Core, Network, Internet, etc.    │
└─────────────────────────────────────┘
```

## Prerequisites

### Windows 11

- **Visual Studio 2022** (with C++ Desktop Development workload)
- **CMake 3.16+** ([download](https://cmake.org/download/))
- **.NET 8 SDK** ([download](https://dotnet.microsoft.com/download/dotnet/8.0))
- **ns-3.41** (see installation below)
- **Git**

### Ubuntu 22.04

```bash
sudo apt update
sudo apt install -y build-essential cmake git
sudo apt install -y python3 python3-dev
sudo apt install -y g++ clang
```

Install .NET 8:
```bash
wget https://dot.net/v1/dotnet-install.sh
chmod +x dotnet-install.sh
./dotnet-install.sh --channel 8.0
```

## Installing ns-3

### Windows (PowerShell)

```powershell
# Clone ns-3
cd C:\
git clone https://gitlab.com/nsnam/ns-3-dev.git ns-3-dev
cd ns-3-dev
git checkout ns-3.41

# Configure and build
python ns3 configure --enable-examples --enable-tests
python ns3 build
```

### Ubuntu

```bash
# Clone ns-3
cd ~
git clone https://gitlab.com/nsnam/ns-3-dev.git
cd ns-3-dev
git checkout ns-3.41

# Configure and build
./ns3 configure --enable-examples --enable-tests
./ns3 build
```

## Building the Adapter

### 1. Build Native Library (ns3shim)

#### Windows

```powershell
cd adapter/native
mkdir build
cd build

cmake .. -DNS3_DIR=C:\ns-3-dev -G "Visual Studio 17 2022" -A x64
cmake --build . --config Release
```

#### Ubuntu

```bash
cd adapter/native
mkdir build
cd build

cmake .. -DNS3_DIR=$HOME/ns-3-dev -DCMAKE_BUILD_TYPE=Release
cmake --build .
```

The native library will be built in `adapter/native/build/`:
- **Windows**: `Release/ns3shim.dll`
- **Linux**: `libns3shim.so`

### 2. Build .NET SDK

```bash
cd adapter/dotnet
dotnet build -c Release
```

### 3. Build Examples

```bash
cd adapter/dotnet/PacketFlow.Ns3Adapter.Examples
dotnet build -c Release
```

### 4. Run Tests

```bash
cd adapter/dotnet/PacketFlow.Ns3Adapter.Tests
dotnet test
```

## Running Examples

### Point-to-Point Echo

```bash
cd adapter/dotnet/PacketFlow.Ns3Adapter.Examples
dotnet run -- p2p
```

### CSMA Bus with Flow Monitor

```bash
dotnet run -- csma
```

### Wi-Fi STA/AP

```bash
dotnet run -- wifi
```

### All Examples

```bash
dotnet run -- all
```

## Usage Examples

### Basic Simulation

```csharp
using PacketFlow.Ns3Adapter;

// Create simulation
using var sim = new Simulation();
sim.SetSeed(12345);

// Create nodes
var nodes = sim.CreateNodes(2);

// Install Internet stack
sim.InstallInternetStack(nodes);

// Create point-to-point link
var (dev0, dev1) = PointToPoint.Install(
    sim, nodes[0], nodes[1], 
    dataRate: "5Mbps", 
    delay: "2ms"
);

// Assign IP addresses
sim.AssignIpv4Addresses(
    new[] { dev0, dev1 }, 
    networkBase: "10.1.1.0", 
    mask: "255.255.255.0"
);

// Create applications
var server = UdpEcho.CreateServer(sim, nodes[1], port: 9);
server.Start(TimeSpan.FromSeconds(1.0));
server.Stop(TimeSpan.FromSeconds(10.0));

var client = UdpEcho.CreateClient(
    sim, nodes[0], 
    destinationIp: "10.1.1.2", 
    port: 9,
    packetSize: 1024, 
    interval: TimeSpan.FromSeconds(1.0), 
    maxPackets: 5
);
client.Start(TimeSpan.FromSeconds(2.0));
client.Stop(TimeSpan.FromSeconds(10.0));

// Run simulation
sim.Stop(TimeSpan.FromSeconds(10.0));
sim.Run();

Console.WriteLine($"Simulation completed at {sim.Now.TotalSeconds}s");
```

### Packet Tracing

```csharp
// Subscribe to TX/RX events
dev0.SubscribeToPacketEvents(
    onTx: evt => Console.WriteLine($"TX: {evt.Bytes} bytes at {evt.Time}"),
    onRx: evt => Console.WriteLine($"RX: {evt.Bytes} bytes at {evt.Time}")
);
```

### Flow Monitor Statistics

```csharp
// Install flow monitor
var flowMon = FlowMonitor.InstallAll(sim);

// Run simulation
sim.Run();

// Collect statistics
var stats = flowMon.CollectStatistics();
Console.WriteLine($"TX Packets: {stats.TxPackets}");
Console.WriteLine($"RX Packets: {stats.RxPackets}");
Console.WriteLine($"Packet Loss: {stats.PacketLossRatio * 100:F2}%");
Console.WriteLine($"Avg Delay: {stats.AverageDelay.TotalMilliseconds:F3} ms");
```

### CSMA Network

```csharp
var nodes = sim.CreateNodes(4);
sim.InstallInternetStack(nodes);

var devices = Csma.Install(sim, nodes, "100Mbps", "6560ns");
sim.AssignIpv4Addresses(devices, "192.168.1.0", "255.255.255.0");
```

### Wi-Fi Network

```csharp
var stations = sim.CreateNodes(3);
var ap = sim.CreateNodes(1)[0];
sim.InstallInternetStack(stations.Append(ap).ToArray());

var (staDevices, apDevice) = WiFi.InstallStationAp(
    sim,
    stations,
    ap,
    WiFiStandard.Std_80211n_2_4GHz,
    dataRate: "HtMcs7",
    channel: 1
);

// Set positions
stations[0].SetPosition(0, 0, 0);
stations[1].SetPosition(10, 0, 0);
stations[2].SetPosition(20, 0, 0);
ap.SetPosition(10, 10, 0);
```

## API Reference

### Core Classes

#### `Simulation`
Main simulation context. Manages lifecycle, time, and scheduling.

**Methods:**
- `SetSeed(uint)` - Set RNG seed
- `Run()` - Run simulation (blocks)
- `Stop(TimeSpan)` - Schedule stop
- `Now` - Current simulation time
- `Schedule(TimeSpan, Action)` - Schedule callback
- `CreateNodes(int)` - Create network nodes
- `InstallInternetStack(Node[])` - Install TCP/IP stack
- `AssignIpv4Addresses(Device[], string, string)` - Assign IPs
- `PopulateRoutingTables()` - Build routing tables

#### `Node`
Network node.

**Methods:**
- `SetPosition(double x, double y, double z)` - Set mobility position

#### `Device`
Network device (NIC).

**Methods:**
- `EnablePcap(string)` - Enable PCAP tracing
- `SubscribeToPacketEvents(Action<PacketEvent>?, Action<PacketEvent>?)` - Subscribe to TX/RX

#### `Application`
Network application.

**Methods:**
- `Start(TimeSpan)` - Schedule start
- `Stop(TimeSpan)` - Schedule stop

### Helper Classes

#### `PointToPoint`
- `Install(Simulation, Node, Node, string dataRate, string delay, uint mtu = 1500)`

#### `Csma`
- `Install(Simulation, Node[], string dataRate, string delay)`

#### `WiFi`
- `InstallStationAp(Simulation, Node[] stations, Node ap, WiFiStandard, string dataRate, int channel)`

#### `UdpEcho`
- `CreateServer(Simulation, Node, ushort port)`
- `CreateClient(Simulation, Node, string dstIp, ushort port, uint packetSize, TimeSpan interval, uint maxPackets)`

#### `FlowMonitor`
- `InstallAll(Simulation)`
- `CollectStatistics()` → `FlowStatistics`

## Threading Model

- **ns-3 is single-threaded**: All simulation logic runs on one thread
- **Callbacks fire on ns-3 thread**: Avoid blocking operations in callbacks
- **Managed callbacks are GC-protected**: Automatic lifetime management
- **Multiple simulations are sequential**: Only one `Simulation` instance should be active at a time

## Error Handling

All methods throw `Ns3Exception` on errors with detailed messages from ns-3:

```csharp
try {
    sim.Run();
} catch (Ns3Exception ex) {
    Console.WriteLine($"ns-3 error: {ex.Message}");
}
```

## Memory Management

- **Simulation**: Disposed via `using` statement or `Dispose()`
- **Nodes/Devices/Apps**: Owned by simulation, auto-cleaned on simulation dispose
- **SafeHandles**: Automatic finalization, deterministic cleanup

## Extending the Adapter

### Adding New ns-3 APIs

1. **Add C ABI function** in `native/include/ns3shim.h`
2. **Implement** in `native/src/ns3shim.cpp`
3. **Declare P/Invoke** in `dotnet/.../Interop/NativeMethods.cs`
4. **Wrap in high-level API** in appropriate C# class

Example:
```c
// ns3shim.h
NS3SHIM_API ns3_status tcp_echo_server(ns3_sim sim, ns3_node node, uint16_t port, ns3_app* outApp);
```

```csharp
// NativeMethods.cs
[DllImport(LibraryName.Ns3Shim, CallingConvention = CallingConvention.Cdecl)]
internal static extern Ns3Status tcp_echo_server(nint sim, nint node, ushort port, out nint outApp);

// TcpEcho.cs
public static Application CreateServer(Simulation sim, Node node, ushort port) {
    var status = NativeMethods.tcp_echo_server(sim.Handle, node.NativeHandle, port, out nint app);
    Ns3Exception.ThrowIfError(status, sim.Handle, nameof(CreateServer));
    return new Application(sim, new AppHandle(app));
}
```

### Future Extensions

The architecture supports extending to:
- **LTE/5G** (LENA module)
- **LoRaWAN**
- **Custom protocols**
- **Animation (NetAnim)**
- **Real-time visualization**

## Troubleshooting

### Native library not found

Set `NS3SHIM_PATH` environment variable:

**Windows:**
```powershell
$env:NS3SHIM_PATH = "C:\path\to\adapter\native\build\Release"
```

**Linux:**
```bash
export NS3SHIM_PATH=/path/to/adapter/native/build
```

### ns-3 libraries not found during build

Ensure `NS3_DIR` points to your ns-3 installation:

```bash
cmake .. -DNS3_DIR=/path/to/ns-3-dev
```

### Application crashes on Run()

- Check that ns-3 was built successfully
- Verify all native dependencies are in PATH (Windows) or LD_LIBRARY_PATH (Linux)
- Enable debug build for more information

## Performance Considerations

- **Callback overhead**: Minimize work in packet callbacks; queue data for processing
- **Large simulations**: ns-3 is event-driven; scales well with node count
- **Memory**: Each simulation context is independent; clean up when done

## Contributing

This is a production-quality reference implementation. To contribute:

1. Follow the existing code style
2. Add tests for new functionality
3. Update documentation
4. Ensure cross-platform compatibility

## License

See LICENSE file in repository root.

## Resources

- [ns-3 Documentation](https://www.nsnam.org/documentation/)
- [ns-3 Tutorial](https://www.nsnam.org/docs/tutorial/html/)
- [.NET P/Invoke Guide](https://learn.microsoft.com/en-us/dotnet/standard/native-interop/pinvoke)

## Support

For issues specific to this adapter, please file an issue in the repository.  
For ns-3 questions, consult the [ns-3 mailing list](https://groups.google.com/g/ns-3-users).

---

**Built with ❤️ for network simulation research and development**

