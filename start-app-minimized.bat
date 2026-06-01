@echo off
title Investment Portfolio Tracker Launcher (minimized)
setlocal EnableExtensions EnableDelayedExpansion

set "ROOT_DIR=%~dp0"
pushd "%ROOT_DIR%"

echo ==============================================
echo   Starting Investment Portfolio Tracker (minimized)
echo ==============================================
echo.

echo [0/3] Releasing occupied ports if needed...
for /f %%P in ('powershell -NoProfile -Command "$listeners = @(Get-NetTCPConnection -LocalPort 5091,4200,4201,4202,4203,4204,4205 -State Listen -ErrorAction SilentlyContinue | Select-Object -ExpandProperty OwningProcess -Unique); foreach ($processId in $listeners) { try { Stop-Process -Id $processId -Force -ErrorAction Stop } catch { } }"') do rem

echo [1/3] Starting Backend (.NET API)...
start "" /min powershell -NoProfile -NoExit -Command "Set-Location '%ROOT_DIR%src\Investment.API'; $host.UI.RawUI.WindowTitle = 'Investment API'; Write-Host 'Starting API on port 5091...' -ForegroundColor Green; dotnet run --launch-profile http"

echo [2/3] Starting Frontend (Angular Web)...
for /f %%P in ('powershell -NoProfile -Command "$ports = 4200..4205; foreach ($port in $ports) { if (-not (Get-NetTCPConnection -LocalPort $port -State Listen -ErrorAction SilentlyContinue)) { Write-Output $port; break } }"') do set ANGULAR_PORT=%%P
if not defined ANGULAR_PORT (
	echo No free Angular port found in range 4200-4205.
	popd
	exit /b 1
)
start "" /min powershell -NoProfile -NoExit -Command "Set-Location '%ROOT_DIR%src\Investment.Web'; $host.UI.RawUI.WindowTitle = 'Investment Web'; Write-Host 'Starting Angular UI on port %ANGULAR_PORT%...' -ForegroundColor Cyan; npm start -- --port %ANGULAR_PORT% --no-open"

echo.
echo [3/3] Waiting 15 seconds for services to initialize before opening browser...
timeout /t 15 /nobreak > nul

echo.
echo Opening your browser to the application...
start http://localhost:%ANGULAR_PORT%

echo.
echo ==============================================
echo   Services are running (windows started minimized).
echo   To stop the app, restore and close those windows.
echo ==============================================
if defined START_APP_SKIP_PAUSE goto :eof
popd
pause
