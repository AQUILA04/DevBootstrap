#requires -Version 5.1
<#
.SYNOPSIS
  Build StackPilot native EXE via dotnet publish.

.EXAMPLE
  .\build.ps1
  .\build.ps1 -SkipZip
  .\build.cmd
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

$dotnet = Get-Command dotnet -ErrorAction SilentlyContinue
if (-not $dotnet) {
    throw "dotnet SDK introuvable. Installe .NET 8 SDK (winget install -e --id Microsoft.DotNet.SDK.8)."
}

$outDir = Join-Path $root "dist\$OutputName"
if (Test-Path -LiteralPath $outDir) {
    Remove-Item -LiteralPath $outDir -Recurse -Force
}
New-Item -ItemType Directory -Path $outDir -Force | Out-Null

Write-Step "dotnet publish StackPilot (win-x64 self-contained)..."
$project = Join-Path $root "src\StackPilot\StackPilot.csproj"
& dotnet publish $project `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -o $outDir
if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish a echoue (code $LASTEXITCODE)."
}

Copy-Item -LiteralPath (Join-Path $root "catalog.json") -Destination (Join-Path $outDir "catalog.json") -Force

$readmeUser = @"
StackPilot v1.2.1
=================

1. Double-clic sur StackPilot.exe
   Windows demandera l'elevation UAC (compte administrateur).
2. Choisis un profil (Base, Frontend, Backend, Fullstack, DevOps, Mobile, UX/UI)
3. Ajuste la checklist si besoin
4. Clique Installer
5. Attends la fin (WSL/Docker/Flutter peuvent demander un redemarrage ou un nouveau terminal)

Prerequis: Windows 10/11 avec winget (App Installer / Microsoft Store).

Garde catalog.json a cote de StackPilot.exe.
"@
Set-Content -LiteralPath (Join-Path $outDir "LIRE-MOI.txt") -Value $readmeUser -Encoding ASCII

# Optional Authenticode signing
$pfxB64 = $env:CODE_SIGN_PFX_BASE64
$pfxPassword = $env:CODE_SIGN_PASSWORD
if ($pfxB64 -and $pfxPassword) {
    Write-Step "Signature Authenticode..."
    $pfxPath = Join-Path $env:TEMP "stackpilot-codesign.pfx"
    [IO.File]::WriteAllBytes($pfxPath, [Convert]::FromBase64String($pfxB64))
    $exePath = Join-Path $outDir "$OutputName.exe"
    $signtool = Get-ChildItem "${env:ProgramFiles(x86)}\Windows Kits\10\bin" -Recurse -Filter signtool.exe -ErrorAction SilentlyContinue |
        Sort-Object FullName -Descending |
        Select-Object -First 1 -ExpandProperty FullName
    if (-not $signtool) {
        Write-Warning "signtool.exe introuvable - signature ignoree."
    }
    else {
        & $signtool sign /fd SHA256 /f $pfxPath /p $pfxPassword /tr http://timestamp.digicert.com /td SHA256 $exePath
        if ($LASTEXITCODE -ne 0) {
            throw "signtool a echoue (code $LASTEXITCODE)."
        }
        Write-Host "[build] EXE signe: $exePath" -ForegroundColor Green
    }
    Remove-Item -LiteralPath $pfxPath -Force -ErrorAction SilentlyContinue
}
else {
    Write-Step "Pas de secrets CODE_SIGN_* - EXE non signe (OK pour ExecutionPolicy)."
}

if (-not $SkipZip) {
    $zipPath = Join-Path $root "dist\$OutputName.zip"
    if (Test-Path -LiteralPath $zipPath) { Remove-Item -LiteralPath $zipPath -Force }
    Write-Step "Creation archive $zipPath"
    Compress-Archive -Path $outDir -DestinationPath $zipPath -Force
}

Write-Host ""
Write-Host "OK - Dossier pret:" -ForegroundColor Green
Write-Host "  $outDir"
if (-not $SkipZip) {
    Write-Host "  $(Join-Path $root "dist\$OutputName.zip")"
}
Write-Host ""
Write-Host "Usage final: double-clic sur $OutputName.exe" -ForegroundColor Yellow
