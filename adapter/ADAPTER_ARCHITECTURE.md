# PacketFlow ns-3 Adapter — Architecture & Internals

> **Production-quality .NET 8 ↔ ns-3 interoperability adapter**
>
> Enables .NET applications to configure, run, and monitor ns-3 network simulations via a four-layer bridging architecture: C# API → P/Invoke → C ABI shim → ns-3 C++.

---

## Table of Contents

1. [Overview](#overview)
2. [Directory Structure](#directory-structure)
3. [Four-Layer Architecture](#four-layer-architecture)
4. [Layer 1 — .NET High-Level API](#layer-1--net-high-level-api)
5. [Layer 2 — P/Invoke Interop Layer](#layer-2--pinvoke-interop-layer)
6. [Layer 3 — Native C ABI Shim (ns3shim)](#layer-3--native-c-abi-shim-ns3shim)
7. [Layer 4 — ns-3 C++ Simulator](#layer-4--ns-3-c-simulator)
8. [Key Design Patterns](#key-design-patterns)
9. [Data Flow Walkthrough](#data-flow-walkthrough)
10. [Build System](#build-system)
11. [Testing Strategy](#testing-strategy)
12. [Supported Topologies](#supported-topologies)
13. [Threading Model](#threading-model)
14. [Error Handling Strategy](#error-handling-strategy)
15. [Memory Management](#memory-management)
16. [Extending the Adapter](#extending-the-adapter)
17. [File Reference Table](#file-reference-table)

---

## Overview

The PacketFlow ns-3 Adapter bridges the managed .NET world with the native C++ ns-3 network simulator. It solves the fundamental challenge of invoking ns-3's C++ API from C# by inserting two tightly-coupled intermediary layers: a **P/Invoke declaration layer** (C#) and a **C ABI shim** (C++/C). This design ensures type safety, resource safety, and cross-platform compatibility.

### Design Goals

| Goal | Implementation |
|------|---------------|
| **Type Safety** | `SafeHandle` wrappers, `readonly record struct` for POD data |
| **Resource Safety** | RAII disposal, GC-protected callbacks, NULL-safe/idempotent destroy |
| **Error Transparency** | C++ exceptions caught at ABI boundary → `ns3_status` → C# `Ns3Exception` |
| **Cross-Platform** | Windows 11 & Ubuntu 22.04; CMake + .NET 8; platform-specific library resolution |
| **Testability** | xUnit tests covering lifecycle, interop, and end-to-end scenarios |

---

## Directory Structure

```
adapter/
├── README.md                              # User-facing documentation
├── CONTRIBUTING.md                        # Developer contribution guide
├── ADAPTER_ARCHITECTURE.md                # This document
├── LICENSE
├── .gitignore
│
├── build-all.ps1                          # Windows: orchestrates full build + test
├── build-all.sh                           # Linux: orchestrates full build + test
├── build-native.ps1                       # Windows: native-only build
├── build-native.sh                        # Linux: native-only build
├── pack-nuget.ps1                         # NuGet packaging script
│
├── native/                                # C++ SHIM LAYER
│   ├── CMakeLists.txt                     # CMake build (finds ns-3, builds libns3shim)
│   ├── include/
│   │   └── ns3shim.h                      # Public C ABI header (347 lines)
│   └── src/
│       └── ns3shim.cpp                    # Implementation (757 lines)
│
└── dotnet/                                # .NET LAYER
    ├── PacketFlow.Ns3Adapter.sln          # Solution file
    │
    ├── PacketFlow.Ns3Adapter/             # Main SDK project
    │   ├── PacketFlow.Ns3Adapter.csproj   # net8.0, C#12, nullable, AllowUnsafeBlocks
    │   ├── Simulation.cs                  # Simulation, Node, Device, PacketEvent
    │   ├── Links.cs                       # PointToPoint, Csma, WiFi helpers
    │   ├── Applications.cs                # Application, UdpEcho, FlowMonitor, FlowStatistics
    │   ├── nuget.config
    │   └── Interop/
    │       ├── NativeMethods.cs           # P/Invoke declarations (245 lines)
    │       ├── SafeHandles.cs             # SimHandle, NodeHandle, DeviceHandle, etc.
    │       ├── NativeLibraryResolver.cs   # Cross-platform library loader
    │       └── Ns3Exception.cs            # Exception + ThrowIfError helper
    │
    ├── PacketFlow.Ns3Adapter.Examples/    # Example programs
    │   ├── Program.cs                     # CLI entry point (p2p | csma | wifi | all)
    │   ├── P2PEchoExample.cs              # Point-to-point UDP Echo
    │   ├── CsmaBusExample.cs              # CSMA bus + FlowMonitor
    │   └── WiFiExample.cs                 # Wi-Fi STA/AP with mobility
    │
    └── PacketFlow.Ns3Adapter.Tests/       # Test project (xUnit)
        ├── SimulationLifecycleTests.cs    # Create/dispose/run lifecycle
        ├── InteropTests.cs               # P/Invoke correctness
        └── EndToEndTests.cs              # Full simulation scenarios
```

---

## Four-Layer Architecture

```
┌──────────────────────────────────────────────────────────────┐
│  LAYER 1: .NET High-Level API (C# 12)                       │
│  ┌─────────────┐ ┌──────────┐ ┌──────────────┐              │
│  │ Simulation  │ │ Links    │ │ Applications │              │
│  │ Node/Device │ │ P2P/Csma │ │ UdpEcho/Flow │              │
│  └──────┬──────┘ └────┬─────┘ └──────┬───────┘              │
│         │              │              │                      │
│  ┌──────┴──────────────┴──────────────┴───────┐              │
│  │  SafeHandles: Sim/Node/Device/App/FlowMon  │              │
│  │  GCHandle-protected callback delegates     │              │
│  └─────────────────────┬──────────────────────┘              │
└────────────────────────┼─────────────────────────────────────┘
                         │
┌────────────────────────┼─────────────────────────────────────┐
│  LAYER 2: P/Invoke Interop (C#)                              │
│  ┌─────────────────────┴──────────────────────┐              │
│  │  NativeMethods (static class)              │              │
│  │  - DllImport("ns3shim", Cdecl)             │              │
│  │  - Delegate types: VoidCallback, PacketCb  │              │
│  │  - Structs: Ns3Attr, Ns3FlowStats          │              │
│  │  - NativeLibraryResolver (custom loader)   │              │
│  │  - Ns3Exception.ThrowIfError()             │              │
│  └─────────────────────┬──────────────────────┘              │
└────────────────────────┼─────────────────────────────────────┘
                         │ Platform Invoke (C Calling Convention)
┌────────────────────────┼─────────────────────────────────────┐
│  LAYER 3: C ABI Shim — ns3shim (C++)                         │
│  ┌─────────────────────┴──────────────────────┐              │
│  │  ns3shim.h / ns3shim.cpp                   │              │
│  │  - Opaque handles (ns3_sim, ns3_node, ...) │              │
│  │  - POD structs only (ns3_flow_stats, ...)  │              │
│  │  - ns3_status return codes                 │              │
│  │  - Context struct (ns3_sim_t)              │              │
│  │    • Per-sim maps: nodes/devices/apps/flow │              │
│  │    • ID generators (nextNodeId, etc.)      │              │
│  │    • Error state (lastError + mutex)       │              │
│  │    • Helper objects (InternetStack, Ipv4)  │              │
│  │  - All exceptions caught at C++ boundary   │              │
│  └─────────────────────┬──────────────────────┘              │
└────────────────────────┼─────────────────────────────────────┘
                         │ Direct C++ calls
┌────────────────────────┼─────────────────────────────────────┐
│  LAYER 4: ns-3 C++ Simulator                                │
│  ┌─────────────────────┴──────────────────────┐              │
│  │  ns-3.41 libraries:                        │              │
│  │  core, network, internet, point-to-point,  │              │
│  │  csma, wifi, mobility, applications,       │              │
│  │  flow-monitor                              │              │
│  └────────────────────────────────────────────┘              │
└──────────────────────────────────────────────────────────────┘
```

### Why a C ABI Shim?

ns-3 is a C++ library with templates, virtual methods, and `ns3::Ptr<T>` smart pointers. None of these cross the C ABI boundary safely. The shim:

1. **Flattens C++ types** into opaque handles (`ns3_sim`, `ns3_node`, etc.) that are just typed pointers to internal IDs
2. **Catches C++ exceptions** at every function boundary and converts them to error codes + stored error strings
3. **Marshals callbacks** through C function pointers with `void* user` context — the only portable callback mechanism across the ABI
4. **Hides memory layout** — the .NET side never sees C++ objects, only `nint` handles

---

## Layer 1 — .NET High-Level API

### Class Hierarchy

```
Simulation (IDisposable)
├── Node              — network node with optional mobility position
├── Device            — network interface with packet tracing + PCAP
├── Application       — ns-3 application (start/stop scheduling)
├── PacketEvent       — readonly record struct (DeviceId, Time, Bytes)
│
PointToPoint (static) — helper: creates P2P links
Csma (static)         — helper: creates CSMA bus
WiFi (static)         — helper: creates Wi-Fi STA/AP topology
│
UdpEcho (static)      — helper: creates UDP echo server/client
│
FlowMonitor           — installs flow monitor, collects statistics
FlowStatistics        — readonly record struct (Tx/Rx packets/bytes, delay, jitter)
```

### Simulation.cs — Key Types

**`Simulation`** (`IDisposable`) — The root context. Internally holds a `SimHandle` (wraps `ns3_sim`). Methods delegate to `NativeMethods.sim_*` P/Invoke calls.
- `SetSeed(uint)` — calls `sim_set_seed`
- `Run()` — calls `sim_run` (blocking)
- `Stop(TimeSpan)` — calls `sim_stop`
- `Now` — calls `sim_now`, returns `TimeSpan`
- `Schedule(TimeSpan, Action)` — creates `GCHandle` for callback, calls `sim_schedule`
- `CreateNodes(int)` — calls `nodes_create`, wraps in `Node[]`
- `InstallInternetStack(Node[])` — calls `internet_install`
- `AssignIpv4Addresses(Device[], string, string)` — calls `ipv4_assign`
- `PopulateRoutingTables()` — calls `ipv4_populate_routing_tables`

**`Node`** — Thin wrapper around `NodeHandle`.
- `SetPosition(double, double, double)` — calls `mobility_set_constant_position`

**`Device`** — Thin wrapper around `DeviceHandle`.
- `EnablePcap(string)` — calls `pcap_enable`
- `SubscribeToPacketEvents(Action<PacketEvent>?, Action<PacketEvent>?)` — creates `GCHandle` for each callback, calls `trace_subscribe_packet_events`

**`PacketEvent`** — `readonly record struct` with `DeviceId` (ulong), `Time` (TimeSpan), `Bytes` (uint). Value semantics, no heap allocation.

### Links.cs — Topology Helpers

All three helper classes follow the same pattern:
1. Validate arguments
2. Marshal node handles to native arrays
3. Call the corresponding `NativeMethods.*_install` function
4. Wrap returned device handles in `Device` objects
5. Return typed results (tuples or arrays)

| Class | Native Function | Input | Output |
|-------|----------------|-------|--------|
| `PointToPoint.Install` | `p2p_install` | 2 nodes, dataRate, delay, MTU | `(Device, Device)` |
| `Csma.Install` | `csma_install` | N nodes, dataRate, delay | `Device[]` |
| `WiFi.InstallStationAp` | `wifi_install_sta_ap` | N stations + 1 AP, standard, dataRate, channel | `(Device[], Device)` |

**`WiFiStandard` enum** — Maps to integer constants expected by the C shim:
- `Std_80211a` (0) through `Std_80211ac` (5)

### Applications.cs — Application Helpers

**`Application`** — Wraps `AppHandle`. Methods `Start(TimeSpan)` / `Stop(TimeSpan)` delegate to `app_start` / `app_stop`.

**`UdpEcho` (static)** — Factory methods:
- `CreateServer(Simulation, Node, ushort port)` → `Application`
- `CreateClient(Simulation, Node, string dstIp, ushort port, uint packetSize, TimeSpan interval, uint maxPackets)` → `Application`

**`FlowMonitor`** — Wraps `FlowMonHandle`.
- `InstallAll(Simulation)` (static) → `FlowMonitor` — calls `flowmon_install_all`
- `CollectStatistics()` → `FlowStatistics` — calls `flowmon_collect`, maps the returned `Ns3FlowStats` struct

**`FlowStatistics`** — `readonly record struct` with computed properties:
- `AverageDelay` = `DelaySum / RxPackets`
- `AverageJitter` = `JitterSum / RxPackets`
- `PacketLossRatio` = `1.0 - (RxPackets / TxPackets)`

---

## Layer 2 — P/Invoke Interop Layer

### NativeMethods.cs

The `NativeMethods` static class contains all `[DllImport]` declarations. Key design decisions:

| Aspect | Choice | Rationale |
|--------|--------|-----------|
| Library name | `"ns3shim"` (no extension) | Cross-platform; `NativeLibraryResolver` handles platform suffix |
| Calling convention | `Cdecl` | Standard C calling convention |
| String marshalling | `UnmanagedType.LPStr` + `CharSet.Ansi` | ns-3 uses UTF-8; strict mapping prevents data loss |
| Array parameters | `nint*` (unsafe pointers) | Direct memory access avoids marshalling overhead |
| Callback delegates | `[UnmanagedFunctionPointer(Cdecl)]` | Ensures correct calling convention for function pointers |
| Struct layout | `LayoutKind.Sequential` / `LayoutKind.Explicit` | Matches C struct layout for POD types |

**Delegate Types:**
- `VoidCallback(nint user)` — for `sim_schedule`
- `PacketCallback(nint user, ulong deviceId, double timeSec, uint bytes)` — for packet trace events

**Struct Types:**
- `Ns3Attr` — `LayoutKind.Explicit` tagged union matching `ns3_attr`; has factory methods (`FromBool`, `FromUInt`, etc.)
- `Ns3FlowStats` — `LayoutKind.Sequential` POD matching `ns3_flow_stats`

### SafeHandles.cs

Five handle types, all deriving from `SafeHandleZeroOrMinusOneIsInvalid`:

| Handle Type | Owns Resource? | ReleaseHandle Behavior |
|-------------|---------------|----------------------|
| `SimHandle` | **Yes** | Calls `sim_destroy()` — destroys entire simulation |
| `NodeHandle` | No | No-op — owned by simulation context |
| `DeviceHandle` | No | No-op — owned by simulation context |
| `AppHandle` | No | No-op — owned by simulation context |
| `FlowMonHandle` | No | No-op — owned by simulation context |

This is a **parent-child ownership model**: `Simulation` owns everything. When `Simulation.Dispose()` is called, `SimHandle.ReleaseHandle()` runs `sim_destroy`, which cleans up all nodes, devices, apps, and flow monitors in one shot via `Simulator::Destroy()`. The child handles are non-owning — they only provide type safety and prevent accidental handle misuse.

### NativeLibraryResolver.cs

Custom `DllImportResolver` registered in `Simulation`'s static constructor. Probe order:

1. **`NS3SHIM_PATH` environment variable** — user-specified path
2. **Application directory** — `AppContext.BaseDirectory`
3. **NuGet runtimes directory** — `runtimes/{rid}/native/` (for packaged deployment)
4. **Assembly directory** — alongside the DLL
5. **Development build output** — relative path `../../../../../native/build/` (for dev workflow)
6. **Default resolution** — system library paths

Platform library name mapping:
- Windows: `ns3shim.dll`
- Linux: `libns3shim.so`
- macOS: `libns3shim.dylib`

### Ns3Exception.cs

Custom exception type with two key facilities:

- **`GetLastError(nint simHandle)`** — Calls `ns3_last_error` with a stack-allocated 1024-byte buffer, reads the UTF-8 error string
- **`ThrowIfError(status, simHandle, operationName)`** — Checks `Ns3Status.Error` → throws `Ns3Exception` with the operation name and native error message

---

## Layer 3 — Native C ABI Shim (ns3shim)

### Opaque Handle System

The shim uses **integer IDs masquerading as pointers**:

```cpp
// Opaque handle types (in anonymous namespace)
struct ns3_node_t    { uint64_t id; };
struct ns3_device_t  { uint64_t id; };
struct ns3_app_t     { uint64_t id; };
struct ns3_flowmon_t { uint64_t id; };

// Conversion
inline uint64_t HandleToId(ns3_node node) { return reinterpret_cast<uint64_t>(node); }
inline ns3_node IdToNodeHandle(uint64_t id) { return reinterpret_cast<ns3_node>(id); }
```

This is NOT a real pointer — the handle is just the `uint64_t` ID cast to a pointer type. The actual `ns3::Ptr<T>` objects live in maps inside `ns3_sim_t`. This design:
- Avoids storing raw C++ pointers in opaque handles (which could dangle)
- Allows handle validation by checking map membership
- Is ABI-stable — the .NET side only sees an `nint`

### ns3_sim_t — The Simulation Context

```cpp
struct ns3_sim_t {
    // Handle maps (ID → Ptr<T>)
    std::map<uint64_t, Ptr<Node>>          nodes;
    std::map<uint64_t, Ptr<NetDevice>>     devices;
    std::map<uint64_t, Ptr<Application>>   apps;
    std::map<uint64_t, Ptr<FlowMonitor>>   flowMons;

    // Reusable helper objects (stateful across calls)
    InternetStackHelper  internetStack;
    Ipv4AddressHelper    ipv4Helper;

    // State
    std::atomic<bool>    isRunning;
    std::string          lastError;
    mutable std::mutex   errorMutex;       // thread-safe error access

    // ID generators (monotonically increasing)
    uint64_t nextNodeId    = 1;
    uint64_t nextDeviceId  = 1;
    uint64_t nextAppId     = 1;
    uint64_t nextFlowMonId = 1;
};
```

### API Categories

The 25 exported C functions are organized into these groups:

| Group | Functions | Count |
|-------|-----------|-------|
| **Error Handling** | `ns3_last_error` | 1 |
| **Simulation Lifecycle** | `sim_create`, `sim_set_seed`, `sim_run`, `sim_stop`, `sim_is_running`, `sim_now`, `sim_schedule`, `sim_destroy` | 8 |
| **Nodes & Topology** | `nodes_create`, `internet_install` | 2 |
| **Network Devices** | `p2p_install`, `csma_install`, `wifi_install_sta_ap` | 3 |
| **Mobility** | `mobility_set_constant_position` | 1 |
| **IP Addressing** | `ipv4_assign`, `ipv4_populate_routing_tables` | 2 |
| **Applications** | `app_udpecho_server`, `app_udpecho_client`, `app_start`, `app_stop` | 4 |
| **Tracing & Stats** | `trace_subscribe_packet_events`, `pcap_enable`, `flowmon_install_all`, `flowmon_collect` | 4 |
| **Configuration** | `config_set` | 1 |

### Error Handling Pattern

Every function follows this exact pattern:

```cpp
NS3SHIM_API ns3_status my_function(ns3_sim sim, ...) {
    if (!ValidateSim(sim)) return NS3_ERR;     // 1. Guard

    try {
        // 2. ns-3 operations (may throw)
        return NS3_OK;                         // 3. Success
    } catch (const std::exception& e) {
        sim->SetError("my_function: " + e.what()); // 4. Capture error
        return NS3_ERR;                        // 5. Return error code
    }
}
```

C++ exceptions **never** cross the ABI boundary. The last error message is stored in `ns3_sim_t::lastError` (mutex-protected) and retrieved by the .NET side via `ns3_last_error`.

### Callback Marshalling

For packet tracing, the shim creates a **persistent heap-allocated context**:

```cpp
struct PacketTraceContext {
    ns3_pkt_cb onTx;       // C function pointer
    ns3_pkt_cb onRx;       // C function pointer
    void* user;             // .NET GCHandle pointer
    uint64_t deviceId;      // For identification
};

// Context is leaked intentionally — tied to ns-3's internal trace lifecycle
auto* ctx = new PacketTraceContext{onTx, onRx, user, deviceId};
```

The `.NET` side pins the delegate via `GCHandle.Alloc()` before passing it. The `user` pointer is the `GCHandle.ToIntPtr()`. When the callback fires:
1. ns-3 invokes the C function pointer
2. The wrapper lambda extracts the `GCHandle` from `user`
3. Retrieves the .NET delegate via `GCHandle.Target`
4. Invokes the delegate with the marshalled data

For scheduled callbacks (`sim_schedule`), the `GCHandle` is freed after the first invocation (one-shot semantics).

### Wi-Fi Standard Mapping

The `phyStandard` integer maps to ns-3 Wi-Fi standards via a switch statement:

| Value | Enum Member | ns-3 Standard |
|-------|------------|---------------|
| 0 | `Std_80211a` | `WIFI_STANDARD_80211a` |
| 1 | `Std_80211b` | `WIFI_STANDARD_80211b` |
| 2 | `Std_80211g` | `WIFI_STANDARD_80211g` |
| 3 | `Std_80211n_2_4GHz` | `WIFI_STANDARD_80211n` |
| 4 | `Std_80211n_5GHz` | `WIFI_STANDARD_80211n` |
| 5 | `Std_80211ac` | `WIFI_STANDARD_80211ac` |

---

## Layer 4 — ns-3 C++ Simulator

The adapter links against these ns-3.41 modules:
- **core** — Simulation engine, RNG, scheduling, attributes
- **network** — Node, NetDevice, Packet, Channel abstractions
- **internet** — IPv4, TCP, UDP, routing
- **point-to-point** — PointToPointHelper, PointToPointNetDevice
- **csma** — CsmaHelper, CsmaNetDevice
- **wifi** — WifiHelper, YansWifiPhyHelper, StaWifiMac, ApWifiMac
- **mobility** — MobilityHelper, ConstantPositionMobilityModel
- **applications** — UdpEchoServerHelper, UdpEchoClientHelper
- **flow-monitor** — FlowMonitorHelper, Ipv4FlowClassifier

---

## Key Design Patterns

### 1. Opaque Handle / ID-Based Lookup

**Problem:** C++ smart pointers (`ns3::Ptr<T>`) can't cross the C ABI. Raw pointers could dangle if ns-3 reallocates.

**Solution:** Handles are integer IDs cast to typed pointers. The real objects live in `std::map` inside the simulation context. Lookup validates the ID and returns the smart pointer.

### 2. Parent-Child Ownership

**Problem:** Nodes, devices, apps, and flow monitors are created by helpers and stored inside the simulation. Who owns what?

**Solution:** `SimHandle` is the only owning `SafeHandle` — its `ReleaseHandle()` calls `sim_destroy()` which calls `Simulator::Destroy()`, cleaning up everything. Child handles (`NodeHandle`, `DeviceHandle`, etc.) have no-op `ReleaseHandle()` — they're purely for type safety.

### 3. Exception-to-Error-Code Bridging

**Problem:** C++ exceptions are undefined behavior across the C ABI boundary.

**Solution:** Every exported function wraps its body in try/catch. Exceptions are caught, the message is stored in `ns3_sim_t::lastError`, and `NS3_ERR` is returned. The .NET side calls `ns3_last_error` to retrieve the message and throws `Ns3Exception`.

### 4. GC-Protected Callbacks

**Problem:** .NET delegates passed to native code must not be garbage-collected while native code holds a reference.

**Solution:** `GCHandle.Alloc(callback)` pins the delegate before passing; `GCHandle.ToIntPtr()` passes the handle as `void* user`. The native callback wrapper retrieves the delegate via `GCHandle.FromIntPtr(user).Target`. For scheduled callbacks, `GCHandle.Free()` is called after invocation.

### 5. String Attribute Arguments

**Problem:** ns-3 uses human-readable strings for data rates ("5Mbps"), delays ("2ms"), and addresses — C# must pass these across the ABI.

**Solution:** Strings are marshalled as `[MarshalAs(UnmanagedType.LPStr)]` (null-terminated UTF-8). The C shim receives `const char*` and passes directly to ns-3's `StringValue` or `Ipv4Address` constructors.

### 6. Unsafe Fixed Buffers for Arrays

**Problem:** Passing arrays of handles across the ABI efficiently.

**Solution:** C# uses `fixed (nint* ptr = array)` to pin managed arrays, then passes the pointer directly. The C shim receives it as `ns3_node*` or `ns3_device*` and iterates with the count parameter. This avoids per-element marshalling overhead.

---

## Data Flow Walkthrough

### Example: Point-to-Point UDP Echo Simulation

Here's the complete data flow for a simple simulation:

```
C# Application                    P/Invoke                         C Shim (ns3shim)                ns-3 C++
══════════════                    ════════                         ════════════════                ═════════

1. new Simulation()
   → static ctor:
     NativeLibraryResolver        LoadLibrary("ns3shim.dll")
     .Initialize()                 → finds lib, sets resolver
   → NativeMethods.sim_create()  ─DllImport→  sim_create()
                                               → new ns3_sim_t{}
                                               → *outSim = sim
   → new SimHandle(handle)
   ─────────────────────────────────────────────────────────────────────────────────────────────────────
2. sim.CreateNodes(2)
   → alloc nint[2]
   → NativeMethods              ─DllImport→  nodes_create()
     .nodes_create(2, ptr)                   → NodeContainer nodes; nodes.Create(2)
                                             → for each: generate ID, store in sim->nodes[id] = ptr
                                             → outArray[i] = IdToNodeHandle(id)
   → new Node(SimHandle) ×2
   ─────────────────────────────────────────────────────────────────────────────────────────────────────
3. PointToPoint.Install()
   → NativeMethods              ─DllImport→  p2p_install()
     .p2p_install(n0,n1,                     → GetNode(sim, a), GetNode(sim, b)
      "5Mbps","2ms",1500)                    → PointToPointHelper; SetDeviceAttribute("DataRate","5Mbps")
                                             → Install → NetDeviceContainer
                                             → store devices, return handles
   → new Device(devA), new Device(devB)
   ─────────────────────────────────────────────────────────────────────────────────────────────────────
4. UdpEcho.CreateServer(sim, n1, 9)
   → NativeMethods              ─DllImport→  app_udpecho_server()
     .app_udpecho_server()                   → UdpEchoServerHelper(port).Install(node)
                                             → store app, return handle
   → new Application(AppHandle)
   ─────────────────────────────────────────────────────────────────────────────────────────────────────
5. dev0.SubscribeToPacketEvents(onTx, onRx)
   → GCHandle.Alloc(onTx)                    (managed heap — pins delegate)
   → GCHandle.Alloc(onRx)
   → NativeMethods              ─DllImport→  trace_subscribe_packet_events()
     .trace_subscribe_...()                  → new PacketTraceContext{onTx, onRx, user, devId}
                                             → TraceConnectWithoutContext("PhyTxEnd", callback)
                                             → TraceConnectWithoutContext("PhyRxEnd", callback)
   ─────────────────────────────────────────────────────────────────────────────────────────────────────
6. sim.Run()
   → NativeMethods.sim_run()   ─DllImport→  sim_run()
                                             → sim->isRunning = true
                                             → Simulator::Run()   ──→ ns-3 event loop
                                                                      → fires TX/RX callbacks
                                                                         → PacketTxCallback(ctx, pkt)
                                                                           → ctx->onTx(user, id, t, sz)
                                                                             └─→ [P/Invoke back to C#]
                                                                                GCHandle.Target()
                                                                                callback(PacketEvent)
                                             → sim->isRunning = false
   ─────────────────────────────────────────────────────────────────────────────────────────────────────
7. sim.Dispose()
   → SimHandle.ReleaseHandle()
   → NativeMethods.sim_destroy() ─DllImport→ sim_destroy()
                                             → Simulator::Destroy()    → ns-3 cleanup
                                             → delete sim              → frees all maps
```

---

## Build System

### Native Build (CMake)

**File:** `native/CMakeLists.txt`

- **CMake 3.16+**, C++17, position-independent code
- Finds ns-3 via `NS3_DIR` cmake variable or `NS3_HOME` env var
- Probes common install locations: `/usr/local`, `$HOME/ns-3-dev`
- Links against 9 ns-3 modules: core, network, internet, point-to-point, csma, wifi, mobility, applications, flow-monitor
- Output: shared library (`ns3shim.dll` on Windows, `libns3shim.so` on Linux)

**Windows build:**
```powershell
cd native; mkdir build; cd build
cmake .. -DNS3_DIR=C:\ns-3-dev -G "Visual Studio 17 2022" -A x64
cmake --build . --config Release
```

**Linux build:**
```bash
cd native; mkdir build; cd build
cmake .. -DNS3_DIR=$HOME/ns-3-dev -DCMAKE_BUILD_TYPE=Release
cmake --build .
```

### .NET Build

**File:** `dotnet/PacketFlow.Ns3Adapter/PacketFlow.Ns3Adapter.csproj`

- Target: `net8.0`, C# 12
- Nullable reference types enabled
- Unsafe blocks allowed (for `fixed` pointer operations)
- XML documentation generation enabled
- NuGet metadata: PacketFlow.Ns3Adapter v1.0.0, MIT license

### Orchestration Scripts

`build-all.ps1` / `build-all.sh` orchestrate the full pipeline:
1. **Step 1/3:** Build native library via `build-native.ps1`/`build-native.sh`
2. **Step 2/3:** `dotnet build -c Release` for the SDK
3. **Step 3/3:** `dotnet test` with `NS3SHIM_PATH` set to the native build output

---

## Testing Strategy

### Test Project Structure

```
PacketFlow.Ns3Adapter.Tests/  (xUnit, net8.0)
├── SimulationLifecycleTests   — Create/dispose/run lifecycle, edge cases
├── InteropTests              — Each native function called once, smoke tests
└── EndToEndTests             — Full simulation scenarios with assertions
```

### Test Categories

**SimulationLifecycleTests** (7 tests):
- `CreateAndDestroy_ShouldNotThrow` — basic instantiation
- `SetSeed_ShouldNotThrow` — RNG seeding
- `Now_InitialTime_ShouldBeZero` — initial state
- `IsRunning_BeforeRun_ShouldBeFalse` — state before execution
- `Run_WithStopTime_ShouldComplete` — basic run + stop
- `Schedule_Callback_ShouldBeInvoked` — callback marshalling
- `Dispose_MultipleTimes_ShouldBeIdempotent` — safety check
- `DisposedSimulation_ShouldThrowObjectDisposedException` — post-dispose guard

**InteropTests** (10 tests):
- Node creation count, internet stack install, P2P/CSMA device creation
- IP assignment, routing table population, position setting
- UDP echo server/client creation, application start/stop
- FlowMonitor installation and statistics collection

**EndToEndTests** (4 tests):
- `P2PEcho_EndToEnd_ShouldTransmitPackets` — full P2P echo with packet counting
- `CallbackMarshalling_ShouldWorkCorrectly` — verifies N scheduled callbacks all fire
- `PacketTracing_ShouldCaptureEvents` — verifies TX/RX events are captured with valid data
- `MultipleSimulations_Sequential_ShouldWork` — verifies no resource leaks across 3 sequential simulations

---

## Supported Topologies

### 1. Point-to-Point

```
Node0 ════════════ Node1
      dataRate Mbps
      delay ms
      MTU bytes
```

Created via `PointToPoint.Install(sim, nodeA, nodeB, dataRate, delay, mtu)`.
Returns exactly 2 devices. Each device supports packet tracing and PCAP capture.

### 2. CSMA Bus (Ethernet-like)

```
Node0 ──┬── Node1 ──┬── Node2 ──┬── Node3
        └───────────┴───────────┘
             shared bus
```

Created via `Csma.Install(sim, nodes[], dataRate, delay)`.
Returns N devices (one per node). All nodes share the same collision domain.

### 3. Wi-Fi STA/AP

```
         ┌─── AP ───┐
         │  (x,y,z)  │
         └──┬──┬──┬──┘
        ┌───┘  │  └───┐
      STA0   STA1   STA2
```

Created via `WiFi.InstallStationAp(sim, stations[], ap, standard, dataRate, channel)`.
Each station and the AP must have positions set via `Node.SetPosition()`.
Supports 802.11a/b/g/n/ac standards.

---

## Threading Model

| Aspect | Behavior |
|--------|----------|
| **ns-3 execution** | Single-threaded. `sim_run()` blocks the calling thread. |
| **Callback execution** | Callbacks fire on the same thread as `sim_run()`. |
| **Multiple simulations** | Sequential only — one `Simulation` instance at a time. |
| **Managed callbacks** | Protected from GC via `GCHandle`; safe to use .NET objects. |
| **Error state** | Protected by `std::mutex` in the shim for thread-safe error retrieval. |
| **Blocking caution** | Avoid long-running operations in packet callbacks — they delay the simulation. |

---

## Error Handling Strategy

```
ns-3 C++ throws std::exception
        │
        ▼
shim catch block
  → stores message in sim->lastError (mutex-protected)
  → returns NS3_ERR (-1)
        │
        ▼
C# NativeMethods returns Ns3Status.Error
        │
        ▼
Ns3Exception.ThrowIfError()
  → calls ns3_last_error (retrieves error string)
  → throws new Ns3Exception($"{operationName} failed: {errorMsg}")
        │
        ▼
C# application catches Ns3Exception
```

**Design rule:** C# code never sees raw error codes — they're always converted to exceptions.

---

## Memory Management

### Ownership Model

```
Simulation (owns everything)
├── SimHandle ──ReleaseHandle()→ sim_destroy() → Simulator::Destroy() → delete sim
│   ├── nodes map      → freed by destructor
│   ├── devices map    → freed by destructor
│   ├── apps map       → freed by destructor
│   └── flowMons map   → freed by destructor
│
├── Node[] → NodeHandle (non-owning)
├── Device[] → DeviceHandle (non-owning)
├── Application[] → AppHandle (non-owning)
└── FlowMonitor → FlowMonHandle (non-owning)
```

### Cleanup Guarantees

- **Deterministic:** `using var sim = new Simulation()` ensures `Dispose()` is called
- **Finalization-safe:** `SafeHandle` provides finalizer as safety net
- **Idempotent:** `sim_destroy` is NULL-safe; `Simulation.Dispose()` can be called multiple times
- **No leaks:** All ns-3 resources are freed when `sim_destroy` calls `Simulator::Destroy()`

### Callback Lifetime

- **One-shot callbacks** (`sim_schedule`): `GCHandle.Free()` is called after first invocation
- **Persistent callbacks** (packet tracing): Context is heap-allocated and lives as long as the simulation
- **Error path cleanup:** If native call fails, `GCHandle.Free()` is called immediately in the C# layer

---

## Extending the Adapter

To add a new ns-3 feature (e.g., TCP Bulk Send), follow this checklist:

### Step 1: C ABI (ns3shim.h)
Add function declaration with `NS3SHIM_API` macro, `ns3_status` return type, opaque handles, and Doxygen comments:
```c
NS3SHIM_API ns3_status app_tcp_bulk_send(ns3_sim sim, ns3_node node, const char* dstIp,
                                          uint16_t port, uint32_t maxBytes, ns3_app* outApp);
```

### Step 2: Implementation (ns3shim.cpp)
Implement with the standard error-handling pattern:
```cpp
NS3SHIM_API ns3_status app_tcp_bulk_send(...) {
    if (!ValidateSim(sim) || !node || !dstIp || !outApp) return NS3_ERR;
    try {
        // Use ns-3 C++ API: BulkSendHelper, etc.
        // Store in sim->apps[id]
        // *outApp = IdToAppHandle(id);
        return NS3_OK;
    } catch (const std::exception& e) {
        sim->SetError(std::string("app_tcp_bulk_send: ") + e.what());
        return NS3_ERR;
    }
}
```

### Step 3: P/Invoke (NativeMethods.cs)
Add `[DllImport]` declaration with correct marshalling:
```csharp
[DllImport(LibraryName.Ns3Shim, CallingConvention = CallingConvention.Cdecl,
           ExactSpelling = true, BestFitMapping = false, ThrowOnUnmappableChar = true, CharSet = CharSet.Ansi)]
internal static extern Ns3Status app_tcp_bulk_send(nint sim, nint node,
    [MarshalAs(UnmanagedType.LPStr)] string dstIp, ushort port, uint maxBytes, out nint outApp);
```

### Step 4: High-Level API (new C# class)
```csharp
public static class TcpBulkSend
{
    public static Application Create(Simulation sim, Node node, string dstIp, ushort port, uint maxBytes)
    {
        var status = NativeMethods.app_tcp_bulk_send(sim.Handle, node.NativeHandle, dstIp, port, maxBytes, out nint app);
        Ns3Exception.ThrowIfError(status, sim.Handle, nameof(Create));
        return new Application(sim, new AppHandle(app));
    }
}
```

### Step 5: Tests
Add tests in `InteropTests.cs` (smoke) and `EndToEndTests.cs` (integration scenario).

### Step 6: Example (optional)
Add a new example class in `Examples/` demonstrating the feature.

---

## File Reference Table

| File | Language | Lines | Purpose |
|------|----------|-------|---------|
| `native/include/ns3shim.h` | C | 347 | C ABI declarations: enums, structs, 25 function prototypes |
| `native/src/ns3shim.cpp` | C++ | 757 | Shim implementation: handle management, ns-3 API wrapping |
| `native/CMakeLists.txt` | CMake | 201 | Build config: finds ns-3, links 9 modules, builds shared lib |
| `dotnet/.../Simulation.cs` | C# | 371 | `Simulation`, `Node`, `Device`, `PacketEvent` |
| `dotnet/.../Links.cs` | C# | 207 | `PointToPoint`, `Csma`, `WiFi`, `WiFiStandard` |
| `dotnet/.../Applications.cs` | C# | 216 | `Application`, `UdpEcho`, `FlowMonitor`, `FlowStatistics` |
| `dotnet/.../Interop/NativeMethods.cs` | C# | 245 | All `[DllImport]` declarations + delegates + structs |
| `dotnet/.../Interop/SafeHandles.cs` | C# | 155 | 5 handle types (1 owning, 4 non-owning) |
| `dotnet/.../Interop/NativeLibraryResolver.cs` | C# | 167 | Cross-platform library probe + load |
| `dotnet/.../Interop/Ns3Exception.cs` | C# | 66 | Exception + `ThrowIfError` + `GetLastError` |
| `dotnet/.../PacketFlow.Ns3Adapter.csproj` | XML | 34 | Project: net8.0, Nullable, Unsafe |
| `dotnet/.../Examples/Program.cs` | C# | 59 | CLI dispatcher (p2p/csma/wifi/all) |
| `dotnet/.../Examples/P2PEchoExample.cs` | C# | 103 | 2-node P2P echo with packet tracing |
| `dotnet/.../Examples/CsmaBusExample.cs` | C# | 96 | 4-node CSMA with flow monitor |
| `dotnet/.../Examples/WiFiExample.cs` | C# | 114 | 3 STA + 1 AP Wi-Fi with mobility |
| `dotnet/.../Tests/SimulationLifecycleTests.cs` | C# | 112 | 7 lifecycle tests |
| `dotnet/.../Tests/InteropTests.cs` | C# | 182 | 10 interop smoke tests |
| `dotnet/.../Tests/EndToEndTests.cs` | C# | 127 | 4 integration tests |
| `README.md` | Markdown | 478 | User docs: install, build, API reference |
| `CONTRIBUTING.md` | Markdown | 325 | Developer guide: patterns, code style |
| `build-all.ps1` | PowerShell | 52 | Windows orchestration script |
| `build-all.sh` | Bash | 42 | Linux orchestration script |
| `build-native.ps1` | PowerShell | — | Windows native build |
| `build-native.sh` | Bash | — | Linux native build |
| `pack-nuget.ps1` | PowerShell | — | NuGet packaging |

---

## Summary

The PacketFlow ns-3 Adapter is a carefully designed interoperability layer that allows .NET applications to use the full power of the ns-3 network simulator. Its four-layer architecture (C# API → P/Invoke → C ABI → ns-3 C++) provides:

- **Type safety** through `SafeHandle` wrappers and `readonly record struct` data types
- **Resource safety** through a parent-child ownership model with deterministic cleanup
- **Error safety** through systematic exception-to-error-code translation at the ABI boundary
- **Callback safety** through `GCHandle`-protected delegate marshalling
- **Cross-platform support** through CMake, .NET 8, and a custom native library resolver
- **Extensibility** through a well-documented 6-step process for adding new ns-3 features
- **Test coverage** across lifecycle, interop, and end-to-end scenarios

The adapter currently supports point-to-point, CSMA, and Wi-Fi topologies with UDP echo applications, packet tracing, PCAP export, and FlowMonitor statistics — with a clear path for extending to LTE/5G, LoRaWAN, custom protocols, and visualization.

---

*Document generated from source code analysis of adapter v1.0.0 · 2026-06-26*
