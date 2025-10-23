// P2PEchoExample.cs
// Point-to-point UDP Echo example
//
// Topology:
//   Node0 -------- Node1
//    (Client)      (Server)
//
// Configuration:
// - Data rate: 5 Mbps
// - Delay: 2 ms
// - Network: 10.1.1.0/24
// - Server port: 9
// - Client sends 5 packets of 1024 bytes every 1 second

using PacketFlow.Ns3Adapter;

namespace PacketFlow.Ns3Adapter.Examples;

public static class P2PEchoExample
{
    public static void Run()
    {
        Console.WriteLine("=== Point-to-Point UDP Echo Example ===\n");

        using var sim = new Simulation();
        
        // Set random seed for reproducibility
        sim.SetSeed(12345);
        
        Console.WriteLine("Creating nodes...");
        var nodes = sim.CreateNodes(2);
        var node0 = nodes[0];
        var node1 = nodes[1];
        
        Console.WriteLine("Installing Internet stack...");
        sim.InstallInternetStack(node0, node1);
        
        Console.WriteLine("Creating point-to-point link...");
        var (dev0, dev1) = PointToPoint.Install(sim, node0, node1, "5Mbps", "2ms");
        
        Console.WriteLine("Assigning IP addresses...");
        sim.AssignIpv4Addresses(new[] { dev0, dev1 }, "10.1.1.0", "255.255.255.0");
        
        Console.WriteLine("Creating UDP Echo server on node 1...");
        var server = UdpEcho.CreateServer(sim, node1, 9);
        server.Start(TimeSpan.FromSeconds(1.0));
        server.Stop(TimeSpan.FromSeconds(10.0));
        
        Console.WriteLine("Creating UDP Echo client on node 0...");
        var client = UdpEcho.CreateClient(
            sim,
            node0,
            "10.1.1.2",  // Server IP
            9,           // Server port
            1024,        // Packet size
            TimeSpan.FromSeconds(1.0),  // Interval
            5            // Max packets
        );
        client.Start(TimeSpan.FromSeconds(2.0));
        client.Stop(TimeSpan.FromSeconds(10.0));
        
        // Subscribe to packet events for tracing
        int txCount = 0, rxCount = 0;
        ulong txBytes = 0, rxBytes = 0;
        
        Console.WriteLine("Subscribing to packet events...");
        dev0.SubscribeToPacketEvents(
            onTx: evt => {
                txCount++;
                txBytes += evt.Bytes;
                Console.WriteLine($"[TX] Device {evt.DeviceId} at {evt.Time.TotalSeconds:F3}s: {evt.Bytes} bytes");
            },
            onRx: evt => {
                rxCount++;
                rxBytes += evt.Bytes;
                Console.WriteLine($"[RX] Device {evt.DeviceId} at {evt.Time.TotalSeconds:F3}s: {evt.Bytes} bytes");
            }
        );
        
        // Enable PCAP tracing
        Console.WriteLine("Enabling PCAP traces...");
        dev0.EnablePcap("p2p-echo-0");
        dev1.EnablePcap("p2p-echo-1");
        
        Console.WriteLine("\nRunning simulation for 10 seconds...\n");
        sim.Stop(TimeSpan.FromSeconds(10.0));
        sim.Run();
        
        Console.WriteLine("\n=== Simulation Complete ===");
        Console.WriteLine($"Total TX packets: {txCount}, bytes: {txBytes}");
        Console.WriteLine($"Total RX packets: {rxCount}, bytes: {rxBytes}");
        Console.WriteLine($"Final simulation time: {sim.Now.TotalSeconds:F3}s");
        
        if (rxCount > 0)
        {
            Console.WriteLine("\n✓ Success: Packets were transmitted and received!");
        }
        else
        {
            Console.WriteLine("\n✗ Warning: No packets received. Check configuration.");
        }
    }
}

