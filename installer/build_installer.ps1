# Builds the StarTooth installer end to end.
#
#   1. Publish a self-contained single-file StarTooth.exe (win-x64, no trimming).
#   2. Sign it with the slohmaier dev cert.
#   3. Compile the Inno Setup installer around it.
#   4. Sign the installer too.
#
# Usage:
#   .\build_installer.ps1                 # full build, signed
#   .\build_installer.ps1 -SkipSign       # unsigned (quick local test)
#   .\build_installer.ps1 -SkipPublish    # reuse the payload from a previous run
#
# The version comes from <Version> in StarTooth.csproj; nothing is hard-coded here.

[CmdletBinding()]
param(
    [switch]$SkipSign,
    [switch]$SkipPublish
)

$ErrorActionPreference = 'Stop'
$here = $PSScriptRoot
$repo = Split-Path $here -Parent
$csproj = Join-Path $repo 'StarTooth.csproj'
$payloadDir = Join-Path $here 'payload'
$signScript = 'C:\Users\slohma\repos\private\dev-cert\sign-exe.ps1'

function Find-ISCC {
    $candidates = @(
        "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe",
        "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
        "$env:ProgramFiles\Inno Setup 6\ISCC.exe"
    )
    foreach ($c in $candidates) { if (Test-Path $c) { return $c } }
    throw "ISCC.exe not found. Install Inno Setup 6."
}

# --- version from the csproj -------------------------------------------------
[xml]$xml = Get-Content $csproj
$version = ($xml.Project.PropertyGroup.Version | Where-Object { $_ } | Select-Object -First 1).Trim()
if (-not $version) { throw "No <Version> in $csproj" }

# Numeric quad for VersionInfoVersion: drop any -rc / -beta suffix, pad to x.y.z.0.
$numeric = ($version -split '-')[0]
while (($numeric -split '\.').Count -lt 4) { $numeric += '.0' }

Write-Host "StarTooth installer" -ForegroundColor Cyan
Write-Host "  display version: $version"
Write-Host "  numeric version: $numeric"

# --- 1. publish --------------------------------------------------------------
if (-not $SkipPublish) {
    Get-Process StarTooth -ErrorAction SilentlyContinue | Stop-Process -Force
    if (Test-Path $payloadDir) { Remove-Item $payloadDir -Recurse -Force }

    Write-Host "Publishing self-contained single-file exe..." -ForegroundColor Cyan
    dotnet publish $csproj -c Release -p:PublishSingleFile=true -o $payloadDir --nologo
    if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed" }
}

$payloadExe = Join-Path $payloadDir 'StarTooth.exe'
if (-not (Test-Path $payloadExe)) { throw "Payload missing: $payloadExe" }

# --- 2. sign the exe ---------------------------------------------------------
if (-not $SkipSign) {
    Write-Host "Signing StarTooth.exe..." -ForegroundColor Cyan
    & $signScript -Path $payloadExe
}

# --- 3. compile the installer ------------------------------------------------
$iscc = Find-ISCC
Write-Host "Compiling installer with $iscc" -ForegroundColor Cyan
& $iscc "/DMyAppVersion=$version" "/DMyAppNumericVersion=$numeric" (Join-Path $here 'StarTooth.iss')
if ($LASTEXITCODE -ne 0) { throw "ISCC failed" }

$installer = Join-Path $here "output\StarTooth-Setup-$version.exe"
if (-not (Test-Path $installer)) { throw "Installer not produced: $installer" }

# --- 4. sign the installer ---------------------------------------------------
if (-not $SkipSign) {
    Write-Host "Signing the installer..." -ForegroundColor Cyan
    & $signScript -Path $installer
}

Write-Host ""
Write-Host "Done: $installer" -ForegroundColor Green
Get-Item $installer | ForEach-Object { "  {0:N1} MB" -f ($_.Length / 1MB) }
