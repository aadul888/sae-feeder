# ==============================================================================
# SAE FEEDER - Build Script: Standalone Windows EXE Installer
# ==============================================================================
param (
    [string]$PhpSource = "C:\laragon\bin\php\php-8.3.30-Win32-vs16-x64",
    [switch]$SkipTests
)

$ErrorActionPreference = "Stop"
$sw = [System.Diagnostics.Stopwatch]::StartNew()

$rootDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$builderDir = Join-Path $rootDir "installer_builder"
$buildDir = Join-Path $rootDir "build"
$stagingDir = Join-Path $buildDir "staging"
$distDir = Join-Path $rootDir "dist"
$payloadZip = Join-Path $buildDir "payload.zip"
$finalInstaller = Join-Path $distDir "SAE-Feeder-Setup.exe"

Write-Host "============================================================" -ForegroundColor Cyan
Write-Host "  SAE FEEDER - PEMBUATAN FILE EXE INSTALLER MANDIRI        " -ForegroundColor Cyan
Write-Host "============================================================" -ForegroundColor Cyan

# 1. Cek Kompiler C# .NET
$csc = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if (-not (Test-Path $csc)) {
    $csc = "C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe"
}
if (-not (Test-Path $csc)) {
    Write-Error "Microsoft .NET Framework 4.x (csc.exe) tidak ditemukan pada sistem!"
}
Write-Host "[1/7] Kompiler C# .NET ditemukan: $csc" -ForegroundColor Green

# 2. Cek Sumber PHP
if (-not (Test-Path $PhpSource)) {
    $fallbackPhp = Get-ChildItem "C:\laragon\bin\php" -Directory | Select-Object -First 1 -ExpandProperty FullName
    if ($fallbackPhp -and (Test-Path "$fallbackPhp\php.exe")) {
        $PhpSource = $fallbackPhp
    } else {
        Write-Error "Direktori sumber PHP tidak ditemukan di: $PhpSource"
    }
}
Write-Host "[2/7] Menggunakan sumber runtime PHP: $PhpSource" -ForegroundColor Green

# 3. Bersihkan & Siapkan Direktori
Write-Host "[3/7] Menyiapkan direktori staging dan output..." -ForegroundColor Yellow
if (Test-Path $stagingDir) {
    Remove-Item $stagingDir -Recurse -Force -ErrorAction SilentlyContinue
}
if (Test-Path $payloadZip) {
    Remove-Item $payloadZip -Force -ErrorAction SilentlyContinue
}
New-Item -ItemType Directory -Force -Path $stagingDir | Out-Null
New-Item -ItemType Directory -Force -Path "$stagingDir\php" | Out-Null
New-Item -ItemType Directory -Force -Path "$stagingDir\php\ext" | Out-Null
New-Item -ItemType Directory -Force -Path "$stagingDir\cache" | Out-Null
New-Item -ItemType Directory -Force -Path "$stagingDir\logs" | Out-Null
New-Item -ItemType Directory -Force -Path $distDir | Out-Null

# 4. Buat Ikon Resolusi Tinggi
Write-Host "[4/7] Memvalidasi ikon aplikasi (sae-feeder.ico)..." -ForegroundColor Yellow
$icoPath = Join-Path $builderDir "sae-feeder.ico"
$makeIcoCs = Join-Path $builderDir "MakeIco.cs"
$makeIcoExe = Join-Path $builderDir "MakeIco.exe"
$pngLogo = Join-Path $rootDir "sae-logo.png"

& $csc /nologo /out:$makeIcoExe $makeIcoCs
& $makeIcoExe $pngLogo $icoPath
Copy-Item $icoPath "$rootDir\sae-feeder.ico" -Force
Copy-Item $icoPath "$stagingDir\sae-feeder.ico" -Force

# 5. Siapkan Runtime PHP Portabel Minimalis & Visual C++ Runtime
Write-Host "[5/7] Mengemas runtime PHP 8.3 & Visual C++ Redistributable mandiri..." -ForegroundColor Yellow
$corePhpFiles = @('php.exe', 'php-win.exe', 'php8ts.dll', 'libcrypto-3-x64.dll', 'libssl-3-x64.dll', 'nghttp2.dll', 'libssh2.dll')
foreach ($f in $corePhpFiles) {
    $src = Join-Path $PhpSource $f
    if (Test-Path $src) {
        Copy-Item $src "$stagingDir\php\$f" -Force
    }
}

# Visual C++ 2015-2022 x64 Runtime DLLs (agar berjalan di server/PC yang belum terinstal VC++ Redist)
$vcDlls = @(
    'vcruntime140.dll',
    'vcruntime140_1.dll',
    'msvcp140.dll',
    'msvcp140_1.dll',
    'msvcp140_2.dll',
    'msvcp140_atomic_wait.dll',
    'msvcp140_codecvt_ids.dll',
    'vcomp140.dll'
)
foreach ($v in $vcDlls) {
    $sysPath = "C:\Windows\System32\$v"
    if (Test-Path $sysPath) {
        Copy-Item $sysPath "$stagingDir\php\$v" -Force
    }
}

$extFiles = @('php_curl.dll', 'php_mbstring.dll', 'php_openssl.dll', 'php_fileinfo.dll', 'php_sockets.dll')
foreach ($e in $extFiles) {
    $src = Join-Path "$PhpSource\ext" $e
    if (Test-Path $src) {
        Copy-Item $src "$stagingDir\php\ext\$e" -Force
    }
}

# php.ini optimal
$phpIniContent = @'
[PHP]
engine = On
short_open_tag = Off
precision = 14
output_buffering = 4096
zlib.output_compression = Off
implicit_flush = Off
max_execution_time = 300
max_input_time = 60
memory_limit = 512M
error_reporting = E_ALL & ~E_DEPRECATED & ~E_STRICT
display_errors = Off
display_startup_errors = Off
log_errors = On
post_max_size = 64M
upload_max_filesize = 64M
default_mimetype = "text/html"
default_charset = "UTF-8"

extension_dir = "ext"
extension=curl
extension=mbstring
extension=openssl
extension=fileinfo
extension=sockets

[Date]
date.timezone = "Asia/Jakarta"

[curl]
curl.cainfo = ""
'@
Set-Content -Path "$stagingDir\php\php.ini" -Value $phpIniContent -Encoding ASCII

# 6. Kompilasi Launcher & Uninstaller
Write-Host "[6/7] Mengompilasi SAE-Feeder.exe (Launcher) dan Uninstall.exe..." -ForegroundColor Yellow

$launcherCs = Join-Path $builderDir "Launcher.cs"
$launcherExe = Join-Path $stagingDir "SAE-Feeder.exe"
& $csc /nologo /target:winexe /win32icon:$icoPath /out:$launcherExe $launcherCs
if (-not (Test-Path $launcherExe)) {
    Write-Error "Gagal mengompilasi SAE-Feeder.exe!"
}

$uninstallerCs = Join-Path $builderDir "Uninstaller.cs"
$uninstallerExe = Join-Path $stagingDir "Uninstall.exe"
& $csc /nologo /target:winexe /win32icon:$icoPath /out:$uninstallerExe $uninstallerCs
if (-not (Test-Path $uninstallerExe)) {
    Write-Error "Gagal mengompilasi Uninstall.exe!"
}

# Salin berkas aplikasi ke staging (TANPA dynamic_config.json — config bersih untuk fresh install)
$appFiles = @('index.php', 'proses.php', 'safety.php', 'sae-logo.png', 'favicon.png', 'run_feeder.bat', '.htaccess', 'VERSION')
foreach ($af in $appFiles) {
    $src = Join-Path $rootDir $af
    if (Test-Path $src) {
        Copy-Item $src "$stagingDir\$af" -Force
    }
}

# Buat dynamic_config.json KOSONG sebagai template default (fresh install tidak boleh pakai config dev)
$blankConfig = @'
{
    "dapodik": {
        "base_url": "http://localhost:5774",
        "npsn": "",
        "token": "",
        "last_token_update": null,
        "token_expires": null
    },
    "sae": {
        "base_url": "",
        "api_url": "",
        "api_key": "",
        "last_key_update": null,
        "is_hosting": false
    },
    "sync": {
        "auto_sync": false,
        "interval": 30,
        "timeout": 30,
        "max_retries": 3
    },
    "system": {
        "environment": "localhost",
        "php_version": "8.3",
        "created_at": null,
        "updated_at": null
    }
}
'@
Set-Content -Path "$stagingDir\dynamic_config.json" -Value $blankConfig -Encoding UTF8

if (Test-Path "$rootDir\assets") {
    Copy-Item "$rootDir\assets" "$stagingDir\assets" -Recurse -Force
}
if (Test-Path "$rootDir\get") {
    Copy-Item "$rootDir\get" "$stagingDir\get" -Recurse -Force
}

# 7. Buat Payload Zip & Kompilasi Setup Installer
Write-Host "[7/7] Memadatkan payload dan mengompilasi installer mandiri (SAE-Feeder-Setup.exe)..." -ForegroundColor Yellow

Add-Type -AssemblyName System.IO.Compression.FileSystem
[System.IO.Compression.ZipFile]::CreateFromDirectory($stagingDir, $payloadZip, [System.IO.Compression.CompressionLevel]::Optimal, $false)

$installerCs = Join-Path $builderDir "Installer.cs"
$resPayload = "/resource:`"$payloadZip`",payload.zip"
$resIco = "/resource:`"$icoPath`",sae-feeder.ico"
$resLogo = "/resource:`"$pngLogo`",sae-logo.png"

$cmdArgs = @(
    "/nologo",
    "/target:winexe",
    "/win32icon:`"$icoPath`"",
    "/r:System.IO.Compression.dll",
    "/r:System.IO.Compression.FileSystem.dll",
    $resPayload,
    $resIco,
    $resLogo,
    "/out:`"$finalInstaller`"",
    "`"$installerCs`""
)

Start-Process -FilePath $csc -ArgumentList $cmdArgs -Wait -NoNewWindow

if (-not (Test-Path $finalInstaller)) {
    Write-Error "Gagal menghasilkan file installer final: $finalInstaller"
}

# Copy to user desktop for convenience
$desktopInstaller = "C:\Users\ZIES\Desktop\SAE-Feeder-Setup.exe"
Copy-Item $finalInstaller $desktopInstaller -Force

$installerItem = Get-Item $finalInstaller
$installerSizeMb = [math]::Round(($installerItem.Length / 1MB), 2)
$hash = (Get-FileHash $finalInstaller -Algorithm SHA256).Hash

$sw.Stop()
Write-Host ""
Write-Host "============================================================" -ForegroundColor Green
Write-Host "  INSTALLER BERHASIL DIBUAT (SERVER & PC READY)!            " -ForegroundColor Green
Write-Host "============================================================" -ForegroundColor Green
Write-Host "  File Output : $finalInstaller" -ForegroundColor White
Write-Host "  Desktop     : $desktopInstaller" -ForegroundColor White
Write-Host "  Ukuran      : $installerSizeMb MB" -ForegroundColor White
Write-Host "  SHA-256     : $hash" -ForegroundColor Gray
Write-Host "  Waktu Build : $([math]::Round($sw.Elapsed.TotalSeconds, 2)) detik" -ForegroundColor White
Write-Host "============================================================" -ForegroundColor Green
Write-Host ""
Write-Host "Runtime PHP kini 100% mandiri termasuk pustaka libssh2 dan Visual C++ Redistributable." -ForegroundColor Cyan
