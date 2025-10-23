// WiFiExample.cs
// Wi-Fi STA/AP example with mobility
//
// Topology:
//   STA0, STA1, STA2 <--> AP
//
// Configuration:
// - Standard: 802.11n (2.4 GHz)
// - Data rate: 65 Mbps
// - Channel: 1
// - Mobility: Constant positions
// - STA0 communicates with AP

using PacketFlow.Ns3Adapter;

namespace PacketFlow.Ns3Adapter.Examples;

public static class WiFiExample
{
    public static void Run()
    {
        Console.WriteLine("=== Wi-Fi STA/AP Example ===\n");

        using var sim = new Simulation();
        
        sim.SetSeed(99999);
        
        Console.WriteLine("Creating nodes (3 STAs + 1 AP)...");
        var allNodes = sim.CreateNodes(4);
        var sta0 = allNodes[0];
        var sta1 = allNodes[1];
        var sta2 = allNodes[2];
        var ap = allNodes[3];
        
        var stations = new[] { sta0, sta1, sta2 };
        
        Console.WriteLine("Installing Internet stack...");
        sim.InstallInternetStack(allNodes);
        
        Console.WriteLine("Setting up Wi-Fi network (802.11n, 2.4 GHz)...");
        var (staDevices, apDevice) = WiFi.InstallStationAp(
            sim,
            stations,
            ap,
            WiFiStandard.Std_80211n_2_4GHz,
            "HtMcs7",  // 65 Mbps for 802.11n
            1          // Channel 1
        );
        
        Console.WriteLine("Setting mobility (constant positions)...");
        // Position stations in a line
        sta0.SetPosition(0.0, 0.0, 0.0);
        sta1.SetPosition(10.0, 0.0, 0.0);
        sta2.SetPosition(20.0, 0.0, 0.0);
        // AP in the middle
        ap.SetPosition(10.0, 10.0, 0.0);
        
        Console.WriteLine("Assigning IP addresses...");
        var allDevices = new Device[4];
        Array.Copy(staDevices, 0, allDevices, 0, 3);
        allDevices[3] = apDevice;
        sim.AssignIpv4Addresses(allDevices, "10.1.2.0", "255.255.255.0");
        
        Console.WriteLine("Populating routing tables...");
        sim.PopulateRoutingTables();
        
        Console.WriteLine("Creating UDP Echo server on AP...");
        var server = UdpEcho.CreateServer(sim, ap, 9);
        server.Start(TimeSpan.FromSeconds(1.0));
        server.Stop(TimeSpan.FromSeconds(10.0));
        
        Console.WriteLine("Creating UDP Echo client on STA0...");
        var client = UdpEcho.CreateClient(
            sim,
            sta0,
            "10.1.2.4",  // AP's IP
            9,
            1024,
            TimeSpan.FromSeconds(1.0),
            5
        );
        client.Start(TimeSpan.FromSeconds(2.0));
        client.Stop(TimeSpan.FromSeconds(10.0));
        
        // Track packets
        int txCount = 0, rxCount = 0;
        
        staDevices[0].SubscribeToPacketEvents(
            onTx: evt => {
                txCount++;
                Console.WriteLine($"[{evt.Time.TotalSeconds:F3}s] STA0 TX: {evt.Bytes} bytes");
            },
            onRx: evt => {
                rxCount++;
                Console.WriteLine($"[{evt.Time.TotalSeconds:F3}s] STA0 RX: {evt.Bytes} bytes");
            }
        );
        
        Console.WriteLine("\nRunning simulation for 10 seconds...\n");
        sim.Stop(TimeSpan.FromSeconds(10.0));
        sim.Run();
        
        Console.WriteLine("\n=== Simulation Complete ===");
        Console.WriteLine($"STA0 TX packets: {txCount}");
        Console.WriteLine($"STA0 RX packets: {rxCount}");
        Console.WriteLine($"Final simulation time: {sim.Now.TotalSeconds:F3}s");
        
        if (rxCount > 0)
        {
            Console.WriteLine("\n✓ Success: Wi-Fi communication successful!");
        }
    }
}

