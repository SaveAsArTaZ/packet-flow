// CsmaBusExample.cs
// CSMA (Ethernet) bus example with flow monitor
//
// Topology:
//   Node0 -- Node1 -- Node2 -- Node3
//   (all on CSMA bus)
//
// Configuration:
// - Data rate: 100 Mbps
// - Delay: 6560 ns
// - Network: 192.168.1.0/24
// - Node 0 sends to Node 3
// - FlowMonitor collects statistics

using PacketFlow.Ns3Adapter;

namespace PacketFlow.Ns3Adapter.Examples;

public static class CsmaBusExample
{
    public static void Run()
    {
        Console.WriteLine("=== CSMA Bus Example with Flow Monitor ===\n");

        using var sim = new Simulation();
        
        // Set random seed
        sim.SetSeed(54321);
        
        Console.WriteLine("Creating 4 nodes...");
        var nodes = sim.CreateNodes(4);
        
        Console.WriteLine("Installing Internet stack...");
        sim.InstallInternetStack(nodes);
        
        Console.WriteLine("Creating CSMA bus...");
        var devices = Csma.Install(sim, nodes, "100Mbps", "6560ns");
        
        Console.WriteLine("Assigning IP addresses...");
        sim.AssignIpv4Addresses(devices, "192.168.1.0", "255.255.255.0");
        
        Console.WriteLine("Installing flow monitor...");
        var flowMon = FlowMonitor.InstallAll(sim);
        
        Console.WriteLine("Creating UDP Echo server on node 3...");
        var server = UdpEcho.CreateServer(sim, nodes[3], 9);
        server.Start(TimeSpan.FromSeconds(1.0));
        server.Stop(TimeSpan.FromSeconds(10.0));
        
        Console.WriteLine("Creating UDP Echo client on node 0...");
        var client = UdpEcho.CreateClient(
            sim,
            nodes[0],
            "192.168.1.4",  // Node 3's IP (base .1 + offset 3)
            9,
            512,            // Smaller packets
            TimeSpan.FromSeconds(0.5),  // More frequent
            10              // More packets
        );
        client.Start(TimeSpan.FromSeconds(2.0));
        client.Stop(TimeSpan.FromSeconds(10.0));
        
        // Subscribe to events on first device
        int packetCount = 0;
        devices[0].SubscribeToPacketEvents(
            onTx: evt => {
                packetCount++;
                Console.WriteLine($"[{evt.Time.TotalSeconds:F3}s] Node 0 TX: {evt.Bytes} bytes");
            },
            onRx: null
        );
        
        Console.WriteLine("\nRunning simulation for 10 seconds...\n");
        sim.Stop(TimeSpan.FromSeconds(10.0));
        sim.Run();
        
        // Collect flow statistics
        Console.WriteLine("\n=== Flow Monitor Statistics ===");
        var stats = flowMon.CollectStatistics();
        
        Console.WriteLine($"Total flows: {stats.FlowCount}");
        Console.WriteLine($"TX packets: {stats.TxPackets}, bytes: {stats.TxBytes}");
        Console.WriteLine($"RX packets: {stats.RxPackets}, bytes: {stats.RxBytes}");
        Console.WriteLine($"Packet loss: {stats.PacketLossRatio * 100:F2}%");
        Console.WriteLine($"Average delay: {stats.AverageDelay.TotalMilliseconds:F3} ms");
        Console.WriteLine($"Average jitter: {stats.AverageJitter.TotalMilliseconds:F3} ms");
        Console.WriteLine($"Final simulation time: {sim.Now.TotalSeconds:F3}s");
        
        if (stats.RxPackets > 0)
        {
            Console.WriteLine("\n✓ Success: Traffic flowed through CSMA bus!");
        }
    }
}

