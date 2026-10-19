<#
.SYNOPSIS
  Career persistence campaign (SPEC-SAVE, REQ-SAVE, exactly-once payouts) in a running test session:
  save A (after earlier payouts) -> accept a taxi fare -> drive until the passenger is aboard -> save B mid-mission ->
  reload B -> bring the mission car back -> finish the fare -> save C -> reload C (no second payout) ->
  reload A (older balance restored exactly).
.DESCRIPTION
  Requires a running test session with a lab city loaded and the player driving a career vehicle.
  Driving uses the TEST AUTOPILOT (simulated input; career rules unchanged).
#>
param([string]$Prefix = 'DTC2-LAB-CAREER')

$results = New-Object System.Collections.Generic.List[object]
function Bridge([string]$cmds, [int]$timeout = 120) {
    $out = & powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot 'bridge.ps1') -Commands $cmds -TimeoutSeconds $timeout 2>&1
    return ($out | Out-String)
}
function Last([string]$text, [string]$cmd) { return ($text -split "`n" | Where-Object { $_ -match "< (OK|ERR).*$cmd" } | Select-Object -Last 1) }
function Wallet([string]$text) { if ($text -match 'wallet=(-?\d+) entries=(\d+)') { return @([long]$Matches[1], [int]$Matches[2]) } return @(-1, -1) }
function Record([string]$step, [bool]$ok, [string]$detail) {
    $results.Add([pscustomobject]@{ Step = $step; Result = $(if ($ok) { 'Passed' } else { 'Failed' }); Detail = $detail })
    Write-Host ("[{0}] {1} :: {2}" -f $(if ($ok) { 'PASS' } else { 'FAIL' }), $step, $detail)
}
function Rejoin([string]$vid) {
    # After a load the player is in city-building mode; walk out, call the mission car, and get in.
    Bridge "player enter;waitstate OnFoot 15;career ui garage:call:$vid;wait 1.5;career gotoveh $vid;player interact;waitstate Driving 15" 120 | Out-Null
    return ((Bridge 'player state') -match 'player=Driving')
}

$st = Bridge 'career status'
$a = Wallet $st
$vid = if ($st -match 'vehicles=\[(V-[A-Za-z0-9]+):') { $Matches[1] } else { $null }
Record 'Start state' ($a[0] -ge 0 -and $vid) ("wallet {0}, ledger entries {1}, vehicle {2}" -f $a[0], $a[1], $vid)

$o = Bridge "save $Prefix-A;wait 2" 300
Record 'Save A' ($o -match "save $Prefix-A -> True") ''

# Accept the shortest offer and drive until the passenger is aboard (leg index 1).
$offers = Last (Bridge 'career offers Taxi') 'career offers'
$best = -1; $bestM = [double]::MaxValue
foreach ($m in [regex]::Matches($offers, '#(\d+) .*? (\d+)m pay .*? eligible=True')) { if ([double]$m.Groups[2].Value -lt $bestM) { $bestM = [double]$m.Groups[2].Value; $best = [int]$m.Groups[1].Value } }
$acc = Last (Bridge "career accept $best Taxi") 'career accept'
Record 'Accept fare' ($acc -match ': Ok') $acc.Substring([Math]::Max(0, $acc.Length - 120))
$d = Last (Bridge 'career drive 600 1' 700) 'career drive'
Record 'Drive to pickup; passenger boards' ($d -match 'reached leg 1' -and $d -match 'pax=Aboard') (($d -split ' :: ')[-1])

$o = Bridge "save $Prefix-B;wait 2;career status" 300
$b = Wallet $o
Record 'Save B mid-mission (passenger aboard, driving)' ($o -match "save $Prefix-B -> True" -and $o -match 'Taxi/Active leg=1') (Last $o 'career status')

$o = Bridge "load $Prefix-B;waitcity 900;wait 4;leakcheck;career status" 1200
$ls = Last $o 'career status'
Record 'Reload B: mission restored at the same stage' ($o -match "load $Prefix-B -> True" -and $ls -match 'leg=1' -and $ls -match 'pax=Aboard' -and (Wallet $ls)[0] -eq $b[0]) $ls
Record 'Reload B: no mod entity in the save' ($o -match 'excluded-signature cars=0 actors=0') ''
Record 'Rejoin the mission car' (Rejoin $vid) ''
$d = Last (Bridge 'career drive 900' 1000) 'career drive'
$w = Wallet $d
Record 'Finish the fare after reload' ($d -match 'mission finished' -and $w[0] -gt $b[0] -and $w[1] -eq $b[1] + 1) (($d -split ' :: ')[-1])

$o = Bridge "save $Prefix-C;wait 2;load $Prefix-C;waitcity 900;wait 4;career status" 1500
$c = Wallet (Last $o 'career status')
Record 'Save C, reload C: same balance, no second payout' ($c[0] -eq $w[0] -and $c[1] -eq $w[1] -and (Last $o 'career status') -match 'mission=\[none\]') (Last $o 'career status')

$o = Bridge "load $Prefix-A;waitcity 900;wait 4;career status" 1200
$r = Wallet (Last $o 'career status')
Record 'Reload older save A: older balance restored exactly' ($r[0] -eq $a[0] -and $r[1] -eq $a[1]) ("A had {0}/{1}; now {2}/{3}" -f $a[0], $a[1], $r[0], $r[1])

$md = "# Career persistence campaign $(Get-Date -Format yyyyMMdd-HHmmss)`n`n| Step | Result | Detail |`n|---|---|---|`n"
foreach ($x in $results) { $md += "| $($x.Step) | $($x.Result) | $($x.Detail -replace '\|','/') |`n" }
$file = Join-Path $env:LOCALAPPDATA ("DriveTheCityII\test\campaigns\career-persist-{0}.md" -f (Get-Date -Format yyyyMMdd-HHmmss))
Set-Content -Encoding UTF8 $file $md
Write-Host "Report: $file"
