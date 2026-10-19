<#
.SYNOPSIS
  Real-time soak test (PHASE-12, BENCHMARK_PROTOCOL): plays the mod continuously for -Minutes of wall-clock time
  (default 120, not compressed) in a disposable lab city and records stability, memory and frame-time data.
.DESCRIPTION
  Activities rotate: taxi missions driven by the TEST AUTOPILOT (simulated input; career rules unchanged), exits
  and walking, returns to city building with the simulation running, saves while driving, and periodic reloads of the
  soak save. A background sampler records the game's process memory every 30 s.
  Output: %LOCALAPPDATA%\DriveTheCityII\test\campaigns\soak-<stamp>.md and soak-<stamp>-memory.csv.
  Requires the mod installed (tools\deploy.ps1 -Install) and no game running (it starts its own test session).
#>
param([int]$Minutes = 120, [string]$City = 'DTC2-LAB-Bench-S1', [string]$SaveName = 'DTC2-LAB-SOAK')

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'env.ps1')
$stamp = (Get-Date).ToString('yyyyMMdd-HHmmss')
$outDir = Join-Path $DtcTestRoot 'campaigns'
New-Item -ItemType Directory -Force $outDir | Out-Null
$memCsv = Join-Path $outDir "soak-$stamp-memory.csv"
$stopFlag = Join-Path $outDir "soak-$stamp.stop"
$events = New-Object System.Collections.Generic.List[string]
$counts = @{ missions = 0; missionFailures = 0; exits = 0; returns = 0; saves = 0; saveFailures = 0; loads = 0; loadFailures = 0; errors = 0 }

function Bridge([string]$cmds, [int]$timeout = 120) {
    $out = & powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot 'bridge.ps1') -Commands $cmds -TimeoutSeconds $timeout 2>&1
    return ($out | Out-String)
}
function Log([string]$text) {
    $line = "{0:HH:mm:ss} {1}" -f (Get-Date), $text
    $events.Add($line)
    Write-Host $line
}
function Alive() { return [bool](Get-Process -Name Cities2 -ErrorAction SilentlyContinue) }
function VehicleId() {
    $s = Bridge 'career status'
    if ($s -match '(V-[A-Za-z0-9]+):car\.basic:') { return $Matches[1] }
    if ($s -match 'vehicles=\[(V-[A-Za-z0-9]+):') { return $Matches[1] }
    return $null
}
function EnsureDriving() {
    $st = Bridge 'player state'
    if ($st -match 'player=Driving') { return $true }
    if ($st -match 'player=Building') { Bridge 'player enter;waitstate OnFoot 15' | Out-Null }
    $vid = VehicleId
    if (-not $vid) { Bridge 'career ui garage:rentbasic;wait 1.5' | Out-Null; $vid = VehicleId }
    $o = Bridge "career gotoveh $vid"
    if ($o -notmatch '< OK') { Bridge "career ui garage:call:$vid;wait 1.5;career gotoveh $vid" | Out-Null }
    $o = Bridge 'player interact;waitstate Driving 15'
    return ($o -match 'state Driving.*reached' -or (Bridge 'player state') -match 'player=Driving')
}

# Memory sampler (separate job; stops when the flag file appears or the game exits).
$sampler = Start-Job -ArgumentList $memCsv, $stopFlag -ScriptBlock {
    param($csv, $flag)
    'utc,workingSetMB,privateMB,handles' | Set-Content -Encoding UTF8 $csv
    while (-not (Test-Path $flag)) {
        $p = Get-Process -Name Cities2 -ErrorAction SilentlyContinue | Select-Object -First 1
        if ($p) { '{0:o},{1:0.0},{2:0.0},{3}' -f (Get-Date).ToUniversalTime(), ($p.WorkingSet64 / 1MB), ($p.PrivateMemorySize64 / 1MB), $p.HandleCount | Add-Content -Encoding UTF8 $csv }
        Start-Sleep -Seconds 30
    }
}

$crashed = $false
try {
    & powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot 'test-session.ps1') -Start | Out-Host
    $o = Bridge "waitmenu 300;load $City;waitcity 900;wait 4" 1200
    if ($o -notmatch "load $City -> True") { throw "could not load $City" }
    Bridge 'perf reset' | Out-Null
    Log "loaded $City; soak for $Minutes minutes"
    $start = Get-Date
    $nextSave = $start.AddMinutes(20)
    $nextLoad = $start.AddMinutes(40)
    $nextPerf = $start.AddMinutes(10)
    $iter = 0
    while (((Get-Date) - $start).TotalMinutes -lt $Minutes) {
        if (-not (Alive)) { $crashed = $true; Log 'GAME PROCESS ENDED'; break }
        $iter++
        # Career time stops while the game window is unfocused (treated as paused); keep the game in front.
        & powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot 'realinput.ps1') -Steps 'focus' | Out-Null
        try {
            if (-not (EnsureDriving)) { $counts.errors++; Log "iteration ${iter}: could not get into a vehicle"; Bridge 'player building;wait 2' | Out-Null; continue }
            $o = Bridge 'career offers Taxi'
            $best = -1; $bestM = [double]::MaxValue
            foreach ($m in [regex]::Matches($o, '#(\d+) .*? (\d+)m pay .*? eligible=True')) { if ([double]$m.Groups[2].Value -lt $bestM) { $bestM = [double]$m.Groups[2].Value; $best = [int]$m.Groups[1].Value } }
            if ($best -ge 0) {
                $a = Bridge "career accept $best Taxi"
                if ($a -match 'accept .*: Ok') {
                    $d = Bridge 'career drive 900' 1000
                    if ($d -match 'mission finished') { $counts.missions++; Log "taxi mission $($counts.missions) done" }
                    else { $counts.missionFailures++; Log ("taxi mission not finished: " + (($d -split "`n" | Select-String 'career drive') -join ' ')); Bridge 'career cancel' | Out-Null }
                }
            }
            # Exit, walk a little, get back in.
            $x = Bridge 'player exit;waitstate OnFoot 15;input fwd=1 t=4;wait 1'
            if ($x -match 'state OnFoot.*reached') { $counts.exits++ }
            if ($iter % 3 -eq 0) {
                Bridge 'player building;wait 30' 90 | Out-Null
                $counts.returns++
            }
            if ((Get-Date) -ge $nextSave) {
                EnsureDriving | Out-Null
                $s = Bridge "save $SaveName;wait 2" 300
                if ($s -match "save $SaveName -> True") { $counts.saves++; Log 'saved while driving' } else { $counts.saveFailures++; Log 'SAVE FAILED' }
                $nextSave = (Get-Date).AddMinutes(20)
            }
            if ((Get-Date) -ge $nextLoad) {
                $l = Bridge "load $SaveName;waitcity 900;wait 4;leakcheck;career status" 1200
                if ($l -match "load $SaveName -> True") { $counts.loads++; Log ('reloaded soak save :: ' + (($l -split "`n" | Select-String 'leakcheck') -join ' ')) } else { $counts.loadFailures++; Log 'LOAD FAILED' }
                $nextLoad = (Get-Date).AddMinutes(40)
            }
            if ((Get-Date) -ge $nextPerf) {
                Log ('perf :: ' + ((Bridge 'perf') -split "`n" | Select-String '< OK' | Select-Object -Last 1))
                $co = ((Bridge 'career company') -split "`n" | Select-String '< OK' | Select-Object -Last 1)
                if ($co -match 'company=(\S+).*?cash=(-?\d+).*?lastPeriod=(-?\d+).*?jobs=(\d+)') { Log ("company {0}: cash {1}, last settled period {2}, simulated jobs {3}" -f $Matches[1], $Matches[2], $Matches[3], $Matches[4]) }
                $nextPerf = (Get-Date).AddMinutes(10)
            }
        }
        catch {
            $counts.errors++
            Log "iteration $iter error: $($_.Exception.Message)"
        }
    }
    $elapsed = ((Get-Date) - $start).TotalMinutes
    if (Alive) {
        $final = Bridge 'perf;career company;player building;wait 2;leakcheck;career status;status'
        Log ('final :: ' + (($final -split "`n" | Select-String '< (OK|ERR)') -join "`n    "))
    }
}
catch {
    Log "SOAK ABORTED: $($_.Exception.Message)"
}
finally {
    New-Item -ItemType File -Force $stopFlag | Out-Null
    Receive-Job $sampler -Wait -AutoRemoveJob | Out-Null
    $mem = @(Import-Csv $memCsv)
    $memLine = if ($mem.Count -ge 2) { "private memory {0} MB -> {1} MB (min {2}, max {3}); working set {4} MB -> {5} MB; samples {6}" -f $mem[0].privateMB, $mem[-1].privateMB, ($mem | Measure-Object privateMB -Minimum).Minimum, ($mem | Measure-Object privateMB -Maximum).Maximum, $mem[0].workingSetMB, $mem[-1].workingSetMB, $mem.Count } else { 'no memory samples' }
    $md = "# Soak $stamp`n`n- Planned: $Minutes min real time; ran: {0:0.0} min; game crashed: $crashed`n- Counts: $(($counts.GetEnumerator() | Sort-Object Name | ForEach-Object { "$($_.Name)=$($_.Value)" }) -join ', ')`n- Memory: $memLine (CSV: $memCsv)`n- Input: missions use the TEST AUTOPILOT (simulated input).`n`n## Events`n`n``````text`n$($events -join "`n")`n```````n" -f $(if ($elapsed) { $elapsed } else { 0 })
    Set-Content -Encoding UTF8 (Join-Path $outDir "soak-$stamp.md") $md
    Remove-Item $stopFlag -ErrorAction SilentlyContinue
    if (Alive) { & powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot 'test-session.ps1') -Stop | Out-Host }
    Write-Host "Soak report: $(Join-Path $outDir "soak-$stamp.md")"
}
