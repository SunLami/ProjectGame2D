Add-Type -AssemblyName System.Drawing

$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '../../..')).Path
$width = 1920
$height = 1080
$outputDirectory = $PSScriptRoot

function New-Canvas {
    $bitmap = New-Object System.Drawing.Bitmap $width, $height
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
    $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::Half
    $graphics.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality
    return @{ Bitmap = $bitmap; Graphics = $graphics }
}

function Save-Png($canvas, [string]$path) {
    $canvas.Bitmap.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
    $jpegPath = [System.IO.Path]::ChangeExtension($path, '.jpg')
    $canvas.Bitmap.Save($jpegPath, [System.Drawing.Imaging.ImageFormat]::Jpeg)
    $canvas.Graphics.Dispose()
    $canvas.Bitmap.Dispose()
}

$backgroundPath = Join-Path $root 'output/intro-cutscene/weapon-free-v1/intro-04-village.png'
$logoPath = Join-Path $root 'Assets/Resources/UI/MainMenu/LightFantasy/orynthals_logo.png'
$outroSourcePath = Join-Path $root 'output/intro-cutscene/weapon-free-v1/intro-05-arrival.png'

$background = [System.Drawing.Image]::FromFile($backgroundPath)
$logo = [System.Drawing.Image]::FromFile($logoPath)
$logoCanvas = New-Canvas
$logoCanvas.Graphics.DrawImage($background, 0, 0, $width, $height)
$topShade = New-Object System.Drawing.Drawing2D.LinearGradientBrush(
    (New-Object System.Drawing.Point 0, 0),
    (New-Object System.Drawing.Point 0, $height),
    ([System.Drawing.Color]::FromArgb(170, 5, 17, 35)),
    ([System.Drawing.Color]::FromArgb(55, 5, 17, 35)))
$logoCanvas.Graphics.FillRectangle($topShade, 0, 0, $width, $height)
$topShade.Dispose()
$logoCanvas.Graphics.DrawImage($logo, 140, 274, 1640, 547)
Save-Png $logoCanvas (Join-Path $outputDirectory 'logo-intro-first-frame.png')
$background.Dispose()
$logo.Dispose()

$outroSource = [System.Drawing.Image]::FromFile($outroSourcePath)
$outroCanvas = New-Canvas
$outroCanvas.Graphics.DrawImage($outroSource, 0, 0, $width, $height)
$bottomShade = New-Object System.Drawing.Drawing2D.LinearGradientBrush(
    (New-Object System.Drawing.Point 0, 720),
    (New-Object System.Drawing.Point 0, $height),
    ([System.Drawing.Color]::FromArgb(0, 7, 18, 30)),
    ([System.Drawing.Color]::FromArgb(95, 7, 18, 30)))
$outroCanvas.Graphics.FillRectangle($bottomShade, 0, 720, $width, 360)
$bottomShade.Dispose()
Save-Png $outroCanvas (Join-Path $outputDirectory 'outro-transition-first-frame.png')
$outroSource.Dispose()
