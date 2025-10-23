# Build script for ns3shim native library (Windows)
# Usage: .\build-native.ps1 [-Ns3Path C:\ns-3-dev] [-Configuration Release|Debug]

param(
    [string]$Ns3Path = "C:\ns-3-dev",
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

Write-Host "=== Building ns3shim native library ===" -ForegroundColor Cyan
Write-Host "ns-3 path: $Ns3Path" -ForegroundColor Gray
Write-Host "Configuration: $Configuration" -ForegroundColor Gray

# Verify ns-3 exists
if (-not (Test-Path $Ns3Path)) {
    Write-Host "ERROR: ns-3 not found at $Ns3Path" -ForegroundColor Red
    Write-Host "Please install ns-3 or specify correct path with -Ns3Path" -ForegroundColor Red
    exit 1
}

# Create build directory
$buildDir = Join-Path $PSScriptRoot "native\build"
if (-not (Test-Path $buildDir)) {
    New-Item -ItemType Directory -Path $buildDir | Out-Null
}

# Run CMake configure
Write-Host "`nConfiguring CMake..." -ForegroundColor Cyan
Push-Location $buildDir
try {
    cmake .. -DNS3_DIR="$Ns3Path" -G "Visual Studio 17 2022" -A x64
    if ($LASTEXITCODE -ne 0) {
        throw "CMake configuration failed"
    }
    
    # Build
    Write-Host "`nBuilding..." -ForegroundColor Cyan
    cmake --build . --config $Configuration
    if ($LASTEXITCODE -ne 0) {
        throw "Build failed"
    }
    
    Write-Host "`n✓ Build successful!" -ForegroundColor Green
    Write-Host "Output: $buildDir\$Configuration\ns3shim.dll" -ForegroundColor Gray
}
finally {
    Pop-Location
}

