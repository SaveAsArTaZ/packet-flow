# Quick Start Guide

Get up and running with PacketFlow ns-3 Adapter in 5 minutes.

## Prerequisites Check

**Windows:**
- [ ] Visual Studio 2022 with C++ tools
- [ ] CMake 3.16+
- [ ] .NET 8 SDK
- [ ] ns-3.41 installed at `C:\ns-3-dev`

**Linux:**
- [ ] g++ or clang
- [ ] CMake 3.16+
- [ ] .NET 8 SDK
- [ ] ns-3.41 installed at `~/ns-3-dev`

## Step 1: Install ns-3 (if needed)

### Windows
```powershell
cd C:\
git clone https://gitlab.com/nsnam/ns-3-dev.git
cd ns-3-dev
git checkout ns-3.41
python ns3 configure --enable-examples
python ns3 build
```

### Linux
```bash
cd ~
git clone https://gitlab.com/nsnam/ns-3-dev.git
cd ns-3-dev
git checkout ns-3.41
./ns3 configure --enable-examples
./ns3 build
```

## Step 2: Build the Adapter

### Windows
```powershell
cd adapter
.\build-all.ps1
```

### Linux
```bash
cd adapter
chmod +x build-all.sh
./build-all.sh
```

## Step 3: Run an Example

```bash
cd dotnet/PacketFlow.Ns3Adapter.Examples
dotnet run -- p2p
```

You should see output like:
```
=== Point-to-Point UDP Echo Example ===

Creating nodes...
Installing Internet stack...
Creating point-to-point link...
...
[TX] Device 1 at 2.000s: 1024 bytes
[RX] Device 1 at 2.004s: 1024 bytes
...

✓ Success: Packets were transmitted and received!
```

## Step 4: Write Your First Simulation

Create a new file `MyFirstSim.cs`:

```csharp
using PacketFlow.Ns3Adapter;

using var sim = new Simulation();

// Create 2 nodes
var nodes = sim.CreateNodes(2);

// Install TCP/IP stack
sim.InstallInternetStack(nodes);

// Connect with 100Mbps link
var (dev0, dev1) = PointToPoint.Install(
    sim, nodes[0], nodes[1], 
    "100Mbps", "10ms"
);

// Assign IPs
sim.AssignIpv4Addresses(
    new[] { dev0, dev1 }, 
    "10.0.0.0", "255.255.255.0"
);

// Create UDP echo server
var server = UdpEcho.CreateServer(sim, nodes[1], 9);
server.Start(TimeSpan.Zero);
server.Stop(TimeSpan.FromSeconds(10));

// Create UDP echo client
var client = UdpEcho.CreateClient(
    sim, nodes[0], 
    "10.0.0.2", 9,
    packetSize: 1024,
    interval: TimeSpan.FromSeconds(1),
    maxPackets: 5
);
client.Start(TimeSpan.FromSeconds(1));
client.Stop(TimeSpan.FromSeconds(10));

// Run!
sim.Stop(TimeSpan.FromSeconds(10));
sim.Run();

Console.WriteLine($"Done! Ran for {sim.Now.TotalSeconds} seconds");
```

Run it:
```bash
dotnet run
```

## Common Issues

### "ns3shim.dll not found"

**Windows:**
```powershell
$env:NS3SHIM_PATH = "C:\path\to\adapter\native\build\Release"
```

**Linux:**
```bash
export NS3SHIM_PATH=/path/to/adapter/native/build
```

### "ns-3 libraries not found" during build

Specify ns-3 path explicitly:

**Windows:**
```powershell
.\build-native.ps1 -Ns3Path "D:\my-ns3"
```

**Linux:**
```bash
./build-native.sh /home/user/my-ns3
```

### Tests fail

Tests require a working ns-3 installation. If you just want to build:
```bash
cd dotnet
dotnet build
```

## Next Steps

- Read the full [README.md](README.md) for detailed documentation
- Explore [Examples](dotnet/PacketFlow.Ns3Adapter.Examples/) for more scenarios
- Check [CONTRIBUTING.md](CONTRIBUTING.md) to extend the adapter
- Review [Tests](dotnet/PacketFlow.Ns3Adapter.Tests/) for API usage patterns

## Need Help?

1. Check the [README](README.md) troubleshooting section
2. Review ns-3 documentation: https://www.nsnam.org/docs/
3. File an issue on GitHub

Happy simulating! 🎉

