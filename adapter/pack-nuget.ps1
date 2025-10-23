# NuGet packaging script for Windows
# Creates a NuGet package with native binaries included

param(
    [string]$Configuration = "Release",
    [string]$Version = "1.0.0"
)

$ErrorActionPreference = "Stop"

Write-Host "=== Creating NuGet Package ===" -ForegroundColor Cyan
Write-Host "Version: $Version" -ForegroundColor Gray
Write-Host "Configuration: $Configuration" -ForegroundColor Gray

$projectDir = Join-Path $PSScriptRoot "dotnet\PacketFlow.Ns3Adapter"
$outputDir = Join-Path $PSScriptRoot "nuget-output"
$nativeBuildDir = Join-Path $PSScriptRoot "native\build\$Configuration"

# Ensure native library is built
if (-not (Test-Path "$nativeBuildDir\ns3shim.dll")) {
    Write-Host "ERROR: Native library not found. Build it first with .\build-native.ps1" -ForegroundColor Red
    exit 1
}

# Create output directory
if (-not (Test-Path $outputDir)) {
    New-Item -ItemType Directory -Path $outputDir | Out-Null
}

# Update version in csproj if different
$csprojPath = Join-Path $projectDir "PacketFlow.Ns3Adapter.csproj"
$csprojContent = Get-Content $csprojPath -Raw
if ($csprojContent -match '<Version>([^<]+)</Version>') {
    $currentVersion = $matches[1]
    if ($currentVersion -ne $Version) {
        Write-Host "Updating version in .csproj from $currentVersion to $Version" -ForegroundColor Yellow
        $csprojContent = $csprojContent -replace '<Version>[^<]+</Version>', "<Version>$Version</Version>"
        Set-Content $csprojPath -Value $csprojContent -NoNewline
    }
}

# Create runtimes directory structure
$runtimesDir = Join-Path $projectDir "runtimes"
$win64Dir = Join-Path $runtimesDir "win-x64\native"
New-Item -ItemType Directory -Path $win64Dir -Force | Out-Null

# Copy native library
Copy-Item "$nativeBuildDir\ns3shim.dll" -Destination $win64Dir -Force
Write-Host "Copied ns3shim.dll to $win64Dir" -ForegroundColor Gray

# Pack NuGet
Write-Host "`nPacking NuGet..." -ForegroundColor Cyan
Push-Location $projectDir
try {
    dotnet pack -c $Configuration -o $outputDir /p:Version=$Version
    if ($LASTEXITCODE -ne 0) {
        throw "NuGet pack failed"
    }
}
finally {
    Pop-Location
    # Clean up runtimes directory
    Remove-Item $runtimesDir -Recurse -Force -ErrorAction SilentlyContinue
}

$nupkgFile = Get-ChildItem $outputDir -Filter "PacketFlow.Ns3Adapter.$Version.nupkg" | Select-Object -First 1

if ($nupkgFile) {
    Write-Host "`n✓ NuGet package created!" -ForegroundColor Green
    Write-Host "Package: $($nupkgFile.FullName)" -ForegroundColor Gray
    Write-Host "Size: $([math]::Round($nupkgFile.Length / 1MB, 2)) MB" -ForegroundColor Gray
    Write-Host "`nTo publish:" -ForegroundColor Cyan
    Write-Host "  dotnet nuget push $($nupkgFile.FullName) --source https://api.nuget.org/v3/index.json --api-key YOUR_API_KEY" -ForegroundColor Gray
}
else {
    Write-Host "ERROR: Package file not found" -ForegroundColor Red
    exit 1
}

