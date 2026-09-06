# StackPilot — Contribution guide

Thanks for helping grow the default catalog.

## Catalog version 2

`catalog.json` uses:

- `"version": 2`
- `"profiles"` — named packs of package keys
- `"packages"` — winget installable tools

StackPilot validates that every key listed under a profile exists in `packages`.

## Add a tool (Windows / winget)

1. Fork [AQUILA04/DevBootstrap](https://github.com/AQUILA04/DevBootstrap)
2. Find the winget ID:

```powershell
winget search "ToolName"
winget show -e --id Publisher.Package
```

3. Append an entry to `catalog.json` under `packages`:

```json
{
  "key": "my-tool",
  "name": "My Tool",
  "id": "Publisher.Package",
  "source": "winget",
  "tags": ["custom"],
  "default": false,
  "notes": "Optional hint for installers"
}
```

4. If the tool belongs to a role pack, add its `key` to the relevant entries in `profiles` (`frontend`, `backend`, `fullstack`, `devops`, `ux-ui-web-designer`, and/or `base`).
5. Keep `"default": true` only for tools that belong to the **Base** profile (they must stay in sync).
6. Open a Pull Request with:
   - why the tool belongs in a shared / role pack
   - the exact `winget` ID you verified

## Add or edit a profile

```json
{
  "key": "frontend",
  "name": "Frontend",
  "description": "Short ASCII description.",
  "packages": ["git", "node", "vscode"]
}
```

Rules:

- `packages` is an ordered list of existing package `key` values (duplicates across profiles are fine).
- `fullstack` should remain the union of frontend + backend keys.
- Prefer curated packs over dumping every possible winget package.

## Rules of thumb

- Prefer **winget** packages (auto-updated versions)
- Set `"default": false` unless it is in the Base profile
- Mark `"requiresAdmin": true` when elevation is required
- Keep notes short and ASCII-friendly
- Tag packages with role tags when useful (`frontend`, `backend`, `devops`, `ux`, …)

## Local check

```powershell
.\bootstrap.ps1 -List
.\bootstrap.ps1 -WhatIf -Profile frontend
.\bootstrap.ps1 -WhatIf -Keys my-tool
```

## Landing / product

- Product: **StackPilot**
- Site: https://stackpilot.optimizesolux.com
