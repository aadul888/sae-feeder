# ==============================================================================
# SAE FEEDER - Portable Builder (tanpa installer)
# Hasil: dist\SAE-Feeder-Portable.zip
#     Jalankan run_feeder.bat. PHP diambil dari:
#       1) folder php\ di dalam zip (jika ada), 2) PHP Dapodik, 3) PATH.
# ==============================================================================
param (
    [string]$PhpSource = "C:\laragon\bin\php\php-8.3.36-Win32-vs16-x64",
    [switch]$BundlePhp
)

$ErrorActionPreference = "Stop"
$rootDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$stagingDir = Join-Path $rootDir "build\portable"
$zipPath = Join-Path $rootDir "dist\SAE-Feeder-Portable.zip"

if (Test-Path $stagingDir) { Remove-Item $stagingDir -Recurse -Force }
New-Item -ItemType Directory -Force -Path $stagingDir | Out-Null
New-Item -ItemType Directory -Force -Path (Join-Path $rootDir "dist") | Out-Null

$appFiles = @('index.php', 'proses.php', 'safety.php', 'update.php', 'sae-logo.png', 'favicon.png', 'run_feeder.bat', '.htaccess', 'VERSION')
foreach ($f in $appFiles) {
    $src = Join-Path $rootDir $f
    if (Test-Path $src) { Copy-Item $src "$stagingDir\$f" -Force }
}
Copy-Item "$rootDir\assets" "$stagingDir\assets" -Recurse -Force

# Config kosong untuk instalasi baru
Copy-Item "$rootDir\dynamic_config.json.example" "$stagingDir\dynamic_config.json.example" -Force -ErrorAction SilentlyContinue

if ($BundlePhp) {
    $phpFound = Get-ChildItem "C:\laragon\bin\php" -Directory -ErrorAction SilentlyContinue |
        Where-Object { Test-Path (Join-Path $_.FullName 'php.exe') } | Select-Object -First 1
    if ($phpFound) { $PhpSource = $phpFound.FullName }
    if (-not (Test-Path (Join-Path $PhpSource 'php.exe'))) { throw "Sumber PHP tidak ditemukan: $PhpSource" }
    New-Item -ItemType Directory -Force -Path "$stagingDir\php\ext" | Out-Null
    $phpFiles = @('php.exe', 'php-win.exe', 'php8ts.dll', 'libcrypto-3-x64.dll', 'libssl-3-x64.dll', 'nghttp2.dll', 'libssh2.dll', 'vcruntime140.dll', 'vcruntime140_1.dll', 'msvcp140.dll')
    foreach ($f in $phpFiles) { Copy-Item (Join-Path $PhpSource $f) "$stagingDir\php\$f" -Force -ErrorAction SilentlyContinue }
    foreach ($e in 'php_curl.dll', 'php_openssl.dll', 'php_mbstring.dll', 'php_fileinfo.dll', 'php_sockets.dll') {
        Copy-Item (Join-Path $PhpSource "ext\$e") "$stagingDir\php\ext\$e" -Force -ErrorAction SilentlyContinue
    }
    @"
extension_dir = "ext"
extension=curl
extension=mbstring
extension=openssl
extension=fileinfo
extension=sockets
date.timezone = Asia/Jakarta
"@ | Set-Content "$stagingDir\php\php.ini" -Encoding ASCII
}

if (Test-Path $zipPath) { Remove-Item $zipPath -Force }
Add-Type -AssemblyName System.IO.Compression.FileSystem
[System.IO.Compression.ZipFile]::CreateFromDirectory($stagingDir, $zipPath, [System.IO.Compression.CompressionLevel]::Optimal, $false)

$sizeMb = [math]::Round((Get-Item $zipPath).Length / 1MB, 2)
Write-Host "Portable dibuat: $zipPath ($sizeMb MB)" -ForegroundColor Green
if (-not $BundlePhp) { Write-Host "PHP diambil dari Dapodik/PATH. Pakai -BundlePhp untuk menyertakan runtime PHP." -ForegroundColor Yellow }
