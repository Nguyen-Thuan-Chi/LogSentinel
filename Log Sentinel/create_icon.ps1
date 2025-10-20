# PowerShell script to create a basic ICO file
# This creates a simple 32x32 icon with Log Sentinel branding

Add-Type -AssemblyName System.Drawing

# Create a 32x32 bitmap
$bitmap = New-Object System.Drawing.Bitmap(32, 32)
$graphics = [System.Drawing.Graphics]::FromImage($bitmap)

# Set background color (light blue)
$graphics.Clear([System.Drawing.Color]::FromArgb(37, 99, 235))

# Create a brush for the shield icon
$whiteBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::White)

# Draw a simple shield shape
$shieldPoints = @(
    [System.Drawing.Point]::new(16, 4),   # Top center
    [System.Drawing.Point]::new(26, 8),   # Top right
    [System.Drawing.Point]::new(26, 18),  # Mid right
    [System.Drawing.Point]::new(16, 28),  # Bottom center
    [System.Drawing.Point]::new(6, 18),   # Mid left
    [System.Drawing.Point]::new(6, 8)     # Top left
)

$graphics.FillPolygon($whiteBrush, $shieldPoints)

# Add a simple "L" for Log
$font = New-Object System.Drawing.Font("Arial", 12, [System.Drawing.FontStyle]::Bold)
$blueBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(37, 99, 235))
$graphics.DrawString("L", $font, $blueBrush, 12, 10)

# Clean up
$graphics.Dispose()
$whiteBrush.Dispose()
$blueBrush.Dispose()
$font.Dispose()

# Save as ICO (this is a simplified approach)
$iconPath = Join-Path $PSScriptRoot "Assets\app_icon.ico"

try {
    # Create icon from bitmap
    $icon = [System.Drawing.Icon]::FromHandle($bitmap.GetHicon())
    $fileStream = [System.IO.FileStream]::new($iconPath, [System.IO.FileMode]::Create)
    $icon.Save($fileStream)
    $fileStream.Close()
    Write-Host "Icon created successfully at: $iconPath"
} catch {
    # Fallback: save as PNG first, then you can convert manually
    $pngPath = Join-Path $PSScriptRoot "Assets\app_icon.png"
    $bitmap.Save($pngPath, [System.Drawing.Imaging.ImageFormat]::Png)
    Write-Host "Created PNG at: $pngPath"
    Write-Host "Please convert this PNG to ICO format manually or use an online converter"
}

$bitmap.Dispose()