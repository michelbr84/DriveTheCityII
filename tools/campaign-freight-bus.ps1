<#
.SYNOPSIS
  Repeats freight and bus jobs in a test session until each completes in game (or the attempt limit is reached).
  Driving uses the TEST AUTOPILOT (simulated input; career rules unchanged). Each attempt is recorded.
.DESCRIPTION
  Requires the mod installed. Starts its own test session, loads -City, and stops the session at the end.
  Results: %LOCALAPPDATA%\DriveTheCityII\test\campaigns\freight-bus-<stamp>.md
#>
param([string]$City = 'DTC2-LAB-CAREERS-H', [int]$FreightAttempts = 4, [int]$BusAttempts = 3, [switch]$SkipFreight, [switch]$SkipBus, [switch]$NoDrive, [string]$BusStartNear = '', [int]$WarpMinutes = 0)

. (Join-Path $PSScriptRoot 'env.ps1')
$stamp = Get-Date -Format yyyyMMdd-HHmmss
$lines = New-Object System.Collections.Generic.List[string]
function Bridge([string]$cmds, [int]$timeout = 120) {
    $out = & powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot 'bridge.ps1') -Commands $cmds -TimeoutSeconds $timeout 2>&1
    return ($out | Out-String)
}
function Last([string]$text, [string]$cmd) { return [string]($text -split "`n" | Where-Object { $_ -match "< (OK|ERR).*$cmd" } | Select-Object -Last 1) }
function Note([string]$t) { $l = "{0:HH:mm:ss} {1}" -f (Get-Date), $t; $lines.Add($l); Write-Host $l }
function Status() { return (Last (Bridge 'career status') 'career status') }
function OnFoot() {
    Bridge 'input brake=1 t=2;input hand=1 t=1;player exit;waitstate OnFoot 15' | Out-Null
    if ((Bridge 'player state') -notmatch 'player=OnFoot') { Bridge 'player enter;waitstate OnFoot 15' | Out-Null }
}
function OwnCar() {
    # Back in the player's own rental, repaired at a service point if it is worn (repairs of the basic rental are free).
    $s = Status
    if ($s -notmatch '(V-[A-Za-z0-9]+):car\.basic:BasicRental:(\w+):fuel\d+:cond(\d+)') { return }
    $vid = $Matches[1]; $cond = [int]$Matches[3]
    OnFoot
    if ($cond -lt 60 -or $Matches[2] -eq 'Immobilized') { Bridge "career goplace ServicePoint;wait 1;career ui garage:repair:$vid" | Out-Null }
    Bridge "career ui garage:call:$vid;wait 2;career gotoveh $vid;player interact;waitstate Driving 15" | Out-Null
}
function Attempt([string]$career, [int]$seconds) {
    if ($career -eq 'Bus' -and $BusStartNear) {
        # TEST SETUP: start the shift near the line's first stop (the employer's bus is provided near the player).
        OnFoot
        $t = Last (Bridge "tpav $BusStartNear;wait 2") 'tpav'
        Note "Bus start: $(($t -split ' :: ')[-1])"
    }
    OwnCar
    $offers = Last (Bridge "career offers $career") 'career offers'
    if ($offers -notmatch 'eligible=True' -and $career -eq 'Freight') {
        # Offers are generated near the player: after a failed job far from industry there may be none. Go to the
        # nearest industrial place with the car and ask again.
        OnFoot
        Bridge 'career goplace Industrial;wait 1' | Out-Null
        OwnCar
        $offers = Last (Bridge "career offers $career") 'career offers'
    }
    $best = -1; $bestM = [double]::MaxValue
    foreach ($m in [regex]::Matches($offers, '#(\d+) .*? (\d+)m pay .*? eligible=True')) { if ([double]$m.Groups[2].Value -lt $bestM) { $bestM = [double]$m.Groups[2].Value; $best = [int]$m.Groups[1].Value } }
    if ($best -lt 0) { Note "$career no eligible offer: $(($offers -split ' :: ')[-1])"; Bridge 'wait 20' | Out-Null; return $false }
    $acc = Last (Bridge "career accept $best $career;wait 3") 'career accept'
    Note "$career accepted: $(($acc -split ' :: ')[-1])"
    $st = Status
    if ($st -notmatch '(SV-?[A-Za-z0-9]+):[a-z]+\.service:Service') { Note "$career no provided vehicle in status"; Bridge 'career cancel' | Out-Null; return $false }
    $svc = $Matches[1]
    OnFoot
    $b = Bridge "career gotoveh $svc;player interact;waitstate Driving 15"
    if ($b -notmatch 'state Driving reached') { Note "$career could not board $svc"; Bridge 'career cancel' | Out-Null; return $false }
    if ($NoDrive) { Note "$career boarded $svc; -NoDrive: stopping here with the session running"; return $true }
    $d = Last (Bridge "career drive $seconds" ($seconds + 120)) 'career drive'
    for ($r = 1; $r -le 2 -and $d -match 'returned to city building' -and (Status) -match 'mission=\[(Freight|Bus)/Active'; $r++) {
        # The player was sent back to city building (a recovery found no safe spot) while the job is still on: get back
        # in the employer's vehicle and carry on, as a player would.
        Note "$career resume $r after return to city building"
        Bridge 'player enter;waitstate OnFoot 20' | Out-Null
        $b2 = Bridge "career gotoveh $svc;player interact;waitstate Driving 15"
        if ($b2 -notmatch 'state Driving reached') { Note "$career could not re-board $svc"; break }
        $d = Last (Bridge "career drive $seconds" ($seconds + 120)) 'career drive'
    }
    $paid = $d -match 'mission finished' -and $d -match 'last=\[MissionSettlement:([1-9]\d*)'
    Note ("{0} attempt result: {1} :: {2}" -f $career, $(if ($paid) { 'COMPLETED AND PAID' } else { 'not completed' }), (($d -split ' :: ', 2)[-1]))
    if (-not ($d -match 'mission finished')) { Bridge 'career cancel' | Out-Null }
    return $paid
}

try {
    & powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot 'test-session.ps1') -Start | Out-Host
    $o = Bridge "waitmenu 300;load $City;waitcity 900;wait 4" 1200
    if ($o -notmatch "load $City -> True") { throw "could not load $City" }
    Note "loaded $City :: $(((Status) -split ' :: ')[-1])"
    if ($WarpMinutes -gt 0) {
        # TEST SETUP: run the city at 3x for a while first (the lab city's downtown is jammed in the evening rush hour;
        # the night is quieter). Career time follows the game clock as for any player waiting.
        $w = Bridge "speed 3;wait $($WarpMinutes * 60);speed 1" ($WarpMinutes * 60 + 120)
        Note "warped $WarpMinutes min at 3x :: $(((Status) -split ' :: ')[-1])"
    }
    $freightOk = $false; $busOk = $false
    if (-not $SkipFreight) {
        for ($i = 1; $i -le $FreightAttempts -and -not $freightOk; $i++) { Note "freight attempt $i"; $freightOk = Attempt 'Freight' 3000 }
    }
    if (-not $SkipBus) {
        for ($i = 1; $i -le $BusAttempts -and -not $busOk; $i++) { Note "bus attempt $i"; $busOk = Attempt 'Bus' 7200 }
    }
    if (-not $NoDrive) { $s = Bridge "save $City-R;wait 2" }
    Note "freight completed: $freightOk; bus completed: $busOk"
}
catch { Note "ABORTED: $($_.Exception.Message)" }
finally {
    $file = Join-Path $DtcTestRoot "campaigns\freight-bus-$stamp.md"
    Set-Content -Encoding UTF8 $file ("# Freight and bus attempts $stamp`n`nDriving: TEST AUTOPILOT (simulated input).`n`n``````text`n" + ($lines -join "`n") + "`n```````n")
    if (-not $NoDrive) { & powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot 'test-session.ps1') -Stop | Out-Host }
    Write-Host "Report: $file"
}
