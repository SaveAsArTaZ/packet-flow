// NativeLibraryResolver.cs
// Custom native library loading logic for cross-platform support
//
// Probes for ns3shim shared library in:
// 1. Application directory
// 2. NuGet runtimes/{rid}/native directory
// 3. System library paths
// 4. Custom paths via NS3SHIM_PATH environment variable

using System.Reflection;
using System.Runtime.InteropServices;

namespace PacketFlow.Ns3Adapter.Interop;

/// <summary>
/// Handles native library resolution for ns3shim across platforms
/// </summary>
internal static class NativeLibraryResolver
{
    private static bool _initialized;
    private static readonly object _lock = new();

    /// <summary>
    /// Initialize custom DllImport resolver
    /// </summary>
    public static void Initialize()
    {
        lock (_lock)
        {
            if (_initialized)
                return;

            NativeLibrary.SetDllImportResolver(typeof(NativeLibraryResolver).Assembly, ResolveLibrary);
            _initialized = true;
        }
    }

    /// <summary>
    /// Resolve native library path
    /// </summary>
    private static nint ResolveLibrary(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
    {
        // Only handle our library
        if (libraryName != LibraryName.Ns3Shim)
            return IntPtr.Zero;

        // Try to load from various locations
        var probePaths = GetProbePaths();

        foreach (var path in probePaths)
        {
            if (NativeLibrary.TryLoad(path, out nint handle))
            {
                Console.WriteLine($"[Ns3Adapter] Loaded native library from: {path}");
                return handle;
            }
        }

        // Fallback to default resolution
        if (NativeLibrary.TryLoad(libraryName, assembly, searchPath, out nint defaultHandle))
        {
            Console.WriteLine($"[Ns3Adapter] Loaded native library using default resolution");
            return defaultHandle;
        }

        throw new DllNotFoundException(
            $"Unable to load native library '{libraryName}'. Searched paths:\n" +
            string.Join("\n", probePaths) +
            $"\n\nPlease ensure ns3shim is built and available. " +
            $"You can set NS3SHIM_PATH environment variable to specify a custom path.");
    }

    /// <summary>
    /// Generate list of paths to probe for native library
    /// </summary>
    private static IEnumerable<string> GetProbePaths()
    {
        var paths = new List<string>();
        var libName = GetPlatformLibraryName();

        // 1. Custom path from environment variable
        var customPath = Environment.GetEnvironmentVariable("NS3SHIM_PATH");
        if (!string.IsNullOrEmpty(customPath))
        {
            paths.Add(Path.Combine(customPath, libName));
        }

        // 2. Application directory
        var appDir = AppContext.BaseDirectory;
        paths.Add(Path.Combine(appDir, libName));

        // 3. NuGet runtimes directory
        var rid = GetRuntimeIdentifier();
        paths.Add(Path.Combine(appDir, "runtimes", rid, "native", libName));

        // 4. Relative to assembly location
        var assemblyDir = Path.GetDirectoryName(typeof(NativeLibraryResolver).Assembly.Location);
        if (!string.IsNullOrEmpty(assemblyDir))
        {
            paths.Add(Path.Combine(assemblyDir, libName));
            paths.Add(Path.Combine(assemblyDir, "runtimes", rid, "native", libName));
        }

        // 5. Development build output (relative paths)
        if (!string.IsNullOrEmpty(assemblyDir))
        {
            // From dotnet bin to native build
            var nativeBuildPath = Path.Combine(assemblyDir, "..", "..", "..", "..", "..", "native", "build", libName);
            paths.Add(Path.GetFullPath(nativeBuildPath));
        }

        return paths.Distinct();
    }

    /// <summary>
    /// Get platform-specific library name
    /// </summary>
    private static string GetPlatformLibraryName()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return "ns3shim.dll";
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return "libns3shim.so";
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return "libns3shim.dylib";
        else
            throw new PlatformNotSupportedException($"Unsupported platform: {RuntimeInformation.OSDescription}");
    }

    /// <summary>
    /// Get runtime identifier (RID)
    /// </summary>
    private static string GetRuntimeIdentifier()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return RuntimeInformation.ProcessArchitecture switch
            {
                Architecture.X64 => "win-x64",
                Architecture.X86 => "win-x86",
                Architecture.Arm64 => "win-arm64",
                _ => "win-x64"
            };
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return RuntimeInformation.ProcessArchitecture switch
            {
                Architecture.X64 => "linux-x64",
                Architecture.Arm64 => "linux-arm64",
                _ => "linux-x64"
            };
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return RuntimeInformation.ProcessArchitecture switch
            {
                Architecture.X64 => "osx-x64",
                Architecture.Arm64 => "osx-arm64",
                _ => "osx-x64"
            };
        }

        return "any";
    }
}

