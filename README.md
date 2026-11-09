# Cod Backup Configs

One small Windows program that saves your Call of Duty settings and moves them
between accounts. Works with **Black Ops 6**, **Black Ops 7** and **Modern Warfare 4**
on **Battle.net**, **Steam** and **Xbox PC**.

It is a single `.exe`. Nothing to install, nothing to configure. It copies files —
it never deletes anything, and nothing leaves your computer.

## How to use it

1. Run `Cod Backup Configs.exe` and click the game card (it finds your folders by itself).
2. Click your account in the list.
3. Use the buttons:

   - **BACKUP ACCOUNT** — saves your account's settings into a dated snapshot.
   - **RESTORE SNAPSHOT** — copies a saved snapshot into any account (for example,
     when you start on a different platform).
   - **LOCK SETTINGS** — makes the graphics settings read-only so the game
     cannot reset them. Click again to unlock.

Hover over any button to read what it does. The flags at the top right switch
the interface between English and Portuguese.

## Where things are stored

- Snapshots: `%LOCALAPPDATA%\CodBackupConfigs\backups\…`
- Preferences: `%LOCALAPPDATA%\CodBackupConfigs\prefs.json`

Both places are inside your user profile only.

## Requirements

Windows 10 or 11, 64-bit. That's all.

## Versions

Releases are tagged (`v1.0.0`, …) and listed in [CHANGELOG.md](CHANGELOG.md).

## Building from source

Requires the [.NET 8 SDK](https://dotnet.microsoft.com/download):

```
dotnet publish src\CodBackupConfig\CodBackupConfig.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o dist
```

Result: `dist\Cod Backup Configs.exe`.

## Docs

- [docs/PLAN.md](docs/PLAN.md) — how it works, detection strategy, UI spec
- [docs/RESEARCH.md](docs/RESEARCH.md) — where the paths and file names come from
- [assets/spec/](assets/spec/) — asset definitions for anyone adding or replacing art

Not affiliated with Activision, Valve or Microsoft. Game names and logos belong
to their owners. MIT licensed (see [LICENSE](LICENSE)).
