# StackPilot

Application Windows pour installer rapidement ton pack d'outils de developpement sur un nouveau poste.
Double-clic, UAC, checklist — sans dependre de l'execution des scripts PowerShell.

## Prerequisites

- Windows 10/11 avec [winget](https://learn.microsoft.com/windows/package-manager/winget/) (`App Installer`)
- Connexion Internet
- Compte administrateur (UAC) pour WSL, Docker Desktop, etc.

## Utilisation (recommandee)

1. Telecharge le dernier ZIP :  
   https://github.com/AQUILA04/DevBootstrap/releases/latest/download/StackPilot.zip
2. Extraits le dossier (garde `StackPilot.exe` et `catalog.json` ensemble)
3. Double-clic sur `StackPilot.exe` (UAC Windows)
4. Choisis un **profil** (Base, Frontend, Backend, Fullstack, DevOps, Mobile, UX/UI Web Designer), ajuste la checklist, puis **Installer**

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

Necessite [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).

```bat
build.cmd
```

ou :

```powershell
.\build.ps1
```

Resultat :

- `dist\StackPilot\StackPilot.exe` (appli WinForms native, self-contained)
- `dist\StackPilot\catalog.json`
- `dist\StackPilot\LIRE-MOI.txt`
- `dist\StackPilot.zip`

Signature Authenticode optionnelle en CI si secrets `CODE_SIGN_PFX_BASE64` + `CODE_SIGN_PASSWORD`.

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

Sur push / PR / tag `v*` :

1. Build natif .NET 8 sur `windows-latest`
2. Artefacts `StackPilot` + `StackPilot-zip`
3. Tag `v*` → GitHub Release avec le zip

```bat
git tag v1.2.1
git push origin v1.2.1
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
