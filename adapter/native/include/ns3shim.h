// ns3shim.h
// C ABI interface for ns-3 network simulator
// 
// This header defines a pure C ABI for interoperability with .NET and other managed runtimes.
// All functions use C linkage, opaque handles, and POD types.
//
// Threading: sim_run() blocks; callbacks fire on ns-3's scheduler thread.
// Error handling: functions return ns3_status; use ns3_last_error() for diagnostics.
// Memory management: call *_destroy() for every handle; idempotent and NULL-safe.

#ifndef NS3SHIM_H
#define NS3SHIM_H

#include <stdint.h>
#include <stddef.h>

#ifdef _WIN32
  #ifdef NS3SHIM_EXPORTS
    #define NS3SHIM_API __declspec(dllexport)
  #else
    #define NS3SHIM_API __declspec(dllimport)
  #endif
#else
  #define NS3SHIM_API __attribute__((visibility("default")))
#endif

#ifdef __cplusplus
extern "C" {
#endif

// ============================================================================
// Opaque Handle Types
// ============================================================================

/// Opaque handle to simulation context
typedef struct ns3_sim_t*     ns3_sim;

/// Opaque handle to network node
typedef struct ns3_node_t*    ns3_node;

/// Opaque handle to network device
typedef struct ns3_device_t*  ns3_device;

/// Opaque handle to application
typedef struct ns3_app_t*     ns3_app;

/// Opaque handle to flow monitor
typedef struct ns3_flowmon_t* ns3_flowmon;

// ============================================================================
// Status & Error Handling
// ============================================================================

/// Return status for all API functions
typedef enum {
    NS3_OK = 0,     ///< Success
    NS3_ERR = -1    ///< Error (use ns3_last_error for details)
} ns3_status;

/// Retrieve last error message for a simulation context
/// @param sim Simulation handle (may be NULL for global errors)
/// @param buf Output buffer for error string (UTF-8, null-terminated)
/// @param len Size of output buffer
/// @return NS3_OK on success
NS3SHIM_API ns3_status ns3_last_error(ns3_sim sim, char* buf, size_t len);

// ============================================================================
// Callback Types
// ============================================================================

/// Generic void callback for scheduled events
/// @param user User-provided context pointer
typedef void(*ns3_void_cb)(void* user);

/// Packet trace callback (TX/RX events)
/// @param user User-provided context pointer
/// @param deviceId Unique device identifier
/// @param timeSec Simulation time in seconds
/// @param bytes Packet size in bytes
typedef void(*ns3_pkt_cb)(void* user, uint64_t deviceId, double timeSec, uint32_t bytes);

// ============================================================================
// Configuration Attributes
// ============================================================================

/// Attribute value type discriminator
typedef enum {
    NS3_ATTR_BOOL,      ///< Boolean value
    NS3_ATTR_UINT,      ///< Unsigned integer (64-bit)
    NS3_ATTR_DOUBLE,    ///< Double-precision float
    NS3_ATTR_STRING     ///< UTF-8 string
} ns3_attr_kind;

/// Tagged union for attribute values
typedef struct {
    ns3_attr_kind kind; ///< Type discriminator
    union {
        uint64_t    u;  ///< Unsigned integer value
        double      d;  ///< Double value
        const char* s;  ///< String value (UTF-8, null-terminated)
        int         b;  ///< Boolean value (0=false, non-zero=true)
    };
} ns3_attr;

/// Set a configuration attribute
/// @param sim Simulation handle
/// @param path Attribute path (e.g., "/NodeList/0/$ns3::Node/DeviceList/0/$ns3::PointToPointNetDevice/Mtu")
/// @param attrName Attribute name
/// @param value Attribute value
/// @return NS3_OK on success, NS3_ERR on failure
NS3SHIM_API ns3_status config_set(ns3_sim sim, const char* path, const char* attrName, ns3_attr value);

// ============================================================================
// Simulation Lifecycle
// ============================================================================

/// Create a new simulation context
/// @param outSim Output handle to created simulation
/// @return NS3_OK on success, NS3_ERR on failure
NS3SHIM_API ns3_status sim_create(ns3_sim* outSim);

/// Set random number generator seed
/// @param sim Simulation handle
/// @param seed RNG seed value
/// @return NS3_OK on success
NS3SHIM_API ns3_status sim_set_seed(ns3_sim sim, uint32_t seed);

/// Run the simulation (blocks until stopped or no events remain)
/// @param sim Simulation handle
/// @return NS3_OK on success
NS3SHIM_API ns3_status sim_run(ns3_sim sim);

/// Schedule a simulation stop at a specific time
/// @param sim Simulation handle
/// @param atTimeSec Simulation time (seconds) to stop
/// @return NS3_OK on success
NS3SHIM_API ns3_status sim_stop(ns3_sim sim, double atTimeSec);

/// Check if simulation is currently running
/// @param sim Simulation handle
/// @param outIsRunning Output: 1 if running, 0 otherwise
/// @return NS3_OK on success
NS3SHIM_API ns3_status sim_is_running(ns3_sim sim, int* outIsRunning);

/// Get current simulation time
/// @param sim Simulation handle
/// @param outTimeSec Output: current time in seconds
/// @return NS3_OK on success
NS3SHIM_API ns3_status sim_now(ns3_sim sim, double* outTimeSec);

/// Schedule a callback at a future time
/// @param sim Simulation handle
/// @param inSeconds Delay in seconds from now
/// @param cb Callback function
/// @param user User context pointer passed to callback
/// @return NS3_OK on success
NS3SHIM_API ns3_status sim_schedule(ns3_sim sim, double inSeconds, ns3_void_cb cb, void* user);

/// Destroy simulation context and free all resources
/// @param sim Simulation handle (NULL-safe, idempotent)
/// @return NS3_OK on success
NS3SHIM_API ns3_status sim_destroy(ns3_sim sim);

// ============================================================================
// Nodes & Topology
// ============================================================================

/// Create multiple network nodes
/// @param sim Simulation handle
/// @param count Number of nodes to create
/// @param outArray Output array of node handles (must be preallocated, size=count)
/// @return NS3_OK on success
NS3SHIM_API ns3_status nodes_create(ns3_sim sim, uint32_t count, ns3_node* outArray);

/// Install Internet stack (IPv4, TCP, UDP, etc.) on nodes
/// @param sim Simulation handle
/// @param nodes Array of node handles
/// @param count Number of nodes in array
/// @return NS3_OK on success
NS3SHIM_API ns3_status internet_install(ns3_sim sim, const ns3_node* nodes, uint32_t count);

// ============================================================================
// Network Devices & Links
// ============================================================================

/// Install point-to-point link between two nodes
/// @param sim Simulation handle
/// @param a First node
/// @param b Second node
/// @param dataRate Data rate (e.g., "5Mbps", "1Gbps")
/// @param delay Propagation delay (e.g., "2ms", "10us")
/// @param mtu Maximum transmission unit in bytes
/// @param outDevA Output: device handle on node A
/// @param outDevB Output: device handle on node B
/// @return NS3_OK on success
NS3SHIM_API ns3_status p2p_install(ns3_sim sim, ns3_node a, ns3_node b,
                                   const char* dataRate, const char* delay, uint32_t mtu,
                                   ns3_device* outDevA, ns3_device* outDevB);

/// Install CSMA (Carrier Sense Multiple Access) bus
/// @param sim Simulation handle
/// @param nodes Array of nodes to connect on bus
/// @param count Number of nodes
/// @param dataRate Data rate (e.g., "100Mbps")
/// @param delay Propagation delay (e.g., "6560ns")
/// @param outDevices Output array of device handles (must be preallocated, size=count)
/// @return NS3_OK on success
NS3SHIM_API ns3_status csma_install(ns3_sim sim, const ns3_node* nodes, uint32_t count,
                                    const char* dataRate, const char* delay,
                                    ns3_device* outDevices);

/// Install Wi-Fi network with stations and access point
/// @param sim Simulation handle
/// @param stas Array of station nodes
/// @param staCount Number of stations
/// @param ap Access point node
/// @param phyStandard PHY standard (80211a=0, 80211b=1, 80211g=2, 80211n_2_4GHZ=3, 80211n_5GHZ=4, 80211ac=5)
/// @param dataRate Data rate (e.g., "54Mbps")
/// @param channelNumber Wi-Fi channel number (1-14 for 2.4GHz, 36-165 for 5GHz)
/// @param outStaDevices Output array of station device handles (size=staCount)
/// @param outApDevice Output: access point device handle
/// @return NS3_OK on success
NS3SHIM_API ns3_status wifi_install_sta_ap(ns3_sim sim, const ns3_node* stas, uint32_t staCount, ns3_node ap,
                                           int phyStandard, const char* dataRate, int channelNumber,
                                           ns3_device* outStaDevices, ns3_device* outApDevice);

// ============================================================================
// Mobility
// ============================================================================

/// Set constant (static) position for a node
/// @param sim Simulation handle
/// @param node Node handle
/// @param x X coordinate (meters)
/// @param y Y coordinate (meters)
/// @param z Z coordinate (meters)
/// @return NS3_OK on success
NS3SHIM_API ns3_status mobility_set_constant_position(ns3_sim sim, ns3_node node, double x, double y, double z);

// ============================================================================
// IP Addressing & Routing
// ============================================================================

/// Assign IPv4 addresses to devices
/// @param sim Simulation handle
/// @param devices Array of device handles
/// @param count Number of devices
/// @param networkBase Network base address (e.g., "10.1.1.0")
/// @param mask Network mask (e.g., "255.255.255.0")
/// @return NS3_OK on success
NS3SHIM_API ns3_status ipv4_assign(ns3_sim sim, const ns3_device* devices, uint32_t count,
                                   const char* networkBase, const char* mask);

/// Populate global IPv4 routing tables
/// @param sim Simulation handle
/// @return NS3_OK on success
NS3SHIM_API ns3_status ipv4_populate_routing_tables(ns3_sim sim);

// ============================================================================
// Applications
// ============================================================================

/// Create UDP Echo server application
/// @param sim Simulation handle
/// @param node Node to host server
/// @param port UDP port number
/// @param outApp Output: application handle
/// @return NS3_OK on success
NS3SHIM_API ns3_status app_udpecho_server(ns3_sim sim, ns3_node node, uint16_t port, ns3_app* outApp);

/// Create UDP Echo client application
/// @param sim Simulation handle
/// @param node Node to host client
/// @param dstIp Destination IP address (e.g., "10.1.1.2")
/// @param port Destination UDP port
/// @param packetSize Packet size in bytes
/// @param intervalSec Interval between packets in seconds
/// @param maxPackets Maximum number of packets to send
/// @param outApp Output: application handle
/// @return NS3_OK on success
NS3SHIM_API ns3_status app_udpecho_client(ns3_sim sim, ns3_node node, const char* dstIp, uint16_t port,
                                          uint32_t packetSize, double intervalSec, uint32_t maxPackets, ns3_app* outApp);

/// Start an application at a specific time
/// @param sim Simulation handle
/// @param app Application handle
/// @param atTimeSec Start time in seconds
/// @return NS3_OK on success
NS3SHIM_API ns3_status app_start(ns3_sim sim, ns3_app app, double atTimeSec);

/// Stop an application at a specific time
/// @param sim Simulation handle
/// @param app Application handle
/// @param atTimeSec Stop time in seconds
/// @return NS3_OK on success
NS3SHIM_API ns3_status app_stop(ns3_sim sim, ns3_app app, double atTimeSec);

// ============================================================================
// Tracing & Statistics
// ============================================================================

/// Subscribe to packet TX/RX events on a device
/// @param sim Simulation handle
/// @param dev Device handle
/// @param onTx Callback for transmitted packets (may be NULL)
/// @param onRx Callback for received packets (may be NULL)
/// @param user User context pointer passed to callbacks
/// @return NS3_OK on success
NS3SHIM_API ns3_status trace_subscribe_packet_events(ns3_sim sim, ns3_device dev, 
                                                      ns3_pkt_cb onTx, ns3_pkt_cb onRx, void* user);

/// Enable PCAP tracing on a device
/// @param sim Simulation handle
/// @param dev Device handle
/// @param filePrefix Prefix for PCAP file name
/// @return NS3_OK on success
NS3SHIM_API ns3_status pcap_enable(ns3_sim sim, ns3_device dev, const char* filePrefix);

/// Flow statistics structure
typedef struct {
    uint64_t txPackets;     ///< Total transmitted packets
    uint64_t rxPackets;     ///< Total received packets
    uint64_t txBytes;       ///< Total transmitted bytes
    uint64_t rxBytes;       ///< Total received bytes
    double   delaySumSec;   ///< Sum of all packet delays (seconds)
    double   jitterSumSec;  ///< Sum of all jitter values (seconds)
    uint32_t flowCount;     ///< Number of flows
} ns3_flow_stats;

/// Install flow monitor on all nodes
/// @param sim Simulation handle
/// @param outFlowMon Output: flow monitor handle
/// @return NS3_OK on success
NS3SHIM_API ns3_status flowmon_install_all(ns3_sim sim, ns3_flowmon* outFlowMon);

/// Collect flow statistics
/// @param sim Simulation handle
/// @param fm Flow monitor handle
/// @param outStats Output: flow statistics structure
/// @return NS3_OK on success
NS3SHIM_API ns3_status flowmon_collect(ns3_sim sim, ns3_flowmon fm, ns3_flow_stats* outStats);

#ifdef __cplusplus
}
#endif

#endif // NS3SHIM_H

