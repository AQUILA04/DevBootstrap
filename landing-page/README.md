# StackPilot landing page

Site marketing statique pour **StackPilot** (`stackpilot.optimizesolux.com`).

## Preview local

```powershell
cd landing-page
npx --yes serve .
```

Ouvre l'URL affichee (souvent `http://localhost:3000`).

## Deploy Contabo (Traefik)

Workflow: `.github/workflows/deploy-landing-page.yml`

Secrets repo requis: `VPS_HOST`, `VPS_USER`, `SSH_PRIVATE_KEY`  
Optionnels: `VPS_LANDING_PATH`, `LANDING_HOST`, `LANDING_COMPOSE_PROJECT`

DNS Cloudflare (DNS only / grey cloud):

| Type | Name | Content |
|------|------|---------|
| A | `stackpilot` | IP Contabo |

## Deploy GitHub Pages

Workflow: `.github/workflows/pages.yml`  
Settings → Pages → Source: GitHub Actions.

URL typique: `https://aquila04.github.io/DevBootstrap/`
