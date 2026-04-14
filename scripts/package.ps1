# Package neverway-devtools for release.
# Usage: .\scripts\package.ps1 -GameDir "C:\path\to\Neverway" [-Version "v0.1.0"]

param(
    [Parameter(Mandatory)][string]$GameDir,
    [string]$Version = "dev"
)

$ErrorActionPreference = "Stop"

$ProjectDir = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
if (-not $ProjectDir) { $ProjectDir = Resolve-Path ".." }
$ModdedDir = Join-Path $GameDir ".modded"

if (-not (Test-Path $ModdedDir)) {
    Write-Error "$ModdedDir not found. Run murder-mod-install on the game first."
    exit 1
}

Write-Host "Building NeverwayMod.DevTools..."
dotnet build $ProjectDir -c Release -p:GameAssemblyPath="$ModdedDir"
if ($LASTEXITCODE -ne 0) { exit 1 }

# Find ImGui.NET managed DLL from NuGet cache
$NuGetDir = Join-Path $env:USERPROFILE ".nuget\packages\imgui.net"
$ImGuiDll = Get-ChildItem -Path $NuGetDir -Filter "ImGui.NET.dll" -Recurse |
    Where-Object { $_.FullName -match "net8\.0" } |
    Sort-Object FullName |
    Select-Object -Last 1

if (-not $ImGuiDll) {
    Write-Error "ImGui.NET.dll not found in NuGet cache. Run dotnet restore first."
    exit 1
}

$RuntimesDir = Join-Path (Split-Path (Split-Path $ImGuiDll.DirectoryName)) "runtimes"

$Platforms = @(
    @{ Name = "windows"; Native = "win-x64\native\cimgui.dll" }
    @{ Name = "macos";   Native = "osx\native\libcimgui.dylib" }
    @{ Name = "linux";   Native = "linux-x64\native\libcimgui.so" }
)

Write-Host "Packaging release zips..."
foreach ($p in $Platforms) {
    $NativeFile = Join-Path $RuntimesDir $p.Native
    if (-not (Test-Path $NativeFile)) {
        Write-Host "  SKIP $($p.Name) (native lib not found)"
        continue
    }

    $Dist = Join-Path $ProjectDir "dist\$($p.Name)\devtools"
    if (Test-Path $Dist) { Remove-Item -Recurse -Force $Dist }
    New-Item -ItemType Directory -Path $Dist -Force | Out-Null

    Copy-Item (Join-Path $ProjectDir "mod.yaml") $Dist
    Copy-Item (Join-Path $ProjectDir "bin\Release\net8.0\NeverwayMod.DevTools.dll") $Dist
    Copy-Item (Join-Path $ProjectDir "bin\Release\net8.0\NeverwayMod.DevTools.pdb") $Dist
    Copy-Item $ImGuiDll.FullName $Dist
    Copy-Item $NativeFile $Dist

    $Zip = Join-Path $ProjectDir "neverway-devtools-$Version-$($p.Name).zip"
    Compress-Archive -Path (Join-Path $ProjectDir "dist\$($p.Name)\devtools") -DestinationPath $Zip -Force
    Write-Host "  $Zip"
}

Remove-Item -Recurse -Force (Join-Path $ProjectDir "dist") -ErrorAction SilentlyContinue
Write-Host "Done."
