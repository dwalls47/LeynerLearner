# Makes LeynerLearner\LeynerLearner.ico: a lamed (ל) from the bundled Hebrew Sheva Na font (Taamey D, GPL-2 with
# font exception; see LeynerLearner\Data\Fonts\README.md), cream on a deep-blue rounded square, at the standard icon
# sizes. Also writes icon-preview.png (all sizes side by side) next to this script.
#
# usage (Windows PowerShell):  powershell -ExecutionPolicy Bypass -File make-icon.ps1
Add-Type -AssemblyName System.Drawing

$root = Resolve-Path (Join-Path $PSScriptRoot '..\..')
$font = Join-Path $root 'LeynerLearner\Data\Fonts\HebrewShevaNa.ttf'
$ico = Join-Path $root 'LeynerLearner\LeynerLearner.ico'
$sizes = 256, 128, 64, 48, 32, 24, 16

$fonts = New-Object System.Drawing.Text.PrivateFontCollection
$fonts.AddFontFile($font)
$family = $fonts.Families[0]

$background = [System.Drawing.Color]::FromArgb(255, 31, 63, 115)    # deep blue
$ink = [System.Drawing.Color]::FromArgb(255, 250, 241, 214)         # parchment cream

function Draw-Icon([int]$size) {
    $bmp = New-Object System.Drawing.Bitmap $size, $size, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = 'AntiAlias'
    $g.PixelOffsetMode = 'HighQuality'
    $g.Clear([System.Drawing.Color]::Transparent)

    # Rounded square, filling the icon (a small margin at large sizes).
    $m = [Math]::Max(0, [Math]::Round($size * 0.03))
    $r = $size * 0.2
    $box = New-Object System.Drawing.Drawing2D.GraphicsPath
    $w = $size - 2 * $m
    $box.AddArc($m, $m, 2 * $r, 2 * $r, 180, 90)
    $box.AddArc($m + $w - 2 * $r, $m, 2 * $r, 2 * $r, 270, 90)
    $box.AddArc($m + $w - 2 * $r, $m + $w - 2 * $r, 2 * $r, 2 * $r, 0, 90)
    $box.AddArc($m, $m + $w - 2 * $r, 2 * $r, 2 * $r, 90, 90)
    $box.CloseFigure()
    $brush = New-Object System.Drawing.SolidBrush $background
    $g.FillPath($brush, $box)

    # The lamed, scaled so its outline fills about 78% of the height, centred on its own bounds.
    $path = New-Object System.Drawing.Drawing2D.GraphicsPath
    $path.AddString([string][char]0x05DC, $family, 0, 1000, (New-Object System.Drawing.PointF 0, 0), [System.Drawing.StringFormat]::GenericTypographic)
    $b = $path.GetBounds()
    $scale = ($size * 0.78) / [Math]::Max($b.Height, $b.Width / 0.8)
    $matrix = New-Object System.Drawing.Drawing2D.Matrix
    $matrix.Translate($size / 2, $size / 2)
    $matrix.Scale($scale, $scale)
    $matrix.Translate(-($b.X + $b.Width / 2), -($b.Y + $b.Height / 2))
    $path.Transform($matrix)
    $inkBrush = New-Object System.Drawing.SolidBrush $ink
    $g.FillPath($inkBrush, $path)
    # Small icons: thicken the strokes a little so the letter stays legible.
    if ($size -le 64) {
        $pen = New-Object System.Drawing.Pen $ink, ([float]([Math]::Max(0.6, $size / 40.0)))
        $pen.LineJoin = 'Round'
        $g.DrawPath($pen, $path)
    }
    $g.Dispose()
    return $bmp
}

# Write the .ico: one PNG-compressed image per size (supported since Windows Vista).
$images = foreach ($s in $sizes) {
    $bmp = Draw-Icon $s
    $ms = New-Object System.IO.MemoryStream
    $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
    [pscustomobject]@{ Size = $s; Bytes = $ms.ToArray(); Bitmap = $bmp }
}
$out = New-Object System.IO.MemoryStream
$wr = New-Object System.IO.BinaryWriter $out
$wr.Write([uint16]0); $wr.Write([uint16]1); $wr.Write([uint16]$images.Count)
$offset = 6 + 16 * $images.Count
foreach ($im in $images) {
    $d = if ($im.Size -ge 256) { 0 } else { $im.Size }
    $wr.Write([byte]$d); $wr.Write([byte]$d); $wr.Write([byte]0); $wr.Write([byte]0)
    $wr.Write([uint16]1); $wr.Write([uint16]32)
    $wr.Write([uint32]$im.Bytes.Length); $wr.Write([uint32]$offset)
    $offset += $im.Bytes.Length
}
foreach ($im in $images) { $wr.Write($im.Bytes) }
[System.IO.File]::WriteAllBytes($ico, $out.ToArray())

# Preview: every size on a light and a dark strip.
[int]$width = ($sizes | Measure-Object -Sum).Sum + 20 * ($sizes.Count + 1)
$preview = New-Object System.Drawing.Bitmap $width, 600
$pg = [System.Drawing.Graphics]::FromImage($preview)
$pg.Clear([System.Drawing.Color]::White)
$pg.FillRectangle([System.Drawing.Brushes]::Black, 0, 300, $width, 300)
$x = 20
foreach ($im in $images) {
    $pg.DrawImage($im.Bitmap, $x, 20, $im.Size, $im.Size)
    $pg.DrawImage($im.Bitmap, $x, 320, $im.Size, $im.Size)
    $x += $im.Size + 20
}
$pg.Dispose()
$preview.Save((Join-Path $PSScriptRoot 'icon-preview.png'), [System.Drawing.Imaging.ImageFormat]::Png)
"Wrote $ico ($((Get-Item $ico).Length) bytes)"
