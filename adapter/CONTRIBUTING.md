# Contributing to PacketFlow ns-3 Adapter

Thank you for your interest in contributing! This document provides guidelines for extending and improving the adapter.

## Development Setup

1. **Install Prerequisites** (see README.md)
2. **Clone and Build:**
   ```bash
   git clone <repo-url>
   cd adapter
   ./build-all.sh  # Linux
   .\build-all.ps1  # Windows
   ```

3. **Run Tests:**
   ```bash
   cd dotnet/PacketFlow.Ns3Adapter.Tests
   dotnet test
   ```

## Project Structure

```
adapter/
├── native/                  # C++ shim layer
│   ├── include/
│   │   └── ns3shim.h       # C ABI header
│   ├── src/
│   │   └── ns3shim.cpp     # Implementation
│   └── CMakeLists.txt
│
├── dotnet/
│   ├── PacketFlow.Ns3Adapter/          # Main SDK
│   │   ├── Interop/                     # P/Invoke layer
│   │   │   ├── NativeMethods.cs
│   │   │   ├── SafeHandles.cs
│   │   │   ├── NativeLibraryResolver.cs
│   │   │   └── Ns3Exception.cs
│   │   ├── Simulation.cs                # High-level API
│   │   ├── Links.cs
│   │   └── Applications.cs
│   │
│   ├── PacketFlow.Ns3Adapter.Examples/ # Example programs
│   └── PacketFlow.Ns3Adapter.Tests/    # Unit tests
│
└── README.md
```

## Adding New ns-3 Features

### 1. Define C ABI Function

Add to `native/include/ns3shim.h`:

```c
/// Documentation comment
/// @param sim Simulation handle
/// @param ... Other parameters
/// @return NS3_OK on success
NS3SHIM_API ns3_status my_new_function(ns3_sim sim, ...);
```

**Guidelines:**
- Use only C-compatible types (no C++ classes)
- Return `ns3_status` for error handling
- Use opaque handles for complex objects
- Pass arrays as `type* array, uint32_t count`
- Strings are `const char*` UTF-8

### 2. Implement in C++

Add to `native/src/ns3shim.cpp`:

```cpp
NS3SHIM_API ns3_status my_new_function(ns3_sim sim, ...) {
    if (!ValidateSim(sim)) return NS3_ERR;
    
    try {
        // ns-3 C++ code here
        // ...
        return NS3_OK;
    } catch (const std::exception& e) {
        sim->SetError(std::string("my_new_function failed: ") + e.what());
        return NS3_ERR;
    }
}
```

**Best Practices:**
- Always validate handles first
- Wrap in try/catch
- Store error messages via `sim->SetError()`
- Never let exceptions cross ABI boundary
- Use RAII for ns-3 objects

### 3. Declare P/Invoke

Add to `dotnet/PacketFlow.Ns3Adapter/Interop/NativeMethods.cs`:

```csharp
[DllImport(LibraryName.Ns3Shim, CallingConvention = CallingConvention.Cdecl,
           ExactSpelling = true, BestFitMapping = false, ThrowOnUnmappableChar = true)]
internal static extern Ns3Status my_new_function(nint sim, ...);
```

**Marshalling Rules:**
- `ns3_sim` → `nint`
- `uint32_t` → `uint`
- `const char*` → `[MarshalAs(UnmanagedType.LPStr)] string`
- Arrays → `nint*` (unsafe) or `IntPtr` with manual marshalling
- Callbacks → Delegate types

### 4. Create High-Level API

Add to appropriate class (e.g., `Simulation.cs`, new helper class):

```csharp
/// <summary>
/// XML documentation
/// </summary>
public void MyNewFeature(...)
{
    ThrowIfDisposed();
    var status = NativeMethods.my_new_function(Handle, ...);
    Ns3Exception.ThrowIfError(status, Handle, nameof(MyNewFeature));
}
```

**API Design:**
- Use .NET conventions (PascalCase, properties, etc.)
- Throw exceptions for errors (don't return status codes)
- Accept `TimeSpan` for time, not `double`
- Use LINQ-friendly collections
- Provide XML documentation

### 5. Add Tests

Add to `dotnet/PacketFlow.Ns3Adapter.Tests/`:

```csharp
[Fact]
public void MyNewFeature_ShouldWork()
{
    // Arrange
    using var sim = new Simulation();
    
    // Act
    sim.MyNewFeature(...);
    
    // Assert
    Assert...
}
```

### 6. Add Example

Optionally add to `Examples/` project demonstrating usage.

## Code Style

### C++
- Follow ns-3 coding style
- Use `const` and RAII
- No raw `new`/`delete` in modern code
- Descriptive variable names

### C#
- Follow .NET conventions
- Enable nullable reference types
- Use `var` for obvious types
- Modern C# features (records, pattern matching, etc.)
- XML documentation on public APIs

## Testing

### Unit Tests
```bash
cd dotnet/PacketFlow.Ns3Adapter.Tests
dotnet test --logger "console;verbosity=detailed"
```

### Manual Testing
```bash
cd dotnet/PacketFlow.Ns3Adapter.Examples
dotnet run -- all
```

### Memory Leaks (Linux)
```bash
valgrind --leak-check=full dotnet run
```

## Common Patterns

### Opaque Handles

C side:
```c
typedef struct ns3_thing_t* ns3_thing;

NS3SHIM_API ns3_status thing_create(ns3_sim sim, ns3_thing* out);
NS3SHIM_API ns3_status thing_destroy(ns3_thing thing);
```

C++ side:
```cpp
struct ns3_sim_t {
    std::map<uint64_t, Ptr<Thing>> things;
    uint64_t nextThingId = 1;
};

NS3SHIM_API ns3_status thing_create(ns3_sim sim, ns3_thing* out) {
    uint64_t id = sim->nextThingId++;
    sim->things[id] = CreateObject<Thing>();
    *out = reinterpret_cast<ns3_thing>(id);
    return NS3_OK;
}
```

C# side:
```csharp
internal sealed class ThingHandle : SafeHandleZeroOrMinusOneIsInvalid {
    protected override bool ReleaseHandle() {
        NativeMethods.thing_destroy(handle);
        return true;
    }
}

public sealed class Thing {
    private readonly ThingHandle _handle;
    // ...
}
```

### Callbacks

C side:
```c
typedef void(*my_callback)(void* user, int value);

NS3SHIM_API ns3_status subscribe(ns3_sim sim, my_callback cb, void* user);
```

C++ side:
```cpp
struct CallbackContext {
    my_callback cb;
    void* user;
};

NS3SHIM_API ns3_status subscribe(ns3_sim sim, my_callback cb, void* user) {
    auto* ctx = new CallbackContext{cb, user};
    // Connect to ns-3 trace source
    traceSource->ConnectWithoutContext([ctx](int value) {
        ctx->cb(ctx->user, value);
    });
    return NS3_OK;
}
```

C# side:
```csharp
public void Subscribe(Action<int> callback) {
    var handle = GCHandle.Alloc(callback);
    
    NativeMethods.my_callback nativeCb = (user, value) => {
        var cb = (Action<int>)GCHandle.FromIntPtr(user).Target!;
        cb(value);
    };
    
    var status = NativeMethods.subscribe(Handle, nativeCb, GCHandle.ToIntPtr(handle));
    // Store nativeCb to prevent GC
}
```

## Platform-Specific Code

Use conditional compilation:

```csharp
#if WINDOWS
// Windows-specific
#elif LINUX
// Linux-specific
#else
#error Unsupported platform
#endif
```

Or runtime checks:
```csharp
if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
    // ...
}
```

## Documentation

- **C ABI**: Doxygen-style comments in `.h` file
- **C# API**: XML documentation comments
- **README**: High-level usage and examples
- **CONTRIBUTING**: This file for developers

## Submitting Changes

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/my-feature`)
3. Make changes with clear, atomic commits
4. Add tests
5. Update documentation
6. Run full test suite
7. Submit pull request

## Questions?

- File an issue for bugs/feature requests
- Consult ns-3 documentation for simulator-specific questions
- Check existing code for patterns

---

Happy coding! 🚀

