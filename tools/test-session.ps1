<#
.SYNOPSIS
  Starts and stops an isolated Drive the City II test session of Cities: Skylines II.
.DESCRIPTION
  -Start
    * refuses if the game is already running (it never takes over a session that may hold unsaved work);
    * backs up the files a test save changes as a side effect: UserState.coc ("last save" pointer) and
      continue_game.json (the launcher's Continue button) into test\sessions\<id>\state-backup\;
    * enables the test bridge marker (the mod then disables autosave for the session and only automates
      disposable DTC2-LAB cities);
    * launches the game to the MAIN MENU (no save is opened automatically).
  -Stop
    * asks the bridge to quit (only allowed from the menu or a lab city), waits, and only then force-stops the exact
      process it started if it does not exit;
    * removes the bridge marker, restores the backed-up files, and copies logs into the session folder as evidence.
.EXAMPLE
  tools\test-session.ps1 -Start
  tools\test-session.ps1 -Stop
#>
param(
    [switch]$Start,
    [switch]$Stop,
    [switch]$Status,
    [string[]]$GameArgs = @('-noSplash')
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'env.ps1')

$sessionFile = Join-Path $DtcTestRoot 'session.json'
$marker = Join-Path $DtcTestRoot 'bridge.enabled'
$stateFiles = @('UserState.coc', 'continue_game.json')
New-Item -ItemType Directory -Force $DtcTestRoot | Out-Null

function Fail($msg) { Write-Host "TEST SESSION: $msg" -ForegroundColor Red; exit 1 }

if ($Status) {
    $p = Get-Process -Name 'Cities2' -ErrorAction SilentlyContinue
    Write-Host "Game running: $([bool]$p) $(if ($p) { 'pid ' + ($p.Id -join ',') })"
    Write-Host "Bridge marker: $(Test-Path $marker)"
    if (Test-Path $sessionFile) { Get-Content $sessionFile -Raw | Write-Host }
    exit 0
}

if ($Start) {
    if (Get-Process -Name 'Cities2' -ErrorAction SilentlyContinue) { Fail 'The game is already running. Not taking over an existing session.' }
    if (-not $DtcGamePath) { Fail 'Game not found.' }
    if (-not (Get-Process -Name 'steam' -ErrorAction SilentlyContinue)) { Fail 'Steam is not running.' }

    $id = (Get-Date).ToString('yyyyMMdd-HHmmss')
    $sessionDir = Join-Path $DtcTestRoot "sessions\$id"
    $backupDir = Join-Path $sessionDir 'state-backup'
    New-Item -ItemType Directory -Force $backupDir | Out-Null
    $backed = @()
    foreach ($f in $stateFiles) {
        $src = Join-Path $DtcUserDataPath $f
        if (Test-Path $src) {
            Copy-Item $src (Join-Path $backupDir $f)
            $backed += [ordered]@{ file = $f; sha256 = (Get-FileHash $src -Algorithm SHA256).Hash; existed = $true }
        } else {
            $backed += [ordered]@{ file = $f; sha256 = $null; existed = $false }
        }
    }

    # Fresh bridge files.
    foreach ($f in @('commands.txt', 'bridge.log')) {
        $p = Join-Path $DtcTestRoot $f
        if (Test-Path $p) { Move-Item $p (Join-Path $sessionDir "previous-$f") }
    }
    Set-Content -Encoding UTF8 $marker "Enabled by tools\test-session.ps1 for session $id"

    $exe = Join-Path $DtcGamePath 'Cities2.exe'
    # Steam passes the app id to games it starts through these variables; setting them for this child process only
    # lets the game's Steam integration initialize without going through a launcher UI or changing any file.
    $env:SteamAppId = '949230'
    $env:SteamGameId = '949230'
    $proc = Start-Process -FilePath $exe -ArgumentList $GameArgs -WorkingDirectory $DtcGamePath -PassThru
    $session = [ordered]@{
        id = $id
        startedUtc = (Get-Date).ToUniversalTime().ToString('o')
        launcherPid = $proc.Id
        args = $GameArgs
        sessionDir = $sessionDir
        stateBackup = $backed
    }
    $session | ConvertTo-Json -Depth 5 | Set-Content -Encoding UTF8 $sessionFile
    Write-Host "STARTED session $id (process $($proc.Id)). Evidence folder: $sessionDir" -ForegroundColor Green
    exit 0
}

if ($Stop) {
    if (-not (Test-Path $sessionFile)) { Fail 'No session record.' }
    $session = Get-Content $sessionFile -Raw | ConvertFrom-Json
    $game = Get-Process -Name 'Cities2' -ErrorAction SilentlyContinue
    if ($game) {
        & (Join-Path $PSScriptRoot 'bridge.ps1') -Commands @('quit') -TimeoutSeconds 60 | Out-Host
        $deadline = (Get-Date).AddSeconds(90)
        while ((Get-Process -Name 'Cities2' -ErrorAction SilentlyContinue) -and (Get-Date) -lt $deadline) { Start-Sleep -Milliseconds 500 }
        $left = Get-Process -Name 'Cities2' -ErrorAction SilentlyContinue
        if ($left) {
            $started = [DateTime]::Parse($session.startedUtc).ToLocalTime()
            foreach ($p in $left) {
                if ($p.StartTime -ge $started.AddSeconds(-5)) {
                    Write-Host "Game did not exit; stopping process $($p.Id) started by this session." -ForegroundColor Yellow
                    Stop-Process -Id $p.Id -Force
                } else {
                    Fail "A Cities2 process ($($p.Id)) predates this session; not stopping it."
                }
            }
        }
    }
    if (Test-Path $marker) { Remove-Item $marker -Force }

    # Restore files changed by test saves.
    $backupDir = Join-Path $session.sessionDir 'state-backup'
    foreach ($b in $session.stateBackup) {
        $dst = Join-Path $DtcUserDataPath $b.file
        if ($b.existed) {
            $cur = if (Test-Path $dst) { (Get-FileHash $dst -Algorithm SHA256).Hash } else { $null }
            if ($cur -ne $b.sha256) {
                Copy-Item (Join-Path $backupDir $b.file) $dst -Force
                Write-Host "Restored $($b.file) (changed during the test session)."
            }
        } elseif (Test-Path $dst) {
            Move-Item $dst (Join-Path $backupDir "created-during-session-$($b.file)")
            Write-Host "Removed $($b.file) created during the test session (kept a copy in the evidence folder)."
        }
    }

    # Evidence: logs.
    $logDir = Join-Path $session.sessionDir 'logs'
    New-Item -ItemType Directory -Force $logDir | Out-Null
    foreach ($l in @('Player.log', 'Logs\DriveTheCityII.log', 'Logs\Modding.log', 'Logs\SceneFlow.log', 'Logs\Serialization.log')) {
        $p = Join-Path $DtcUserDataPath $l
        if (Test-Path $p) { Copy-Item $p (Join-Path $logDir (Split-Path $l -Leaf)) }
    }
    $bl = Join-Path $DtcTestRoot 'bridge.log'
    if (Test-Path $bl) { Copy-Item $bl (Join-Path $session.sessionDir 'bridge.log') }
    $cap = Join-Path $DtcTestRoot 'captures'
    if (Test-Path $cap) {
        $dstCap = Join-Path $session.sessionDir 'captures'
        New-Item -ItemType Directory -Force $dstCap | Out-Null
        Get-ChildItem $cap -File | Move-Item -Destination $dstCap
    }
    Remove-Item $sessionFile -Force
    Write-Host "STOPPED. Evidence: $($session.sessionDir)" -ForegroundColor Green
    exit 0
}

Write-Host 'Specify -Start, -Stop, or -Status.'
exit 2
