<#
.SYNOPSIS
  Career coverage campaign in a running test session (lab city loaded): garage at a service point (call, repair,
  buy, refuel charged once, sell, return), delivery, freight with the shipper's truck, bus line with the operator's
  bus, challenge run, home, and company founding/hiring/contract/fleet.
.DESCRIPTION
  Driving uses the TEST AUTOPILOT (simulated input; career rules unchanged). Progression gates are opened with test
  seeding (career xp/rep/grant), which marks the profile "test-seeded" permanently; this is labelled in the report.
  Writes %LOCALAPPDATA%\DriveTheCityII\test\campaigns\careers-<stamp>.md.
#>
param([string]$SaveName = 'DTC2-LAB-CAREERS', [switch]$SkipBus)

$stamp = Get-Date -Format yyyyMMdd-HHmmss
$results = New-Object System.Collections.Generic.List[object]
function Bridge([string]$cmds, [int]$timeout = 120) {
    $out = & powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot 'bridge.ps1') -Commands $cmds -TimeoutSeconds $timeout 2>&1
    return ($out | Out-String)
}
function Last([string]$text, [string]$cmd) { return [string]($text -split "`n" | Where-Object { $_ -match "< (OK|ERR).*$cmd" } | Select-Object -Last 1) }
function Tail([string]$line, [int]$n = 260) { if (-not $line) { return '' }; $x = ($line -split ' :: ', 2)[-1]; return $x.Substring(0, [Math]::Min($n, $x.Length)) }
function Wallet([string]$text) { if ($text -match 'wallet=(-?\d+) entries=(\d+)') { return @([long]$Matches[1], [int]$Matches[2]) } return @(-1, -1) }
function Record([string]$step, [bool]$ok, [string]$detail) {
    $results.Add([pscustomobject]@{ Step = $step; Result = $(if ($ok) { 'Passed' } else { 'Failed' }); Detail = $detail })
    Write-Host ("[{0}] {1} :: {2}" -f $(if ($ok) { 'PASS' } else { 'FAIL' }), $step, $detail)
}
function Status() { return (Last (Bridge 'career status') 'career status') }
function OnFoot() { Bridge 'player exit;waitstate OnFoot 15' | Out-Null; if ((Bridge 'player state') -notmatch 'player=OnFoot') { Bridge 'player enter;waitstate OnFoot 15' | Out-Null } }
function Cond([string]$status, [string]$vid) { if ($status -match "${vid}:[^,\]]*:cond(\d+)") { return [int]$Matches[1] }; return -1 }
function Fuel([string]$status, [string]$vid) { if ($status -match "${vid}:[^,\]]*:fuel(\d+)") { return [int]$Matches[1] }; return -1 }
function VehicleOf([string]$status, [string]$pattern) { if ($status -match "(V-[A-Za-z0-9]+):$pattern") { return $Matches[1] }; return $null }
function Board([string]$vid) {
    $o = Bridge "career gotoveh $vid;player interact;waitstate Driving 15"
    return ($o -match 'state Driving reached')
}
function RunCareer([string]$career, [int]$seconds) {
    $offers = Last (Bridge "career offers $career") 'career offers'
    $best = -1; $bestM = [double]::MaxValue
    foreach ($m in [regex]::Matches($offers, '#(\d+) .*? (\d+)m pay .*? eligible=True')) { if ([double]$m.Groups[2].Value -lt $bestM) { $bestM = [double]$m.Groups[2].Value; $best = [int]$m.Groups[1].Value } }
    if ($best -lt 0) { return @{ ok = $false; detail = "no eligible offer: $(Tail $offers 300)" } }
    $acc = Last (Bridge "career accept $best $career;wait 2") 'career accept'
    if ($acc -notmatch ': Ok') { return @{ ok = $false; detail = "accept failed: $(Tail $acc)" } }
    $st = Status
    # Employer-provided vehicle (freight truck, bus): walk to it and drive it.
    $svc = VehicleOf $st '[a-z]+\.service:Service'
    if ($svc) {
        OnFoot
        if (-not (Board $svc)) { return @{ ok = $false; detail = "could not board the provided vehicle $svc" } }
    }
    elseif ((Bridge 'player state') -notmatch 'player=Driving') {
        $own = VehicleOf $st '[a-z]+\.[a-z]+:[A-Za-z]+:(InUse|Parked)'
        if ($own) { Board $own | Out-Null }
    }
    $d = Last (Bridge "career drive $seconds" ($seconds + 120)) 'career drive'
    return @{ ok = ($d -match 'mission finished' -and $d -notmatch 'MissionSettlement:0;'); detail = (Tail $d 400); svc = $svc }
}

# ---- Setup: on foot, test-seeded money for purchases.
$s0 = Status
$w0 = Wallet $s0
$basic = VehicleOf $s0 'car\.basic'
Record 'Start state' ($w0[0] -ge 0) (Tail $s0)

# ---- Pause: career time must not advance while the game is paused (setting on by default).
$c0 = if ($s0 -match 'clock=(\d+)') { [int]$Matches[1] } else { -1 }
$o = Bridge 'speed 0;wait 8;career status;speed 1'
$c1 = if ((Last $o 'career status') -match 'clock=(\d+)') { [int]$Matches[1] } else { -1 }
Record 'Career clock stops while the game is paused (8 s real time)' ($c0 -ge 0 -and ($c1 - $c0) -le 1) ("clock {0} -> {1}" -f $c0, $c1)

# ---- Garage at a service point.
OnFoot
$o = Last (Bridge 'career goplace ServicePoint;wait 1') 'career goplace'
Record 'Walk to a service point (test teleport)' ($o -match '< OK') (Tail $o)
if ($basic) {
    $c0 = Cond $s0 $basic
    $o = Bridge "career ui garage:call:$basic;wait 2;career ui garage:repair:$basic;wait 1;career status"
    $c1 = Cond (Last $o 'career status') $basic
    Record 'Call the rental to the service point and repair it' ($c1 -ge 99 -and $c1 -ge $c0) ("condition {0}% -> {1}%; {2}" -f $c0, $c1, (Tail (Last $o 'garage:repair') 160))
}
$g = Last (Bridge 'career grant 400000') 'career grant'
Record 'Test seeding: grant 4,000 cr (profile marked test-seeded)' ($g -match '< OK') (Tail $g)
$o = Bridge 'career ui garage:buy:car.used;wait 2;career status'
$st = Last $o 'career status'
$owned = VehicleOf $st 'car\.used'
Record 'Buy a used car' ([bool]$owned) (Tail (Last $o 'garage:buy'))
if ($owned) {
    $wBefore = Wallet $st
    # Drive it a little, set the tank to 30 % (test seeding), then refuel twice in the same instant (double click).
    Board $owned | Out-Null
    Bridge 'input throttle=1 t=6;input brake=1 t=3;input hand=1 t=1' 60 | Out-Null
    OnFoot
    Bridge "career fuel $owned 0.3;career goplace ServicePoint;wait 1" | Out-Null
    $o = Bridge "career ui garage:call:$owned;wait 2;career status"
    $mid = Wallet (Last $o 'career status')
    $f0 = Fuel (Last $o 'career status') $owned
    $o = Bridge "career ui garage:refuel:$owned;career ui garage:refuel:$owned;wait 1;career status"
    $after = Wallet (Last $o 'career status')
    $f1 = Fuel (Last $o 'career status') $owned
    Record 'Refuel at a service point, charged once on a double click' ($f1 -ge 99 -and $f1 -gt $f0 -and $after[1] -eq $mid[1] + 1 -and $after[0] -lt $mid[0]) ("fuel {0}% -> {1}%; ledger entries {2} -> {3}; wallet {4} -> {5}" -f $f0, $f1, $mid[1], $after[1], $mid[0], $after[0])
    $o = Bridge "career ui garage:sell:$owned;wait 1;career status"
    $sold = Wallet (Last $o 'career status')
    Record 'Sell the used car' ($sold[0] -gt $after[0]) ("wallet {0} -> {1}" -f $after[0], $sold[0])
}

# ---- Delivery with the rental car.
if ($basic) { Bridge "career ui garage:call:$basic;wait 2" | Out-Null; Board $basic | Out-Null }
$r = RunCareer 'Delivery' 1200
Record 'Delivery job (load, multi-stop drop-offs, settlement)' $r.ok $r.detail

# ---- Freight with the shipper's service truck (gate opened by test seeding).
Bridge 'career xp Delivery 300' | Out-Null
$r = RunCareer 'Freight' 1500
Record 'Freight job with the provided rigid truck (test-seeded unlock)' $r.ok $r.detail

# ---- Bus line with the operator's bus.
if (-not $SkipBus) {
    Bridge 'career xp Taxi 300' | Out-Null
    $r = RunCareer 'Bus' 2400
    Record 'Bus line run with the provided bus (test-seeded unlock)' $r.ok $r.detail
}

# ---- Challenge.
$st = Status
$veh = VehicleOf $st 'car\.basic'
if ($veh -and (Bridge 'player state') -notmatch 'player=Driving') { OnFoot; Bridge "career ui garage:call:$veh;wait 2" | Out-Null; Board $veh | Out-Null }
$c = Last (Bridge 'career ui challenges:regen;wait 1;career challenges') 'career challenges'
$cid = if ($c -match 'challenges=\[([^:\]]+):') { $Matches[1] } else { $null }
if ($cid) {
    $o = Bridge "career ui challenges:start:$cid;wait 1;career chdrive 900;career challenges" 1100
    $res = Last $o 'career challenges'
    Record 'Challenge run (checkpoints, time, record)' ($res -match "${cid}:[^|]*tries1" -or $res -match "${cid}:[^|]*:best=\d") (Tail $res 400)
}
else { Record 'Challenge run (checkpoints, time, record)' $false "no challenge generated: $(Tail $c)" }

# ---- Home.
OnFoot
$o = Bridge 'career goplace Residential;wait 1;career ui profile:sethome;wait 1;career status'
Record 'Set home at the nearest house' ((Last $o 'profile:sethome') -match '< OK') (Tail (Last $o 'profile:sethome'))
$o = Bridge 'input fwd=1 run=1 t=6;career ui profile:gohome;wait 2;player state'
Record 'Go home' ((Last $o 'profile:gohome') -match '< OK') (Tail (Last $o 'profile:gohome'))

# ---- Company (gates opened by test seeding).
Bridge 'career xp Taxi 1150;career xp Delivery 300;career rep 60;career grant 2500000' | Out-Null
Bridge 'career goplace Office;wait 1' | Out-Null
$o = Bridge 'career ui company:found;wait 1;career company'
$co = Last $o 'career company'
Record 'Found a company (test-seeded gates)' ($co -match '< OK' -and $co -notmatch 'company=none') (Tail $co 300)
$cand = if ($co -match 'candidates=\[([^:\]]+):') { $Matches[1] } else { $null }
$ctr = $null; $cls = 'Car'
if ($co -match 'contractOffers=\[([^:\]]+):([A-Za-z]+):([A-Za-z]+):') { $ctr = $Matches[1]; $cls = $Matches[3] }
$model = @{ Car = 'car.used'; Taxi = 'taxi.sedan'; Van = 'van.light'; RigidTruck = 'truck.rigid'; Bus = 'bus.city' }[$cls]
if (-not $model) { $model = 'car.used' }
$o = Bridge "career ui company:deposit:1500000;career ui company:hire:$cand;career ui company:buy:$model;career ui company:accept:$ctr;career ui company:autoassign;wait 1;career company"
$co = Last $o 'career company'
Record "Deposit, hire, buy a fleet vehicle ($model), accept a contract ($cls), assign" ($co -match 'employees=\[[^\]]' -and $co -match 'fleet=\[V' -and $co -match 'contracts=\[[^\]]') (Tail $co 400)

$o = Bridge "save $SaveName;wait 2;career status" 300
Record "Save $SaveName with all careers' state" ($o -match "save $SaveName -> True") (Tail (Last $o 'career status') 300)

$md = "# Career coverage campaign $stamp`n`nDriving: TEST AUTOPILOT (simulated input). Unlocks and money: test seeding (profile marked test-seeded).`n`n| Step | Result | Detail |`n|---|---|---|`n"
foreach ($x in $results) { $md += "| $($x.Step) | $($x.Result) | $($x.Detail -replace '\|','/') |`n" }
$file = Join-Path $env:LOCALAPPDATA "DriveTheCityII\test\campaigns\careers-$stamp.md"
Set-Content -Encoding UTF8 $file $md
Write-Host "Report: $file"
