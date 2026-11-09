# Cod Backup Configs

Lightweight Windows tool to back up, move and lock local Call of Duty settings
between platforms and accounts. Unofficial. Not affiliated with Activision.

| Black Ops 6 | Black Ops 7 | Modern Warfare 4 |
|---|---|---|
| `s.1.0.cod24.*` | `s.1.0.cod25.*` | `s.1.0.cod26.*` |

## What it does

- Finds the players folders for **Black Ops 6**, **Black Ops 7** and
  **Modern Warfare 4** automatically — Battle.net, Steam and Xbox PC accounts
  all live as numeric folders in the same trees, and the app tells you which
  id looks like which platform (label only, never required).
- **BACKUP ACCOUNT** — copies every file of the selected account into a dated
  snapshot under `%LOCALAPPDATA%\CodBackupConfigs\backups\...`.
- **RESTORE SNAPSHOT** — copies a snapshot into any account folder (creates it
  if it does not exist yet), so you can stage a config for a fresh account.
- **LOCK SETTINGS** — marks the graphic-settings files (`s.1.0.*`) read-only so
  the game cannot reset them, and unlocks them again in one click.
- Copy only. Nothing is ever moved or deleted, and nothing leaves your machine.

## Requirements

Windows 10/11 x64. That is all — the exe is self-contained, no runtime install.

## Usage

1. Pick a game card (auto-detection runs; if it fails, use *choose folder…* on
   the card).
2. Select an account in the list.
3. `BACKUP ACCOUNT` to snapshot it, `RESTORE SNAPSHOT` to apply a snapshot to
   the target account id, `LOCK SETTINGS` to stop the game resetting the files.

Hover any button for a description of what it does. The flags in the top-right
switch the interface between English and Portuguese.

## Building

Needs the [.NET 8 SDK](https://dotnet.microsoft.com/download).

```
dotnet publish src\CodBackupConfig\CodBackupConfig.csproj -c Release -r win-x64 ^
  --self-contained true -p:PublishSingleFile=true ^
  -p:IncludeNativeLibrariesForSelfExtract=true -o dist
```

Artifact: `dist\Cod Backup Configs.exe`.

## Where things go

- Snapshots: `%LOCALAPPDATA%\CodBackupConfigs\backups\<game>\<account>\<date>\`
- Preferences: `%LOCALAPPDATA%\CodBackupConfigs\prefs.json`
- Games are never modified except for the read-only flag on `s.1.0.*` files.

## Docs

- [docs/PLAN.md](docs/PLAN.md) — planning, detection strategy, safety rules, UI spec
- [docs/RESEARCH.md](docs/RESEARCH.md) — sourced research on paths, file families and id patterns

MIT licensed.
