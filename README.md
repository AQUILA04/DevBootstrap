# StackPilot

Application Windows pour installer rapidement ton pack d'outils de developpement sur un nouveau poste.
Double-clic, UAC, checklist — sans dependre de l'execution des scripts PowerShell.

**Canal principal (prevu) : Microsoft Store.**  
**Secours : MSI signe GitHub Releases.** La fiche Store n'est pas encore publiee ; le lien ci-dessous est un **placeholder** (recherche) jusqu'a attribution du Product ID Partner Center.

## Prerequisites

- Windows 10/11 avec [winget](https://learn.microsoft.com/windows/package-manager/winget/) (`App Installer`)
- Connexion Internet
- Compte administrateur (UAC) pour WSL, Docker Desktop, etc.

## Installation

### 1. Microsoft Store (recommandee)

Placeholder (pas encore de Product ID) :

https://apps.microsoft.com/search?query=StackPilot%20OptimizeSolux

Quand la fiche sera live, ce lien sera remplace par `https://apps.microsoft.com/detail/<PRODUCT_ID>` dans `Directory.Build.props` / `branding.ps1` / `version.json` (voir [docs/microsoft-store.md](docs/microsoft-store.md)).

### 2. MSI signe GitHub (secours)

1. Telecharge le MSI versionne :  
   https://github.com/AQUILA04/DevBootstrap/releases/latest/download/StackPilot-1.2.4-x64.msi  
   (ou la version courante publiee sur la release `vX.Y.Z`)
2. Installe (UI) ou en silencieux :  
   `msiexec /i StackPilot-1.2.4-x64.msi /qn /norestart`
3. Lance **StackPilot** depuis le menu Demarrer (UAC Windows)
4. Choisis un **profil**, ajuste la checklist, puis **Installer**

ZIP de secours (meme build) : `StackPilot-1.2.4-x64.zip` + `StackPilot-1.2.4-SHA256SUMS.txt`.

## Profils (catalogue v2)

Le fichier `catalog.json` (`version: 2`) definit des profils metier : chaque profil coche un pack d'outils standards.

| Profil | Cle CLI | Contenu typique |
|---|---|---|
| Base | `base` | WSL, Git, Chrome, VS Code, Cursor, Terminal, GitHub CLI, PowerToys, PowerShell 7, IDEs AI |
| Frontend | `frontend` | Base + Node, fnm, Firefox, Arc, Postman, Figma, WebStorm |
| Backend | `backend` | Base + JDK 21, IntelliJ, Docker, PostgreSQL, pgAdmin, DBeaver, SQL Developer, Postman, Python, Maven |
| Fullstack | `fullstack` | Union Frontend + Backend |
| DevOps | `devops` | Base + Docker, kubectl, Helm, Terraform, AWS/Azure CLI, MobaXterm, Oh My Posh, Python |
| Mobile | `mobile` | Base + JDK 17, Android Studio, Platform-Tools, Flutter SDK, Dart, Node/fnm, Watchman, scrcpy, Firebase CLI, Postman, Figma, Docker |
| UX/UI Web Designer | `ux-ui-web-designer` | Base + Figma, Inkscape, GIMP, Blender, ShareX, Notion, navigateurs |

PostgreSQL 17 (`postgresql`) et Oracle SQL Developer (`sqldeveloper`) sont inclus dans Backend / Fullstack.

Le profil **Mobile** installe Flutter via telechargement officiel (pas winget) : `flutter doctor` apres reboot/nouveau terminal.

## Build (developpeurs)

Necessite [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) et, pour le MSI, [WiX Toolset 5](https://wixtoolset.org/) (via le SDK NuGet `WixToolset.Sdk/5.0.2`, build sur Windows).

```bat
build.cmd
```

ou :

```powershell
.\build.ps1
.\build.ps1 -Phase PublishExe
.\build.ps1 -Phase BuildMsi
.\scripts\validate-packaging.ps1
```

Metadonnees centralisees : `Directory.Build.props`, `branding.ps1`, `version.json` (garder les trois synchronises).

Resultat typique :

- `dist\StackPilot\StackPilot.exe` (+ `catalog.json`)
- `dist\msi\StackPilot-1.2.4-x64.msi`
- `dist\StackPilot-1.2.4-x64.zip`
- `dist\StackPilot-1.2.4-SHA256SUMS.txt`

Signature :

- **Release tags** : Azure Artifact Signing (`azure/trusted-signing-action`) signe l'EXE puis le MSI
- **Optionnel local** : secrets `CODE_SIGN_PFX_BASE64` + `CODE_SIGN_PASSWORD` (compatible `build.ps1`)

Publication Store : [docs/microsoft-store.md](docs/microsoft-store.md)  
Confidentialite : [https://stackpilot.optimizesolux.com/privacy.html](https://stackpilot.optimizesolux.com/privacy.html)

## CLI optionnelle (machines non verrouillees)

Les scripts `bootstrap.ps1` / `gui.ps1` restent disponibles pour le developpement, mais **ne sont plus** le chemin principal ni inclus dans le ZIP utilisateur.

```powershell
.\bootstrap.ps1 -List
.\bootstrap.ps1 -Defaults
.\bootstrap.ps1 -Profile frontend
.\bootstrap.ps1 -Profile mobile
.\bootstrap.ps1 -Profile devops -WhatIf
```

## CI / CD (GitHub Actions)

Repo: `https://github.com/AQUILA04/DevBootstrap`

Sur push / PR :

1. Build .NET 8 + MSI WiX sur `windows-latest` (non signe sauf PFX optionnel)
2. Smoke test install / uninstall MSI silencieux
3. Artefacts EXE + MSI + ZIP + SHA256

Sur tag `v*` **exactement egal** a `v` + `ProductVersion` (ex. `v1.2.4`) :

1. Azure Artifact Signing de l'EXE
2. Packaging MSI contenant l'EXE signe
3. Signature Authenticode du MSI + verification editeur
4. Release GitHub immuable avec MSI / ZIP / SHA256SUMS

```bat
git tag v1.2.4
git push origin v1.2.4
```

## Site web (landing)

Dossier `landing-page/` — https://stackpilot.optimizesolux.com

| Cible | Workflow |
|---|---|
| Contabo + Traefik | `deploy-landing-page.yml` |
| GitHub Pages | `pages.yml` |

## Ajouter un outil

Edite `catalog.json` (voir [CONTRIBUTING.md](CONTRIBUTING.md)).

```powershell
winget search "NomDuLogiciel"
```
