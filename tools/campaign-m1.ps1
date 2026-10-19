<#
.SYNOPSIS
  M1 acceptance campaign (ROADMAP §3.1, PHASE-05 §13): the required playable loop with REAL keyboard input:
  load disposable city -> walk -> enter vehicle -> accelerate/steer/brake -> stop -> exit -> walk ->
  return to city management -> save -> close the game -> relaunch -> reload -> continue safely.
.DESCRIPTION
  Requires the mod to be installed (tools\deploy.ps1 -Install). Uses tools\test-session.ps1 (isolated session),
  tools\bridge.ps1 (state checks and setup) and tools\realinput.ps1 (real keys to the verified game window).
  The vehicle comes from a career basic rental when the career is available, otherwise a test vehicle.
  Writes a result table to %LOCALAPPDATA%\DriveTheCityII\test\campaigns\m1-<timestamp>.md.
#>
param([string]$City = 'DTC2-LAB-Bench-S1', [string]$SaveName = 'DTC2-LAB-M1', [switch]$KeepRunning)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'env.ps1')
$results = New-Object System.Collections.Generic.List[object]
$stamp = (Get-Date).ToString('yyyyMMdd-HHmmss')
$outDir = Join-Path $DtcTestRoot 'campaigns'
New-Item -ItemType Directory -Force $outDir | Out-Null

function Bridge([string]$cmds, [int]$timeout = 120) {
    $out = & powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot 'bridge.ps1') -Commands $cmds -TimeoutSeconds $timeout 2>&1
    return ($out | Out-String)
}
function Keys([string]$steps) {
    $out = & powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot 'realinput.ps1') -Steps $steps 2>&1
    if ($LASTEXITCODE -ne 0) { throw "real input failed: $out" }
}
function State() {
    $s = Bridge 'player state'
    if ($s -match 'player=(\w+)') { return $Matches[1] }
    return 'unknown'
}
function Field([string]$text, [string]$name) {
    if ($text -match "$name=\(([-\d\.]+), ([-\d\.]+), ([-\d\.]+)\)") { return @([double]$Matches[1], [double]$Matches[2], [double]$Matches[3]) }
    return $null
}
function Speed([string]$text, [double]$fallback) {
    # Vehicle speed from the player line (" speed=X gear="); case-sensitive so "aSpeed=" never matches.
    if ($text -cmatch ' speed=([-\d\.]+) gear=') { return [double]$Matches[1] }
    return $fallback
}
function Num([string]$text, [string]$name) {
    if ($text -cmatch "$name=([-\d\.]+)") { return [double]$Matches[1] }
    return [double]::NaN
}
function TrackWhile([double]$seconds, [string]$steps) {
    # Starts the in-game "track" measurement, waits until the game picked it up, then sends the real keys.
    $bridgeScript = Join-Path $PSScriptRoot 'bridge.ps1'
    $job = Start-Job -ScriptBlock { param($b, $sec) & powershell -NoProfile -ExecutionPolicy Bypass -File $b -Commands "track $sec" -TimeoutSeconds 120 2>&1 | Out-String } -ArgumentList $bridgeScript, $seconds
    $log = Join-Path $DtcTestRoot 'bridge.log'
    $deadline = (Get-Date).AddSeconds(30)
    while ((Get-Date) -lt $deadline) {
        if ((Get-Content $log -Tail 4 -Encoding UTF8 -ErrorAction SilentlyContinue) -match '> track') { break }
        Start-Sleep -Milliseconds 100
    }
    Keys $steps
    $out = Receive-Job $job -Wait -AutoRemoveJob
    return (($out | Out-String) -split "`n" | Where-Object { $_ -match '< (OK|ERR).*track' } | Select-Object -Last 1)
}
function Record([string]$step, [bool]$ok, [string]$detail) {
    $results.Add([pscustomobject]@{ Step = $step; Result = $(if ($ok) { 'Passed' } else { 'Failed' }); Detail = $detail })
    Write-Host ("[{0}] {1} :: {2}" -f $(if ($ok) { 'PASS' } else { 'FAIL' }), $step, $detail)
}
function Dist($a, $b) { if (-not $a -or -not $b) { return -1 }; return [math]::Sqrt(($a[0]-$b[0])*($a[0]-$b[0]) + ($a[2]-$b[2])*($a[2]-$b[2])) }

try {
    & powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot 'test-session.ps1') -Start | Out-Host
    $o = Bridge "waitmenu 300;load $City;waitcity 900;wait 4;status" 1200
    Record 'Load disposable city' ($o -match "city='DTC2-LAB") ($o -split "`n" | Select-String "status ::" | Select-Object -Last 1)

    Keys 'focus;tap F8;sleep 1500'
    $st = Bridge 'player state'
    Record 'Enter character mode (real F8)' ($st -match 'player=OnFoot') ''
    $a0 = Field $st 'avatar'

    Keys 'hold W 2500;sleep 300'
    $st = Bridge 'player state'
    $a1 = Field $st 'avatar'
    $walk = Dist $a0 $a1
    Record 'Walk (real W 2.5 s)' ($walk -gt 2.0) ("moved {0:0.0} m" -f $walk)

    $rent = Bridge 'career ui garage:rentbasic;wait 1.5;career status'
    if ($rent -notmatch 'car.basic') { Bridge 'spawncar car;wait 1.5' | Out-Null; $vehicleSource = 'test vehicle' } else { $vehicleSource = 'career basic rental' }
    Bridge 'gotoroad;wait 1' | Out-Null
    Keys 'tap E;sleep 1500'
    $st = Bridge 'player state'
    Record 'Enter vehicle (real E)' ($st -match 'player=Driving') "vehicle: $vehicleSource"

    # Accelerate then brake in one measured window (real W, then real S).
    $t = TrackWhile 7 'hold W 3000;hold S 2500'
    Record 'Accelerate (real W 3 s)' ((Num $t 'maxSpeed') -gt 3 -and (Num $t 'dist') -gt 5) ("max {0:0.0} m/s, {1:0.0} m, impacts {2}" -f (Num $t 'maxSpeed'), (Num $t 'dist'), (Num $t 'impacts'))
    Record 'Brake to a stop (real S)' ((Num $t 'stoppedAt') -gt 0) ("stopped {0:0.00} s into the window (S pressed at about 3 s); gears {1}" -f (Num $t 'stoppedAt'), $(if ($t -match 'gears=(\S+)') { $Matches[1] } else { '?' }))
    # Steer while moving (real D then A with W held), then stop.
    $t = TrackWhile 7 'down W;sleep 1200;down D;sleep 700;up D;down A;sleep 700;up A;up W;hold S 2500'
    Record 'Steer (real D, A while W held)' ((Num $t 'maxYawChange') -gt 5) ("max heading change {0:0} deg, max {1:0.0} m/s, impacts {2}" -f (Num $t 'maxYawChange'), (Num $t 'maxSpeed'), (Num $t 'impacts'))
    $st = Bridge 'player state'
    Keys 'hold SPACE 1500'
    $st = Bridge 'player state'
    Record 'Stopped before exit' ([math]::Abs((Speed $st 99)) -lt 0.6) ("speed {0:0.00} m/s" -f (Speed $st 99))

    Keys 'hold SPACE 400;tap E;sleep 2000'
    $st = Bridge 'player state'
    Record 'Park and exit (real Space, E)' ($st -match 'player=OnFoot') (($st -split "aGround=")[1] -split ' ')[0]
    $a2 = Field $st 'avatar'

    Keys 'hold W 1500;sleep 300'
    $st = Bridge 'player state'
    Record 'Walk after exit (real W)' ((Dist $a2 (Field $st 'avatar')) -gt 1.0) ''

    Keys 'tap F8;sleep 1500'
    $st = Bridge 'status'
    Record 'Return to city management (real F8)' ($st -match 'player=Building' -and $st -match 'actors=0') ''

    $o = Bridge "save $SaveName;wait 2" 300
    Record 'Save' ($o -match "save $SaveName -> True") ''
    $o = Bridge 'quit' 60
    $deadline = (Get-Date).AddSeconds(90)
    while ((Get-Process -Name Cities2 -ErrorAction SilentlyContinue) -and (Get-Date) -lt $deadline) { Start-Sleep 1 }
    Record 'Close the game' (-not (Get-Process -Name Cities2 -ErrorAction SilentlyContinue)) ''
    & powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot 'test-session.ps1') -Stop | Out-Host

    & powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot 'test-session.ps1') -Start | Out-Host
    $o = Bridge "waitmenu 300;load $SaveName;waitcity 900;wait 4;leakcheck;career status" 1200
    Record 'Relaunch and reload' ($o -match "load $SaveName -> True") ''
    Record 'No mod entity persisted in the save' ($o -match 'excluded-signature cars=0 actors=0') ''
    Keys 'focus;tap F8;sleep 1500'
    $st = Bridge 'player state'
    Record 'Continue safely (real F8 after reload)' ($st -match 'player=OnFoot') ''
    Keys 'tap F8;sleep 1000'
    Record 'Return to Building after reload' ((State) -eq 'Building') ''
    if (-not $KeepRunning) {
        & powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot 'test-session.ps1') -Stop | Out-Host
    }
}
catch {
    Record 'Campaign aborted' $false $_.Exception.Message
}
finally {
    $md = "# M1 campaign $stamp`n`n| Step | Result | Detail |`n|---|---|---|`n"
    foreach ($r in $results) { $md += "| $($r.Step) | $($r.Result) | $($r.Detail -replace '\|','/') |`n" }
    $file = Join-Path $outDir "m1-$stamp.md"
    Set-Content -Encoding UTF8 $file $md
    Write-Host "Campaign report: $file"
}
