@echo off
setlocal
cd /d "%~dp0"
if exist "%~dp0dist\StackPilot\StackPilot.exe" (
  start "" "%~dp0dist\StackPilot\StackPilot.exe"
  exit /b 0
)
if exist "%~dp0StackPilot.exe" (
  start "" "%~dp0StackPilot.exe"
  exit /b 0
)
echo StackPilot.exe introuvable. Telecharge la release:
echo https://github.com/AQUILA04/DevBootstrap/releases/latest/download/StackPilot.zip
pause
endlocal
