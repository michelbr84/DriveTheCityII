# Drive the City II - process-local development environment.
# Dot-source this file:  . .\tools\env.ps1
# It changes only the current PowerShell process (no global PATH, registry, or policy changes).

$script:DtcProjectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$script:DtcLocalRoot = Join-Path $env:LOCALAPPDATA 'DriveTheCityII'
$script:DtcDotnetRoot = Join-Path $DtcLocalRoot 'tools\dotnet'

if (-not (Test-Path (Join-Path $DtcDotnetRoot 'dotnet.exe'))) {
    throw "Private .NET SDK not found at '$DtcDotnetRoot'. See docs/engineering/BUILD_AND_RUN.md (section: one-time setup)."
}

$env:DOTNET_ROOT = $DtcDotnetRoot
if (-not ($env:PATH -split ';' | Where-Object { $_ -eq $DtcDotnetRoot })) {
    $env:PATH = "$DtcDotnetRoot;$env:PATH"
}
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:DOTNET_NOLOGO = '1'
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = '1'
$env:NUGET_PACKAGES = Join-Path $DtcLocalRoot 'nuget\packages'
$env:MSBUILDDISABLENODEREUSE = '1'

# Game locations (resolved, not assumed): Steam library manifest first, then the standard path.
function Get-DtcGamePath {
    $candidates = @()
    $steam = (Get-ItemProperty -Path 'HKCU:\Software\Valve\Steam' -Name SteamPath -ErrorAction SilentlyContinue).SteamPath
    if ($steam) {
        $lib = Join-Path $steam 'steamapps\libraryfolders.vdf'
        if (Test-Path $lib) {
            foreach ($m in [regex]::Matches((Get-Content $lib -Raw), '"path"\s+"([^"]+)"')) {
                $candidates += (Join-Path ($m.Groups[1].Value -replace '\\\\', '\') 'steamapps\common\Cities Skylines II')
            }
        }
    }
    $candidates += 'C:\Program Files (x86)\Steam\steamapps\common\Cities Skylines II'
    foreach ($c in $candidates) {
        if (Test-Path (Join-Path $c 'Cities2.exe')) { return (Resolve-Path $c).Path }
    }
    return $null
}

$script:DtcGamePath = Get-DtcGamePath
if ($DtcGamePath) {
    $env:CSII_MANAGEDPATH = Join-Path $DtcGamePath 'Cities2_Data\Managed'
}
$script:DtcUserDataPath = Join-Path $env:USERPROFILE 'AppData\LocalLow\Colossal Order\Cities Skylines II'
$script:DtcModsPath = Join-Path $DtcUserDataPath 'Mods'
$script:DtcModFolder = Join-Path $DtcModsPath 'DriveTheCityII'
$script:DtcBuildRoot = Join-Path $DtcLocalRoot 'build'
$script:DtcPackagesRoot = Join-Path $DtcLocalRoot 'packages'
$script:DtcDeployRoot = Join-Path $DtcLocalRoot 'deploy'
$script:DtcTestRoot = Join-Path $DtcLocalRoot 'test'
