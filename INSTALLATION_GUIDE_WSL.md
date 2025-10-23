# PacketFlow ns-3 Adapter - WSL Installation Guide

## Overview

This document describes the installation and setup process for the PacketFlow ns-3 Adapter on Windows 11 using WSL (Windows Subsystem for Linux).

## Stage Summary

**Date:** October 23, 2024  
**Environment:** Windows 11 with WSL2 (Ubuntu)  
**Goal:** Install ns-3 in WSL and successfully build the PacketFlow adapter

---

## Initial Situation

- **Problem:** ns-3 was not installed for the project
- **Decision:** Install ns-3 inside WSL on Windows 11 instead of native Windows
- **Target:** ns-3.41 (as specified in project documentation)

---

## Installation Steps Completed

### 1. Prerequisites Installation

Installed required build tools in WSL Ubuntu:

```bash
sudo apt update
sudo apt install -y build-essential cmake git
sudo apt install -y python3 python3-dev
sudo apt install -y g++ clang
sudo apt install -y pkg-config
```

### 2. .NET 8 SDK Installation

Installed .NET 8 SDK in WSL:

```bash
wget https://dot.net/v1/dotnet-install.sh
chmod +x dotnet-install.sh
./dotnet-install.sh --channel 8.0
```

Added to PATH:
```bash
export DOTNET_ROOT=$HOME/.dotnet
export PATH=$PATH:$HOME/.dotnet:$HOME/.dotnet/tools
```

### 3. ns-3.41 Installation

Cloned and built ns-3.41 in WSL:

```bash
cd ~
git clone https://gitlab.com/nsnam/ns-3-dev.git
cd ns-3-dev
git checkout ns-3.41

# Configure and build
./ns3 configure --enable-examples --enable-tests
./ns3 build
```

**Installation Location:** `/home/artaz/ns-3-dev`  
**Build Output:** `/home/artaz/ns-3-dev/build/lib/`

---

## Issues Encountered & Fixes

### Issue 1: Library Naming Convention

**Problem:**  
CMake couldn't find ns-3 libraries because they were named with build profile suffixes:
- Expected: `libns3.41-core.so`
- Actual: `libns3.41-core-default.so`

**Root Cause:**  
ns-3.41 appends the build profile name (`-default`, `-optimized`, etc.) to library names, but the CMakeLists.txt wasn't searching for these variants.

**Fix:**  
Updated `adapter/native/CMakeLists.txt` (lines 95-110) to include build profile suffixes in library search:

```cmake
find_library(NS3_${MODULE}_LIB
    NAMES 
        ns3-${MODULE}
        ns3.41-${MODULE}
        ns3.41-${MODULE}-default      # Added
        ns3.41-${MODULE}-optimized    # Added
        ns3.42-${MODULE}
        ns3.42-${MODULE}-default      # Added
        ns3.42-${MODULE}-optimized    # Added
        ns3.43-${MODULE}
        ns3.43-${MODULE}-default      # Added
        ns3.43-${MODULE}-optimized    # Added
        libns3-${MODULE}
    PATHS ${NS3_LIB_DIR}
    NO_DEFAULT_PATH
)
```

**Result:**  
✅ All 9 required ns-3 modules found successfully:
- core, network, internet, point-to-point, csma, wifi, mobility, applications, flow-monitor

### Issue 2: Namespace Mismatch for ns3_sim_t

**Problem:**  
Compilation errors about incomplete type `struct ns3_sim_t`:
```
error: invalid use of incomplete type 'struct ns3_sim_t'
```

**Root Cause:**  
The `ns3_sim_t` structure was defined inside an anonymous namespace in `ns3shim.cpp`, but the header file had a forward declaration in the global namespace. C++ treated these as different types.

**Fix:**  
Moved `ns3_sim_t` definition from anonymous namespace to global namespace in `adapter/native/src/ns3shim.cpp` (line 40):

```cpp
// Before (line 42 in anonymous namespace):
namespace {
    struct ns3_sim_t {
        // ...
    };
}

// After (line 40 in global namespace):
struct ns3_sim_t {
    // Handle maps
    std::map<uint64_t, Ptr<Node>> nodes;
    std::map<uint64_t, Ptr<NetDevice>> devices;
    // ... rest of structure
};

namespace {
    // Other internal structures
}
```

**Result:**  
✅ Compiler could now access `ns3_sim_t` members correctly

### Issue 3: Callback Lambda Incompatibility

**Problem:**  
ns-3's `TraceConnectWithoutContext` couldn't accept C++ lambdas:
```
error: cannot convert 'lambda' to 'const ns3::CallbackBase&'
```

**Root Cause:**  
ns-3 requires proper `Callback` objects created with `MakeCallback` or `MakeBoundCallback`, not raw C++ lambdas.

**Fix:**  
Created helper functions and used `MakeBoundCallback` in `adapter/native/src/ns3shim.cpp`:

```cpp
// Added helper functions (lines 148-157):
void PacketTxCallback(PacketTraceContext* ctx, Ptr<const Packet> packet) {
    double now = Simulator::Now().GetSeconds();
    ctx->onTx(ctx->user, ctx->deviceId, now, packet->GetSize());
}

void PacketRxCallback(PacketTraceContext* ctx, Ptr<const Packet> packet) {
    double now = Simulator::Now().GetSeconds();
    ctx->onRx(ctx->user, ctx->deviceId, now, packet->GetSize());
}

// Updated trace registration (lines 627-637):
if (onTx) {
    device->GetObject<PointToPointNetDevice>()->TraceConnectWithoutContext(
        "PhyTxEnd",
        MakeBoundCallback(&PacketTxCallback, ctx)  // Instead of lambda
    );
}
```

**Result:**  
✅ Callbacks properly registered with ns-3 tracing system

---

## Build Results

### Native Library Build
```
[ 50%] Building CXX object CMakeFiles/ns3shim.dir/src/ns3shim.cpp.o
[100%] Linking CXX shared library libns3shim.so
[100%] Built target ns3shim
```

**Output:** `/mnt/c/Users/nooba/Desktop/packet-flow/packet-flow/adapter/native/build/libns3shim.so`

### .NET SDK Build
```
PacketFlow.Ns3Adapter -> bin/Release/net8.0/PacketFlow.Ns3Adapter.dll
PacketFlow.Ns3Adapter.Examples -> bin/Release/net8.0/PacketFlow.Ns3Adapter.Examples.dll
PacketFlow.Ns3Adapter.Tests -> bin/Release/net8.0/PacketFlow.Ns3Adapter.Tests.dll

Build succeeded.
    0 Warning(s)
    0 Error(s)
```

### Example Execution
Successfully ran the Point-to-Point UDP Echo example:
```
Running simulation for 10 seconds...

[TX] Device 1 at 2.002s: 1054 bytes
[TX] Device 1 at 2.007s: 1054 bytes
[TX] Device 1 at 3.002s: 1054 bytes
...
Final simulation time: 10.000s
```

---

## Files Modified

1. **`adapter/native/CMakeLists.txt`**
   - Added support for ns-3 library naming with build profiles
   - Lines 95-110 modified

2. **`adapter/native/src/ns3shim.cpp`**
   - Moved `ns3_sim_t` to global namespace (line 40)
   - Added callback helper functions (lines 148-157)
   - Updated packet event tracing to use `MakeBoundCallback` (lines 627-637)

---

## How to Build (WSL)

### Set Environment Variables
```bash
export DOTNET_ROOT=$HOME/.dotnet
export PATH=$HOME/.dotnet:$HOME/.dotnet/tools:$PATH
export LD_LIBRARY_PATH=/mnt/c/Users/nooba/Desktop/packet-flow/packet-flow/adapter/native/build:$HOME/ns-3-dev/build/lib:$LD_LIBRARY_PATH
```

### Build Native Library
```bash
cd /mnt/c/Users/nooba/Desktop/packet-flow/packet-flow/adapter/native/build
cmake .. -DNS3_DIR=$HOME/ns-3-dev -DCMAKE_BUILD_TYPE=Release
cmake --build .
```

### Build .NET SDK
```bash
cd /mnt/c/Users/nooba/Desktop/packet-flow/packet-flow/adapter/dotnet
dotnet build -c Release
```

### Run Examples
```bash
cd /mnt/c/Users/nooba/Desktop/packet-flow/packet-flow/adapter/dotnet/PacketFlow.Ns3Adapter.Examples
dotnet run -c Release -- p2p    # Point-to-Point example
dotnet run -c Release -- csma   # CSMA example
dotnet run -c Release -- wifi   # WiFi example
dotnet run -c Release -- all    # All examples
```

---

## Verification Checklist

- [x] ns-3.41 installed in WSL
- [x] ns-3 builds successfully
- [x] ns-3 hello-simulator example runs
- [x] CMake finds all required ns-3 modules
- [x] Native library (libns3shim.so) compiles without errors
- [x] .NET SDK compiles without errors
- [x] Examples compile and run
- [x] Packet tracing callbacks work

---

## Key Learnings

1. **ns-3 Library Naming:** ns-3 build profiles affect library names. Always check actual library names in `build/lib/` directory.

2. **C++ Namespace Matching:** Forward declarations must match actual definitions in namespace scope for C ABI boundaries.

3. **ns-3 Callbacks:** ns-3's callback system requires proper `Callback` objects, not raw C++ lambdas. Use `MakeCallback` or `MakeBoundCallback`.

4. **WSL File Access:** Windows files are accessible at `/mnt/c/` in WSL, but performance is better when using native WSL filesystem.

5. **Cross-Platform Paths:** Be careful with path differences between Windows (`C:\`) and WSL (`/home/user/`).

---

## Next Steps

1. Run comprehensive tests: `dotnet test` in the Tests project
2. Try all examples to verify complete functionality
3. Consider adding ns-3 path to permanent WSL environment variables
4. Explore creating custom simulations using the adapter

---

## Troubleshooting

### If libraries are not found:
```bash
# Verify ns-3 libraries exist
ls -la ~/ns-3-dev/build/lib/ | grep libns3

# Check library naming pattern
find ~/ns-3-dev/build/lib -name "*libns3.41-core*"
```

### If .NET is not found:
```bash
# Verify .NET installation
~/.dotnet/dotnet --version

# Add to current session
export DOTNET_ROOT=$HOME/.dotnet
export PATH=$HOME/.dotnet:$PATH
```

### If simulation crashes:
```bash
# Ensure all libraries are in LD_LIBRARY_PATH
export LD_LIBRARY_PATH=$HOME/ns-3-dev/build/lib:/path/to/adapter/native/build:$LD_LIBRARY_PATH

# Run with verbose logging
NS3SHIM_DEBUG=1 dotnet run -- p2p
```

---

## References

- [ns-3 Documentation](https://www.nsnam.org/documentation/)
- [ns-3.41 Release Notes](https://www.nsnam.org/releases/ns-3-41/)
- [.NET on Linux](https://learn.microsoft.com/en-us/dotnet/core/install/linux)
- [WSL Documentation](https://learn.microsoft.com/en-us/windows/wsl/)

---

**Status:** ✅ Installation Complete and Verified  
**Last Updated:** October 23, 2024

