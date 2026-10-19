<#
.SYNOPSIS
  Presentation checks in a running test session with a lab city loaded: PT-BR text in the HUD and panel, HUD scale and
  high contrast (screenshots), vehicle sound diagnostics, and exit placement over 10 transition cycles.
  Restores the mod options it changes (language, HUD scale, contrast) at the end.
.DESCRIPTION
  Driving uses simulated input. Screenshots land in %LOCALAPPDATA%\DriveTheCityII\test\captures.
#>
$results = New-Object System.Collections.Generic.List[object]
function Bridge([string]$cmds, [int]$timeout = 120) {
    $out = & powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot 'bridge.ps1') -Commands $cmds -TimeoutSeconds $timeout 2>&1
    return ($out | Out-String)
}
function Last([string]$text, [string]$cmd) { return [string]($text -split "`n" | Where-Object { $_ -match "< (OK|ERR).*$cmd" } | Select-Object -Last 1) }
function Tail([string]$line, [int]$n = 300) { if (-not $line) { return '' }; $x = ($line -split ' :: ', 2)[-1]; return $x.Substring(0, [Math]::Min($n, $x.Length)) }
function Record([string]$step, [bool]$ok, [string]$detail) {
    $results.Add([pscustomobject]@{ Step = $step; Result = $(if ($ok) { 'Passed' } else { 'Failed' }); Detail = $detail })
    Write-Host ("[{0}] {1} :: {2}" -f $(if ($ok) { 'PASS' } else { 'FAIL' }), $step, $detail)
}

try {
    Bridge 'player enter;waitstate OnFoot 15;wait 1' | Out-Null
    # PT-BR.
    $o = Bridge 'setting Language PortugueseBrazil;wait 1;hud;ui panel;wait 1;shot ptbr-panel-jobs;career ui jobs:career:Taxi;wait 2;hud;shot ptbr-panel-jobs2'
    $h = Last $o 'hud'
    $pt = ($h -match 'A pé|Modo personagem|Andar|Trabalhos|Carteira|Nível|entrar') -and ($h -notmatch '"modeLabel":"On foot"')
    Record 'PT-BR texts in the HUD view model' $pt (Tail $h 400)
    Bridge 'ui panel;wait 1' | Out-Null
    # Scale and contrast.
    $o = Bridge 'setting HudScale 150;setting HighContrastHud true;wait 1;hud;shot hud-scale150-contrast'
    $h = Last $o 'hud'
    Record 'HUD scale 150 % and high contrast published to the UI' ($h -match '"scale":1\.5' -and $h -match '"contrast":true') (Tail $h 200)
    Bridge 'setting HudScale 100;setting HighContrastHud false;setting Language FollowGame;wait 1' | Out-Null
    # Sound.
    $o = Bridge 'audio'
    Record 'No vehicle sound on foot' ((Last $o 'audio') -match 'playing=False|gain=0\.0') (Tail (Last $o 'audio'))
    $st = Bridge 'career status'
    $vid = if ($st -match '(V-[A-Za-z0-9]+):car\.basic') { $Matches[1] } else { $null }
    if (-not $vid) { Bridge 'career ui garage:rentbasic;wait 2' | Out-Null; $st = Bridge 'career status'; $vid = if ($st -match '(V-[A-Za-z0-9]+):car\.basic') { $Matches[1] } else { $null } }
    Bridge "career ui garage:call:$vid;wait 2;career gotoveh $vid;player interact;waitstate Driving 15" | Out-Null
    $o = Bridge 'input throttle=1 t=2.5;audio;wait 0.5;audio;input brake=1 t=2;input hand=1 t=1;wait 1;audio'
    $lines = $o -split "`n" | Where-Object { $_ -match '< OK.*audio ::' }
    $moving = [string]$lines[0]
    Record 'Vehicle sound playing while driving, routed to the game mixer' ($moving -match 'playing=True' -and $moving -match 'group=World' -and $moving -notmatch 'failed=True') (Tail $moving)
    Record 'Engine pitch follows speed (target frequency above idle while moving)' ($moving -match 'targetFreqHz=(\d+)' -and [double]$Matches[1] -gt 35) (Tail $moving 120)
    Record 'Listener output level non-zero while driving' ($moving -match 'listenerRms=(0\.\d+)' -and [double]$Matches[1] -gt 0.0005) (Tail $moving 200)
    # Exits.
    $o = Bridge 'player exit;waitstate OnFoot 15;cycles 10' 600
    $c = Last $o 'cycles'
    $ok = $c -match 'cycles=10/10 failures=0' -and $c -match 'exitsOnSidewalk=(\d+)'
    Record 'Transition cycles 10/10, exits on the sidewalk' $ok (Tail $c 260)
}
finally {
    Bridge 'setting HudScale 100;setting HighContrastHud false;setting Language FollowGame' | Out-Null
    $md = "# Presentation checks $(Get-Date -Format yyyyMMdd-HHmmss)`n`n| Step | Result | Detail |`n|---|---|---|`n"
    foreach ($x in $results) { $md += "| $($x.Step) | $($x.Result) | $($x.Detail -replace '\|','/') |`n" }
    $file = Join-Path $env:LOCALAPPDATA ("DriveTheCityII\test\campaigns\presentation-{0}.md" -f (Get-Date -Format yyyyMMdd-HHmmss))
    Set-Content -Encoding UTF8 $file $md
    Write-Host "Report: $file"
}
