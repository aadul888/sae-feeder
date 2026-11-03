Add-Type -AssemblyName System.Drawing

$pngPath = "c:\laragon\www\sae-feeder\sae-logo.png"
$icoPath = "c:\laragon\www\sae-feeder\installer_builder\sae-feeder.ico"

# If public favicon.ico exists from SAE, check it
$saeIco = "c:\laragon\www\sae\public\favicon.ico"
if (Test-Path $saeIco) {
    Copy-Item $saeIco $icoPath -Force
    Write-Output "Copied SAE favicon.ico to $icoPath"
} else {
    $bmp = [System.Drawing.Bitmap]::FromFile($pngPath)
    $hIcon = $bmp.GetHicon()
    $icon = [System.Drawing.Icon]::FromHandle($hIcon)
    $fs = New-Object System.IO.FileStream($icoPath, [System.IO.FileMode]::Create)
    $icon.Save($fs)
    $fs.Close()
    $icon.Dispose()
    $bmp.Dispose()
    Write-Output "Generated icon from PNG"
}

# Verify
if (Test-Path $icoPath) {
    Write-Output "Icon created successfully: $((Get-Item $icoPath).Length) bytes"
} else {
    Write-Error "Failed to create icon"
}
