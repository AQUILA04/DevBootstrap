# StackPilot — Contribution guide

Thanks for helping grow the default catalog.

## Add a tool (Windows / winget)

1. Fork [AQUILA04/DevBootstrap](https://github.com/AQUILA04/DevBootstrap)
2. Find the winget ID:

```powershell
winget search "ToolName"
```

3. Append an entry to `catalog.json`:

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

4. Open a Pull Request with:
   - why the tool belongs in a shared dev pack
   - the exact `winget` ID you verified

## Rules of thumb

- Prefer **winget** packages (auto-updated versions)
- Set `"default": false` unless it is broadly useful
- Mark `"requiresAdmin": true` when elevation is required
- Keep notes short and ASCII-friendly

## Local check

```powershell
.\bootstrap.ps1 -List
.\bootstrap.ps1 -WhatIf -Keys my-tool
```

## Landing / product

- Product: **StackPilot**
- Site: https://stackpilot.optimizesolux.com
