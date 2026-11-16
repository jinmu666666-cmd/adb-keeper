@echo off
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Install-Dependencies.ps1"
if errorlevel 1 (
  echo Dependency setup failed. Read the error above.
  pause
  exit /b 1
)
echo Ready. Run AdbKeeper.exe.
pause
