#requires -Version 5.1
<#
.SYNOPSIS
  Build StackPilot native EXE via dotnet publish.

.EXAMPLE
  .\build.ps1
  .\build.ps1 -SkipZip
  .\build.ps1 -SkipMsi -SkipZip
  .\build.ps1 -MsiOnly
  .\build.cmd
#>
[CmdletBinding()]
param(
    [switch]$SkipZip,
    [switch]$SkipMsi,
    [switch]$MsiOnly,
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

$versionProps = [xml](Get-Content -LiteralPath (Join-Path $root "Directory.Build.props") -Raw)
$version = [string]$versionProps.Project.PropertyGroup.StackPilotVersion
if ($version -notmatch '^\d+\.\d+\.\d+$') {
    throw "StackPilotVersion doit respecter major.minor.patch pour le MSI (valeur: '$version')."
}

$outDir = Join-Path $root "dist\$OutputName"
$distDir = Join-Path $root "dist"
$msiPath = Join-Path $distDir "$OutputName-$version-x64.msi"

if ($MsiOnly -and $SkipMsi) {
    throw "-MsiOnly et -SkipMsi ne peuvent pas etre utilises ensemble."
}

if (-not $MsiOnly) {
    if (Test-Path -LiteralPath $outDir) {
        Remove-Item -LiteralPath $outDir -Recurse -Force
    }
    New-Item -ItemType Directory -Path $outDir -Force | Out-Null

    Write-Step "dotnet publish StackPilot $version (win-x64 self-contained)..."
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
StackPilot
==========

1. Double-clic sur StackPilot.exe
   Windows demandera l'elevation UAC (compte administrateur).
2. Coche les outils a installer
3. Clique Installer
4. Attends la fin (WSL/Docker peuvent demander un redemarrage)

Prerequis: Windows 10/11 avec winget (App Installer / Microsoft Store).

Garde catalog.json a cote de StackPilot.exe.
"@
    Set-Content -LiteralPath (Join-Path $outDir "LIRE-MOI.txt") -Value $readmeUser -Encoding ASCII
}
elseif (-not (Test-Path -LiteralPath (Join-Path $outDir "$OutputName.exe"))) {
    throw "-MsiOnly requiert un build existant dans $outDir."
}

# Optional Authenticode signing
$pfxB64 = $env:CODE_SIGN_PFX_BASE64
$pfxPassword = $env:CODE_SIGN_PASSWORD
$pfxPath = $null
$signtool = $null
if (($pfxB64 -and -not $pfxPassword) -or ($pfxPassword -and -not $pfxB64)) {
    throw "CODE_SIGN_PFX_BASE64 et CODE_SIGN_PASSWORD doivent etre definis ensemble."
}
if ($pfxB64 -and $pfxPassword) {
    $pfxPath = Join-Path $env:TEMP "stackpilot-codesign.pfx"
    [IO.File]::WriteAllBytes($pfxPath, [Convert]::FromBase64String($pfxB64))
    $signtool = Get-ChildItem "${env:ProgramFiles(x86)}\Windows Kits\10\bin" -Recurse -Filter signtool.exe -ErrorAction SilentlyContinue |
        Sort-Object FullName -Descending |
        Select-Object -First 1 -ExpandProperty FullName
    if (-not $signtool) {
        throw "signtool.exe introuvable alors que les secrets de signature sont fournis."
    }
}

function Sign-Artifact([string]$Path) {
    if (-not $script:signtool) {
        return
    }

    Write-Step "Signature Authenticode: $Path"
    & $script:signtool sign /fd SHA256 /f $script:pfxPath /p $pfxPassword `
        /tr http://timestamp.digicert.com /td SHA256 $Path
    if ($LASTEXITCODE -ne 0) {
        throw "signtool a echoue pour $Path (code $LASTEXITCODE)."
    }
    & $script:signtool verify /pa /all /v $Path
    if ($LASTEXITCODE -ne 0) {
        throw "Verification Authenticode echouee pour $Path."
    }
}

try {
    if (-not $MsiOnly) {
        Sign-Artifact (Join-Path $outDir "$OutputName.exe")
    }

    if (-not $SkipMsi) {
        Write-Step "Creation du MSI WiX $version..."
        if (Test-Path -LiteralPath $msiPath) {
            Remove-Item -LiteralPath $msiPath -Force
        }

        $installerProject = Join-Path $root "installer\StackPilot.Installer.wixproj"
        & dotnet build $installerProject `
            -c Release `
            -p:ProductVersion=$version `
            -p:PublishDir=$outDir `
            -o $distDir
        if ($LASTEXITCODE -ne 0) {
            throw "Construction MSI echouee (code $LASTEXITCODE)."
        }
        if (-not (Test-Path -LiteralPath $msiPath)) {
            throw "MSI attendu introuvable: $msiPath"
        }
        Sign-Artifact $msiPath
    }
}
finally {
    if ($pfxPath) {
        Remove-Item -LiteralPath $pfxPath -Force -ErrorAction SilentlyContinue
    }
}

if (-not $signtool) {
    Write-Step "Artefacts non signes localement; la CI Store utilisera Azure Artifact Signing."
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
if (-not $SkipMsi) {
    Write-Host "  $msiPath"
}
if (-not $SkipZip) {
    Write-Host "  $(Join-Path $root "dist\$OutputName.zip")"
}
Write-Host ""
Write-Host "Usage final: installe $OutputName-$version-x64.msi" -ForegroundColor Yellow
