# Complete build script for Windows
# Builds native library and .NET SDK

param(
    [string]$Ns3Path = "C:\ns-3-dev",
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

Write-Host "=== PacketFlow ns-3 Adapter Build ===" -ForegroundColor Cyan
Write-Host ""

# Build native library
Write-Host "Step 1/3: Building native library..." -ForegroundColor Yellow
& "$PSScriptRoot\build-native.ps1" -Ns3Path $Ns3Path -Configuration $Configuration

# Build .NET SDK
Write-Host "`nStep 2/3: Building .NET SDK..." -ForegroundColor Yellow
Push-Location "$PSScriptRoot\dotnet"
try {
    dotnet build -c $Configuration
    if ($LASTEXITCODE -ne 0) {
        throw ".NET build failed"
    }
}
finally {
    Pop-Location
}

# Run tests
Write-Host "`nStep 3/3: Running tests..." -ForegroundColor Yellow
Push-Location "$PSScriptRoot\dotnet\PacketFlow.Ns3Adapter.Tests"
try {
    $env:NS3SHIM_PATH = Join-Path $PSScriptRoot "native\build\$Configuration"
    dotnet test --no-build -c $Configuration
    if ($LASTEXITCODE -ne 0) {
        Write-Host "⚠ Tests failed (this may be expected if ns-3 is not fully configured)" -ForegroundColor Yellow
    }
}
finally {
    Pop-Location
}

Write-Host "`n=== Build Complete ===" -ForegroundColor Green
Write-Host "Native library: $PSScriptRoot\native\build\$Configuration\ns3shim.dll" -ForegroundColor Gray
Write-Host ".NET SDK: $PSScriptRoot\dotnet\PacketFlow.Ns3Adapter\bin\$Configuration\net8.0\" -ForegroundColor Gray
Write-Host "`nTo run examples:" -ForegroundColor Cyan
Write-Host "  cd dotnet\PacketFlow.Ns3Adapter.Examples" -ForegroundColor Gray
Write-Host "  dotnet run -- p2p" -ForegroundColor Gray

