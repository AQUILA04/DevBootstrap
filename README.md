# StackPilot

Bootstrapper Windows pour installer rapidement ton pack d'outils de developpement via **winget** — toujours la derniere version disponible.

## Prerequisites

- Windows 10/11 avec [winget](https://learn.microsoft.com/windows/package-manager/winget/) (`App Installer`)
- Connexion Internet
- PowerShell **en Administrateur** recommande (requis pour WSL et Docker Desktop)

## Quick start (simple - GUI)

Double-clic sur `StackPilot.cmd` (ou `DevBootstrap.cmd`), ou:

```powershell
cd ~\Projects\dev-bootstrap
Set-ExecutionPolicy -Scope Process Bypass
.\gui.ps1
```

Coche les outils, puis **Installer**.

## Quick start (CLI)

```powershell
cd ~\Projects\dev-bootstrap
Set-ExecutionPolicy -Scope Process Bypass
.\bootstrap.ps1
```

Menu interactif :

- `all` - tout installer
- `defaults` / `d` - selection par defaut (`*` dans le catalogue)
- `1,3,5-8` - selection multiple
- `q` - quitter

## Generer un EXE (pour partage / nouveau PC)

```powershell
.\build.ps1
```

Resultat :

- `dist\StackPilot\StackPilot.exe` - double-clic (UAC admin)
- `dist\StackPilot\bootstrap.ps1` + `catalog.json` + `branding.ps1` (a garder avec l'EXE)
- `dist\StackPilot.zip` - archive a copier sur une cle USB / OneDrive

### EXE vs MSI

| Format | Faisable | Verdict |
|---|---|---|
| **EXE** (ps2exe + GUI) | Oui | **Recommande** - double-clic, UAC, checklist |
| **MSI** (WiX / Advanced Installer) | Oui mais plus lourd | Utile en entreprise (GPO/Intune), pas necessaire pour usage perso |

L'EXE n'embarque pas les logiciels eux-memes : il orchestre **winget** pour telecharger la derniere version a l'installation.

## CI / CD (GitHub Actions)

Repo: `https://github.com/AQUILA04/DevBootstrap`

A chaque push (ou PR) qui touche le catalogue / scripts / workflow, Actions :

1. Build `StackPilot.exe` sur `windows-latest`
2. Publie les artefacts `StackPilot` et `StackPilot-zip`

Un tag `v*` (ex. `v1.0.0`) declenche aussi une **GitHub Release** avec le zip.

```powershell
git tag v1.0.0
git push origin v1.0.0
```

## Site web (landing)

Dossier `landing-page/` — presentation produit, telechargement, contribution.

Preview local:

```powershell
cd landing-page
npx --yes serve .
```

Deploiements:

| Cible | Workflow | URL |
|---|---|---|
| Contabo + Traefik | `deploy-landing-page.yml` | https://stackpilot.optimizesolux.com |
| GitHub Pages | `pages.yml` | https://aquila04.github.io/DevBootstrap/ |

Contabo: secrets `VPS_HOST`, `VPS_USER`, `SSH_PRIVATE_KEY` (+ DNS A `stackpilot` → IP VPS, grey cloud).  
Pages: Settings → Pages → Source **GitHub Actions**.

Ne deploie Contabo qu'apres review locale (voir `landing-page/README.md`).

## Commandes utiles

```powershell
# Voir le catalogue
.\bootstrap.ps1 -List

# Tout installer
.\bootstrap.ps1 -All

# Uniquement la selection par defaut
.\bootstrap.ps1 -Defaults

# Par cles
.\bootstrap.ps1 -Keys git,node,docker,cursor

# Par tags
.\bootstrap.ps1 -Tags java,ide

# Simulation (rien n'est installe)
.\bootstrap.ps1 -WhatIf -All
```

## Catalogue actuel

| Cle | Outil | ID winget |
|---|---|---|
| `wsl` | Windows Subsystem for Linux | `Microsoft.WSL` |
| `git` | Git | `Git.Git` |
| `chrome` | Google Chrome | `Google.Chrome` |
| `arc` | Arc Browser | `TheBrowserCompany.Arc` |
| `temurin17` | Temurin JDK 17 | `EclipseAdoptium.Temurin.17.JDK` |
| `temurin21` | Temurin JDK 21 | `EclipseAdoptium.Temurin.21.JDK` |
| `temurin25` | Temurin JDK 25 | `EclipseAdoptium.Temurin.25.JDK` |
| `intellij-ultimate` | IntelliJ IDEA Ultimate | `JetBrains.IntelliJIDEA.Ultimate` |
| `intellij-community` | IntelliJ IDEA Community | `JetBrains.IntelliJIDEA.Community` |
| `docker` | Docker Desktop | `Docker.DockerDesktop` |
| `cursor` | Cursor | `Anysphere.Cursor` |
| `antigravity-ide` | Antigravity IDE | `Google.AntigravityIDE` |
| `antigravity` | Antigravity Agent | `Google.Antigravity` |
| `kiro` | Kiro IDE | `Amazon.Kiro` |
| `dbeaver` | DBeaver Community | `DBeaver.DBeaver.Community` |
| `pgadmin` | pgAdmin 4 | `PostgreSQL.pgAdmin` |
| `postman` | Postman | `Postman.Postman` |
| `node` | Node.js LTS | `OpenJS.NodeJS.LTS` |
| `python` | Python 3.13 | `Python.Python.3.13` |
| `vscode` | VS Code | `Microsoft.VisualStudioCode` |
| `mobaxterm` | MobaXterm | `Mobatek.MobaXterm` |

## Notes importantes

- **WSL** est detecte nativement (`wsl --status` / composants Windows). S'il manque, le script active `Microsoft-Windows-Subsystem-Linux` + `VirtualMachinePlatform`, installe `Microsoft.WSL`, puis force WSL2. Un **redemarrage** peut etre necessaire avant Docker Desktop.
- **IntelliJ Ultimate** est dans la selection par defaut (licence requise). Community est disponible mais desactivee par defaut.
- **Antigravity IDE** est installe avant **Antigravity Agent**. Les deux produits ont deja eu des conflits de repertoire d'installation ; si un seul des deux doit tourner, desactive l'autre dans le menu.
- Un rapport JSON `install-report-*.json` est ecrit apres chaque run.

## Ajouter un outil

Edite `catalog.json` :

```json
{
  "key": "mon-outil",
  "name": "Mon Outil",
  "id": "Publisher.Package",
  "source": "winget",
  "tags": ["custom"],
  "default": true
}
```

Trouve l'ID avec :

```powershell
winget search "NomDuLogiciel"
```

## Prochaines evolutions possibles

- Profils (`frontend`, `backend`, `fullstack`)
- Post-hooks (extensions Cursor/VS Code, config Git)
- Fallback Scoop pour machines sans droits admin
- Package MSI (WiX) pour deploiement Intune / GPO
