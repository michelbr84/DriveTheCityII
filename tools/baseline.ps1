<#
.SYNOPSIS
  TASK-0003 helper: temporarily installs the locally built upstream Beta Test Driving Mod (pinned commit, built
  outside the project) into the game's local mods folder for a runtime baseline, and removes it again.
.DESCRIPTION
  The upstream source and build live under %LOCALAPPDATA%\DriveTheCityII\upstream and build\upstream-baseline (never
  inside the project). Install records the exact files it copied in deploy\baseline-installed.json; -Remove deletes only
  those files. Refuses to run while the game is running.
.EXAMPLE
  tools\baseline.ps1 -Install
  tools\baseline.ps1 -Remove
#>
param([switch]$Install, [switch]$Remove)
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'env.ps1')
$src = Join-Path $DtcBuildRoot 'upstream-baseline\bin'
$dst = Join-Path $DtcModsPath 'BetaTestDrivingMod'
$record = Join-Path $DtcDeployRoot 'baseline-installed.json'
if (Get-Process -Name 'Cities2' -ErrorAction SilentlyContinue) { Write-Host 'BASELINE: close the game first.'; exit 1 }
New-Item -ItemType Directory -Force $DtcDeployRoot | Out-Null

if ($Install) {
    if (Test-Path $dst) { Write-Host "BASELINE: $dst already exists; not touching it."; exit 1 }
    if (-not (Test-Path (Join-Path $src 'BetaTestDrivingMod.dll'))) { Write-Host 'BASELINE: upstream build not found.'; exit 1 }
    New-Item -ItemType Directory -Force $dst | Out-Null
    $files = @()
    foreach ($f in @('BetaTestDrivingMod.dll', 'BetaTestDrivingMod.pdb', 'BetaTestDrivingMod.mjs')) {
        Copy-Item (Join-Path $src $f) (Join-Path $dst $f)
        $files += [ordered]@{ path = $f; sha256 = (Get-FileHash (Join-Path $dst $f) -Algorithm SHA256).Hash }
    }
    [ordered]@{ target = $dst; commit = 'eefd6bb4fe2dcc479df837a1678f3529ac6e0f76'; installedUtc = (Get-Date).ToUniversalTime().ToString('o'); files = $files } |
        ConvertTo-Json -Depth 4 | Set-Content -Encoding UTF8 $record
    Write-Host "BASELINE INSTALLED at $dst"
    exit 0
}
if ($Remove) {
    if (-not (Test-Path $record)) { Write-Host 'BASELINE: nothing recorded.'; exit 0 }
    $r = Get-Content $record -Raw | ConvertFrom-Json
    foreach ($f in $r.files) { $p = Join-Path $r.target $f.path; if (Test-Path $p) { Remove-Item $p -Force } }
    if ((Test-Path $r.target) -and -not (Get-ChildItem $r.target -Force)) { Remove-Item $r.target -Force }
    Remove-Item $record -Force
    Write-Host 'BASELINE REMOVED'
    exit 0
}
Write-Host 'Specify -Install or -Remove.'
exit 2
