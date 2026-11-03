# ==============================================================================
# SAE FEEDER - Release Script
# Build installer, bump VERSION, commit, tag, push, (opsional) upload release.
# ==============================================================================
param (
    [string]$Version,
    [string]$Message = "",
    [switch]$NoBuild,
    [switch]$NoPush
)

$ErrorActionPreference = "Stop"
$rootDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repo = "aadul888/sae-feeder"

$versionFile = Join-Path $rootDir "VERSION"
if (-not $Version) {
    $Version = (Get-Content $versionFile -Raw).Trim()
}
if ($Version -notmatch '^\d+\.\d+\.\d+$') {
    throw "Versi harus semver tanpa 'v', mis. 1.1.0"
}
Set-Content -Path $versionFile -Value $Version -NoNewline -Encoding ASCII
Write-Host "Versi: $Version" -ForegroundColor Cyan

# Sinkronkan versi ke skrip Inno Setup
$iss = Join-Path $rootDir "sae-feeder-setup.iss"
if (Test-Path $iss) {
    (Get-Content $iss -Raw) -replace '#define MyAppVersion ".*"', "#define MyAppVersion `"$Version`"" |
        Set-Content -Path $iss -Encoding ASCII
}

# Build installer
if (-not $NoBuild) {
    Write-Host "Membangun installer..." -ForegroundColor Yellow
    & powershell -ExecutionPolicy Bypass -File (Join-Path $rootDir "build-installer.ps1")
    Write-Host "Membangun paket portable..." -ForegroundColor Yellow
    & powershell -ExecutionPolicy Bypass -File (Join-Path $rootDir "build-portable.ps1")
}
$installer = Join-Path $rootDir "dist\SAE-Feeder-Setup.exe"
$portable = Join-Path $rootDir "dist\SAE-Feeder-Portable.zip"
if (-not (Test-Path $installer)) {
    throw "Installer tidak ditemukan: $installer"
}

# Commit + tag + push
if (-not $Message) { $Message = "Release v$Version" }
if (-not (Test-Path (Join-Path $rootDir ".git"))) {
    throw "Folder ini bukan repo git. Jalankan dari hasil clone repo $repo."
}

git -C $rootDir add -A
if (git -C $rootDir status --porcelain) {
    git -C $rootDir commit -m $Message
} else {
    Write-Host "Tidak ada perubahan untuk di-commit." -ForegroundColor DarkGray
}
git -C $rootDir tag -f "v$Version"

if (-not $NoPush) {
    git -C $rootDir push origin main
    git -C $rootDir push origin "v$Version" --force
}

# GitHub Release + upload installer (butuh GITHUB_TOKEN)
if ($env:GITHUB_TOKEN) {
    $headers = @{
        Authorization = "Bearer $($env:GITHUB_TOKEN)"
        Accept        = "application/vnd.github+json"
    }
    $releaseBody = @{
        tag_name   = "v$Version"
        name       = "SAE Feeder v$Version"
        body       = $Message
        draft      = $false
        prerelease = $false
    } | ConvertTo-Json

    $release = Invoke-RestMethod -Uri "https://api.github.com/repos/$repo/releases" -Method Post `
        -Headers $headers -Body $releaseBody -ContentType "application/json"

    $uploadUrl = $release.upload_url.Replace("{?name,label}", "?name=SAE-Feeder-Setup.exe")
    Invoke-RestMethod -Uri $uploadUrl -Method Post -Headers $headers `
        -InFile $installer -ContentType "application/octet-stream"

    if (Test-Path $portable) {
        $uploadPortable = $release.upload_url.Replace("{?name,label}", "?name=SAE-Feeder-Portable.zip")
        Invoke-RestMethod -Uri $uploadPortable -Method Post -Headers $headers `
            -InFile $portable -ContentType "application/zip"
    }

    Write-Host "Release selesai: $($release.html_url)" -ForegroundColor Green
} else {
    Write-Host "GITHUB_TOKEN tidak diset." -ForegroundColor Yellow
    Write-Host "Buat release dan unggah manual: https://github.com/$repo/releases" -ForegroundColor Yellow
    Write-Host "Agent installer yang dicek UI: $installer" -ForegroundColor DarkGray
}
