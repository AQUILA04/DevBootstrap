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
echo https://apps.microsoft.com/search?query=StackPilot
start "" "https://apps.microsoft.com/search?query=StackPilot"
pause
endlocal
