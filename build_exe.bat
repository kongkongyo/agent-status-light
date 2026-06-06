@echo off
setlocal

cd /d "%~dp0"

powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0scripts\Build.ps1" %*
if errorlevel 1 (
  echo.
  echo Build failed.
  exit /b 1
)

echo.
echo Build finished: dist\AgentStatusLight.exe
exit /b 0
