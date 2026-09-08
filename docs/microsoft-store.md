# Microsoft Store publication — StackPilot

This document covers packaging StackPilot as a signed Win32 MSI for Microsoft Store (Partner Center) submission, Azure Artifact Signing, and certification notes.

StackPilot remains a classic Win32 app (`requireAdministrator`) that drives winget / WSL / DISM. That behavior is intentional and must stay documented for Store certification.

## Current status (honest)

| Item | Status |
|---|---|
| Centralized version / branding | Done in-repo (`Directory.Build.props`, `branding.ps1`, `version.json`) |
| WiX MSI project (pinned 5.0.2) | Done (`src/StackPilot.Installer`) |
| CI unsigned PR builds + tests | Done (`.github/workflows/build.yml`) |
| Release tag = product version | Enforced (`v1.2.4` must match `ProductVersion`) |
| Azure Artifact Signing wiring | Done in workflow (needs tenant secrets) |
| Partner Center company account | **Manual blocker** — not performed by automation |
| Identity / tax / payout validation | **Manual blocker** |
| Store Product ID + live listing URL | **Manual blocker** — placeholder search URL in use |
| Final Store submission | **Manual blocker** |

Do **not** claim the Microsoft Store listing is live until Partner Center publishes it.

## Store URL placeholder

Until Partner Center assigns a Product ID, public links use a search fallback:

`https://apps.microsoft.com/search?query=StackPilot%20OptimizeSolux`

Replace in **all** of these when the Product ID exists:

1. `Directory.Build.props` → `MicrosoftStoreUrl` = `https://apps.microsoft.com/detail/<PRODUCT_ID>`
2. Set `MicrosoftStoreUrlIsPlaceholder` to `false`
3. `branding.ps1` and `version.json` (same values)
4. README, `StackPilot.cmd`, landing page CTAs

## MSI parameters (Partner Center / Win32)

| Parameter | Value |
|---|---|
| Installer | `StackPilot-<version>-x64.msi` |
| Architecture | x64 |
| Install scope | Per-machine (`Program Files\OptimizeSolux\StackPilot`) |
| Silent install | `msiexec /i StackPilot-<version>-x64.msi /qn /norestart` |
| Silent uninstall | `msiexec /x StackPilot-<version>-x64.msi /qn /norestart` |
| Upgrade | Major upgrade via stable UpgradeCode `6DA385A7-4697-43B9-AF87-481959D0AD86` |
| Payload | Self-contained `StackPilot.exe` + `catalog.json` |
| Shortcut | Start Menu → StackPilot |
| ARP icon | `StackPilot.ico` |

Expected install path after silent install:

`%ProgramFiles%\OptimizeSolux\StackPilot\StackPilot.exe`

## Azure Artifact Signing (roles & secrets)

Release tags sign **EXE first**, build MSI from the signed EXE, then sign the **MSI**.

### Azure resources

1. Artifact Signing (Trusted Signing) account + public trust certificate profile
2. App registration with federated credential for GitHub OIDC (`repo:AQUILA04/DevBootstrap:ref:refs/tags/v*`) **or** client secret
3. RBAC: grant the app **Artifact Signing Certificate Profile Signer** on the account/profile

### GitHub secrets

| Secret | Purpose |
|---|---|
| `AZURE_CLIENT_ID` | App registration client ID |
| `AZURE_TENANT_ID` | Entra tenant ID |
| `AZURE_SUBSCRIPTION_ID` | Azure subscription ID (OIDC `azure/login`) |
| `AZURE_CLIENT_SECRET` | App client secret (secret-based signing; optional if OIDC alone is configured) |
| `AZURE_TRUSTED_SIGNING_ENDPOINT` | e.g. `https://eus.codesigning.azure.net/` |
| `AZURE_TRUSTED_SIGNING_ACCOUNT` | Signing account name |
| `AZURE_TRUSTED_SIGNING_CERTIFICATE_PROFILE` | Certificate profile name |

Optional non-release / local fallback (compatible with `build.ps1`):

| Secret | Purpose |
|---|---|
| `CODE_SIGN_PFX_BASE64` | Base64 PFX (local/CI optional) |
| `CODE_SIGN_PASSWORD` | PFX password |

Prefer Azure Artifact Signing for Store-bound releases. Keep PFX only as a compatible local escape hatch.

Workflow action: `azure/trusted-signing-action@v2`.

## Partner Center checklist (manual)

1. Create / use OptimizeSolux company account in Partner Center
2. Complete company identity verification, tax, and payout profiles
3. Create a **Win32 / classic desktop** product for StackPilot
4. Upload Store logos (`assets/icons/StoreLogo.png`, generate additional sizes as required)
5. Set age rating, categories, listing text (FR/EN as needed)
6. Privacy policy URL: `https://stackpilot.optimizesolux.com/privacy.html`
7. Support contact: `contact.stackpilot@optimizesolux.com`
8. Submit the signed MSI with the silent install/uninstall switches above
9. After publication, replace the Store URL placeholder everywhere

## Certification notes (UAC, winget, WSL, Docker, third-party)

Be explicit in the Store listing and certification Q&A:

### UAC / administrator

- `StackPilot.exe` embeds `requireAdministrator` in `app.manifest`.
- Launching the app shows a standard Windows UAC consent prompt.
- Elevation is required to enable Windows features and install system-level developer tools.
- The MSI itself is per-machine and typically elevates during install/uninstall.

### winget

- StackPilot orchestrates installs primarily through **winget** (`App Installer`).
- winget must be available on the target machine (Windows 10/11).
- Package IDs live in `catalog.json` and resolve to whatever version winget serves at install time.

### WSL / DISM

- The **WSL** package path may enable Windows features (`Microsoft-Windows-Subsystem-Linux`, Virtual Machine Platform) via DISM / `wsl --install` flows.
- Feature enablement can require a **reboot**. Document this in the listing.

### Docker Desktop

- Docker Desktop is a third-party optional package in profiles.
- It may require WSL2, reboot, and accepting Docker’s own EULA at first launch.
- StackPilot does not redistribute Docker binaries; it triggers installation through winget when selected.

### Third-party software

- StackPilot is a bootstrapper/orchestrator, not a redistributor of JetBrains, Google, Oracle, etc. binaries (except where a package uses a documented direct download such as Flutter).
- Each selected tool remains subject to its own license, telemetry, and terms.
- Users choose profiles/packages before install; nothing outside the selection should be installed by StackPilot itself.

### What Store certification should expect

- After MSI install, launching StackPilot prompts UAC, then shows the GUI checklist.
- No always-on background service is installed by the MSI.
- Clean silent uninstall removes Program Files payload and Start Menu shortcut.

## Build & release commands

```bat
build.cmd
build.cmd -Phase PublishExe -SkipMsi
build.cmd -Phase BuildMsi
```

```powershell
.\build.ps1
.\scripts\validate-packaging.ps1
```

Release:

```bat
git tag v1.2.4
git push origin v1.2.4
```

Immutable GitHub assets (fallback channel):

- `StackPilot-1.2.4-x64.msi`
- `StackPilot-1.2.4-x64.zip`
- `StackPilot-1.2.4-SHA256SUMS.txt`

## Remaining manual blockers (do not automate here)

1. Legal Partner Center account creation / company verification
2. Tax and payout configuration
3. Azure Artifact Signing account provisioning + GitHub secret values in the repo
4. Assignment of Microsoft Store Product ID and replacement of placeholder URLs
5. Human submission / certification responses in Partner Center
6. Final Store publication approval by Microsoft
