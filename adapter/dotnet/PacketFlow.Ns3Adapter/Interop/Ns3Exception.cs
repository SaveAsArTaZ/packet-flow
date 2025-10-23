// Ns3Exception.cs
// Exception type for ns-3 errors

using System.Runtime.InteropServices;
using System.Text;

namespace PacketFlow.Ns3Adapter.Interop;

/// <summary>
/// Exception thrown when an ns-3 operation fails
/// </summary>
public class Ns3Exception : Exception
{
    /// <summary>
    /// Creates a new Ns3Exception with the specified message
    /// </summary>
    public Ns3Exception(string message) : base(message)
    {
    }

    /// <summary>
    /// Creates a new Ns3Exception with the specified message and inner exception
    /// </summary>
    public Ns3Exception(string message, Exception innerException) : base(message, innerException)
    {
    }

    /// <summary>
    /// Retrieve last error from simulation handle
    /// </summary>
    internal static unsafe string GetLastError(nint simHandle)
    {
        const int bufferSize = 1024;
        Span<byte> buffer = stackalloc byte[bufferSize];

        fixed (byte* ptr = buffer)
        {
            var status = NativeMethods.ns3_last_error(simHandle, ptr, (nuint)bufferSize);
            if (status == NativeMethods.Ns3Status.Ok)
            {
                // Find null terminator
                int length = 0;
                while (length < bufferSize && buffer[length] != 0)
                    length++;

                return Encoding.UTF8.GetString(buffer[..length]);
            }
        }

        return "Unknown error (failed to retrieve error message)";
    }

    /// <summary>
    /// Throw exception if status indicates error
    /// </summary>
    internal static void ThrowIfError(NativeMethods.Ns3Status status, nint simHandle, string operationName)
    {
        if (status == NativeMethods.Ns3Status.Error)
        {
            var errorMsg = GetLastError(simHandle);
            throw new Ns3Exception($"{operationName} failed: {errorMsg}");
        }
    }
}

