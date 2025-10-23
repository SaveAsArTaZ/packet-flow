// ns3shim.cpp
// Implementation of C ABI shim for ns-3
//
// This file wraps ns-3's C++ API into a pure C interface suitable for P/Invoke.
// 
// Architecture:
// - ns3_sim_t holds per-simulation context (nodes, devices, apps, error state)
// - All Ptr<T> are stored in maps keyed by opaque handle values
// - Callbacks marshal through C function pointers with void* user data
// - Thread safety: ns-3 is single-threaded; callbacks fire on scheduler thread

#define NS3SHIM_EXPORTS
#include "ns3shim.h"

#include <ns3/core-module.h>
#include <ns3/network-module.h>
#include <ns3/internet-module.h>
#include <ns3/point-to-point-module.h>
#include <ns3/csma-module.h>
#include <ns3/wifi-module.h>
#include <ns3/mobility-module.h>
#include <ns3/applications-module.h>
#include <ns3/flow-monitor-module.h>

#include <map>
#include <vector>
#include <string>
#include <sstream>
#include <cstring>
#include <atomic>
#include <mutex>

using namespace ns3;

// ============================================================================
// Internal Structures
// ============================================================================

namespace {

/// Per-simulation context
struct ns3_sim_t {
    // Handle maps
    std::map<uint64_t, Ptr<Node>> nodes;
    std::map<uint64_t, Ptr<NetDevice>> devices;
    std::map<uint64_t, Ptr<Application>> apps;
    std::map<uint64_t, Ptr<FlowMonitor>> flowMons;
    
    // Helpers (stateful objects reused for configuration)
    InternetStackHelper internetStack;
    Ipv4AddressHelper ipv4Helper;
    
    // State
    std::atomic<bool> isRunning{false};
    std::string lastError;
    std::mutex errorMutex;
    
    // ID generators
    uint64_t nextNodeId = 1;
    uint64_t nextDeviceId = 1;
    uint64_t nextAppId = 1;
    uint64_t nextFlowMonId = 1;
    
    // Utility
    void SetError(const std::string& msg) {
        std::lock_guard<std::mutex> lock(errorMutex);
        lastError = msg;
    }
    
    std::string GetError() const {
        std::lock_guard<std::mutex> lock(const_cast<std::mutex&>(errorMutex));
        return lastError;
    }
};

// Opaque handle type definitions
struct ns3_node_t { uint64_t id; };
struct ns3_device_t { uint64_t id; };
struct ns3_app_t { uint64_t id; };
struct ns3_flowmon_t { uint64_t id; };

// Helper to convert handle to ID
inline uint64_t HandleToId(ns3_node node) { return reinterpret_cast<uint64_t>(node); }
inline uint64_t HandleToId(ns3_device dev) { return reinterpret_cast<uint64_t>(dev); }
inline uint64_t HandleToId(ns3_app app) { return reinterpret_cast<uint64_t>(app); }
inline uint64_t HandleToId(ns3_flowmon fm) { return reinterpret_cast<uint64_t>(fm); }

// Helper to convert ID to handle
inline ns3_node IdToNodeHandle(uint64_t id) { return reinterpret_cast<ns3_node>(id); }
inline ns3_device IdToDeviceHandle(uint64_t id) { return reinterpret_cast<ns3_device>(id); }
inline ns3_app IdToAppHandle(uint64_t id) { return reinterpret_cast<ns3_app>(id); }
inline ns3_flowmon IdToFlowMonHandle(uint64_t id) { return reinterpret_cast<ns3_flowmon>(id); }

// Validate simulation handle
bool ValidateSim(ns3_sim sim) {
    return sim != nullptr;
}

// Lookup helpers with error handling
Ptr<Node> GetNode(ns3_sim sim, ns3_node node) {
    if (!sim || !node) return nullptr;
    auto it = sim->nodes.find(HandleToId(node));
    if (it == sim->nodes.end()) {
        sim->SetError("Invalid node handle");
        return nullptr;
    }
    return it->second;
}

Ptr<NetDevice> GetDevice(ns3_sim sim, ns3_device dev) {
    if (!sim || !dev) return nullptr;
    auto it = sim->devices.find(HandleToId(dev));
    if (it == sim->devices.end()) {
        sim->SetError("Invalid device handle");
        return nullptr;
    }
    return it->second;
}

Ptr<Application> GetApp(ns3_sim sim, ns3_app app) {
    if (!sim || !app) return nullptr;
    auto it = sim->apps.find(HandleToId(app));
    if (it == sim->apps.end()) {
        sim->SetError("Invalid application handle");
        return nullptr;
    }
    return it->second;
}

Ptr<FlowMonitor> GetFlowMon(ns3_sim sim, ns3_flowmon fm) {
    if (!sim || !fm) return nullptr;
    auto it = sim->flowMons.find(HandleToId(fm));
    if (it == sim->flowMons.end()) {
        sim->SetError("Invalid flow monitor handle");
        return nullptr;
    }
    return it->second;
}

// Callback context for packet traces
struct PacketTraceContext {
    ns3_pkt_cb onTx;
    ns3_pkt_cb onRx;
    void* user;
    uint64_t deviceId;
};

} // anonymous namespace

// ============================================================================
// Error Handling
// ============================================================================

NS3SHIM_API ns3_status ns3_last_error(ns3_sim sim, char* buf, size_t len) {
    if (!buf || len == 0) return NS3_ERR;
    
    std::string msg = sim ? sim->GetError() : "No simulation context";
    size_t copyLen = std::min(msg.size(), len - 1);
    std::memcpy(buf, msg.c_str(), copyLen);
    buf[copyLen] = '\0';
    
    return NS3_OK;
}

// ============================================================================
// Simulation Lifecycle
// ============================================================================

NS3SHIM_API ns3_status sim_create(ns3_sim* outSim) {
    if (!outSim) return NS3_ERR;
    
    try {
        *outSim = new ns3_sim_t();
        return NS3_OK;
    } catch (const std::exception& e) {
        return NS3_ERR;
    }
}

NS3SHIM_API ns3_status sim_set_seed(ns3_sim sim, uint32_t seed) {
    if (!ValidateSim(sim)) return NS3_ERR;
    
    try {
        RngSeedManager::SetSeed(seed);
        return NS3_OK;
    } catch (const std::exception& e) {
        sim->SetError(std::string("sim_set_seed failed: ") + e.what());
        return NS3_ERR;
    }
}

NS3SHIM_API ns3_status sim_run(ns3_sim sim) {
    if (!ValidateSim(sim)) return NS3_ERR;
    
    try {
        sim->isRunning = true;
        Simulator::Run();
        sim->isRunning = false;
        return NS3_OK;
    } catch (const std::exception& e) {
        sim->isRunning = false;
        sim->SetError(std::string("sim_run failed: ") + e.what());
        return NS3_ERR;
    }
}

NS3SHIM_API ns3_status sim_stop(ns3_sim sim, double atTimeSec) {
    if (!ValidateSim(sim)) return NS3_ERR;
    
    try {
        Simulator::Stop(Seconds(atTimeSec));
        return NS3_OK;
    } catch (const std::exception& e) {
        sim->SetError(std::string("sim_stop failed: ") + e.what());
        return NS3_ERR;
    }
}

NS3SHIM_API ns3_status sim_is_running(ns3_sim sim, int* outIsRunning) {
    if (!ValidateSim(sim) || !outIsRunning) return NS3_ERR;
    
    *outIsRunning = sim->isRunning ? 1 : 0;
    return NS3_OK;
}

NS3SHIM_API ns3_status sim_now(ns3_sim sim, double* outTimeSec) {
    if (!ValidateSim(sim) || !outTimeSec) return NS3_ERR;
    
    try {
        *outTimeSec = Simulator::Now().GetSeconds();
        return NS3_OK;
    } catch (const std::exception& e) {
        sim->SetError(std::string("sim_now failed: ") + e.what());
        return NS3_ERR;
    }
}

NS3SHIM_API ns3_status sim_schedule(ns3_sim sim, double inSeconds, ns3_void_cb cb, void* user) {
    if (!ValidateSim(sim) || !cb) return NS3_ERR;
    
    try {
        Simulator::Schedule(Seconds(inSeconds), [cb, user]() {
            cb(user);
        });
        return NS3_OK;
    } catch (const std::exception& e) {
        sim->SetError(std::string("sim_schedule failed: ") + e.what());
        return NS3_ERR;
    }
}

NS3SHIM_API ns3_status sim_destroy(ns3_sim sim) {
    if (!sim) return NS3_OK; // NULL-safe, idempotent
    
    try {
        // Clean up ns-3 state
        Simulator::Destroy();
        delete sim;
        return NS3_OK;
    } catch (...) {
        // Best effort cleanup
        delete sim;
        return NS3_ERR;
    }
}

// ============================================================================
// Nodes & Topology
// ============================================================================

NS3SHIM_API ns3_status nodes_create(ns3_sim sim, uint32_t count, ns3_node* outArray) {
    if (!ValidateSim(sim) || !outArray || count == 0) return NS3_ERR;
    
    try {
        NodeContainer nodes;
        nodes.Create(count);
        
        for (uint32_t i = 0; i < count; ++i) {
            uint64_t id = sim->nextNodeId++;
            sim->nodes[id] = nodes.Get(i);
            outArray[i] = IdToNodeHandle(id);
        }
        
        return NS3_OK;
    } catch (const std::exception& e) {
        sim->SetError(std::string("nodes_create failed: ") + e.what());
        return NS3_ERR;
    }
}

NS3SHIM_API ns3_status internet_install(ns3_sim sim, const ns3_node* nodes, uint32_t count) {
    if (!ValidateSim(sim) || !nodes || count == 0) return NS3_ERR;
    
    try {
        NodeContainer nc;
        for (uint32_t i = 0; i < count; ++i) {
            Ptr<Node> node = GetNode(sim, nodes[i]);
            if (!node) return NS3_ERR;
            nc.Add(node);
        }
        
        sim->internetStack.Install(nc);
        return NS3_OK;
    } catch (const std::exception& e) {
        sim->SetError(std::string("internet_install failed: ") + e.what());
        return NS3_ERR;
    }
}

// ============================================================================
// Network Devices & Links
// ============================================================================

NS3SHIM_API ns3_status p2p_install(ns3_sim sim, ns3_node a, ns3_node b,
                                   const char* dataRate, const char* delay, uint32_t mtu,
                                   ns3_device* outDevA, ns3_device* outDevB) {
    if (!ValidateSim(sim) || !a || !b || !dataRate || !delay || !outDevA || !outDevB) return NS3_ERR;
    
    try {
        Ptr<Node> nodeA = GetNode(sim, a);
        Ptr<Node> nodeB = GetNode(sim, b);
        if (!nodeA || !nodeB) return NS3_ERR;
        
        PointToPointHelper p2p;
        p2p.SetDeviceAttribute("DataRate", StringValue(dataRate));
        p2p.SetChannelAttribute("Delay", StringValue(delay));
        p2p.SetDeviceAttribute("Mtu", UintegerValue(mtu));
        
        NodeContainer nc(nodeA, nodeB);
        NetDeviceContainer devices = p2p.Install(nc);
        
        uint64_t idA = sim->nextDeviceId++;
        uint64_t idB = sim->nextDeviceId++;
        
        sim->devices[idA] = devices.Get(0);
        sim->devices[idB] = devices.Get(1);
        
        *outDevA = IdToDeviceHandle(idA);
        *outDevB = IdToDeviceHandle(idB);
        
        return NS3_OK;
    } catch (const std::exception& e) {
        sim->SetError(std::string("p2p_install failed: ") + e.what());
        return NS3_ERR;
    }
}

NS3SHIM_API ns3_status csma_install(ns3_sim sim, const ns3_node* nodes, uint32_t count,
                                    const char* dataRate, const char* delay,
                                    ns3_device* outDevices) {
    if (!ValidateSim(sim) || !nodes || count == 0 || !dataRate || !delay || !outDevices) return NS3_ERR;
    
    try {
        NodeContainer nc;
        for (uint32_t i = 0; i < count; ++i) {
            Ptr<Node> node = GetNode(sim, nodes[i]);
            if (!node) return NS3_ERR;
            nc.Add(node);
        }
        
        CsmaHelper csma;
        csma.SetChannelAttribute("DataRate", StringValue(dataRate));
        csma.SetChannelAttribute("Delay", StringValue(delay));
        
        NetDeviceContainer devices = csma.Install(nc);
        
        for (uint32_t i = 0; i < count; ++i) {
            uint64_t id = sim->nextDeviceId++;
            sim->devices[id] = devices.Get(i);
            outDevices[i] = IdToDeviceHandle(id);
        }
        
        return NS3_OK;
    } catch (const std::exception& e) {
        sim->SetError(std::string("csma_install failed: ") + e.what());
        return NS3_ERR;
    }
}

NS3SHIM_API ns3_status wifi_install_sta_ap(ns3_sim sim, const ns3_node* stas, uint32_t staCount, ns3_node ap,
                                           int phyStandard, const char* dataRate, int channelNumber,
                                           ns3_device* outStaDevices, ns3_device* outApDevice) {
    if (!ValidateSim(sim) || !stas || staCount == 0 || !ap || !dataRate || !outStaDevices || !outApDevice) return NS3_ERR;
    
    try {
        NodeContainer staNodes;
        for (uint32_t i = 0; i < staCount; ++i) {
            Ptr<Node> node = GetNode(sim, stas[i]);
            if (!node) return NS3_ERR;
            staNodes.Add(node);
        }
        
        Ptr<Node> apNode = GetNode(sim, ap);
        if (!apNode) return NS3_ERR;
        
        // Create Wi-Fi channel
        YansWifiChannelHelper channel = YansWifiChannelHelper::Default();
        YansWifiPhyHelper phy;
        phy.SetChannel(channel.Create());
        
        // Wi-Fi helper
        WifiHelper wifi;
        WifiMacHelper mac;
        
        // Set standard
        switch (phyStandard) {
            case 0: wifi.SetStandard(WIFI_STANDARD_80211a); break;
            case 1: wifi.SetStandard(WIFI_STANDARD_80211b); break;
            case 2: wifi.SetStandard(WIFI_STANDARD_80211g); break;
            case 3: wifi.SetStandard(WIFI_STANDARD_80211n); break;
            case 4: wifi.SetStandard(WIFI_STANDARD_80211n); break;
            case 5: wifi.SetStandard(WIFI_STANDARD_80211ac); break;
            default: wifi.SetStandard(WIFI_STANDARD_80211n); break;
        }
        
        wifi.SetRemoteStationManager("ns3::ConstantRateWifiManager",
                                     "DataMode", StringValue(dataRate),
                                     "ControlMode", StringValue(dataRate));
        
        // Configure SSID
        Ssid ssid = Ssid("ns3-wifi");
        
        // Install STA devices
        mac.SetType("ns3::StaWifiMac",
                   "Ssid", SsidValue(ssid),
                   "ActiveProbing", BooleanValue(false));
        NetDeviceContainer staDevices = wifi.Install(phy, mac, staNodes);
        
        // Install AP device
        mac.SetType("ns3::ApWifiMac",
                   "Ssid", SsidValue(ssid));
        NetDeviceContainer apDevices = wifi.Install(phy, mac, apNode);
        
        // Store devices
        for (uint32_t i = 0; i < staCount; ++i) {
            uint64_t id = sim->nextDeviceId++;
            sim->devices[id] = staDevices.Get(i);
            outStaDevices[i] = IdToDeviceHandle(id);
        }
        
        uint64_t apId = sim->nextDeviceId++;
        sim->devices[apId] = apDevices.Get(0);
        *outApDevice = IdToDeviceHandle(apId);
        
        return NS3_OK;
    } catch (const std::exception& e) {
        sim->SetError(std::string("wifi_install_sta_ap failed: ") + e.what());
        return NS3_ERR;
    }
}

// ============================================================================
// Mobility
// ============================================================================

NS3SHIM_API ns3_status mobility_set_constant_position(ns3_sim sim, ns3_node node, double x, double y, double z) {
    if (!ValidateSim(sim) || !node) return NS3_ERR;
    
    try {
        Ptr<Node> n = GetNode(sim, node);
        if (!n) return NS3_ERR;
        
        MobilityHelper mobility;
        mobility.SetMobilityModel("ns3::ConstantPositionMobilityModel");
        mobility.Install(n);
        
        Ptr<MobilityModel> mobModel = n->GetObject<MobilityModel>();
        if (mobModel) {
            mobModel->SetPosition(Vector(x, y, z));
        }
        
        return NS3_OK;
    } catch (const std::exception& e) {
        sim->SetError(std::string("mobility_set_constant_position failed: ") + e.what());
        return NS3_ERR;
    }
}

// ============================================================================
// IP Addressing & Routing
// ============================================================================

NS3SHIM_API ns3_status ipv4_assign(ns3_sim sim, const ns3_device* devices, uint32_t count,
                                   const char* networkBase, const char* mask) {
    if (!ValidateSim(sim) || !devices || count == 0 || !networkBase || !mask) return NS3_ERR;
    
    try {
        NetDeviceContainer devContainer;
        for (uint32_t i = 0; i < count; ++i) {
            Ptr<NetDevice> dev = GetDevice(sim, devices[i]);
            if (!dev) return NS3_ERR;
            devContainer.Add(dev);
        }
        
        sim->ipv4Helper.SetBase(networkBase, mask);
        sim->ipv4Helper.Assign(devContainer);
        
        return NS3_OK;
    } catch (const std::exception& e) {
        sim->SetError(std::string("ipv4_assign failed: ") + e.what());
        return NS3_ERR;
    }
}

NS3SHIM_API ns3_status ipv4_populate_routing_tables(ns3_sim sim) {
    if (!ValidateSim(sim)) return NS3_ERR;
    
    try {
        Ipv4GlobalRoutingHelper::PopulateRoutingTables();
        return NS3_OK;
    } catch (const std::exception& e) {
        sim->SetError(std::string("ipv4_populate_routing_tables failed: ") + e.what());
        return NS3_ERR;
    }
}

// ============================================================================
// Applications
// ============================================================================

NS3SHIM_API ns3_status app_udpecho_server(ns3_sim sim, ns3_node node, uint16_t port, ns3_app* outApp) {
    if (!ValidateSim(sim) || !node || !outApp) return NS3_ERR;
    
    try {
        Ptr<Node> n = GetNode(sim, node);
        if (!n) return NS3_ERR;
        
        UdpEchoServerHelper server(port);
        ApplicationContainer apps = server.Install(n);
        
        uint64_t id = sim->nextAppId++;
        sim->apps[id] = apps.Get(0);
        *outApp = IdToAppHandle(id);
        
        return NS3_OK;
    } catch (const std::exception& e) {
        sim->SetError(std::string("app_udpecho_server failed: ") + e.what());
        return NS3_ERR;
    }
}

NS3SHIM_API ns3_status app_udpecho_client(ns3_sim sim, ns3_node node, const char* dstIp, uint16_t port,
                                          uint32_t packetSize, double intervalSec, uint32_t maxPackets, ns3_app* outApp) {
    if (!ValidateSim(sim) || !node || !dstIp || !outApp) return NS3_ERR;
    
    try {
        Ptr<Node> n = GetNode(sim, node);
        if (!n) return NS3_ERR;
        
        UdpEchoClientHelper client(Ipv4Address(dstIp), port);
        client.SetAttribute("MaxPackets", UintegerValue(maxPackets));
        client.SetAttribute("Interval", TimeValue(Seconds(intervalSec)));
        client.SetAttribute("PacketSize", UintegerValue(packetSize));
        
        ApplicationContainer apps = client.Install(n);
        
        uint64_t id = sim->nextAppId++;
        sim->apps[id] = apps.Get(0);
        *outApp = IdToAppHandle(id);
        
        return NS3_OK;
    } catch (const std::exception& e) {
        sim->SetError(std::string("app_udpecho_client failed: ") + e.what());
        return NS3_ERR;
    }
}

NS3SHIM_API ns3_status app_start(ns3_sim sim, ns3_app app, double atTimeSec) {
    if (!ValidateSim(sim) || !app) return NS3_ERR;
    
    try {
        Ptr<Application> a = GetApp(sim, app);
        if (!a) return NS3_ERR;
        
        a->SetStartTime(Seconds(atTimeSec));
        return NS3_OK;
    } catch (const std::exception& e) {
        sim->SetError(std::string("app_start failed: ") + e.what());
        return NS3_ERR;
    }
}

NS3SHIM_API ns3_status app_stop(ns3_sim sim, ns3_app app, double atTimeSec) {
    if (!ValidateSim(sim) || !app) return NS3_ERR;
    
    try {
        Ptr<Application> a = GetApp(sim, app);
        if (!a) return NS3_ERR;
        
        a->SetStopTime(Seconds(atTimeSec));
        return NS3_OK;
    } catch (const std::exception& e) {
        sim->SetError(std::string("app_stop failed: ") + e.what());
        return NS3_ERR;
    }
}

// ============================================================================
// Tracing & Statistics
// ============================================================================

NS3SHIM_API ns3_status trace_subscribe_packet_events(ns3_sim sim, ns3_device dev, 
                                                      ns3_pkt_cb onTx, ns3_pkt_cb onRx, void* user) {
    if (!ValidateSim(sim) || !dev) return NS3_ERR;
    
    try {
        Ptr<NetDevice> device = GetDevice(sim, dev);
        if (!device) return NS3_ERR;
        
        uint64_t deviceId = HandleToId(dev);
        
        // Create persistent context (leaked intentionally - managed by ns-3 lifetime)
        auto* ctx = new PacketTraceContext{onTx, onRx, user, deviceId};
        
        if (onTx) {
            device->GetObject<PointToPointNetDevice>()->TraceConnectWithoutContext(
                "PhyTxEnd",
                [ctx](Ptr<const Packet> packet) {
                    double now = Simulator::Now().GetSeconds();
                    ctx->onTx(ctx->user, ctx->deviceId, now, packet->GetSize());
                }
            );
        }
        
        if (onRx) {
            device->GetObject<PointToPointNetDevice>()->TraceConnectWithoutContext(
                "PhyRxEnd",
                [ctx](Ptr<const Packet> packet) {
                    double now = Simulator::Now().GetSeconds();
                    ctx->onRx(ctx->user, ctx->deviceId, now, packet->GetSize());
                }
            );
        }
        
        return NS3_OK;
    } catch (const std::exception& e) {
        sim->SetError(std::string("trace_subscribe_packet_events failed: ") + e.what());
        return NS3_ERR;
    }
}

NS3SHIM_API ns3_status pcap_enable(ns3_sim sim, ns3_device dev, const char* filePrefix) {
    if (!ValidateSim(sim) || !dev || !filePrefix) return NS3_ERR;
    
    try {
        Ptr<NetDevice> device = GetDevice(sim, dev);
        if (!device) return NS3_ERR;
        
        PointToPointHelper p2p;
        p2p.EnablePcap(filePrefix, device, true);
        
        return NS3_OK;
    } catch (const std::exception& e) {
        sim->SetError(std::string("pcap_enable failed: ") + e.what());
        return NS3_ERR;
    }
}

NS3SHIM_API ns3_status flowmon_install_all(ns3_sim sim, ns3_flowmon* outFlowMon) {
    if (!ValidateSim(sim) || !outFlowMon) return NS3_ERR;
    
    try {
        FlowMonitorHelper flowHelper;
        Ptr<FlowMonitor> monitor = flowHelper.InstallAll();
        
        uint64_t id = sim->nextFlowMonId++;
        sim->flowMons[id] = monitor;
        *outFlowMon = IdToFlowMonHandle(id);
        
        return NS3_OK;
    } catch (const std::exception& e) {
        sim->SetError(std::string("flowmon_install_all failed: ") + e.what());
        return NS3_ERR;
    }
}

NS3SHIM_API ns3_status flowmon_collect(ns3_sim sim, ns3_flowmon fm, ns3_flow_stats* outStats) {
    if (!ValidateSim(sim) || !fm || !outStats) return NS3_ERR;
    
    try {
        Ptr<FlowMonitor> monitor = GetFlowMon(sim, fm);
        if (!monitor) return NS3_ERR;
        
        FlowMonitorHelper flowHelper;
        Ptr<Ipv4FlowClassifier> classifier = DynamicCast<Ipv4FlowClassifier>(flowHelper.GetClassifier());
        
        std::map<FlowId, FlowMonitor::FlowStats> stats = monitor->GetFlowStats();
        
        uint64_t txPackets = 0, rxPackets = 0;
        uint64_t txBytes = 0, rxBytes = 0;
        double delaySum = 0.0, jitterSum = 0.0;
        
        for (auto& flow : stats) {
            txPackets += flow.second.txPackets;
            rxPackets += flow.second.rxPackets;
            txBytes += flow.second.txBytes;
            rxBytes += flow.second.rxBytes;
            delaySum += flow.second.delaySum.GetSeconds();
            delaySum += flow.second.jitterSum.GetSeconds();
        }
        
        outStats->txPackets = txPackets;
        outStats->rxPackets = rxPackets;
        outStats->txBytes = txBytes;
        outStats->rxBytes = rxBytes;
        outStats->delaySumSec = delaySum;
        outStats->jitterSumSec = jitterSum;
        outStats->flowCount = stats.size();
        
        return NS3_OK;
    } catch (const std::exception& e) {
        sim->SetError(std::string("flowmon_collect failed: ") + e.what());
        return NS3_ERR;
    }
}

// ============================================================================
// Configuration
// ============================================================================

NS3SHIM_API ns3_status config_set(ns3_sim sim, const char* path, const char* attrName, ns3_attr value) {
    if (!ValidateSim(sim) || !path || !attrName) return NS3_ERR;
    
    try {
        std::string fullPath = std::string(path) + "/" + attrName;
        
        switch (value.kind) {
            case NS3_ATTR_BOOL:
                Config::Set(fullPath, BooleanValue(value.b != 0));
                break;
            case NS3_ATTR_UINT:
                Config::Set(fullPath, UintegerValue(value.u));
                break;
            case NS3_ATTR_DOUBLE:
                Config::Set(fullPath, DoubleValue(value.d));
                break;
            case NS3_ATTR_STRING:
                if (!value.s) return NS3_ERR;
                Config::Set(fullPath, StringValue(value.s));
                break;
            default:
                sim->SetError("Invalid attribute kind");
                return NS3_ERR;
        }
        
        return NS3_OK;
    } catch (const std::exception& e) {
        sim->SetError(std::string("config_set failed: ") + e.what());
        return NS3_ERR;
    }
}

