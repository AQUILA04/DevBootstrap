#requires -Version 5.1
<#
.SYNOPSIS
  Interface graphique StackPilot (selection + installation).

.DESCRIPTION
  Produit commercial: StackPilot
  Projet technique:   dev-bootstrap

  Double-clic via dist\StackPilot\StackPilot.exe, ou:
    powershell -ExecutionPolicy Bypass -File .\gui.ps1
#>
Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# Remplace par le JSON du catalogue lors du build (EXE autonome).
$script:EmbeddedCatalog = $null

$brandingPath = Join-Path (if ($PSScriptRoot) { $PSScriptRoot } else { Split-Path -Parent $MyInvocation.MyCommand.Path }) "branding.ps1"
if (Test-Path -LiteralPath $brandingPath) {
    . $brandingPath
}
else {
    $script:ProductName = "StackPilot"
    $script:ProductTagline = "Pack outils de developpement"
}

Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing
[System.Windows.Forms.Application]::EnableVisualStyles()

function Get-ScriptRootPath {
    if ($PSScriptRoot) { return $PSScriptRoot }
    if ($MyInvocation.MyCommand.Path) {
        return (Split-Path -Parent $MyInvocation.MyCommand.Path)
    }
    return (Get-Location).Path
}

function Test-IsAdministrator {
    $identity = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = New-Object Security.Principal.WindowsPrincipal($identity)
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

function Get-AppCatalog {
    if ($script:EmbeddedCatalog) {
        return ($script:EmbeddedCatalog | ConvertFrom-Json)
    }

    $root = Get-ScriptRootPath
    $path = Join-Path $root "catalog.json"
    if (-not (Test-Path -LiteralPath $path)) {
        throw "catalogue introuvable: $path"
    }
    return (Get-Content -LiteralPath $path -Raw -Encoding UTF8 | ConvertFrom-Json)
}

function Request-ElevationIfNeeded {
    if (Test-IsAdministrator) { return $true }

    $answer = [System.Windows.Forms.MessageBox]::Show(
        "L'installation de WSL, Docker et certains outils necessite les droits Administrateur.`n`nRelancer en tant qu'administrateur ?",
        $script:ProductName,
        [System.Windows.Forms.MessageBoxButtons]::YesNo,
        [System.Windows.Forms.MessageBoxIcon]::Question
    )

    if ($answer -ne [System.Windows.Forms.DialogResult]::Yes) {
        return $false
    }

    $root = Get-ScriptRootPath
    $guiPath = Join-Path $root "gui.ps1"
    $exeCandidate = [System.Diagnostics.Process]::GetCurrentProcess().MainModule.FileName

    try {
        if ($exeCandidate -and ($exeCandidate -like "*.exe") -and ($exeCandidate -notlike "*powershell*")) {
            Start-Process -FileName $exeCandidate -Verb RunAs | Out-Null
        }
        elseif (Test-Path -LiteralPath $guiPath) {
            Start-Process -FileName "powershell.exe" -Verb RunAs -ArgumentList @(
                "-NoProfile",
                "-ExecutionPolicy", "Bypass",
                "-File", "`"$guiPath`""
            ) | Out-Null
        }
        else {
            throw "Impossible de determiner le lanceur a elever."
        }
    }
    catch {
        [System.Windows.Forms.MessageBox]::Show(
            "Elevation refusee ou impossible: $($_.Exception.Message)",
            $script:ProductName,
            [System.Windows.Forms.MessageBoxButtons]::OK,
            [System.Windows.Forms.MessageBoxIcon]::Error
        ) | Out-Null
        return $false
    }

    return $false  # l'instance actuelle doit se fermer
}

function Get-Prop {
    param($Object, [string]$Name, $Default = $null)
    $prop = $Object.PSObject.Properties[$Name]
    if ($null -eq $prop) { return $Default }
    return $prop.Value
}

function Sort-InstallSelection {
    param([object[]]$Packages, [object[]]$Selected)

    $catalogKeys = @($Packages | ForEach-Object { $_.key })
    return @(
        $Selected | Sort-Object @{
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
}

function Start-BootstrapInstall {
    param(
        [string[]]$Keys,
        [System.Windows.Forms.TextBox]$LogBox
    )

    $root = Get-ScriptRootPath
    $bootstrap = Join-Path $root "bootstrap.ps1"

    if (-not (Test-Path -LiteralPath $bootstrap)) {
        throw "bootstrap.ps1 introuvable a cote de l'application: $bootstrap"
    }

    $keysArg = ($Keys -join ",")
    $LogBox.AppendText("Lancement installation...`r`n")
    $LogBox.AppendText("bootstrap.ps1 -Keys $keysArg`r`n`r`n")
    [System.Windows.Forms.Application]::DoEvents()

    $output = & powershell.exe -NoProfile -ExecutionPolicy Bypass -File $bootstrap -Keys $keysArg 2>&1
    $exitCode = $LASTEXITCODE

    foreach ($line in @($output)) {
        $LogBox.AppendText("$line`r`n")
        $LogBox.SelectionStart = $LogBox.Text.Length
        $LogBox.ScrollToCaret()
        [System.Windows.Forms.Application]::DoEvents()
    }

    $LogBox.AppendText("`r`nCode de sortie: $exitCode`r`n")
    return $exitCode
}

# --- UI ---
try {
    $catalog = Get-AppCatalog
}
catch {
    [System.Windows.Forms.MessageBox]::Show(
        $_.Exception.Message,
        $script:ProductName,
        [System.Windows.Forms.MessageBoxButtons]::OK,
        [System.Windows.Forms.MessageBoxIcon]::Error
    ) | Out-Null
    exit 1
}

$packages = @($catalog.packages)

$form = New-Object System.Windows.Forms.Form
$form.Text = $script:ProductName
$form.Size = New-Object System.Drawing.Size(760, 640)
$form.StartPosition = "CenterScreen"
$form.MinimumSize = New-Object System.Drawing.Size(640, 520)
$form.Font = New-Object System.Drawing.Font("Segoe UI", 9)

$title = New-Object System.Windows.Forms.Label
$title.Text = $script:ProductName
$title.Font = New-Object System.Drawing.Font("Segoe UI Semibold", 14)
$title.Location = New-Object System.Drawing.Point(16, 12)
$title.AutoSize = $true
$form.Controls.Add($title)

$subtitle = New-Object System.Windows.Forms.Label
$subtitle.Text = "$($script:ProductTagline) - Coche les logiciels a installer, puis clique Installer. winget telecharge toujours la derniere version."
$subtitle.Location = New-Object System.Drawing.Point(18, 44)
$subtitle.Size = New-Object System.Drawing.Size(700, 36)
$form.Controls.Add($subtitle)

$checkedList = New-Object System.Windows.Forms.CheckedListBox
$checkedList.Location = New-Object System.Drawing.Point(20, 88)
$checkedList.Size = New-Object System.Drawing.Size(700, 260)
$checkedList.CheckOnClick = $true
$checkedList.Anchor = "Top,Left,Right"
$form.Controls.Add($checkedList)

foreach ($pkg in $packages) {
    $label = $pkg.name
    if (Get-Prop $pkg "requiresAdmin" $false) { $label = "$label [admin]" }
    $idx = $checkedList.Items.Add($label)
    if (Get-Prop $pkg "default" $false) {
        $checkedList.SetItemChecked($idx, $true)
    }
}

$btnAll = New-Object System.Windows.Forms.Button
$btnAll.Text = "Tout cocher"
$btnAll.Location = New-Object System.Drawing.Point(20, 360)
$btnAll.Size = New-Object System.Drawing.Size(110, 30)
$btnAll.Add_Click({
    for ($i = 0; $i -lt $checkedList.Items.Count; $i++) {
        $checkedList.SetItemChecked($i, $true)
    }
})
$form.Controls.Add($btnAll)

$btnNone = New-Object System.Windows.Forms.Button
$btnNone.Text = "Tout decocher"
$btnNone.Location = New-Object System.Drawing.Point(140, 360)
$btnNone.Size = New-Object System.Drawing.Size(110, 30)
$btnNone.Add_Click({
    for ($i = 0; $i -lt $checkedList.Items.Count; $i++) {
        $checkedList.SetItemChecked($i, $false)
    }
})
$form.Controls.Add($btnNone)

$btnDefaults = New-Object System.Windows.Forms.Button
$btnDefaults.Text = "Par defaut"
$btnDefaults.Location = New-Object System.Drawing.Point(260, 360)
$btnDefaults.Size = New-Object System.Drawing.Size(110, 30)
$btnDefaults.Add_Click({
    for ($i = 0; $i -lt $packages.Count; $i++) {
        $checkedList.SetItemChecked($i, [bool](Get-Prop $packages[$i] "default" $false))
    }
})
$form.Controls.Add($btnDefaults)

$adminLabel = New-Object System.Windows.Forms.Label
$adminLabel.Location = New-Object System.Drawing.Point(400, 365)
$adminLabel.Size = New-Object System.Drawing.Size(320, 24)
if (Test-IsAdministrator) {
    $adminLabel.Text = "Session: Administrateur"
    $adminLabel.ForeColor = [System.Drawing.Color]::ForestGreen
}
else {
    $adminLabel.Text = "Session: standard (elevation conseillee)"
    $adminLabel.ForeColor = [System.Drawing.Color]::DarkOrange
}
$form.Controls.Add($adminLabel)

$logBox = New-Object System.Windows.Forms.TextBox
$logBox.Location = New-Object System.Drawing.Point(20, 400)
$logBox.Size = New-Object System.Drawing.Size(700, 140)
$logBox.Multiline = $true
$logBox.ScrollBars = "Vertical"
$logBox.ReadOnly = $true
$logBox.Font = New-Object System.Drawing.Font("Consolas", 9)
$logBox.Anchor = "Top,Bottom,Left,Right"
$form.Controls.Add($logBox)

$btnInstall = New-Object System.Windows.Forms.Button
$btnInstall.Text = "Installer"
$btnInstall.Location = New-Object System.Drawing.Point(490, 555)
$btnInstall.Size = New-Object System.Drawing.Size(110, 32)
$btnInstall.Anchor = "Bottom,Right"
$btnInstall.BackColor = [System.Drawing.Color]::FromArgb(0, 120, 212)
$btnInstall.ForeColor = [System.Drawing.Color]::White
$btnInstall.FlatStyle = "Flat"
$form.Controls.Add($btnInstall)

$btnClose = New-Object System.Windows.Forms.Button
$btnClose.Text = "Fermer"
$btnClose.Location = New-Object System.Drawing.Point(610, 555)
$btnClose.Size = New-Object System.Drawing.Size(110, 32)
$btnClose.Anchor = "Bottom,Right"
$btnClose.Add_Click({ $form.Close() })
$form.Controls.Add($btnClose)

$btnInstall.Add_Click({
    $selected = @()
    for ($i = 0; $i -lt $checkedList.Items.Count; $i++) {
        if ($checkedList.GetItemChecked($i)) {
            $selected += $packages[$i]
        }
    }

    if ($selected.Count -eq 0) {
        [System.Windows.Forms.MessageBox]::Show(
            "Selectionne au moins un outil.",
            $script:ProductName,
            [System.Windows.Forms.MessageBoxButtons]::OK,
            [System.Windows.Forms.MessageBoxIcon]::Information
        ) | Out-Null
        return
    }

    $needsAdmin = @($selected | Where-Object { Get-Prop $_ "requiresAdmin" $false }).Count -gt 0
    if ($needsAdmin -and -not (Test-IsAdministrator)) {
        $continue = Request-ElevationIfNeeded
        if (-not $continue) {
            if (-not (Test-IsAdministrator)) {
                # soit refuse, soit nouvelle instance lancee
                $form.Close()
            }
            return
        }
    }

    $selected = Sort-InstallSelection -Packages $packages -Selected $selected
    $keys = @($selected | ForEach-Object { $_.key })

    $btnInstall.Enabled = $false
    $btnAll.Enabled = $false
    $btnNone.Enabled = $false
    $btnDefaults.Enabled = $false
    $checkedList.Enabled = $false
    $form.Cursor = [System.Windows.Forms.Cursors]::WaitCursor

    try {
        $code = Start-BootstrapInstall -Keys $keys -LogBox $logBox
        if ($code -eq 0) {
            [System.Windows.Forms.MessageBox]::Show(
                "Installation terminee. Consulte le journal pour le detail.`nUn redemarrage peut etre necessaire (WSL/Docker).",
                $script:ProductName,
                [System.Windows.Forms.MessageBoxButtons]::OK,
                [System.Windows.Forms.MessageBoxIcon]::Information
            ) | Out-Null
        }
        else {
            [System.Windows.Forms.MessageBox]::Show(
                "Installation terminee avec des erreurs (code $code). Voir le journal.",
                $script:ProductName,
                [System.Windows.Forms.MessageBoxButtons]::OK,
                [System.Windows.Forms.MessageBoxIcon]::Warning
            ) | Out-Null
        }
    }
    catch {
        $logBox.AppendText("ERREUR: $($_.Exception.Message)`r`n")
        [System.Windows.Forms.MessageBox]::Show(
            $_.Exception.Message,
            $script:ProductName,
            [System.Windows.Forms.MessageBoxButtons]::OK,
            [System.Windows.Forms.MessageBoxIcon]::Error
        ) | Out-Null
    }
    finally {
        $btnInstall.Enabled = $true
        $btnAll.Enabled = $true
        $btnNone.Enabled = $true
        $btnDefaults.Enabled = $true
        $checkedList.Enabled = $true
        $form.Cursor = [System.Windows.Forms.Cursors]::Default
    }
})

[void]$form.ShowDialog()

