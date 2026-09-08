# Publication Microsoft Store

StackPilot est publié comme application Win32 avec un MSI x64. Ce choix conserve
l'élévation UAC nécessaire à winget, WSL et DISM. Le Microsoft Store ne signe pas
les soumissions MSI : `StackPilot.exe` et le MSI doivent donc être signés avant
chaque soumission.

## 1. Compte société et signature

1. Créer un compte **Company** sur
   [Partner Center](https://partner.microsoft.com/dashboard/registration).
2. Faire valider le nom légal de l'organisation et réserver le nom `StackPilot`.
3. Créer un compte Azure Artifact Signing et un profil de certificat `Public
   Trust` au nom validé d'OptimizeSolux.
4. Donner à l'identité GitHub Actions le rôle **Artifact Signing Certificate
   Profile Signer** sur le profil.
5. Ajouter les secrets GitHub suivants :

   - `AZURE_TENANT_ID`
   - `AZURE_CLIENT_ID`
   - `AZURE_CLIENT_SECRET`
   - `AZURE_ARTIFACT_SIGNING_ENDPOINT`
   - `AZURE_CODE_SIGNING_NAME`
   - `AZURE_CERT_PROFILE_NAME`
   - `EXPECTED_PUBLISHER` : fragment du Subject du certificat, par exemple le
     nom légal validé de la société

La clé privée reste dans le service de signature. Les anciens secrets PFX sont
encore acceptés uniquement pour les builds locaux.

## 2. Créer une release candidate

La version de référence se trouve dans `Directory.Build.props`.

```powershell
git tag v1.1.0
git push origin v1.1.0
```

Le tag doit correspondre exactement à `StackPilotVersion`. La CI :

1. publie et signe `StackPilot.exe` ;
2. construit le MSI avec l'EXE signé ;
3. signe le MSI ;
4. vérifie les deux signatures ;
5. teste l'installation et la désinstallation silencieuses ;
6. publie une URL immuable de la forme :
   `https://github.com/AQUILA04/DevBootstrap/releases/download/v1.1.0/StackPilot-1.1.0-x64.msi`.

## 3. Données de la première soumission

- Type d'application : `MSI`
- Architecture : `x64`
- Langue : `Français (France)`
- Mode : application gratuite
- Catégorie suggérée : `Developer tools`
- Paramètres d'installation silencieuse : `/qn /norestart`
- Paramètres de désinstallation silencieuse : `/qn /norestart`
- Systèmes : Windows 10 et Windows 11, PC uniquement
- Prérequis : compte administrateur, connexion Internet et App Installer/winget
- URL de support : `https://stackpilot.optimizesolux.com/`
- URL de confidentialité :
  `https://stackpilot.optimizesolux.com/privacy.html`

### Notes de certification

> StackPilot installe uniquement les outils explicitement sélectionnés par
> l'utilisateur. Il appelle Windows Package Manager (winget) et affiche les
> résultats dans son interface. L'élévation UAC est requise au lancement pour
> les installations machine et, si l'utilisateur sélectionne WSL, pour activer
> Microsoft-Windows-Subsystem-Linux et VirtualMachinePlatform via DISM. Aucun
> service ni pilote StackPilot n'est installé. Certains outils tiers choisis
> par l'utilisateur, notamment Docker Desktop, peuvent installer leurs propres
> services ou pilotes. Une connexion Internet est nécessaire.

## 4. Fiche Store

Nom court :

> StackPilot

Description courte :

> Préparez un poste de développement Windows en sélectionnant vos outils, puis
> laissez StackPilot les installer avec winget.

Fonctionnalités :

- catalogue versionné d'outils de développement ;
- installation d'une sélection ou d'un pack complet ;
- orchestration de WSL avant Docker Desktop ;
- journal d'installation visible ;
- versions récentes fournies par winget.

Les captures doivent montrer l'écran principal, la sélection des outils et le
journal d'installation. Ne pas masquer la mention indiquant que l'UAC et un
redémarrage peuvent être nécessaires.

## 5. Vérifications avant soumission

- installer depuis une VM Windows 10 propre ;
- installer depuis une VM Windows 11 propre ;
- vérifier que l'UAC affiche l'éditeur signé, et non « Éditeur inconnu » ;
- vérifier Git, un IDE, WSL et le scénario WSL + redémarrage + Docker ;
- tester une mise à niveau depuis la version Store précédente ;
- désinstaller StackPilot et vérifier la suppression du raccourci et du dossier ;
- ne jamais remplacer le binaire derrière une URL de release déjà soumise.

La validation du compte société, la réservation du nom, le Product ID et le
bouton final **Submit for certification** restent des opérations Partner Center
effectuées par un représentant autorisé d'OptimizeSolux.
