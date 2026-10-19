<#
.SYNOPSIS
  Test helper: lists offers for a career, accepts the one with the shortest planned distance, and drives it with the
  TEST AUTOPILOT (simulated input; career rules unchanged). Prints the bridge results.
.EXAMPLE
  tools\career-mission.ps1 -Career Taxi -Seconds 900
#>
param([string]$Career = 'Taxi', [int]$Seconds = 900)

function Bridge([string]$cmds, [int]$timeout = 120) {
    $out = & powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot 'bridge.ps1') -Commands $cmds -TimeoutSeconds $timeout 2>&1
    return ($out | Out-String)
}

$o = Bridge "career offers $Career"
$line = ($o -split "`n" | Where-Object { $_ -match '< (OK|ERR).*career offers' } | Select-Object -Last 1)
Write-Host $line
$best = -1; $bestM = [double]::MaxValue
foreach ($m in [regex]::Matches($line, '#(\d+) .*? (\d+)m pay .*? eligible=(\w+)')) {
    if ($m.Groups[3].Value -eq 'True' -and [double]$m.Groups[2].Value -lt $bestM) { $bestM = [double]$m.Groups[2].Value; $best = [int]$m.Groups[1].Value }
}
if ($best -lt 0) { Write-Host "NO ELIGIBLE OFFER"; exit 1 }
$r = Bridge "career accept $best $Career;career status;career drive $Seconds;career status" ($Seconds + 120)
$r -split "`n" | Where-Object { $_ -match '< (OK|ERR)' } | ForEach-Object { Write-Host $_ }
if ($r -match 'mission finished') { exit 0 } else { exit 1 }
