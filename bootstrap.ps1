#requires -Version 5.1
<#
.SYNOPSIS
  Bootstrapper de poste de developpement via winget.

.EXAMPLE
  .\bootstrap.ps1
  .\bootstrap.ps1 -All
  .\bootstrap.ps1 -Defaults
  .\bootstrap.ps1 -Profile frontend
  .\bootstrap.ps1 -Profile devops
  .\bootstrap.ps1 -Keys git,node,docker
  .\bootstrap.ps1 -Tags java,ide
  .\bootstrap.ps1 -List
  .\bootstrap.ps1 -WhatIf -All
#>
[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [switch]$All,
    [switch]$Defaults,
    [string]$Profile,
    [string[]]$Keys,
    [string[]]$Tags,
    [switch]$List,
    [switch]$SkipInstalled,
    [string]$CatalogPath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Write-Info([string]$Message) { Write-Host "[*] $Message" -ForegroundColor Cyan }
function Write-Ok([string]$Message) { Write-Host "[+] $Message" -ForegroundColor Green }
function Write-WarnMsg([string]$Message) { Write-Host "[!] $Message" -ForegroundColor Yellow }
function Write-ErrMsg([string]$Message) { Write-Host "[x] $Message" -ForegroundColor Red }

function Test-Administrator {
    $identity = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = New-Object Security.Principal.WindowsPrincipal($identity)
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

function Assert-Winget {
    $winget = Get-Command winget -ErrorAction SilentlyContinue
    if (-not $winget) {
        throw "winget est introuvable. Installez 'App Installer' depuis le Microsoft Store, puis relancez."
    }
    Write-Info "winget detecte: $($winget.Source)"
    winget source update --disable-interactivity 2>$null | Out-Null
}

function Get-Catalog {
    param([string]$Path)
    if (-not (Test-Path -LiteralPath $Path)) {
        throw "Catalogue introuvable: $Path"
    }
    $raw = Get-Content -LiteralPath $Path -Raw -Encoding UTF8
    $catalog = $raw | ConvertFrom-Json
    if (-not $catalog.packages) {
        throw "Catalogue invalide: propriete 'packages' manquante."
    }
    return $catalog
}

function Get-Prop {
    param(
        [Parameter(Mandatory = $true)]$Object,
        [Parameter(Mandatory = $true)][string]$Name,
        $Default = $null
    )
    $prop = $Object.PSObject.Properties[$Name]
    if ($null -eq $prop) { return $Default }
    return $prop.Value
}

function Show-ProfileTable {
    param([object[]]$Profiles)
    if (-not $Profiles -or $Profiles.Count -eq 0) {
        Write-Host "Aucun profil defini (catalogue v1)." -ForegroundColor DarkGray
        return
    }

    Write-Host "=== Profils ===" -ForegroundColor Magenta
    foreach ($profile in $Profiles) {
        $pkgKeys = @(Get-Prop $profile "packages" @())
        $desc = Get-Prop $profile "description" ""
        Write-Host ("  {0,-22} {1,3} outils  {2}" -f $profile.key, $pkgKeys.Count, $profile.name) -ForegroundColor Cyan
        if ($desc) {
            Write-Host ("    {0}" -f $desc) -ForegroundColor DarkGray
        }
    }
    Write-Host ""
    Write-Host "  Exemple: .\bootstrap.ps1 -Profile frontend" -ForegroundColor DarkGray
    Write-Host ""
}

function Show-PackageTable {
    param([object[]]$Packages)
    $i = 1
    foreach ($pkg in $Packages) {
        $isDefault = [bool](Get-Prop $pkg "default" $false)
        $mark = if ($isDefault) { "*" } else { " " }
        $needsAdmin = [bool](Get-Prop $pkg "requiresAdmin" $false)
        $admin = if ($needsAdmin) { " [admin]" } else { "" }
        $tagList = @(Get-Prop $pkg "tags" @())
        $tags = if ($tagList.Count -gt 0) { ($tagList -join ",") } else { "-" }
        Write-Host ("{0,2}{1} {2,-34} {3,-42} {4}" -f $i, $mark, $pkg.name, $pkg.id, $tags) -NoNewline
        if ($admin) { Write-Host $admin -ForegroundColor DarkYellow } else { Write-Host "" }
        $i++
    }
    Write-Host ""
    Write-Host "  * = inclus dans -Defaults / profil Base" -ForegroundColor DarkGray
}

function Select-PackagesInteractive {
    param([object[]]$Packages)

    Write-Host ""
    Write-Host "=== StackPilot ===" -ForegroundColor Magenta
    Write-Host "Selectionne les outils a installer." -ForegroundColor Gray
    Write-Host "  all            -> tout installer"
    Write-Host "  defaults / d   -> profil Base (*)"
    Write-Host "  1,3,5-8        -> selection multiple"
    Write-Host "  q              -> quitter"
    Write-Host "  (ou: .\bootstrap.ps1 -Profile frontend|backend|fullstack|devops|ux-ui-web-designer)"
    Write-Host ""
    Show-PackageTable -Packages $Packages

    while ($true) {
        $answer = Read-Host "Choix"
        if ([string]::IsNullOrWhiteSpace($answer)) { continue }
        $answer = $answer.Trim().ToLowerInvariant()

        if ($answer -in @("q", "quit", "exit")) {
            return @()
        }
        if ($answer -eq "all") {
            return $Packages
        }
        if ($answer -in @("d", "defaults", "default")) {
            return @($Packages | Where-Object { [bool](Get-Prop $_ "default" $false) })
        }

        try {
            $indexes = @()
            foreach ($part in ($answer -split ",")) {
                $part = $part.Trim()
                if ($part -match '^\d+$') {
                    $indexes += [int]$part
                }
                elseif ($part -match '^(\d+)\s*-\s*(\d+)$') {
                    $start = [int]$Matches[1]
                    $end = [int]$Matches[2]
                    if ($start -gt $end) { $tmp = $start; $start = $end; $end = $tmp }
                    $indexes += $start..$end
                }
                else {
                    throw "Segment invalide: $part"
                }
            }

            $selected = foreach ($n in ($indexes | Select-Object -Unique)) {
                if ($n -lt 1 -or $n -gt $Packages.Count) {
                    throw "Index hors limites: $n"
                }
                $Packages[$n - 1]
            }
            return @($selected)
        }
        catch {
            Write-WarnMsg $_.Exception.Message
        }
    }
}

function Test-PackageInstalled {
    param([string]$Id)

    $output = & winget list --id $Id --exact --accept-source-agreements 2>$null | Out-String
    if ($LASTEXITCODE -ne 0) { return $false }
    # winget list renvoie parfois un header sans resultat utile
    return ($output -match [regex]::Escape($Id))
}

function Test-WindowsFeatureEnabled {
    param([string]$FeatureName)

    try {
        $feature = Get-WindowsOptionalFeature -Online -FeatureName $FeatureName -ErrorAction Stop
        return ($feature.State -eq "Enabled")
    }
    catch {
        return $false
    }
}

function Test-WslInstalled {
    $wslCmd = Get-Command wsl.exe -ErrorAction SilentlyContinue
    if (-not $wslCmd) { return $false }

    $prevEap = $ErrorActionPreference
    $ErrorActionPreference = "Continue"
    try {
        $statusOut = & wsl.exe --status 2>&1 | Out-String
        $statusCode = $LASTEXITCODE
    }
    finally {
        $ErrorActionPreference = $prevEap
    }

    if ($statusCode -eq 0) { return $true }
    if ($statusOut -match '(?i)not installed|pas install') { return $false }

    $ErrorActionPreference = "Continue"
    try {
        $null = & wsl.exe -l -q 2>&1
        if ($LASTEXITCODE -eq 0) { return $true }
    }
    finally {
        $ErrorActionPreference = $prevEap
    }

    if (Test-WindowsFeatureEnabled -FeatureName "Microsoft-Windows-Subsystem-Linux") {
        return $true
    }

    return (Test-PackageInstalled -Id "Microsoft.WSL")
}

function Test-IsPackageInstalled {
    param([object]$Package)

    $detect = Get-Prop $Package "detect" "winget"
    switch ($detect) {
        "wsl" { return Test-WslInstalled }
        default { return Test-PackageInstalled -Id $Package.id }
    }
}

function Enable-WslWindowsFeatures {
    $features = @(
        "Microsoft-Windows-Subsystem-Linux",
        "VirtualMachinePlatform"
    )
    $changed = $false

    foreach ($name in $features) {
        if (Test-WindowsFeatureEnabled -FeatureName $name) {
            Write-Ok "Composant Windows deja actif: $name"
            continue
        }

        Write-Info "Activation du composant Windows: $name"
        $dism = & dism.exe /online /enable-feature /featurename:$name /all /norestart 2>&1 | Out-String
        if ($LASTEXITCODE -in @(0, 3010)) {
            $changed = $true
            Write-Ok "Composant active (ou en attente de reboot): $name"
        }
        else {
            Write-WarnMsg "Echec activation $name (dism code $LASTEXITCODE)"
            if ($dism) { Write-Host $dism -ForegroundColor DarkGray }
        }
    }

    return $changed
}

function Install-WslPackage {
    param([object]$Package)

    $rebootLikely = $false

    if ($PSCmdlet.ShouldProcess("Windows features + $($Package.id)", "Install WSL")) {
        Write-Info "Installation: $($Package.name)"

        if (Enable-WslWindowsFeatures) {
            $rebootLikely = $true
        }

        $args = @(
            "install",
            "--id", $Package.id,
            "--exact",
            "--accept-package-agreements",
            "--accept-source-agreements",
            "--disable-interactivity"
        )
        & winget @args
        $wingetCode = $LASTEXITCODE

        if ($wingetCode -notin @(0, -1978335189)) {
            Write-WarnMsg "winget Microsoft.WSL a renvoye $wingetCode - tentative via wsl --install"
            $prevEap = $ErrorActionPreference
            $ErrorActionPreference = "Continue"
            try {
                & wsl.exe --install --no-distribution 2>&1 | Out-Host
                $wslInstallCode = $LASTEXITCODE
            }
            finally {
                $ErrorActionPreference = $prevEap
            }

            if ($wslInstallCode -notin @(0, 3010)) {
                return [pscustomobject]@{
                    status  = "failed"
                    message = "winget=$wingetCode, wsl --install=$wslInstallCode"
                }
            }
            $rebootLikely = $true
        }
        elseif ($wingetCode -eq -1978335189) {
            Write-Ok "WSL deja present via winget"
        }

        $prevEap = $ErrorActionPreference
        $ErrorActionPreference = "Continue"
        try {
            & wsl.exe --set-default-version 2 2>&1 | Out-Null
        }
        finally {
            $ErrorActionPreference = $prevEap
        }

        if (Test-WslInstalled) {
            $msg = if ($rebootLikely) { "OK (reboot recommande)" } else { "OK" }
            if ($rebootLikely) {
                Write-WarnMsg "WSL installe/active - redemarre Windows avant Docker Desktop."
            }
            return [pscustomobject]@{ status = "installed"; message = $msg }
        }

        if ($rebootLikely) {
            Write-WarnMsg "WSL active mais pas encore utilisable - un redemarrage est probablement requis."
            return [pscustomobject]@{ status = "installed"; message = "OK (reboot requis)" }
        }

        return [pscustomobject]@{ status = "failed"; message = "WSL non detecte apres installation" }
    }

    Write-Info "WhatIf: activer WSL features + winget install $($Package.id)"
    return [pscustomobject]@{ status = "whatif"; message = "Simulation" }
}

function Install-Package {
    param(
        [object]$Package,
        [bool]$IsAdmin,
        [bool]$CheckInstalled
    )

    $result = [ordered]@{
        key     = $Package.key
        name    = $Package.name
        id      = $Package.id
        status  = "pending"
        message = ""
    }

    if ([bool](Get-Prop $Package "requiresAdmin" $false) -and -not $IsAdmin -and -not $WhatIfPreference) {
        $result.status = "skipped"
        $result.message = "Droits administrateur requis"
        Write-WarnMsg "$($Package.name): saute (admin requis). Relance en PowerShell Admin."
        return [pscustomobject]$result
    }

    if ($CheckInstalled -and (Test-IsPackageInstalled -Package $Package)) {
        $result.status = "already"
        $result.message = "Deja installe"
        Write-Ok "$($Package.name): deja installe"
        return [pscustomobject]$result
    }

    $notes = Get-Prop $Package "notes" $null
    if ($notes) {
        Write-Info "$($Package.name): $notes"
    }

    $installer = Get-Prop $Package "installer" "winget"
    if ($installer -eq "wsl") {
        $wslResult = Install-WslPackage -Package $Package
        $result.status = $wslResult.status
        $result.message = $wslResult.message
        if ($result.status -eq "failed") {
            Write-ErrMsg "$($Package.name): $($result.message)"
        }
        elseif ($result.status -eq "installed") {
            Write-Ok "$($Package.name): $($result.message)"
        }
        return [pscustomobject]$result
    }

    $args = @(
        "install",
        "--id", $Package.id,
        "--exact",
        "--accept-package-agreements",
        "--accept-source-agreements",
        "--disable-interactivity"
    )

    if ($PSCmdlet.ShouldProcess($Package.id, "winget install")) {
        Write-Info "Installation: $($Package.name) ($($Package.id))"
        & winget @args
        if ($LASTEXITCODE -eq 0) {
            $result.status = "installed"
            $result.message = "OK"
            Write-Ok "$($Package.name): installe"
        }
        elseif ($LASTEXITCODE -eq -1978335189) {
            # No applicable upgrade / already installed (codes can vary; treat common already-present)
            $result.status = "already"
            $result.message = "Deja present (winget)"
            Write-Ok "$($Package.name): deja present"
        }
        else {
            $result.status = "failed"
            $result.message = "winget exit code $LASTEXITCODE"
            Write-ErrMsg "$($Package.name): echec (code $LASTEXITCODE)"
        }
    }
    else {
        $result.status = "whatif"
        $result.message = "Simulation"
        Write-Info "WhatIf: winget install $($Package.id)"
    }

    return [pscustomobject]$result
}

function Normalize-ListArg {
    param([string[]]$Values)
    if (-not $Values) { return @() }
    $result = foreach ($value in $Values) {
        if ([string]::IsNullOrWhiteSpace($value)) { continue }
        foreach ($part in ($value -split ",")) {
            $trimmed = $part.Trim()
            if ($trimmed) { $trimmed }
        }
    }
    return @($result)
}

function Get-ProfileByKey {
    param(
        [object]$Catalog,
        [string]$Key
    )
    $profiles = @(Get-Prop $Catalog "profiles" @())
    if ($profiles.Count -eq 0) { return $null }
    $wanted = $Key.ToLowerInvariant()
    return @($profiles | Where-Object { $_.key.ToLowerInvariant() -eq $wanted } | Select-Object -First 1)
}

function Resolve-ProfilePackages {
    param(
        [object]$Catalog,
        [object[]]$Packages,
        [string]$ProfileKey
    )

    $profile = Get-ProfileByKey -Catalog $Catalog -Key $ProfileKey
    if (-not $profile) {
        $available = @(Get-Prop $Catalog "profiles" @() | ForEach-Object { $_.key })
        if ($available.Count -eq 0) {
            throw "Aucun profil dans le catalogue. Utilise -Defaults, -Keys ou -Tags."
        }
        throw "Profil inconnu: $ProfileKey. Disponibles: $($available -join ', ')."
    }

    $wanted = @(Get-Prop $profile "packages" @() | ForEach-Object { $_.ToLowerInvariant() })
    $selected = @($Packages | Where-Object { $wanted -contains $_.key.ToLowerInvariant() })
    $selectedKeys = @($selected | ForEach-Object { $_.key.ToLowerInvariant() })
    $missing = @($wanted | Where-Object { $selectedKeys -notcontains $_ })
    if ($missing.Count -gt 0) {
        throw "Profil '$ProfileKey': cles inconnues dans packages: $($missing -join ', ')."
    }
    return $selected
}

function Resolve-Selection {
    param(
        [object]$Catalog,
        [object[]]$Packages,
        [switch]$All,
        [switch]$Defaults,
        [string]$Profile,
        [string[]]$Keys,
        [string[]]$Tags
    )

    if ($All) { return $Packages }

    if (-not [string]::IsNullOrWhiteSpace($Profile)) {
        return Resolve-ProfilePackages -Catalog $Catalog -Packages $Packages -ProfileKey $Profile.Trim()
    }

    if ($Defaults) {
        $baseProfile = Get-ProfileByKey -Catalog $Catalog -Key "base"
        if ($baseProfile) {
            return Resolve-ProfilePackages -Catalog $Catalog -Packages $Packages -ProfileKey "base"
        }
        return @($Packages | Where-Object { [bool](Get-Prop $_ "default" $false) })
    }

    $Keys = Normalize-ListArg -Values $Keys
    $Tags = Normalize-ListArg -Values $Tags

    if ($Keys.Count -gt 0) {
        $wanted = @($Keys | ForEach-Object { $_.ToLowerInvariant() })
        $selected = @($Packages | Where-Object { $wanted -contains $_.key.ToLowerInvariant() })
        $selectedKeys = @($selected | ForEach-Object { $_.key.ToLowerInvariant() })
        $missing = @($wanted | Where-Object { $selectedKeys -notcontains $_ })
        if ($missing.Count -gt 0) {
            throw "Cles inconnues: $($missing -join ', '). Utilise -List."
        }
        return $selected
    }

    if ($Tags.Count -gt 0) {
        $wanted = @($Tags | ForEach-Object { $_.ToLowerInvariant() })
        $selected = @($Packages | Where-Object {
                $pkgTags = @(Get-Prop $_ "tags" @() | ForEach-Object { $_.ToLowerInvariant() })
                ($wanted | Where-Object { $pkgTags -contains $_ }).Count -gt 0
            })
        if ($selected.Count -eq 0) {
            throw "Aucun paquet pour les tags: $($Tags -join ', ')"
        }
        return $selected
    }

    return $null
}

# --- main ---
$scriptRoot = if ($PSScriptRoot) { $PSScriptRoot } else { Split-Path -Parent $MyInvocation.MyCommand.Path }
if (-not $CatalogPath) {
    $CatalogPath = Join-Path $scriptRoot "catalog.json"
}

Assert-Winget
$catalog = Get-Catalog -Path $CatalogPath
$packages = @($catalog.packages)
$profiles = @(Get-Prop $catalog "profiles" @())

if ($List) {
    Write-Host "Catalogue version: $(Get-Prop $catalog 'version' '?')" -ForegroundColor DarkGray
    Show-ProfileTable -Profiles $profiles
    Show-PackageTable -Packages $packages
    Write-Host "Cles: $(($packages | ForEach-Object { $_.key }) -join ', ')" -ForegroundColor DarkGray
    exit 0
}

$selection = Resolve-Selection -Catalog $catalog -Packages $packages -All:$All -Defaults:$Defaults -Profile $Profile -Keys $Keys -Tags $Tags
if ($null -eq $selection) {
    $selection = Select-PackagesInteractive -Packages $packages
}
$selection = @($selection)

if ($selection.Count -eq 0) {
    Write-WarnMsg "Aucune selection. Abandon."
    exit 0
}

# Ordre: WSL avant Docker, Antigravity IDE avant Agent
$catalogKeys = @($packages | ForEach-Object { $_.key })
$selection = @(
    $selection | Sort-Object @{
        Expression = {
            switch ($_.key) {
                "wsl" { 0 }
                "docker" { 1 }
                "antigravity-ide" { 2 }
                "antigravity" { 3 }
                default { 4 }
            }
        }
    }, @{ Expression = { [array]::IndexOf($catalogKeys, $_.key) } }
)

$isAdmin = Test-Administrator
if (-not $isAdmin) {
    Write-WarnMsg "Session non-admin: WSL / Docker Desktop (et certains paquets) pourront etre sautes."
}

Write-Host ""
Write-Info "A installer ($($selection.Count)):"
foreach ($pkg in $selection) {
    Write-Host "  - $($pkg.name)"
}
Write-Host ""

$checkInstalled = $SkipInstalled -or (-not $WhatIfPreference)
$results = @()
foreach ($pkg in $selection) {
    $results += Install-Package -Package $pkg -IsAdmin $isAdmin -CheckInstalled:$checkInstalled
}

$reportPath = Join-Path $scriptRoot ("install-report-{0:yyyyMMdd-HHmmss}.json" -f (Get-Date))
$results | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath $reportPath -Encoding UTF8

Write-Host ""
Write-Host "=== Resume ===" -ForegroundColor Magenta
$groups = $results | Group-Object status
foreach ($g in $groups) {
    Write-Host ("  {0,-10} {1}" -f $g.Name, $g.Count)
}
Write-Info "Rapport: $reportPath"

$failed = @($results | Where-Object { $_.status -eq "failed" })
if ($failed.Count -gt 0) {
    exit 1
}
exit 0
