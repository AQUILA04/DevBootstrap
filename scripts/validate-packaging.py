#!/usr/bin/env python3
"""Cross-platform packaging validation (Linux cloud agents + local)."""
from __future__ import annotations

import json
import re
import sys
import xml.etree.ElementTree as ET
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
failures: list[str] = []


def ok(msg: str) -> None:
    print(f"[ok] {msg}")


def bad(msg: str) -> None:
    print(f"[FAIL] {msg}")
    failures.append(msg)


def text(el: ET.Element | None) -> str:
    return (el.text or "").strip() if el is not None else ""


def branding_value(branding: str, key: str) -> str | None:
    m = re.search(rf'\$script:{re.escape(key)}\s*=\s*"([^"]*)"', branding)
    return m.group(1) if m else None


def main() -> int:
    props = ET.parse(ROOT / "Directory.Build.props").getroot()
    pg = props.find("PropertyGroup")
    assert pg is not None
    version = text(pg.find("ProductVersion"))
    name = text(pg.find("ProductName"))
    company = text(pg.find("Company"))
    upgrade = text(pg.find("MsiUpgradeCode"))
    store = text(pg.find("MicrosoftStoreUrl"))
    placeholder = text(pg.find("MicrosoftStoreUrlIsPlaceholder"))

    branding = (ROOT / "branding.ps1").read_text(encoding="utf-8")
    expected = {
        "ProductVersion": version,
        "ProductName": name,
        "CompanyName": company,
        "MsiUpgradeCode": upgrade,
    }
    for key, exp in expected.items():
        got = branding_value(branding, key)
        if got != exp:
            bad(f"branding.ps1 {key}: got {got!r} expected {exp!r}")
        else:
            ok(f"branding {key}={exp}")

    vj = json.loads((ROOT / "version.json").read_text(encoding="utf-8"))
    if vj.get("version") != version:
        bad("version.json out of sync")
    else:
        ok(f"version.json {version}")
    if bool(vj.get("microsoftStoreUrlIsPlaceholder")) != (placeholder == "true"):
        bad("Store placeholder flag mismatch")
    else:
        ok("Store placeholder flags aligned")

    manifest = (ROOT / "src/StackPilot/app.manifest").read_text(encoding="utf-8")
    if f'version="{version}.0"' not in manifest:
        bad("app.manifest version mismatch")
    else:
        ok("app.manifest version")
    if "requireAdministrator" not in manifest:
        bad("requireAdministrator missing")
    else:
        ok("requireAdministrator")

    csproj = (ROOT / "src/StackPilot/StackPilot.csproj").read_text(encoding="utf-8")
    if "StackPilot.ico" not in csproj:
        bad("csproj missing ApplicationIcon")
    else:
        ok("ApplicationIcon wired")

    required = [
        "assets/icons/StackPilot.svg",
        "assets/icons/StackPilot.ico",
        "assets/icons/StackPilot.png",
        "assets/icons/StoreLogo.png",
        "src/StackPilot.Installer/StackPilot.Installer.wixproj",
        "src/StackPilot.Installer/Package.wxs",
        "src/StackPilot.Installer/License.rtf",
        "docs/microsoft-store.md",
        "landing-page/privacy.html",
        "build.ps1",
        "build.cmd",
        ".github/workflows/build.yml",
    ]
    for rel in required:
        if not (ROOT / rel).exists():
            bad(f"missing {rel}")
        else:
            ok(rel)

    wixproj = (ROOT / "src/StackPilot.Installer/StackPilot.Installer.wixproj").read_text(encoding="utf-8")
    if "WixToolset.Sdk/5.0.2" not in wixproj:
        bad("WiX not pinned to 5.0.2")
    else:
        ok("WiX pinned 5.0.2")

    wxs = (ROOT / "src/StackPilot.Installer/Package.wxs").read_text(encoding="utf-8")
    for needle in [
        'Scope="perMachine"',
        "MajorUpgrade",
        "ProgramFiles64Folder",
        "StartMenuShortcut",
        "ARPPRODUCTICON",
        "$(var.MsiUpgradeCode)",
        "!(bindpath.PublishDir)StackPilot.exe",
        "!(bindpath.PublishDir)catalog.json",
    ]:
        if needle not in wxs:
            bad(f"Package.wxs missing {needle}")
        else:
            ok(f"Package.wxs has {needle}")

    workflow = (ROOT / ".github/workflows/build.yml").read_text(encoding="utf-8")
    for needle in [
        "trusted-signing-action",
        "Publish unsigned EXE",
        "Sign EXE",
        "Build MSI",
        "Sign MSI",
        "Silent MSI install",
        "must exactly match product version",
        "AZURE_TRUSTED_SIGNING",
    ]:
        if needle not in workflow:
            bad(f"workflow missing {needle}")
        else:
            ok(f"workflow has {needle}")

    readme = (ROOT / "README.md").read_text(encoding="utf-8")
    if "Microsoft Store" not in readme or "placeholder" not in readme.lower():
        bad("README missing Store-primary + placeholder messaging")
    else:
        ok("README Store-primary messaging")

    if placeholder != "true":
        print("[warn] Store URL placeholder is false")
    else:
        ok(f"Store URL placeholder: {store}")

    if not re.search(r"(?m)^jobs:\s*$", workflow):
        bad("workflow missing jobs:")
    else:
        ok("workflow YAML has jobs")

    # Ensure landing privacy + store CTAs exist
    index = (ROOT / "landing-page/index.html").read_text(encoding="utf-8")
    if "data-store-cta" not in index:
        bad("landing missing data-store-cta")
    else:
        ok("landing Store CTAs")
    if "privacy.html" not in (ROOT / "landing-page/index.html").read_text(encoding="utf-8"):
        bad("landing footer missing privacy link")
    else:
        ok("landing privacy link")

    if failures:
        print(f"\n{len(failures)} failure(s)")
        for f in failures:
            print(f" - {f}")
        return 1
    print("\nPackaging validation passed.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
