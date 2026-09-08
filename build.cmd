@echo off
setlocal EnableExtensions
cd /d "%~dp0"

where dotnet >nul 2>&1
if errorlevel 1 (
  echo [build] dotnet SDK introuvable. Installe .NET 8 SDK puis relance.
  echo         winget install -e --id Microsoft.DotNet.SDK.8
  exit /b 1
)

set PHASE=All
set EXTRA=
:parse
if "%~1"=="" goto run
if /I "%~1"=="-SkipZip" set EXTRA=%EXTRA% -SkipZip& shift& goto parse
if /I "%~1"=="--skip-zip" set EXTRA=%EXTRA% -SkipZip& shift& goto parse
if /I "%~1"=="-SkipMsi" set EXTRA=%EXTRA% -SkipMsi& shift& goto parse
if /I "%~1"=="--skip-msi" set EXTRA=%EXTRA% -SkipMsi& shift& goto parse
if /I "%~1"=="-SkipSign" set EXTRA=%EXTRA% -SkipSign& shift& goto parse
if /I "%~1"=="-Phase" set PHASE=%~2& shift& shift& goto parse
echo [build] Argument inconnu: %~1
exit /b 1

:run
echo [build] Delegation a build.ps1 -Phase %PHASE% %EXTRA%
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0build.ps1" -Phase %PHASE% %EXTRA%
exit /b %ERRORLEVEL%
