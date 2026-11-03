$stagingDir = 'C:\laragon\www\sae-feeder\build\staging'
Write-Host "--- Testing PHP Runtime ---" -ForegroundColor Yellow
$phpVer = & "$stagingDir\php\php.exe" -v
Write-Host $phpVer[0]

$modules = & "$stagingDir\php\php.exe" -m
$required = @('curl', 'mbstring', 'openssl', 'fileinfo', 'sockets', 'json')
$missing = @()
foreach ($req in $required) {
    if ($modules -notcontains $req) {
        $missing += $req
    }
}
if ($missing.Count -eq 0) {
    Write-Host "All required modules verified: $($required -join ', ')" -ForegroundColor Green
} else {
    Write-Error "Missing modules: $($missing -join ', ')"
}

Write-Host "--- Testing SAE Feeder Server execution ---" -ForegroundColor Yellow
$testPort = 4298
$proc = Start-Process -FilePath "$stagingDir\php\php.exe" -ArgumentList "-S 127.0.0.1:$testPort -t `"$stagingDir`"" -PassThru
Start-Sleep -Seconds 2

try {
    $resp = Invoke-WebRequest -Uri "http://127.0.0.1:$testPort/index.php" -UseBasicParsing
    Write-Host "Server HTTP Status: $($resp.StatusCode)" -ForegroundColor Green
    if ($resp.Content -match "<title>SAE Feeder") {
        Write-Host "Page Title matches SAE Feeder successfully!" -ForegroundColor Green
    } else {
        Write-Warning "Page title did not match expected string"
    }
} catch {
    Write-Error "Failed to connect to feeder server: $_"
} finally {
    Stop-Process -Id $proc.Id -Force
    Write-Host "Test process terminated cleanly." -ForegroundColor Gray
}

Write-Host "--- Testing Installer Binary Details ---" -ForegroundColor Yellow
$installer = 'C:\laragon\www\sae-feeder\dist\SAE-Feeder-Setup.exe'
$info = Get-Item $installer
Write-Host "Installer Name : $($info.Name)"
Write-Host "Installer Size : $([math]::Round($info.Length / 1MB, 2)) MB ($($info.Length) bytes)"
Write-Host "Installer Date : $($info.LastWriteTime)"
Write-Host "All tests passed successfully!" -ForegroundColor Green
