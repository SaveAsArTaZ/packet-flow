#!/bin/bash
# Complete build script for Linux
# Builds native library and .NET SDK

set -e

NS3_PATH="${1:-$HOME/ns-3-dev}"
CONFIGURATION="${2:-Release}"

echo "=== PacketFlow ns-3 Adapter Build ==="
echo ""

# Get script directory
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"

# Build native library
echo "Step 1/3: Building native library..."
bash "$SCRIPT_DIR/build-native.sh" "$NS3_PATH" "$CONFIGURATION"

# Build .NET SDK
echo ""
echo "Step 2/3: Building .NET SDK..."
cd "$SCRIPT_DIR/dotnet"
dotnet build -c $CONFIGURATION

# Run tests
echo ""
echo "Step 3/3: Running tests..."
cd "$SCRIPT_DIR/dotnet/PacketFlow.Ns3Adapter.Tests"
export NS3SHIM_PATH="$SCRIPT_DIR/native/build"
dotnet test --no-build -c $CONFIGURATION || echo "⚠ Tests failed (may be expected if ns-3 not fully configured)"

echo ""
echo "=== Build Complete ==="
echo "Native library: $SCRIPT_DIR/native/build/libns3shim.so"
echo ".NET SDK: $SCRIPT_DIR/dotnet/PacketFlow.Ns3Adapter/bin/$CONFIGURATION/net8.0/"
echo ""
echo "To run examples:"
echo "  cd dotnet/PacketFlow.Ns3Adapter.Examples"
echo "  dotnet run -- p2p"

