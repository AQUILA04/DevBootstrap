# StackPilot

Application Windows pour installer rapidement ton pack d'outils de developpement sur un nouveau poste.
Double-clic, UAC, checklist — sans dependre de l'execution des scripts PowerShell.

## Prerequisites

- Windows 10/11 avec [winget](https://learn.microsoft.com/windows/package-manager/winget/) (`App Installer`)
- Connexion Internet
- Compte administrateur (UAC) pour WSL, Docker Desktop, etc.

## Utilisation

### Microsoft Store (recommande)

1. Recherche **StackPilot** dans le
   [Microsoft Store](https://apps.microsoft.com/search?query=StackPilot)
2. Installe puis lance StackPilot
3. Accepte l'UAC Windows
4. Coche les outils, puis **Installer**

Le Store est le canal principal : l'application y est distribuee par une source
verifiee. Un MSI x64 signe est egalement disponible dans les
[releases GitHub](https://github.com/AQUILA04/DevBootstrap/releases).

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
- `dist\StackPilot-<version>-x64.msi`
- `dist\StackPilot.zip` (distribution portable secondaire)

La version est centralisee dans `Directory.Build.props`. Les releases taguees
sont signees via Azure Artifact Signing ; la CI refuse une release si l'EXE ou
le MSI n'a pas une signature Authenticode valide.

## CLI optionnelle (machines non verrouillees)

Les scripts `bootstrap.ps1` / `gui.ps1` restent disponibles pour le developpement, mais **ne sont plus** le chemin principal ni inclus dans le ZIP utilisateur.

```powershell
.\bootstrap.ps1 -List
.\bootstrap.ps1 -Defaults
```

## CI / CD (GitHub Actions)

Repo: `https://github.com/AQUILA04/DevBootstrap`

Sur push / PR / tag `v*` :

1. Build natif .NET 8 et MSI WiX sur `windows-latest`
2. Test silencieux d'installation/desinstallation
3. Tag `v*` → signature EXE + MSI et GitHub Release versionnee

```bat
git tag v1.1.0
git push origin v1.1.0
```

Le tag doit correspondre a `StackPilotVersion`. Voir
[`docs/microsoft-store-submission.md`](docs/microsoft-store-submission.md) pour
la configuration de la signature, Partner Center et la checklist de soumission.

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
