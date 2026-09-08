#requires -Version 5.1
<#
.SYNOPSIS
  Validate StackPilot packaging metadata, assets, and workflow consistency.
#>
[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$root = if ($PSScriptRoot) { Split-Path -Parent $PSScriptRoot } else { (Get-Location).Path }
Set-Location $root

. (Join-Path $root "branding.ps1")

$failures = New-Object System.Collections.Generic.List[string]

function Ok([string]$Message) { Write-Host "[ok] $Message" -ForegroundColor Green }
function Bad([string]$Message) { Write-Host "[FAIL] $Message" -ForegroundColor Red; $script:failures.Add($Message) }

Write-Host "Validating packaging metadata in $root"

[xml]$props = Get-Content (Join-Path $root "Directory.Build.props") -Raw
$pg = $props.Project.PropertyGroup
$version = [string]$pg.ProductVersion
$name = [string]$pg.ProductName
$company = [string]$pg.Company
$upgrade = [string]$pg.MsiUpgradeCode
$storeUrl = [string]$pg.MicrosoftStoreUrl
$storePlaceholder = [string]$pg.MicrosoftStoreUrlIsPlaceholder

if ($version -ne $ProductVersion) { Bad "Version drift props($version) vs branding($ProductVersion)" } else { Ok "Version $version" }
if ($name -ne $ProductName) { Bad "ProductName drift" } else { Ok "ProductName $name" }
if ($company -ne $CompanyName) { Bad "Company drift" } else { Ok "Company $company" }
if ($upgrade -ne $MsiUpgradeCode) { Bad "UpgradeCode drift" } else { Ok "UpgradeCode $upgrade" }

$vj = Get-Content (Join-Path $root "version.json") -Raw | ConvertFrom-Json
if ($vj.version -ne $version) { Bad "version.json mismatch" } else { Ok "version.json synchronized" }
if ([bool]$vj.microsoftStoreUrlIsPlaceholder -ne $true -and $storePlaceholder -eq "true") {
    Bad "Store placeholder flag mismatch between version.json and props"
}

$manifest = Get-Content (Join-Path $root "src\StackPilot\app.manifest") -Raw
if ($manifest -notmatch [regex]::Escape("version=`"$version.0`"")) {
    Bad "app.manifest assemblyIdentity version is not $version.0"
} else { Ok "app.manifest version $version.0" }
if ($manifest -notmatch 'requireAdministrator') {
    Bad "app.manifest missing requireAdministrator"
} else { Ok "requireAdministrator preserved" }

$csproj = Get-Content (Join-Path $root "src\StackPilot\StackPilot.csproj") -Raw
if ($csproj -notmatch 'StackPilot\.ico') { Bad "csproj missing ApplicationIcon" } else { Ok "ApplicationIcon wired" }

foreach ($asset in @(
        "assets\icons\StackPilot.svg",
        "assets\icons\StackPilot.ico",
        "assets\icons\StackPilot.png",
        "assets\icons\StoreLogo.png",
        "assets\icons\generate-icons.py",
        "src\StackPilot.Installer\StackPilot.Installer.wixproj",
        "src\StackPilot.Installer\Package.wxs",
        "src\StackPilot.Installer\License.rtf",
        "docs\microsoft-store.md",
        "landing-page\privacy.html"
    )) {
    if (-not (Test-Path (Join-Path $root $asset))) { Bad "Missing $asset" } else { Ok "Found $asset" }
}

$wxs = Get-Content (Join-Path $root "src\StackPilot.Installer\Package.wxs") -Raw
if ($wxs -notmatch '\$\(var\.MsiUpgradeCode\)') { Bad "Package.wxs UpgradeCode not using \$(var.MsiUpgradeCode)" } else { Ok "UpgradeCode via var.MsiUpgradeCode" }
if ($wxs -notmatch 'Scope="perMachine"') { Bad "MSI not perMachine" } else { Ok "MSI perMachine" }
if ($wxs -notmatch 'MajorUpgrade') { Bad "Missing MajorUpgrade" } else { Ok "MajorUpgrade present" }
if ($wxs -notmatch 'ProgramFiles64Folder') { Bad "Missing ProgramFiles64Folder" } else { Ok "Program Files x64 install" }
if ($wxs -notmatch 'StartMenuShortcut') { Bad "Missing Start Menu shortcut" } else { Ok "Start Menu shortcut" }
if ($wxs -notmatch 'ARPPRODUCTICON') { Bad "Missing ARP branding icon" } else { Ok "ARP branding" }

$wixproj = Get-Content (Join-Path $root "src\StackPilot.Installer\StackPilot.Installer.wixproj") -Raw
if ($wixproj -notmatch 'WixToolset\.Sdk/5\.0\.2') { Bad "WiX SDK not pinned to 5.0.2" } else { Ok "WiX SDK pinned 5.0.2" }

$workflow = Get-Content (Join-Path $root ".github\workflows\build.yml") -Raw
foreach ($needle in @(
        "trusted-signing-action",
        "Publish unsigned EXE",
        "Sign EXE",
        "Build MSI",
        "Sign MSI",
        "Silent MSI install",
        "must exactly match product version"
    )) {
    if ($workflow -notmatch [regex]::Escape($needle)) { Bad "Workflow missing: $needle" } else { Ok "Workflow has: $needle" }
}

if ($storePlaceholder -ne "true") {
    Write-Host "[warn] Store URL placeholder flag is false — ensure Product ID URL is live." -ForegroundColor Yellow
} else {
    Ok "Store URL is explicitly marked placeholder: $storeUrl"
}

if ($failures.Count -gt 0) {
    Write-Host ""
    Write-Host "$($failures.Count) validation failure(s)." -ForegroundColor Red
    $failures | ForEach-Object { Write-Host " - $_" }
    exit 1
}

Write-Host ""
Write-Host "Packaging validation passed." -ForegroundColor Green
exit 0
