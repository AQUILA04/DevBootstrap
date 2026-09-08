@echo off
setlocal
cd /d "%~dp0"

REM Store-first launcher helper. Store URL is a PLACEHOLDER until Partner Center Product ID exists.
set "STORE_URL=https://apps.microsoft.com/search?query=StackPilot%%20OptimizeSolux"
set "MSI_URL=https://github.com/AQUILA04/DevBootstrap/releases/latest/download/StackPilot-1.2.4-x64.msi"
set "ZIP_URL=https://github.com/AQUILA04/DevBootstrap/releases/latest/download/StackPilot-1.2.4-x64.zip"

if exist "%~dp0dist\StackPilot\StackPilot.exe" (
  start "" "%~dp0dist\StackPilot\StackPilot.exe"
  exit /b 0
)
if exist "%~dp0StackPilot.exe" (
  start "" "%~dp0StackPilot.exe"
  exit /b 0
)

echo StackPilot.exe introuvable dans ce dossier.
echo.
echo Installation recommandee: Microsoft Store
echo   %STORE_URL%
echo   (lien placeholder — la fiche Store n'est pas encore publiee)
echo.
echo Secours: MSI signe GitHub
echo   %MSI_URL%
echo ZIP: %ZIP_URL%
echo.
echo Ouvrir le Microsoft Store (recherche) maintenant ? [O/N]
choice /C ON /N /M ""
if errorlevel 2 goto msi
if errorlevel 1 (
  start "" "%STORE_URL%"
  exit /b 0
)

:msi
echo Ouverture de la page de telechargement MSI...
start "" "%MSI_URL%"
pause
endlocal
