<#
.SYNOPSIS
  Publishes Drive the City II to Paradox Mods with the ModPublisher that ships with the game.
.DESCRIPTION
  Content: the installed mod folder (<CS2 user data>\Mods\DriveTheCityII), which must match the package recorded by
  tools\deploy.ps1 and the version in src\DriveTheCityII.Mod\Properties\PublishConfiguration.xml.
  Login: ModPublisher's automatic login with the Paradox account the game is signed in to. This script never passes
  an email or a password; if the automatic login fails, start the game once, sign in to the Paradox account, quit,
  and run the script again.
  Commands: Publish (first upload; prints the new mod id, which must then be written into ModId), NewVersion (new
  build), Update (listing metadata only, for example AccessLevel). The output is logged to
  %LOCALAPPDATA%\DriveTheCityII\publish\.
.EXAMPLE
  tools\publish-paradox.ps1 -Command Publish
  tools\publish-paradox.ps1 -Command Update
#>
param(
    [Parameter(Mandatory = $true)][ValidateSet('Publish', 'NewVersion', 'Update')][string]$Command
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'env.ps1')
function Fail($msg) { Write-Host "PUBLISH FAILED: $msg" -ForegroundColor Red; exit 1 }

$publisher = Join-Path $DtcGamePath 'Cities2_Data\Content\Game\.ModdingToolchain\ModPublisher\ModPublisher.dll'
if (-not (Test-Path $publisher)) { Fail "ModPublisher not found at $publisher" }
$projectDir = Join-Path $DtcProjectRoot 'src\DriveTheCityII.Mod'
$configRel = 'Properties\PublishConfiguration.xml'
$config = Join-Path $projectDir $configRel
[xml]$xml = Get-Content $config -Raw -Encoding UTF8
$modId = $xml.Publish.ModId.Value
$modVersion = $xml.Publish.ModVersion.Value
$access = $xml.Publish.AccessLevel.Value
if ($Command -eq 'Publish' -and $modId) { Fail "ModId is already set ($modId); use NewVersion or Update" }
if ($Command -ne 'Publish' -and -not $modId) { Fail 'ModId is empty; the first upload must use Publish' }
if ($access -cnotin @('Public', 'Private', 'Unlisted')) { Fail "AccessLevel '$access' is not Public, Private or Unlisted (case-sensitive)" }
if ($xml.Publish.ShortDescription.Value.Length -gt 200) { Fail 'ShortDescription is longer than 200 characters' }
foreach ($n in @($xml.Publish.Thumbnail) + @($xml.Publish.Screenshot)) {
    $p = Join-Path $projectDir $n.Value
    if (-not (Test-Path $p)) { Fail "image not found: $($n.Value)" }
    if ((Get-Item $p).Length -gt 2MB) { Fail "image larger than 2 MB: $($n.Value)" }
}

# The content folder must be exactly the installed package of the configured version.
$installed = Join-Path $DtcDeployRoot 'installed.json'
if (-not (Test-Path $installed)) { Fail 'no install record; run tools\deploy.ps1 -Install first' }
$own = Get-Content $installed -Raw | ConvertFrom-Json
if (-not ($own.package -like "DriveTheCityII-$modVersion-*")) { Fail "installed package $($own.package) is not version $modVersion" }
if ($own.package -like '*+dirty*') { Fail "installed package $($own.package) was built from uncommitted changes" }
$present = @(Get-ChildItem $DtcModFolder -Recurse -File | ForEach-Object { $_.FullName.Substring($DtcModFolder.Length + 1) })
$owned = @($own.files | ForEach-Object { $_.path })
if (Compare-Object ($present | Sort-Object) ($owned | Sort-Object)) { Fail "the mod folder holds files that are not part of the installed package: $($present -join ', ')" }
foreach ($f in $own.files) {
    if ((Get-FileHash (Join-Path $DtcModFolder $f.path) -Algorithm SHA256).Hash -ne $f.sha256) { Fail "installed $($f.path) differs from the package" }
}

$logDir = Join-Path $DtcLocalRoot 'publish'
New-Item -ItemType Directory -Force $logDir | Out-Null
$log = Join-Path $logDir ("{0}-{1}.log" -f (Get-Date).ToString('yyyyMMdd-HHmmss'), $Command)
Write-Host "ModPublisher $Command  package=$($own.package)  version=$modVersion  access=$access  modId=$(if ($modId) { $modId } else { '(new)' })"

$env:DOTNET_ROLL_FORWARD = 'Major'
Push-Location $projectDir
try {
    & dotnet $publisher $Command $configRel -c $DtcModFolder -v 2>&1 | Tee-Object -FilePath $log | ForEach-Object { "$_" }
    $code = $LASTEXITCODE
} finally { Pop-Location }
$text = Get-Content $log -Raw
if ($text -match 'Id\s*=\s*(\d+)') { Write-Host "MOD ID: $($Matches[1])" -ForegroundColor Green }
Write-Host "ModPublisher exit code $code; log: $log"
exit $code
