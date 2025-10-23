#!/bin/bash
# Build script for ns3shim native library (Linux)
# Usage: ./build-native.sh [ns3-path] [configuration]

set -e

NS3_PATH="${1:-$HOME/ns-3-dev}"
CONFIGURATION="${2:-Release}"

echo "=== Building ns3shim native library ==="
echo "ns-3 path: $NS3_PATH"
echo "Configuration: $CONFIGURATION"

# Verify ns-3 exists
if [ ! -d "$NS3_PATH" ]; then
    echo "ERROR: ns-3 not found at $NS3_PATH"
    echo "Please install ns-3 or specify correct path as first argument"
    exit 1
fi

# Get script directory
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
BUILD_DIR="$SCRIPT_DIR/native/build"

# Create build directory
mkdir -p "$BUILD_DIR"

# Run CMake configure
echo ""
echo "Configuring CMake..."
cd "$BUILD_DIR"
cmake .. -DNS3_DIR="$NS3_PATH" -DCMAKE_BUILD_TYPE=$CONFIGURATION

# Build
echo ""
echo "Building..."
cmake --build . --config $CONFIGURATION

echo ""
echo "✓ Build successful!"
echo "Output: $BUILD_DIR/libns3shim.so"

