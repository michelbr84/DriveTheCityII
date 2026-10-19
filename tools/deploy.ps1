<#
.SYNOPSIS
  Installs, rolls back, or removes the Drive the City II local mod package.
.DESCRIPTION
  Target: <CS2 user data>\Mods\DriveTheCityII\  (the game's native local-mods folder, CSII_LOCALMODSPATH).
  Every install writes %LOCALAPPDATA%\DriveTheCityII\deploy\installed.json listing the files we own and their
  hashes. The previous install is moved to deploy\backups\ so that -Rollback restores it exactly.
  The script refuses to touch a target folder it does not own and refuses to run while the game is running.
.EXAMPLE
  tools\deploy.ps1 -Install            # installs packages\LATEST.txt
  tools\deploy.ps1 -Install -Package DriveTheCityII-0.1.0-abc123-...
  tools\deploy.ps1 -Rollback           # restores the previous install
  tools\deploy.ps1 -Uninstall          # removes only our files
  tools\deploy.ps1 -Status
#>
param(
    [switch]$Install,
    [switch]$Rollback,
    [switch]$Uninstall,
    [switch]$Status,
    [string]$Package
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'env.ps1')

$installedPath = Join-Path $DtcDeployRoot 'installed.json'
$backupRoot = Join-Path $DtcDeployRoot 'backups'
New-Item -ItemType Directory -Force $DtcDeployRoot, $backupRoot | Out-Null

function Fail($msg) { Write-Host "DEPLOY FAILED: $msg" -ForegroundColor Red; exit 1 }

function Assert-GameClosed {
    if (Get-Process -Name 'Cities2' -ErrorAction SilentlyContinue) { Fail 'Cities: Skylines II is running. Close it first.' }
}

function Read-Installed {
    if (Test-Path $installedPath) { return (Get-Content $installedPath -Raw | ConvertFrom-Json) }
    return $null
}

function Test-OwnedTarget($installed) {
    # True if the target folder is absent, or contains only files our manifest lists.
    if (-not (Test-Path $DtcModFolder)) { return $true }
    if (-not $installed) { return $false }
    $owned = @($installed.files | ForEach-Object { $_.path })
    $present = @(Get-ChildItem $DtcModFolder -File -Recurse | ForEach-Object { $_.FullName.Substring($DtcModFolder.Length + 1) })
    $foreign = @($present | Where-Object { $owned -notcontains $_ })
    if ($foreign.Count -gt 0) {
        Write-Host ("Unrecognized files in target: " + ($foreign -join ', ')) -ForegroundColor Yellow
        return $false
    }
    return $true
}

if ($Status) {
    $i = Read-Installed
    Write-Host "Mods folder: $DtcModsPath"
    Write-Host "Target:      $DtcModFolder (exists: $(Test-Path $DtcModFolder))"
    if ($i) {
        Write-Host "Installed:   $($i.package) at $($i.installedUtc)"
        foreach ($f in $i.files) {
            $p = Join-Path $DtcModFolder $f.path
            $ok = (Test-Path $p) -and ((Get-FileHash $p -Algorithm SHA256).Hash -eq $f.sha256)
            Write-Host ("  {0,-24} {1}" -f $f.path, $(if ($ok) { 'OK' } else { 'MISSING/CHANGED' }))
        }
        if ($i.backup) { Write-Host "Rollback to: $($i.backup)" }
    } else {
        Write-Host 'Installed:   none recorded'
    }
    exit 0
}

if ($Install) {
    Assert-GameClosed
    if (-not $Package) {
        $latest = Join-Path $DtcPackagesRoot 'LATEST.txt'
        if (-not (Test-Path $latest)) { Fail 'No package. Run tools\build.ps1 first.' }
        $Package = (Get-Content $latest -Raw).Trim()
    }
    $pkgDir = Join-Path $DtcPackagesRoot $Package
    $pkgManifestPath = Join-Path $pkgDir 'package-manifest.json'
    if (-not (Test-Path $pkgManifestPath)) { Fail "Package not found: $pkgDir" }
    $pkg = Get-Content $pkgManifestPath -Raw | ConvertFrom-Json
    foreach ($f in $pkg.files) {
        $src = Join-Path $pkgDir $f.path
        if ((Get-FileHash $src -Algorithm SHA256).Hash -ne $f.sha256) { Fail "Package file hash mismatch: $($f.path)" }
    }

    $installed = Read-Installed
    if (-not (Test-OwnedTarget $installed)) { Fail "Target $DtcModFolder exists but is not owned by this installer. Nothing changed." }

    New-Item -ItemType Directory -Force $DtcModsPath | Out-Null
    $staging = Join-Path $DtcModsPath '.DriveTheCityII.staging'
    if (Test-Path $staging) { Remove-Item -Recurse -Force $staging }
    New-Item -ItemType Directory -Force $staging | Out-Null
    foreach ($f in $pkg.files) { Copy-Item (Join-Path $pkgDir $f.path) (Join-Path $staging $f.path) }
    foreach ($f in $pkg.files) {
        if ((Get-FileHash (Join-Path $staging $f.path) -Algorithm SHA256).Hash -ne $f.sha256) { Remove-Item -Recurse -Force $staging; Fail "Staging copy mismatch: $($f.path)" }
    }

    $backup = $null
    if (Test-Path $DtcModFolder) {
        $backup = Join-Path $backupRoot ((Get-Date).ToString('yyyyMMdd-HHmmss') + '-' + ($(if ($installed) { $installed.package } else { 'unknown' })))
        Move-Item $DtcModFolder $backup
        if ($installed) { Copy-Item $installedPath (Join-Path $backup 'installed.json') }
    }
    try {
        Rename-Item $staging 'DriveTheCityII'
    } catch {
        if ($backup) { Move-Item $backup $DtcModFolder }
        Fail "Could not activate the new install: $($_.Exception.Message)"
    }

    $record = [ordered]@{
        package = $Package
        version = $pkg.version
        stamp = $pkg.stamp
        installedUtc = (Get-Date).ToUniversalTime().ToString('o')
        target = $DtcModFolder
        backup = $backup
        files = $pkg.files
    }
    $record | ConvertTo-Json -Depth 5 | Set-Content -Encoding UTF8 $installedPath
    Write-Host "INSTALLED $Package -> $DtcModFolder" -ForegroundColor Green
    if ($backup) { Write-Host "Previous install kept for rollback: $backup" }
    exit 0
}

if ($Rollback) {
    Assert-GameClosed
    $installed = Read-Installed
    if (-not $installed -or -not $installed.backup -or -not (Test-Path $installed.backup)) { Fail 'No previous install recorded to roll back to.' }
    if (-not (Test-OwnedTarget $installed)) { Fail 'Target contains files this installer does not own. Nothing changed.' }
    $prevRecordPath = Join-Path $installed.backup 'installed.json'
    $trash = Join-Path $backupRoot ((Get-Date).ToString('yyyyMMdd-HHmmss') + '-rolledback-' + $installed.package)
    if (Test-Path $DtcModFolder) { Move-Item $DtcModFolder $trash }
    Move-Item $installed.backup $DtcModFolder
    if (Test-Path (Join-Path $DtcModFolder 'installed.json')) {
        Move-Item (Join-Path $DtcModFolder 'installed.json') $installedPath -Force
    } else {
        Remove-Item $installedPath -Force
    }
    Write-Host "ROLLED BACK. Replaced install kept at $trash" -ForegroundColor Green
    exit 0
}

if ($Uninstall) {
    Assert-GameClosed
    $installed = Read-Installed
    if (-not (Test-Path $DtcModFolder)) { Write-Host 'Nothing installed.'; if (Test-Path $installedPath) { Remove-Item $installedPath }; exit 0 }
    if (-not (Test-OwnedTarget $installed)) { Fail 'Target contains files this installer does not own. Nothing removed.' }
    foreach ($f in $installed.files) {
        $p = Join-Path $DtcModFolder $f.path
        if (Test-Path $p) { Remove-Item $p -Force }
    }
    if (-not (Get-ChildItem $DtcModFolder -Force)) { Remove-Item $DtcModFolder -Force }
    Remove-Item $installedPath -Force
    Write-Host 'UNINSTALLED (only files listed in the ownership manifest were removed).' -ForegroundColor Green
    exit 0
}

Write-Host 'Specify -Install, -Rollback, -Uninstall, or -Status.'
exit 2
