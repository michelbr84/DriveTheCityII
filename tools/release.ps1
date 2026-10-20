<#
.SYNOPSIS
  Turns a staged package into a player-ready release archive (GitHub Releases, manual install).
.DESCRIPTION
  Input: a package folder made by tools\build.ps1 (default: packages\LATEST.txt).
  Output: %LOCALAPPDATA%\DriveTheCityII\releases\<version>\
    DriveTheCityII-<version>.zip   one top-level folder "DriveTheCityII\" with the mod files, LICENSE.txt and README.txt,
                                   ready to be extracted into <CS2 user data>\Mods\
    SHA256SUMS.txt                 checksum of the archive
    release-manifest.json          package name, commit, file hashes
  The package must come from a clean commit (no "+dirty" stamp) unless -AllowDirty is given.
.EXAMPLE
  tools\release.ps1 -Version 0.1.0
  tools\release.ps1 -Version 0.1.0 -Package DriveTheCityII-0.1.0-abc1234-20261020T120000Z
#>
param(
    [Parameter(Mandatory = $true)][string]$Version,
    [string]$Package,
    [switch]$AllowDirty
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'env.ps1')
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem

function Fail($msg) { Write-Host "RELEASE FAILED: $msg" -ForegroundColor Red; exit 1 }

if (-not $Package) {
    $latest = Join-Path $DtcPackagesRoot 'LATEST.txt'
    if (-not (Test-Path $latest)) { Fail 'no package yet; run tools\build.ps1 first' }
    $Package = (Get-Content $latest -Raw).Trim()
}
$pkgDir = Join-Path $DtcPackagesRoot $Package
if (-not (Test-Path $pkgDir)) { Fail "package not found: $pkgDir" }
$manifest = Get-Content (Join-Path $pkgDir 'package-manifest.json') -Raw | ConvertFrom-Json
if ($manifest.version -ne $Version) { Fail "package version is $($manifest.version), not $Version" }
if ($manifest.stamp -like '*+dirty*' -and -not $AllowDirty) { Fail "package $Package was built from uncommitted changes" }

# Verify the staged files against the package manifest before shipping them.
foreach ($f in $manifest.files) {
    $p = Join-Path $pkgDir $f.path
    if (-not (Test-Path $p)) { Fail "missing $($f.path)" }
    if ((Get-FileHash $p -Algorithm SHA256).Hash -ne $f.sha256) { Fail "hash mismatch for $($f.path)" }
}

$outDir = Join-Path $DtcLocalRoot "releases\$Version"
if (Test-Path $outDir) { Remove-Item -Recurse -Force $outDir }
New-Item -ItemType Directory -Force $outDir | Out-Null
$zipPath = Join-Path $outDir "DriveTheCityII-$Version.zip"
$commit = ($manifest.stamp -split '-')[0]

$readme = @"
Drive the City II $Version - unofficial code mod for Cities: Skylines II (Windows)
Source, documentation and issue tracker: https://github.com/michelbr84/DriveTheCityII
Build: $($manifest.stamp)  (commit $commit)

REQUIREMENTS
  Cities: Skylines II 1.6.2f1 (tested on Steam, Windows 11). Other game versions are untested.
  Do not run the Beta Test Driving Mod at the same time: Drive the City II detects it and stays inactive.
  This is a development build. Back up your saves first.

INSTALL
  1. Close the game.
  2. Extract this archive into the game's local mods folder, so that you get
       %USERPROFILE%\AppData\LocalLow\Colossal Order\Cities Skylines II\Mods\DriveTheCityII\DriveTheCityII.dll
     (paste %USERPROFILE%\AppData\LocalLow\Colossal Order\Cities Skylines II\Mods into the Explorer address bar;
     create the Mods folder if it does not exist).
  3. Start the game, load a city and press F8 (or the "Drive the City" button, top left).

UNINSTALL
  Close the game and delete the Mods\DriveTheCityII folder. Your cities keep loading without the mod.

CONTROLS
  F8 enter/leave character mode - WASD walk / drive - Shift run - E enter/exit a vehicle - V camera
  Tab jobs, garage, company, challenges, profile - Backspace move to a safe spot - Space handbrake
  Full guide: https://github.com/michelbr84/DriveTheCityII/blob/main/START_HERE.md
  Known issues: https://github.com/michelbr84/DriveTheCityII/blob/main/docs/quality/KNOWN_ISSUES.md

PORTUGUES (BRASIL)
  Instalar: feche o jogo e extraia este arquivo em
    %USERPROFILE%\AppData\LocalLow\Colossal Order\Cities Skylines II\Mods
  (deve ficar Mods\DriveTheCityII\DriveTheCityII.dll). Abra o jogo, carregue uma cidade e aperte F8.
  Desinstalar: feche o jogo e apague a pasta Mods\DriveTheCityII.
  O idioma do mod pode ser escolhido em Opcoes > Drive the City II > Geral.

License: MIT (see LICENSE.txt). Not affiliated with Paradox Interactive or Colossal Order.
"@

$entries = @()
foreach ($f in $manifest.files) { $entries += [pscustomobject]@{ name = "DriveTheCityII/$($f.path)"; source = (Join-Path $pkgDir $f.path); text = $null } }
$license = Get-Content (Join-Path $DtcProjectRoot 'LICENSE') -Raw
$entries += [pscustomobject]@{ name = 'DriveTheCityII/LICENSE.txt'; source = $null; text = ($license -replace "`r?`n", "`r`n") }
$entries += [pscustomobject]@{ name = 'DriveTheCityII/README.txt'; source = $null; text = ($readme -replace "`r?`n", "`r`n") }

$fs = [IO.File]::Open($zipPath, [IO.FileMode]::CreateNew)
try {
    $zip = New-Object IO.Compression.ZipArchive($fs, [IO.Compression.ZipArchiveMode]::Create)
    try {
        if ($manifest.builtUtc -is [datetime]) { $time = [DateTimeOffset]$manifest.builtUtc }
        else { $time = [DateTimeOffset]::Parse([string]$manifest.builtUtc, [Globalization.CultureInfo]::InvariantCulture) }
        foreach ($e in $entries) {
            $entry = $zip.CreateEntry($e.name, [IO.Compression.CompressionLevel]::Optimal)
            $entry.LastWriteTime = $time
            $s = $entry.Open()
            try {
                if ($e.source) { $bytes = [IO.File]::ReadAllBytes($e.source) }
                else { $bytes = (New-Object Text.UTF8Encoding($false)).GetBytes($e.text) }
                $s.Write($bytes, 0, $bytes.Length)
            } finally { $s.Dispose() }
        }
    } finally { $zip.Dispose() }
} finally { $fs.Dispose() }

# Read the archive back and check every mod file round-trips byte for byte.
$check = [IO.Compression.ZipFile]::OpenRead($zipPath)
try {
    foreach ($f in $manifest.files) {
        $entry = $check.GetEntry("DriveTheCityII/$($f.path)")
        if (-not $entry) { Fail "archive is missing $($f.path)" }
        $ms = New-Object IO.MemoryStream
        $s = $entry.Open(); try { $s.CopyTo($ms) } finally { $s.Dispose() }
        $sha = [BitConverter]::ToString([Security.Cryptography.SHA256]::Create().ComputeHash($ms.ToArray())) -replace '-', ''
        if ($sha -ne $f.sha256) { Fail "archive copy of $($f.path) differs" }
    }
    $names = @($check.Entries | ForEach-Object { $_.FullName })
} finally { $check.Dispose() }

$zipSha = (Get-FileHash $zipPath -Algorithm SHA256).Hash.ToLowerInvariant()
Set-Content -Encoding ASCII (Join-Path $outDir 'SHA256SUMS.txt') "$zipSha  DriveTheCityII-$Version.zip"
[ordered]@{
    version = $Version
    package = $Package
    stamp = $manifest.stamp
    commit = $commit
    archive = "DriveTheCityII-$Version.zip"
    archiveSha256 = $zipSha
    archiveBytes = (Get-Item $zipPath).Length
    entries = $names
    files = $manifest.files
} | ConvertTo-Json -Depth 5 | Set-Content -Encoding UTF8 (Join-Path $outDir 'release-manifest.json')

Write-Host "Entries: $($names -join ', ')"
Write-Host "SHA-256: $zipSha"
Write-Host "RELEASE OK: $zipPath" -ForegroundColor Green
