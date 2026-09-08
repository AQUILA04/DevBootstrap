#requires -Version 5.1
<#
.SYNOPSIS
  Phased StackPilot build: publish EXE, optional sign, WiX MSI, optional MSI sign, versioned ZIP + checksums.

.EXAMPLE
  .\build.ps1
  .\build.ps1 -Phase PublishExe
  .\build.ps1 -Phase BuildMsi
  .\build.ps1 -SkipSign -SkipZip
  .\build.cmd
#>
[CmdletBinding()]
param(
    [ValidateSet("All", "PublishExe", "SignExe", "BuildMsi", "SignMsi", "Package")]
    [string]$Phase = "All",
    [switch]$SkipZip,
    [switch]$SkipMsi,
    [switch]$SkipSign,
    [string]$OutputName = "StackPilot",
    [string]$PublishDir = "",
    [string]$MsiOutDir = ""
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$root = if ($PSScriptRoot) { $PSScriptRoot } else { Split-Path -Parent $MyInvocation.MyCommand.Path }
Set-Location $root

. (Join-Path $root "branding.ps1")

function Write-Step([string]$Message) {
    Write-Host "[build] $Message" -ForegroundColor Cyan
}

function Get-ProductMetadata {
    $propsPath = Join-Path $root "Directory.Build.props"
    [xml]$xml = Get-Content -LiteralPath $propsPath -Raw
    $pg = $xml.Project.PropertyGroup
    $version = [string]$pg.ProductVersion
    $name = [string]$pg.ProductName
    $company = [string]$pg.Company
    if (-not $version) { throw "ProductVersion missing in Directory.Build.props" }
    if ($version -ne $script:ProductVersion) {
        throw "Version drift: Directory.Build.props=$version branding.ps1=$($script:ProductVersion)"
    }
    if ($name -ne $script:ProductName) {
        throw "ProductName drift: Directory.Build.props=$name branding.ps1=$($script:ProductName)"
    }
    return [pscustomobject]@{
        Version = $version
        Name    = $name
        Company = $company
        Tag     = "v$version"
    }
}

function Find-SignTool {
    $kits = Join-Path ${env:ProgramFiles(x86)} "Windows Kits\10\bin"
    if (-not (Test-Path -LiteralPath $kits)) { return $null }
    return Get-ChildItem $kits -Recurse -Filter signtool.exe -ErrorAction SilentlyContinue |
        Sort-Object FullName -Descending |
        Select-Object -First 1 -ExpandProperty FullName
}

function Invoke-OptionalPfxSign {
    param(
        [Parameter(Mandatory = $true)][string]$FilePath,
        [Parameter(Mandatory = $true)][string]$Label
    )
    if ($SkipSign) {
        Write-Step "SkipSign: $Label non signe."
        return
    }
    $pfxB64 = $env:CODE_SIGN_PFX_BASE64
    $pfxPassword = $env:CODE_SIGN_PASSWORD
    if (-not $pfxB64 -or -not $pfxPassword) {
        Write-Step "Pas de secrets CODE_SIGN_* - $Label non signe localement (Azure Artifact Signing en CI release)."
        return
    }
    Write-Step "Signature Authenticode PFX: $Label"
    $pfxPath = Join-Path $env:TEMP "stackpilot-codesign.pfx"
    try {
        [IO.File]::WriteAllBytes($pfxPath, [Convert]::FromBase64String($pfxB64))
        $signtool = Find-SignTool
        if (-not $signtool) {
            Write-Warning "signtool.exe introuvable - signature PFX ignoree."
            return
        }
        & $signtool sign /fd SHA256 /f $pfxPath /p $pfxPassword /tr http://timestamp.digicert.com /td SHA256 $FilePath
        if ($LASTEXITCODE -ne 0) {
            throw "signtool a echoue pour $Label (code $LASTEXITCODE)."
        }
        Write-Host "[build] Signe: $FilePath" -ForegroundColor Green
    }
    finally {
        Remove-Item -LiteralPath $pfxPath -Force -ErrorAction SilentlyContinue
    }
}

function Invoke-PublishExe {
    param([Parameter(Mandatory = $true)]$Meta)

    $dotnet = Get-Command dotnet -ErrorAction SilentlyContinue
    if (-not $dotnet) {
        throw "dotnet SDK introuvable. Installe .NET 8 SDK (winget install -e --id Microsoft.DotNet.SDK.8)."
    }

    if (Test-Path -LiteralPath $script:ResolvedPublishDir) {
        Remove-Item -LiteralPath $script:ResolvedPublishDir -Recurse -Force
    }
    New-Item -ItemType Directory -Path $script:ResolvedPublishDir -Force | Out-Null

    Write-Step "dotnet publish $($Meta.Name) $($Meta.Version) (win-x64 self-contained)..."
    $project = Join-Path $root "src\StackPilot\StackPilot.csproj"
    & dotnet publish $project `
        -c Release `
        -r win-x64 `
        --self-contained true `
        -p:PublishSingleFile=true `
        -p:IncludeNativeLibrariesForSelfExtract=true `
        -p:Version=$($Meta.Version) `
        -p:ProductVersion=$($Meta.Version) `
        -o $script:ResolvedPublishDir
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet publish a echoue (code $LASTEXITCODE)."
    }

    Copy-Item -LiteralPath (Join-Path $root "catalog.json") -Destination (Join-Path $script:ResolvedPublishDir "catalog.json") -Force

    $readmeUser = @"
$($Meta.Name) v$($Meta.Version)
=================

Installation recommandee: Microsoft Store (quand la fiche est publiee).
Secours: MSI signe GitHub Releases.

1. Lance $($Meta.Name).exe
   Windows demandera l'elevation UAC (compte administrateur).
2. Choisis un profil (Base, Frontend, Backend, Fullstack, DevOps, Mobile, UX/UI)
3. Ajuste la checklist si besoin
4. Clique Installer
5. Attends la fin (WSL/Docker/Flutter peuvent demander un redemarrage ou un nouveau terminal)

Prerequis: Windows 10/11 avec winget (App Installer / Microsoft Store).

Garde catalog.json a cote de $($Meta.Name).exe.
Site: $($script:PackageProjectUrl)
Confidentialite: $($script:PrivacyPolicyUrl)
"@
    Set-Content -LiteralPath (Join-Path $script:ResolvedPublishDir "LIRE-MOI.txt") -Value $readmeUser -Encoding ASCII
}

function Invoke-BuildMsi {
    param([Parameter(Mandatory = $true)]$Meta)

    $dotnet = Get-Command dotnet -ErrorAction SilentlyContinue
    if (-not $dotnet) {
        throw "dotnet SDK introuvable pour le build MSI."
    }

    $exePath = Join-Path $script:ResolvedPublishDir "$OutputName.exe"
    $catalogPath = Join-Path $script:ResolvedPublishDir "catalog.json"
    if (-not (Test-Path -LiteralPath $exePath)) {
        throw "EXE manquant ($exePath). Lance d'abord -Phase PublishExe."
    }
    if (-not (Test-Path -LiteralPath $catalogPath)) {
        throw "catalog.json manquant dans le dossier publish."
    }

    if (-not (Test-Path -LiteralPath $script:ResolvedMsiOutDir)) {
        New-Item -ItemType Directory -Path $script:ResolvedMsiOutDir -Force | Out-Null
    }

    $wixproj = Join-Path $root "src\StackPilot.Installer\StackPilot.Installer.wixproj"
    Write-Step "WiX MSI ($($Meta.Version)) depuis EXE publie..."
    & dotnet build $wixproj `
        -c Release `
        -p:StackPilotPublishDir="$($script:ResolvedPublishDir.TrimEnd('\'))\" `
        -p:ProductVersion=$($Meta.Version) `
        -p:OutputPath="$($script:ResolvedMsiOutDir.TrimEnd('\'))\" `
        -p:BaseOutputPath="$($script:ResolvedMsiOutDir.TrimEnd('\'))\"
    if ($LASTEXITCODE -ne 0) {
        throw "Build MSI WiX a echoue (code $LASTEXITCODE). WiX 5 requiert Windows."
    }

    $msiName = "$OutputName-$($Meta.Version)-x64.msi"
    $msiPath = Join-Path $script:ResolvedMsiOutDir $msiName
    if (-not (Test-Path -LiteralPath $msiPath)) {
        $found = Get-ChildItem -LiteralPath $script:ResolvedMsiOutDir -Filter "*.msi" -Recurse -ErrorAction SilentlyContinue |
            Select-Object -First 1
        if ($found) {
            $dest = Join-Path $script:ResolvedMsiOutDir $msiName
            Copy-Item -LiteralPath $found.FullName -Destination $dest -Force
            $msiPath = $dest
        }
        else {
            throw "MSI introuvable apres build WiX (attendu: $msiPath)."
        }
    }
    Write-Host "[build] MSI: $msiPath" -ForegroundColor Green
    return $msiPath
}

function Invoke-PackageArtifacts {
    param(
        [Parameter(Mandatory = $true)]$Meta,
        [string]$MsiPath
    )

    $distRoot = Join-Path $root "dist"
    if (-not (Test-Path -LiteralPath $distRoot)) {
        New-Item -ItemType Directory -Path $distRoot -Force | Out-Null
    }

    $zipName = "$OutputName-$($Meta.Version)-x64.zip"
    $zipPath = Join-Path $distRoot $zipName

    if (-not $SkipZip) {
        if (Test-Path -LiteralPath $zipPath) { Remove-Item -LiteralPath $zipPath -Force }
        Write-Step "Creation archive versionnee $zipPath"
        Compress-Archive -Path (Join-Path $script:ResolvedPublishDir "*") -DestinationPath $zipPath -Force
    }

    $checksumPath = Join-Path $distRoot "$OutputName-$($Meta.Version)-SHA256SUMS.txt"
    $hashLines = @()
    foreach ($candidate in @(
            (Join-Path $script:ResolvedPublishDir "$OutputName.exe"),
            $zipPath,
            $MsiPath
        )) {
        if ($candidate -and (Test-Path -LiteralPath $candidate)) {
            $hash = (Get-FileHash -LiteralPath $candidate -Algorithm SHA256).Hash.ToLowerInvariant()
            $name = Split-Path -Leaf $candidate
            $hashLines += "$hash  $name"
            Write-Host "[build] SHA256 $name = $hash"
        }
    }
    Set-Content -LiteralPath $checksumPath -Value ($hashLines -join "`n") -Encoding ASCII
    Write-Host "[build] Checksums: $checksumPath" -ForegroundColor Green
}

# --- main ---
$meta = Get-ProductMetadata
$script:ResolvedPublishDir = if ($PublishDir) { $PublishDir } else { Join-Path $root "dist\$OutputName" }
$script:ResolvedMsiOutDir = if ($MsiOutDir) { $MsiOutDir } else { Join-Path $root "dist\msi" }

Write-Step "$($meta.Name) $($meta.Version) (tag $($meta.Tag)) — phase $Phase"

$runPublish = $Phase -in @("All", "PublishExe")
$runSignExe = $Phase -in @("All", "SignExe")
$runMsi = ($Phase -in @("All", "BuildMsi")) -and (-not $SkipMsi)
$runSignMsi = ($Phase -in @("All", "SignMsi")) -and (-not $SkipMsi)
$runPackage = $Phase -in @("All", "Package")

$msiPath = $null

if ($runPublish) {
    Invoke-PublishExe -Meta $meta
}

if ($runSignExe) {
    $exePath = Join-Path $script:ResolvedPublishDir "$OutputName.exe"
    if (-not (Test-Path -LiteralPath $exePath)) {
        throw "EXE manquant pour SignExe: $exePath"
    }
    Invoke-OptionalPfxSign -FilePath $exePath -Label "EXE"
}

if ($runMsi) {
    $msiPath = Invoke-BuildMsi -Meta $meta
}

if ($runSignMsi) {
    if (-not $msiPath) {
        $msiPath = Join-Path $script:ResolvedMsiOutDir "$OutputName-$($meta.Version)-x64.msi"
    }
    if (-not (Test-Path -LiteralPath $msiPath)) {
        throw "MSI manquant pour SignMsi: $msiPath"
    }
    Invoke-OptionalPfxSign -FilePath $msiPath -Label "MSI"
}

if ($runPackage -or ($Phase -eq "All")) {
    if (-not $msiPath) {
        $candidate = Join-Path $script:ResolvedMsiOutDir "$OutputName-$($meta.Version)-x64.msi"
        if (Test-Path -LiteralPath $candidate) { $msiPath = $candidate }
    }
    Invoke-PackageArtifacts -Meta $meta -MsiPath $msiPath
}

Write-Host ""
Write-Host "OK - Artifacts:" -ForegroundColor Green
Write-Host "  $($script:ResolvedPublishDir)"
if ($msiPath -and (Test-Path -LiteralPath $msiPath)) {
    Write-Host "  $msiPath"
}
$zipPath = Join-Path $root "dist\$OutputName-$($meta.Version)-x64.zip"
if ((-not $SkipZip) -and (Test-Path -LiteralPath $zipPath)) {
    Write-Host "  $zipPath"
}
Write-Host ""
Write-Host "Store (placeholder until Product ID): $($script:MicrosoftStoreUrl)" -ForegroundColor Yellow
Write-Host "Fallback MSI GitHub: $($script:GitHubMsiUrl)" -ForegroundColor Yellow
