cmake_minimum_required(VERSION 3.16)
project(ns3shim VERSION 1.0.0 LANGUAGES CXX)

# ==============================================================================
# Project Configuration
# ==============================================================================

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
set(CMAKE_CXX_EXTENSIONS OFF)

set(CMAKE_POSITION_INDEPENDENT_CODE ON)

# ==============================================================================
# Build Type
# ==============================================================================

if(NOT CMAKE_BUILD_TYPE)
    set(CMAKE_BUILD_TYPE Release CACHE STRING "Build type" FORCE)
endif()

message(STATUS "Build type: ${CMAKE_BUILD_TYPE}")

# ==============================================================================
# Find ns-3
# ==============================================================================

# Option 1: User-provided ns-3 path
set(NS3_DIR "" CACHE PATH "Path to ns-3 installation directory")

if(NS3_DIR)
    message(STATUS "Using ns-3 from: ${NS3_DIR}")
    set(NS3_INCLUDE_DIR "${NS3_DIR}/build/include")
    set(NS3_LIB_DIR "${NS3_DIR}/build/lib")
else()
    # Option 2: Try to find ns-3 in common locations
    find_path(NS3_INCLUDE_DIR
        NAMES ns3/core-module.h
        PATHS
            /usr/local/include
            /usr/include
            ${CMAKE_SOURCE_DIR}/../../../ns-3-dev/build/include
            ${CMAKE_SOURCE_DIR}/../../ns-3-dev/build/include
            ENV NS3_HOME
        PATH_SUFFIXES include
    )
    
    find_library(NS3_CORE_LIB
        NAMES ns3-core ns3.41-core ns3.42-core libns3-core
        PATHS
            /usr/local/lib
            /usr/lib
            ${CMAKE_SOURCE_DIR}/../../../ns-3-dev/build/lib
            ${CMAKE_SOURCE_DIR}/../../ns-3-dev/build/lib
            ENV NS3_HOME
        PATH_SUFFIXES lib
    )
    
    if(NS3_CORE_LIB)
        get_filename_component(NS3_LIB_DIR "${NS3_CORE_LIB}" DIRECTORY)
    endif()
endif()

if(NOT NS3_INCLUDE_DIR OR NOT NS3_LIB_DIR)
    message(FATAL_ERROR 
        "ns-3 not found. Please specify NS3_DIR or set NS3_HOME environment variable.\n"
        "Example: cmake -DNS3_DIR=/path/to/ns-3-dev .."
    )
endif()

message(STATUS "ns-3 include directory: ${NS3_INCLUDE_DIR}")
message(STATUS "ns-3 library directory: ${NS3_LIB_DIR}")

# ==============================================================================
# ns-3 Libraries
# ==============================================================================

# Common ns-3 modules needed
set(NS3_MODULES
    core
    network
    internet
    point-to-point
    csma
    wifi
    mobility
    applications
    flow-monitor
)

# Find all required ns-3 libraries
set(NS3_LIBRARIES "")
foreach(MODULE ${NS3_MODULES})
    # Try different naming conventions
    find_library(NS3_${MODULE}_LIB
        NAMES 
            ns3-${MODULE}
            ns3.41-${MODULE}
            ns3.42-${MODULE}
            ns3.43-${MODULE}
            libns3-${MODULE}
        PATHS ${NS3_LIB_DIR}
        NO_DEFAULT_PATH
    )
    
    if(NS3_${MODULE}_LIB)
        list(APPEND NS3_LIBRARIES ${NS3_${MODULE}_LIB})
        message(STATUS "Found ns-3 module: ${MODULE}")
    else()
        message(WARNING "ns-3 module not found: ${MODULE}")
    endif()
endforeach()

if(NOT NS3_LIBRARIES)
    message(FATAL_ERROR "No ns-3 libraries found in ${NS3_LIB_DIR}")
endif()

# ==============================================================================
# Build Shared Library
# ==============================================================================

add_library(ns3shim SHARED
    src/ns3shim.cpp
)

target_include_directories(ns3shim
    PUBLIC
        $<BUILD_INTERFACE:${CMAKE_CURRENT_SOURCE_DIR}/include>
        $<INSTALL_INTERFACE:include>
    PRIVATE
        ${NS3_INCLUDE_DIR}
)

target_link_libraries(ns3shim
    PRIVATE
        ${NS3_LIBRARIES}
)

# Platform-specific settings
if(WIN32)
    target_compile_definitions(ns3shim PRIVATE NS3SHIM_EXPORTS)
    # Ensure DLL exports are visible
    set_target_properties(ns3shim PROPERTIES
        WINDOWS_EXPORT_ALL_SYMBOLS OFF  # We use explicit exports
    )
elseif(UNIX)
    target_compile_options(ns3shim PRIVATE -fvisibility=hidden)
    set_target_properties(ns3shim PROPERTIES
        CXX_VISIBILITY_PRESET hidden
        VISIBILITY_INLINES_HIDDEN ON
    )
endif()

# Set output name
set_target_properties(ns3shim PROPERTIES
    OUTPUT_NAME "ns3shim"
    VERSION ${PROJECT_VERSION}
    SOVERSION 1
)

# ==============================================================================
# Installation
# ==============================================================================

include(GNUInstallDirs)

install(TARGETS ns3shim
    EXPORT ns3shimTargets
    LIBRARY DESTINATION ${CMAKE_INSTALL_LIBDIR}
    ARCHIVE DESTINATION ${CMAKE_INSTALL_LIBDIR}
    RUNTIME DESTINATION ${CMAKE_INSTALL_BINDIR}
)

install(FILES include/ns3shim.h
    DESTINATION ${CMAKE_INSTALL_INCLUDEDIR}
)

# ==============================================================================
# Summary
# ==============================================================================

message(STATUS "")
message(STATUS "========================================")
message(STATUS "ns3shim Configuration Summary")
message(STATUS "========================================")
message(STATUS "Version:          ${PROJECT_VERSION}")
message(STATUS "Build Type:       ${CMAKE_BUILD_TYPE}")
message(STATUS "Install Prefix:   ${CMAKE_INSTALL_PREFIX}")
message(STATUS "ns-3 Include:     ${NS3_INCLUDE_DIR}")
message(STATUS "ns-3 Libraries:   ${NS3_LIB_DIR}")
message(STATUS "C++ Standard:     C++${CMAKE_CXX_STANDARD}")
message(STATUS "========================================")
message(STATUS "")

