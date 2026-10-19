<#
.SYNOPSIS
  BR-07 / REQ-SAVE-006: a city saved with Drive the City II still loads when the mod's code does not run.
.DESCRIPTION
  Runs inside an ACTIVE test session (tools\test-session.ps1 -Start) so that the session's original backups of
  UserState.coc and continue_game.json stay in charge and are restored by tools\test-session.ps1 -Stop afterwards.
  1. With the mod: load -From, save it as -Save (this also points the game's "Continue" at the lab save), quit.
  2. Relaunch the SAME game with -continuelastsave -disableCodeModding (code mods off; nothing is uninstalled).
  3. Wait for SceneFlow.log to report the lab save loading and "Loading completed"; confirm Modding.log did not load
     Drive the City II; capture a screenshot of the screen.
  4. Close that game process (lab city; nothing to save) and leave restoring the state files to test-session -Stop.
#>
param([string]$From = 'DTC2-LAB-CAREER-C', [string]$Save = 'DTC2-LAB-NOMOD', [int]$TimeoutSeconds = 600)

. (Join-Path $PSScriptRoot 'env.ps1')
$sessionFile = Join-Path $DtcTestRoot 'session.json'
if (-not (Test-Path $sessionFile)) { Write-Host 'No active test session; start one with tools\test-session.ps1 -Start.'; exit 2 }
$session = Get-Content $sessionFile -Raw | ConvertFrom-Json
$logs = Join-Path $DtcUserDataPath 'Logs'
$evidence = Join-Path $session.sessionDir 'nomod'
New-Item -ItemType Directory -Force $evidence | Out-Null
$results = New-Object System.Collections.Generic.List[string]
function Note([string]$t) { $results.Add($t); Write-Host $t }

# 1. Save with the mod.
$o = & powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot 'bridge.ps1') -Commands "load $From;waitcity 900;wait 3;career status;save $Save;wait 3;quit" -TimeoutSeconds 1500 2>&1 | Out-String
$saved = $o -match "save $Save -> True"
Note ("With the mod: save {0} -> {1}" -f $Save, $saved)
Note ("Career state saved: " + (($o -split "`n" | Where-Object { $_ -match '< OK.*career status' } | Select-Object -Last 1) -replace '^.*:: ', ''))
$deadline = (Get-Date).AddSeconds(120)
while ((Get-Process -Name Cities2 -ErrorAction SilentlyContinue) -and (Get-Date) -lt $deadline) { Start-Sleep 1 }
if (-not $saved -or (Get-Process -Name Cities2 -ErrorAction SilentlyContinue)) { Note 'ABORT: save failed or the game did not exit.'; exit 1 }

# 2. Relaunch with code mods disabled, continuing the lab save.
$exe = Join-Path $DtcGamePath 'Cities2.exe'
$env:SteamAppId = '949230'
$env:SteamGameId = '949230'
$started = Get-Date
$proc = Start-Process -FilePath $exe -ArgumentList @('-noSplash', '-continuelastsave', '-disableCodeModding') -WorkingDirectory $DtcGamePath -PassThru
Note "Relaunched without code mods (process $($proc.Id)) with -continuelastsave -disableCodeModding"

# 3. Wait for the load to complete.
$scene = Join-Path $logs 'SceneFlow.log'
$loaded = $false
$deadline = (Get-Date).AddSeconds($TimeoutSeconds)
while ((Get-Date) -lt $deadline) {
    Start-Sleep 3
    if (-not (Get-Process -Id $proc.Id -ErrorAction SilentlyContinue)) { Note 'GAME PROCESS ENDED during load'; break }
    if ((Test-Path $scene) -and (Get-Item $scene).LastWriteTime -gt $started) {
        $text = Get-Content $scene -Raw -Encoding UTF8
        if ($text -match "Starting game from '$Save" -and $text -match "(?s)Starting game from '$Save.*Loading completed") { $loaded = $true; break }
    }
}
Start-Sleep 8
$modding = if (Test-Path (Join-Path $logs 'Modding.log')) { Get-Content (Join-Path $logs 'Modding.log') -Raw -Encoding UTF8 } else { '' }
$modLoaded = $modding -match 'Loaded DriveTheCityII'
Note ("Lab save loaded without the mod: {0}" -f $loaded)
Note ("Drive the City II code loaded in that run: {0}" -f $modLoaded)
Copy-Item $scene (Join-Path $evidence 'SceneFlow.log') -ErrorAction SilentlyContinue
Copy-Item (Join-Path $logs 'Modding.log') (Join-Path $evidence 'Modding.log') -ErrorAction SilentlyContinue
$errs = (Get-Content $scene -Encoding UTF8 | Select-String 'ERROR|Exception|DtcSave|DriveTheCityII' | Select-Object -Last 10) -join "`n"
if ($errs) { Note "SceneFlow errors/mentions:`n$errs" }

# Screenshot of the primary screen.
try {
    Add-Type -AssemblyName System.Windows.Forms, System.Drawing
    $b = [System.Windows.Forms.Screen]::PrimaryScreen.Bounds
    $bmp = New-Object System.Drawing.Bitmap $b.Width, $b.Height
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.CopyFromScreen($b.Location, [System.Drawing.Point]::Empty, $b.Size)
    $shot = Join-Path $evidence 'nomod-loaded.png'
    $bmp.Save($shot, [System.Drawing.Imaging.ImageFormat]::Png)
    $g.Dispose(); $bmp.Dispose()
    Note "Screenshot: $shot"
} catch { Note "Screenshot failed: $($_.Exception.Message)" }

# 4. Close the mod-less game (lab city only).
if (Get-Process -Id $proc.Id -ErrorAction SilentlyContinue) {
    $null = $proc.CloseMainWindow()
    $deadline = (Get-Date).AddSeconds(60)
    while ((Get-Process -Id $proc.Id -ErrorAction SilentlyContinue) -and (Get-Date) -lt $deadline) { Start-Sleep 1 }
    if (Get-Process -Id $proc.Id -ErrorAction SilentlyContinue) { Stop-Process -Id $proc.Id -Force; Note 'Closed the mod-less game process (forced after 60 s).' } else { Note 'Closed the mod-less game process.' }
}
$results | Set-Content -Encoding UTF8 (Join-Path $evidence 'result.txt')
Write-Host "Evidence: $evidence. Now run tools\test-session.ps1 -Stop to restore the Continue pointer."
if ($loaded -and -not $modLoaded) { exit 0 } else { exit 1 }
