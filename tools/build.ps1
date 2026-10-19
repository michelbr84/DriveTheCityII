<#
.SYNOPSIS
  Builds Drive the City II, runs the unit tests, and stages a local package.
.DESCRIPTION
  Output goes to %LOCALAPPDATA%\DriveTheCityII\ (never into the OneDrive project folder):
    build\      compiler intermediates and binaries
    packages\   DriveTheCityII-<version>-<stamp>\ staged packages, plus LATEST.txt (last good package)
  A failed build or failing tests never replace LATEST.txt.
.EXAMPLE
  powershell -NoProfile -File tools\build.ps1
  powershell -NoProfile -File tools\build.ps1 -SkipTests
#>
param(
    [switch]$SkipTests,
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'env.ps1')

function Fail($msg) { Write-Host "BUILD FAILED: $msg" -ForegroundColor Red; exit 1 }

if (-not $DtcGamePath) { Fail 'Cities: Skylines II installation not found.' }
Write-Host "Game:        $DtcGamePath"
Write-Host "Project:     $DtcProjectRoot"
Write-Host "Build root:  $DtcBuildRoot"

# Build stamp: short commit (+dirty) and UTC time.
$commit = 'nogit'
$dirty = ''
Push-Location $DtcProjectRoot
try {
    $c = (& git rev-parse --short HEAD 2>$null)
    if ($LASTEXITCODE -eq 0 -and $c) {
        $commit = $c.Trim()
        $st = (& git status --porcelain 2>$null)
        if ($st) { $dirty = '+dirty' }
    }
} finally { Pop-Location }
$stamp = "$commit$dirty-$((Get-Date).ToUniversalTime().ToString('yyyyMMddTHHmmssZ'))"
Write-Host "Build stamp: $stamp"

if (-not $SkipTests) {
    Write-Host '== Unit tests (Core, no game required) =='
    & dotnet test (Join-Path $DtcProjectRoot 'tests\DriveTheCityII.Core.Tests\DriveTheCityII.Core.Tests.csproj') -c $Configuration -nologo -v:minimal
    if ($LASTEXITCODE -ne 0) { Fail 'unit tests failed' }
}

Write-Host '== Mod build =='
& dotnet build (Join-Path $DtcProjectRoot 'src\DriveTheCityII.Mod\DriveTheCityII.Mod.csproj') -c $Configuration -nologo -v:minimal "-p:DtcBuildStamp=$stamp"
if ($LASTEXITCODE -ne 0) { Fail 'mod build failed' }

$binDir = Join-Path $DtcBuildRoot ("artifacts\bin\DriveTheCityII.Mod\" + $Configuration.ToLowerInvariant())
$files = @('DriveTheCityII.dll', 'DriveTheCityII.pdb', 'DriveTheCityII.mjs')
foreach ($f in $files) {
    if (-not (Test-Path (Join-Path $binDir $f))) { Fail "missing build output $f in $binDir" }
}

# Version from the built assembly's informational version.
$ver = (Get-Item (Join-Path $binDir 'DriveTheCityII.dll')).VersionInfo.ProductVersion
if (-not $ver) { $ver = '0.0.0' }
$ver = ($ver -split '\+')[0]

$pkgName = "DriveTheCityII-$ver-$stamp"
$pkgDir = Join-Path $DtcPackagesRoot $pkgName
$tmpDir = "$pkgDir.partial"
if (Test-Path $tmpDir) { Remove-Item -Recurse -Force $tmpDir }
New-Item -ItemType Directory -Force $tmpDir | Out-Null
$manifestFiles = @()
foreach ($f in $files) {
    Copy-Item (Join-Path $binDir $f) (Join-Path $tmpDir $f)
    $h = (Get-FileHash (Join-Path $tmpDir $f) -Algorithm SHA256).Hash
    $manifestFiles += [ordered]@{ path = $f; sha256 = $h; bytes = (Get-Item (Join-Path $tmpDir $f)).Length }
}
$manifest = [ordered]@{
    name = 'DriveTheCityII'
    version = $ver
    stamp = $stamp
    builtUtc = (Get-Date).ToUniversalTime().ToString('o')
    gameManagedPath = $env:CSII_MANAGEDPATH
    files = $manifestFiles
}
$manifest | ConvertTo-Json -Depth 5 | Set-Content -Encoding UTF8 (Join-Path $tmpDir 'package-manifest.json')
Rename-Item $tmpDir $pkgName
Set-Content -Encoding UTF8 (Join-Path $DtcPackagesRoot 'LATEST.txt') $pkgName
Write-Host "PACKAGE OK: $pkgDir" -ForegroundColor Green
