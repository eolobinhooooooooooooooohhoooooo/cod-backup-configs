# Asset Definitions

Normative spec for every visual asset Cod Backup Configs consumes. A new
agent or person can create or replace any asset using only this folder and
the codebase — no prior context needed.

Read order: this file (pipeline) → `ASSETS.md` (what to make, per slot) →
`TOKENS.md` (visual style constraints).

## Where things live

| Path | Role |
|---|---|
| `assets/*.jpg`, `assets/*.png` | images that get embedded (covers, flags) |
| `assets/kit/` | curated source kit: official platform marks, COD lockups, button icons, app-icon masters, Hitmarker fonts |
| `assets/SOURCES.md` | provenance ledger, one line per file. **Must be updated whenever a file changes.** |
| `assets/spec/` | this folder — the definitions |
| `src/CodBackupConfig/CodBackupConfig.csproj` | `<EmbeddedResource>` list — single source of truth for what actually ships in the exe |
| `assets/kit/icon.ico` | shipped application icon (`<ApplicationIcon>`), referenced directly by the csproj — single canonical file |

Runtime rule: the app never reads assets from disk. Everything ships as an
embedded resource and loads through `Assets.Img("name")` (resource
`img/<name>`) or, for fonts, as embedded bytes under `font/<name>`.

## Pipeline for creating / replacing an asset

1. Find the slot in `ASSETS.md`. Note its status, exact file name and path,
   source dimensions and acceptance criteria.
2. Produce the file with the **exact name at the exact path** — code loads by
   resource name, so never rename a slot.
3. Validate against the acceptance criteria. The decisive test is
   legibility at the stated display size inside the running app, not in
   isolation.
4. Add or replace the line for the file in `assets/SOURCES.md` (source URL or
   "handmade", plus license/trademark note).
5. Only if it is a **new** file name: add an `<EmbeddedResource>` entry to the
   csproj following the existing pattern (`LogicalName="img/<name>"` for
   images, `font/<name>` for `.ttf`).
6. Rebuild and eyeball both languages:
   ```
   dotnet publish src\CodBackupConfig\CodBackupConfig.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o dist
   ```
   Then run `dist\Cod Backup Configs.exe` and check EN + PT.

## Hard rules

- The runtime never reads assets from disk. Don't introduce loose-file loads.
- Determinism where files are generated: fixed seeds, no wall-clock input.
  Same input → same bytes.
- Size budgets: covers ≤150 KB each, marks/icons ≤30 KB each, flags ≤10 KB
  each, app icon ≤30 KB. The published exe must stay near ~139 MB (the .NET
  runtime dominates; the image payload stays in the low-MB range).
- Dark-first: an asset only counts if it looks right on `#0A0B0C` under the
  app's own painting (scrim, borders). Test in the app, never in isolation.
- Trademarks (Steam, Battle.net, Xbox, Activision game names/logos/art,
  Hitmarker) are used for identification only, in an unofficial utility that
  is not sold. Every proprietary file needs its SOURCES.md line.
- Prefer official or CC0 sources: brand portals, Wikimedia Commons,
  simpleicons.org, flagcdn.com. Record exact URLs.
- Transparent 32-bit RGBA PNG for all marks/icons/logos/flags; JPEG only for
  photographic covers.
- Style constraints live in `TOKENS.md`. When in doubt: flatter, darker,
  thinner.

## Suggested task split for an asset-producing agent

- `ASSETS.md` sections 1-3 — brand imagery (covers, platform marks, game lockups)
- Section 4 — button icon set (must be one coherent set)
- Sections 5-6 — flags, app icon
- Optional upgrades: section 7 (extra Hitmarker weights), section 8 (grain tile retune)
