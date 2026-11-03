@echo off
setlocal
cd /d "%~dp0"
set "PHP_EXE=%~dp0php\php.exe"
if not exist "%PHP_EXE%" set "PHP_EXE=php"
set "PATH=%~dp0php;%PATH%"
start "SAE Feeder" http://localhost:4215
"%PHP_EXE%" -S 0.0.0.0:4215 index.php
pause
