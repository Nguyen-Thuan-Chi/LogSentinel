# PowerShell script to create a shield icon based on MaterialDesign ShieldOutline
# This creates a shield icon similar to the one used in the dashboard

Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName System.Drawing.Imaging

# Create a 64x64 bitmap for higher quality
$size = 64
$bitmap = New-Object System.Drawing.Bitmap($size, $size)
$graphics = [System.Drawing.Graphics]::FromImage($bitmap)

# Enable anti-aliasing for smoother shapes
$graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias

# Set background to transparent
$graphics.Clear([System.Drawing.Color]::Transparent)

# Create the shield color (matching the blue from MaterialDesign - #2563EB)
$shieldColor = [System.Drawing.Color]::FromArgb(37, 99, 235)
$shieldBrush = New-Object System.Drawing.SolidBrush($shieldColor)
$shieldPen = New-Object System.Drawing.Pen($shieldColor, 3)

# Define shield outline points (ShieldOutline style)
$centerX = $size / 2
$centerY = $size / 2

# Shield shape points (similar to MaterialDesign ShieldOutline)
$shieldPoints = @(
    [System.Drawing.Point]::new($centerX, 8),        # Top center
    [System.Drawing.Point]::new($centerX + 16, 16),  # Top right
    [System.Drawing.Point]::new($centerX + 16, 32),  # Mid right
    [System.Drawing.Point]::new($centerX, $size - 8), # Bottom center
    [System.Drawing.Point]::new($centerX - 16, 32),  # Mid left
    [System.Drawing.Point]::new($centerX - 16, 16)   # Top left
)

# Draw shield outline
$graphics.DrawPolygon($shieldPen, $shieldPoints)

# For filled version, uncomment this line:
# $graphics.FillPolygon($shieldBrush, $shieldPoints)

# Create smaller sizes for ico format
$sizes = @(16, 24, 32, 48, 64)
$bitmaps = @{}

foreach ($s in $sizes) {
    $smallBitmap = New-Object System.Drawing.Bitmap($s, $s)
    $smallGraphics = [System.Drawing.Graphics]::FromImage($smallBitmap)
    $smallGraphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $smallGraphics.Clear([System.Drawing.Color]::Transparent)
    
    # Scale the shield for smaller sizes
    $scale = $s / 64.0
    $smallCenterX = $s / 2
    $smallCenterY = $s / 2
    
    $smallShieldPoints = @(
        [System.Drawing.Point]::new($smallCenterX, 2 * $scale + 2),
        [System.Drawing.Point]::new($smallCenterX + 8 * $scale, 4 * $scale + 2),
        [System.Drawing.Point]::new($smallCenterX + 8 * $scale, 16 * $scale + 2),
        [System.Drawing.Point]::new($smallCenterX, $s - 2 * $scale - 2),
        [System.Drawing.Point]::new($smallCenterX - 8 * $scale, 16 * $scale + 2),
        [System.Drawing.Point]::new($smallCenterX - 8 * $scale, 4 * $scale + 2)
    )
    
    $smallPen = New-Object System.Drawing.Pen($shieldColor, [Math]::Max(1, 2 * $scale))
    $smallGraphics.DrawPolygon($smallPen, $smallShieldPoints)
    
    $bitmaps[$s] = $smallBitmap
    $smallPen.Dispose()
    $smallGraphics.Dispose()
}

# Save as PNG first (for backup)
$pngPath = Join-Path $PSScriptRoot "Assets\app_icon_shield.png"
$bitmap.Save($pngPath, [System.Drawing.Imaging.ImageFormat]::Png)
Write-Host "Shield PNG created at: $pngPath"

# Try to create ICO file with multiple sizes
$iconPath = Join-Path $PSScriptRoot "Assets\app_icon_shield.ico"

try {
    # Create icon stream
    $memoryStream = New-Object System.IO.MemoryStream
    
    # ICO header (6 bytes)
    $memoryStream.WriteByte(0)  # Reserved
    $memoryStream.WriteByte(0)  # Reserved
    $memoryStream.WriteByte(1)  # Type (1 = ICO)
    $memoryStream.WriteByte(0)  # Type high byte
    $memoryStream.WriteByte($sizes.Count)  # Number of images
    $memoryStream.WriteByte(0)  # Number of images high byte
    
    # Calculate offset for image data
    $imageDataOffset = 6 + ($sizes.Count * 16)  # Header + directory entries
    $currentOffset = $imageDataOffset
    
    # Directory entries and image data
    $imageData = @()
    
    foreach ($s in $sizes) {
        $bmp = $bitmaps[$s]
        
        # Convert to PNG data
        $pngStream = New-Object System.IO.MemoryStream
        $bmp.Save($pngStream, [System.Drawing.Imaging.ImageFormat]::Png)
        $pngData = $pngStream.ToArray()
        $pngStream.Close()
        
        # Directory entry (16 bytes)
        $memoryStream.WriteByte($s)     # Width
        $memoryStream.WriteByte($s)     # Height
        $memoryStream.WriteByte(0)      # Color count
        $memoryStream.WriteByte(0)      # Reserved
        $memoryStream.WriteByte(1)      # Planes
        $memoryStream.WriteByte(0)      # Planes high byte
        $memoryStream.WriteByte(32)     # Bits per pixel
        $memoryStream.WriteByte(0)      # Bits per pixel high byte
        
        # Data size (4 bytes, little endian)
        $dataSize = $pngData.Length
        $memoryStream.WriteByte([byte]($dataSize -band 0xFF))
        $memoryStream.WriteByte([byte](($dataSize -shr 8) -band 0xFF))
        $memoryStream.WriteByte([byte](($dataSize -shr 16) -band 0xFF))
        $memoryStream.WriteByte([byte](($dataSize -shr 24) -band 0xFF))
        
        # Offset (4 bytes, little endian)
        $memoryStream.WriteByte([byte]($currentOffset -band 0xFF))
        $memoryStream.WriteByte([byte](($currentOffset -shr 8) -band 0xFF))
        $memoryStream.WriteByte([byte](($currentOffset -shr 16) -band 0xFF))
        $memoryStream.WriteByte([byte](($currentOffset -shr 24) -band 0xFF))
        
        $imageData += $pngData
        $currentOffset += $dataSize
    }
    
    # Write image data
    foreach ($data in $imageData) {
        $memoryStream.Write($data, 0, $data.Length)
    }
    
    # Save to file
    $fileBytes = $memoryStream.ToArray()
    [System.IO.File]::WriteAllBytes($iconPath, $fileBytes)
    $memoryStream.Close()
    
    Write-Host "Shield ICO created at: $iconPath"
    Write-Host "Icon contains sizes: $($sizes -join ', ')"
    
} catch {
    Write-Host "Error creating ICO: $($_.Exception.Message)"
    Write-Host "Using the PNG version instead."
}

# Clean up
$graphics.Dispose()
$shieldBrush.Dispose()
$shieldPen.Dispose()
$bitmap.Dispose()

foreach ($bmp in $bitmaps.Values) {
    $bmp.Dispose()
}

Write-Host "Shield icon creation complete!"
Write-Host "Replace the old app_icon.ico with app_icon_shield.ico if you prefer the new design."