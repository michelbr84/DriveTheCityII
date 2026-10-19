<#
.SYNOPSIS
  Makes small JPEG previews of test screenshots (originals are kept unchanged as evidence).
.EXAMPLE
  tools\shrink-captures.ps1                 # all new captures in test\captures
  tools\shrink-captures.ps1 -Width 960
#>
param(
    [string]$Folder,
    [int]$Width = 1280
)
. (Join-Path $PSScriptRoot 'env.ps1')
if (-not $Folder) { $Folder = Join-Path $DtcTestRoot 'captures' }
Add-Type -AssemblyName System.Drawing
$outDir = Join-Path $Folder 'preview'
New-Item -ItemType Directory -Force $outDir | Out-Null
$codec = [System.Drawing.Imaging.ImageCodecInfo]::GetImageEncoders() | Where-Object { $_.MimeType -eq 'image/jpeg' }
$params = New-Object System.Drawing.Imaging.EncoderParameters(1)
$params.Param[0] = New-Object System.Drawing.Imaging.EncoderParameter([System.Drawing.Imaging.Encoder]::Quality, [long]80)
Get-ChildItem $Folder -Filter *.png | ForEach-Object {
    $dst = Join-Path $outDir ($_.BaseName + '.jpg')
    if (Test-Path $dst) { return }
    $img = [System.Drawing.Image]::FromFile($_.FullName)
    try {
        $h = [int]($img.Height * $Width / $img.Width)
        $bmp = New-Object System.Drawing.Bitmap($Width, $h)
        $g = [System.Drawing.Graphics]::FromImage($bmp)
        $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
        $g.DrawImage($img, 0, 0, $Width, $h)
        $g.Dispose()
        $bmp.Save($dst, $codec, $params)
        $bmp.Dispose()
        Write-Host $dst
    } finally { $img.Dispose() }
}
