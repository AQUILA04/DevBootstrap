#requires -Version 5.1
[CmdletBinding()]
param(
    [switch]$RequireSignature,
    [string]$ExpectedPublisher = "OptimizeSolux"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
$props = [xml](Get-Content -LiteralPath (Join-Path $root "Directory.Build.props") -Raw)
$version = [string]$props.Project.PropertyGroup.StackPilotVersion
$exe = Join-Path $root "dist\StackPilot\StackPilot.exe"
$msi = Join-Path $root "dist\StackPilot-$version-x64.msi"
$catalog = Join-Path $root "dist\StackPilot\catalog.json"

foreach ($path in @($exe, $msi, $catalog)) {
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Artefact requis introuvable: $path"
    }
}

foreach ($path in @($exe, $msi)) {
    $signature = Get-AuthenticodeSignature -LiteralPath $path
    if ($RequireSignature -and $signature.Status -ne [System.Management.Automation.SignatureStatus]::Valid) {
        throw "Signature invalide pour $path : $($signature.Status) $($signature.StatusMessage)"
    }
    if ($signature.Status -eq [System.Management.Automation.SignatureStatus]::Valid -and
        $signature.SignerCertificate.Subject -notlike "*$ExpectedPublisher*") {
        throw "Editeur inattendu pour $path : $($signature.SignerCertificate.Subject)"
    }
}

$hashes = @($exe, $msi) | Get-FileHash -Algorithm SHA256
$checksumPath = Join-Path $root "dist\SHA256SUMS.txt"
$hashes |
    ForEach-Object { "$($_.Hash.ToLowerInvariant())  $(Split-Path -Leaf $_.Path)" } |
    Set-Content -LiteralPath $checksumPath -Encoding ASCII

Write-Host "Release StackPilot $version validee."
Write-Host "Empreintes: $checksumPath"
