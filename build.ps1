#requires -Version 5.1
<#
.SYNOPSIS
  Compile StackPilot (projet technique: dev-bootstrap) en dossier distributable EXE.

.DESCRIPTION
  Produit dist\StackPilot\ contenant:
    - StackPilot.exe  (interface graphique, elevation UAC)
    - bootstrap.ps1
    - catalog.json
    - branding.ps1
    - LIRE-MOI.txt

  Necessite le module PowerShell ps2exe (installe automatiquement si possible).

.EXAMPLE
  .\build.ps1
  .\build.ps1 -SkipZip
#>
[CmdletBinding()]
param(
    [switch]$SkipZip,
    [string]$OutputName = "StackPilot"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$root = if ($PSScriptRoot) { $PSScriptRoot } else { Split-Path -Parent $MyInvocation.MyCommand.Path }
Set-Location $root

function Write-Step([string]$Message) {
    Write-Host "[build] $Message" -ForegroundColor Cyan
}

function Ensure-Ps2Exe {
    $cmd = Get-Command Invoke-ps2exe -ErrorAction SilentlyContinue
    if ($cmd) { return }

    Write-Step "Module ps2exe introuvable - installation (CurrentUser)..."
    try {
        [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
        Set-PSRepository -Name PSGallery -InstallationPolicy Trusted -ErrorAction SilentlyContinue
    } catch {}

    if (-not (Get-PackageProvider -Name NuGet -ErrorAction SilentlyContinue)) {
        Install-PackageProvider -Name NuGet -MinimumVersion 2.8.5.201 -Force -Scope CurrentUser -ErrorAction SilentlyContinue | Out-Null
    }
    Install-Module -Name ps2exe -Scope CurrentUser -Force -AllowClobber -ErrorAction Stop
    Import-Module ps2exe -Force -ErrorAction Stop

    if (-not (Get-Command Invoke-ps2exe -ErrorAction SilentlyContinue)) {
        throw "ps2exe installe mais Invoke-ps2exe introuvable. Relance la session PowerShell puis .\build.ps1"
    }
}

# ASCII only messages in generated files for PS 5.1 encoding safety
$required = @("gui.ps1", "bootstrap.ps1", "catalog.json", "branding.ps1")
foreach ($f in $required) {
    if (-not (Test-Path -LiteralPath (Join-Path $root $f))) {
        throw "Fichier manquant: $f"
    }
}

Ensure-Ps2Exe
Import-Module ps2exe -ErrorAction SilentlyContinue

$distRoot = Join-Path $root "dist"
$outDir = Join-Path $distRoot $OutputName
if (Test-Path -LiteralPath $outDir) {
    Remove-Item -LiteralPath $outDir -Recurse -Force
}
New-Item -ItemType Directory -Path $outDir -Force | Out-Null

$exePath = Join-Path $outDir "$OutputName.exe"
$packedGui = Join-Path $outDir "_gui.packed.ps1"

Write-Step "Preparation du script GUI..."
Copy-Item -LiteralPath (Join-Path $root "gui.ps1") -Destination $packedGui -Force

Write-Step "Compilation EXE (ps2exe, requireAdmin)..."
# noConsole = fenetre Windows Forms uniquement
Invoke-ps2exe `
    -inputFile $packedGui `
    -outputFile $exePath `
    -noConsole `
    -requireAdmin `
    -title "StackPilot" `
    -description "Installateur du pack outils de developpement" `
    -company "StackPilot" `
    -product "StackPilot" `
    -version "1.0.0.0" `
    -copyright "Local use" `
    -noOutput `
    -noError

Remove-Item -LiteralPath $packedGui -Force -ErrorAction SilentlyContinue

Write-Step "Copie des dependances a cote de l'EXE..."
Copy-Item -LiteralPath (Join-Path $root "bootstrap.ps1") -Destination (Join-Path $outDir "bootstrap.ps1") -Force
Copy-Item -LiteralPath (Join-Path $root "catalog.json") -Destination (Join-Path $outDir "catalog.json") -Force
Copy-Item -LiteralPath (Join-Path $root "branding.ps1") -Destination (Join-Path $outDir "branding.ps1") -Force

$readmeUser = @"
StackPilot
==========
(nom technique du projet: dev-bootstrap)

1. Clic droit sur $OutputName.exe -> Executer en tant qu'administrateur
   (ou double-clic: Windows demandera l'elevation UAC)
2. Coche les outils a installer
3. Clique Installer
4. Attends la fin (WSL/Docker peuvent demander un redemarrage)

Prerequis: Windows 10/11 avec winget (App Installer / Microsoft Store).

Ne separe pas les fichiers de ce dossier: l'EXE a besoin de bootstrap.ps1, catalog.json et branding.ps1.
"@
Set-Content -LiteralPath (Join-Path $outDir "LIRE-MOI.txt") -Value $readmeUser -Encoding ASCII

if (-not $SkipZip) {
    $zipPath = Join-Path $distRoot "$OutputName.zip"
    if (Test-Path -LiteralPath $zipPath) { Remove-Item -LiteralPath $zipPath -Force }
    Write-Step "Creation archive $zipPath"
    Compress-Archive -Path $outDir -DestinationPath $zipPath -Force
}

Write-Host ""
Write-Host "OK - Dossier pret:" -ForegroundColor Green
Write-Host "  $outDir"
if (-not $SkipZip) {
    Write-Host "  $(Join-Path $distRoot "$OutputName.zip")"
}
Write-Host ""
Write-Host "Usage final utilisateur: double-clic sur $OutputName.exe" -ForegroundColor Yellow
