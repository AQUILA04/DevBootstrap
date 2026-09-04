@echo off
setlocal EnableExtensions
cd /d "%~dp0"

where dotnet >nul 2>&1
if errorlevel 1 (
  echo [build] dotnet SDK introuvable. Installe .NET 8 SDK puis relance.
  echo         winget install -e --id Microsoft.DotNet.SDK.8
  exit /b 1
)

set OUTDIR=%~dp0dist\StackPilot
if exist "%OUTDIR%" rmdir /s /q "%OUTDIR%"
mkdir "%OUTDIR%"

echo [build] dotnet publish StackPilot...
dotnet publish "%~dp0src\StackPilot\StackPilot.csproj" ^
  -c Release ^
  -r win-x64 ^
  --self-contained true ^
  -p:PublishSingleFile=true ^
  -p:IncludeNativeLibrariesForSelfExtract=true ^
  -o "%OUTDIR%"
if errorlevel 1 exit /b 1

copy /y "%~dp0catalog.json" "%OUTDIR%\catalog.json" >nul

> "%OUTDIR%\LIRE-MOI.txt" (
  echo StackPilot
  echo ==========
  echo.
  echo 1. Double-clic sur StackPilot.exe
  echo    Windows demandera l'elevation UAC ^(compte administrateur^).
  echo 2. Coche les outils a installer
  echo 3. Clique Installer
  echo 4. Attends la fin ^(WSL/Docker peuvent demander un redemarrage^)
  echo.
  echo Prerequis: Windows 10/11 avec winget ^(App Installer / Microsoft Store^).
  echo.
  echo Garde catalog.json a cote de StackPilot.exe.
)

if /I "%~1"=="-SkipZip" goto done
if /I "%~1"=="--skip-zip" goto done

echo [build] Creation archive dist\StackPilot.zip
if exist "%~dp0dist\StackPilot.zip" del /f /q "%~dp0dist\StackPilot.zip"
powershell -NoProfile -Command "Compress-Archive -Path '%OUTDIR%' -DestinationPath '%~dp0dist\StackPilot.zip' -Force"
if errorlevel 1 exit /b 1

:done
echo.
echo OK - Dossier pret: %OUTDIR%
if exist "%~dp0dist\StackPilot.zip" echo      Zip: %~dp0dist\StackPilot.zip
echo Usage: double-clic sur StackPilot.exe
exit /b 0
