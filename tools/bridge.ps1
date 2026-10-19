<#
.SYNOPSIS
  Sends commands to the in-game test bridge and waits for their results.
.DESCRIPTION
  Writes one command file (the whole sequence at once, as the bridge expects), waits until the mod has picked it
  up, then waits until every command has a result line in bridge.log, and prints those result lines.
  Exit code 0 = all OK, 1 = some command failed, 2 = timeout or the bridge did not pick up the file.
.EXAMPLE
  tools\bridge.ps1 -Commands 'status'
  tools\bridge.ps1 -Commands 'waitmenu 300','maps' -TimeoutSeconds 400
#>
param(
    [Parameter(Mandatory = $true)][string[]]$Commands,
    [int]$TimeoutSeconds = 120
)

. (Join-Path $PSScriptRoot 'env.ps1')
# Commands may also be given as one string separated by ';' (needed with powershell -File).
$Commands = @($Commands | ForEach-Object { $_ -split ';' } | ForEach-Object { $_.Trim() } | Where-Object { $_ })
$cmdFile = Join-Path $DtcTestRoot 'commands.txt'
$logFile = Join-Path $DtcTestRoot 'bridge.log'

# Wait for any previous command file to be consumed.
$deadline = (Get-Date).AddSeconds($TimeoutSeconds)
while ((Test-Path $cmdFile) -and (Get-Date) -lt $deadline) { Start-Sleep -Milliseconds 250 }
if (Test-Path $cmdFile) { Write-Host 'BRIDGE: previous command file was never consumed (is the game running with the bridge enabled?)'; exit 2 }

$startLines = if (Test-Path $logFile) { @(Get-Content $logFile -Encoding UTF8).Count } else { 0 }
$tmp = "$cmdFile.tmp"
Set-Content -Encoding UTF8 $tmp ($Commands -join "`r`n")
Move-Item $tmp $cmdFile -Force

$pickupDeadline = (Get-Date).AddSeconds([Math]::Min(60, $TimeoutSeconds))
while ((Test-Path $cmdFile) -and (Get-Date) -lt $pickupDeadline) { Start-Sleep -Milliseconds 200 }
if (Test-Path $cmdFile) {
    Remove-Item $cmdFile -Force -ErrorAction SilentlyContinue
    Write-Host 'BRIDGE: command file not picked up within 60 s'
    exit 2
}

$expected = @($Commands | Where-Object { $_.Trim() -and -not $_.Trim().StartsWith('#') }).Count
$results = @()
while ((Get-Date) -lt $deadline) {
    $lines = if (Test-Path $logFile) { @(Get-Content $logFile -Encoding UTF8) } else { @() }
    $new = if ($lines.Count -gt $startLines) { $lines[$startLines..($lines.Count - 1)] } else { @() }
    $results = @($new | Where-Object { $_ -match ' < (OK|ERR) ' })
    if ($results.Count -ge $expected) { break }
    Start-Sleep -Milliseconds 300
}
$new | ForEach-Object { Write-Host $_ }
if ($results.Count -lt $expected) { Write-Host "BRIDGE: timeout ($($results.Count)/$expected results)"; exit 2 }
if ($results | Where-Object { $_ -match ' < ERR ' }) { exit 1 }
exit 0
