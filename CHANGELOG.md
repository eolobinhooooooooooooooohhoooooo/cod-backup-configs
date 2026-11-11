# Changelog

All notable changes. Releases are date-encoded `1.MM.DD` (release month.day,
valid SemVer): each tag (`v1.11.10`, …) marks a published release of the
single-file exe. The v-badge in the app reads the assembly version, so the
header always matches the build.

## [1.11.11] - 2026-11-11

Plain-language preset workflow:

- Saved copies are now called presets everywhere: `SAVE PRESET`,
  `APPLY PRESET`, section header `PRESETS` (EN and PT).
- Applying a preset no longer needs a typed account id: the destination is a
  dropdown of the detected accounts with platform marks. It follows the
  selection in the account list, picking a preset fills in that preset's
  source account, and saving a preset auto-selects it in the list.
- Lock button reads `LOCK CONFIGS` / `UNLOCK CONFIGS` (same behavior: every
  settings file except `.swp` swap files toggled read-only).
- Account list and preset picker are owner-drawn dark lists with themed
  scroll track and thumb — no white Win32 scrollbar, four full rows visible
  unmaximized.

- Black Ops 6 and Black Ops 7 cards show grayed out and unclickable in this
  release: it only works with Modern Warfare 4. Detection still runs for all
  three titles, so the disabled cards keep showing found paths and account
  counts.

## [1.11.10] - 2026-11-10

First public version.

- Auto-detects the players folders of Black Ops 6, Black Ops 7 and Modern
  Warfare 4 across Battle.net, Steam and Xbox PC accounts (no typing paths).
- Back up an account into a dated snapshot, restore any snapshot into any
  account. Copy only — nothing is ever moved or deleted.
- Lock/unlock every settings file of an account (all files except `.swp` swap
  files) as read-only in one click.
- English and Portuguese interface, switchable at runtime.
- Dark minimalist UI with official platform marks, game lockups and the full
  six-face Hitmarker family (embedded, memory-only); subtle deterministic
  background grain.
- Ships as one self-contained `Cod Backup Configs.exe` (~139 MB): no installer,
  no sidecar files, no `.pdb`, nothing left on disk after the run.
- Per-monitor DPI crispness from 100% to 300% scaling.
