@echo off
setlocal EnableExtensions
title SAE Feeder (Local)
cd /d "%~dp0"

rem --- Cari PHP: bundle sendiri, PHP Dapodik, lalu PATH ---
set "PHP_EXE="
if exist "%~dp0php\php.exe" set "PHP_EXE=%~dp0php\php.exe"
if not defined PHP_EXE if exist "C:\Program Files (x86)\Dapodik\php\php.exe" set "PHP_EXE=C:\Program Files (x86)\Dapodik\php\php.exe"
if not defined PHP_EXE if exist "C:\Program Files\Dapodik\php\php.exe" set "PHP_EXE=C:\Program Files\Dapodik\php\php.exe"
if not defined PHP_EXE for /f "delims=" %%i in ('where php 2^>nul') do if not defined PHP_EXE set "PHP_EXE=%%i"

if not defined PHP_EXE (
    echo [X] PHP tidak ditemukan.
    echo     Pasang Dapodik di PC ini, atau taruh folder "php" di samping file ini.
    pause
    exit /b 1
)

set "PORT=4215"
echo [i] PHP  : %PHP_EXE%
echo [i] Buka : http://localhost:%PORT%
echo     Tutup jendela ini untuk menghentikan SAE Feeder.

rem Buka browser setelah server siap
start "" cmd /c "timeout /t 2 /nobreak >nul & start "" http://localhost:%PORT%"

"%PHP_EXE%" -d display_errors=0 -d snmp.disable=1 -S 127.0.0.1:%PORT% -t "%~dp0"

echo.
echo Server berhenti.
pause
