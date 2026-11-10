# Style Tokens & Constraints

Style contract for every Cod Backup Configs asset (images and generated
textures). Code mirror: `src/CodBackupConfig/Ui.cs` — change a value here and
in Ui.cs together, or not at all.

## Palette (rehab.ac values, verbatim)

| Token | Hex | Role |
|---|---|---|
| Bg | `#0A0B0C` | window background — the only surface that carries grain |
| Panel | `#131417` | controls, cards, dropdowns — always flat |
| PanelHi | `#1B1F25` | hover state |
| Line | `#212328` | every 1 px border |
| Text | `#E9E6DF` | primary text; default ink for icons and marks |
| Dim | `#8A8D94` | secondary text, legend labels |
| Faint | `#4B4E56` | tertiary text, section labels, placeholders |
| Accent | `#0159B3` | selection, focus borders, account counts |
| AccentDim | `#0F243C` | filled accent states (highlight buttons, checked box fill) |
| SelRow | `#0E1928` | selected list row |
| Error | `#C8102E` | error status text only |

Assets built for the generic UI use `Text` as ink. Brand marks (platform
logos) keep their official colors — that is the sanctioned palette exception.

## Typography

- Body, numbers and controls: **Consolas** — 9.5 pt body, 8.5 pt labels,
  10 pt bold card names. Account IDs, timestamps and counts are tabular and
  must never move to a proportional face.
- Display headers only: **Hitmarker Condensed Bold** — title 12.5 pt, section
  labels 10 pt uppercase, registered memory-only from the embedded TTF with a
  silent Consolas Bold fallback. Nothing else in the UI may use it.
- Type-like assets (lockups, wordmarks) are flattened vector-quality PNGs:
  no bitmap fonts, no aliased edges, no added outlines for contrast.

## Structure

- 12 px grid. Window width dynamic (starts 980, minimum 920; every control
  reflows live on resize), margins 24, gap between blocks 12, panel inner
  padding 14.
- 1 px `Line` borders are the entire structural language: **no rounded
  corners, no drop shadows, no strokes thicker than 1 px.**
- No gradients on panels or controls. Exactly one sanctioned gradient in the
  app: the card cover scrim (horizontal, Bg→Bg, alpha 240 / 225 @45 % / 248).
- Panels are flat solids. Any asset placed on a panel must assume a flat,
  grain-free surface underneath it.

## Legibility (dark-first)

- Contrast ladder: Text for primary, Dim for secondary, Faint for tertiary —
  nothing may be dimmer than Faint on Bg.
- Marks and icons are designed for the small end first: they must survive
  their 14-18 px render with no merged strokes (≥12 % safe margin on the
  mark).
- Covers are always seen through the scrim — evaluate them darkened.
- Grain ceiling: background texture keeps total peak-to-peak swing ≤ 26
  levels (see ASSETS.md §8); panels, buttons and cards must show zero
  variance in a screenshot sample.

## File & naming conventions

- Kebab-case file names; the resource name in the csproj is the contract.
- PNG 32-bit RGBA for marks/icons/logos/flags; JPEG q ≥ 80 for covers.
- Source dimensions at least 2× the display size, in clean multiples.
- Generated assets are deterministic (fixed seed, reproducible bytes).
- Budgets: covers ≤150 KB, marks/icons ≤30 KB, flags ≤10 KB, app icon ≤30 KB.

## Trademark note

Steam (Valve), Battle.net (Activision Blizzard), Xbox (Microsoft), Call of
Duty game names, logos and key art, and the Hitmarker typeface (Activision)
are trademarks/copyrights of their owners, used here strictly for
identification in an unofficial, non-commercial utility. Every proprietary
file must carry its line in `assets/SOURCES.md`.
