// Program.cs
// Main entry point for ns-3 adapter examples

using PacketFlow.Ns3Adapter.Examples;

Console.WriteLine("PacketFlow ns-3 Adapter Examples");
Console.WriteLine("=================================\n");

if (args.Length == 0)
{
    Console.WriteLine("Usage: PacketFlow.Ns3Adapter.Examples <example>");
    Console.WriteLine("\nAvailable examples:");
    Console.WriteLine("  p2p      - Point-to-point UDP Echo");
    Console.WriteLine("  csma     - CSMA bus with flow monitor");
    Console.WriteLine("  wifi     - Wi-Fi STA/AP network");
    Console.WriteLine("  all      - Run all examples");
    return;
}

try
{
    switch (args[0].ToLowerInvariant())
    {
        case "p2p":
            P2PEchoExample.Run();
            break;
        
        case "csma":
            CsmaBusExample.Run();
            break;
        
        case "wifi":
            WiFiExample.Run();
            break;
        
        case "all":
            P2PEchoExample.Run();
            Console.WriteLine("\n" + new string('=', 60) + "\n");
            CsmaBusExample.Run();
            Console.WriteLine("\n" + new string('=', 60) + "\n");
            WiFiExample.Run();
            break;
        
        default:
            Console.WriteLine($"Unknown example: {args[0]}");
            Console.WriteLine("Use 'p2p', 'csma', 'wifi', or 'all'");
            return;
    }
    
    Console.WriteLine("\nAll examples completed successfully!");
}
catch (Exception ex)
{
    Console.WriteLine($"\n❌ Error: {ex.Message}");
    Console.WriteLine($"\nStack trace:\n{ex.StackTrace}");
    Environment.ExitCode = 1;
}

