# Asset Slots

Every asset the app can consume, defined by slot. File name and path are part
of the contract — code loads by resource name.

Status legend:
- **SHIP** — embedded in the exe and rendered
- **DEPRECATED** — still on disk, referenced nowhere; do not re-embed
- **OPTIONAL** — lives in the kit, an upgrade that is not required

Shared acceptance (applies to every slot unless overridden):
- correct format and dimensions, transparent where stated
- no baked checkerboard, no white halo around edges, no drop shadow, no glow
- no upscaling — source dimensions ≥ display dimensions
- legible at the stated display size on `#0A0B0C`, verified in the running app
- deterministic: regenerated files must be byte-identical

## Slot reference

| # | Slot | File (repo path) | Embedded as | Display size | Source format/dims | Status |
|---|---|---|---|---|---|---|
| 1.1 | BO6 cover | `assets/bo6.jpg` | `img/bo6.jpg` | full-bleed 289×108 | JPEG landscape ≥460×215 | SHIP |
| 1.2 | BO7 cover | `assets/bo7.jpg` | `img/bo7.jpg` | full-bleed 289×108 | JPEG landscape ≥460×215 | SHIP |
| 1.3 | MW4 cover | `assets/mw4.jpg` | `img/mw4.jpg` | full-bleed 289×108 | JPEG landscape ≥460×215 | SHIP |
| 2.1 | Battle.net mark | `assets/kit/platform-battlenet.png` | `img/platform-battlenet.png` | 18×18 | PNG RGBA 64×64 | SHIP |
| 2.2 | Steam mark | `assets/kit/platform-steam.png` | `img/platform-steam.png` | 18×18 | PNG RGBA 64×64 | SHIP |
| 2.3 | Xbox mark | `assets/kit/platform-xbox.png` | `img/platform-xbox.png` | 18×18 | PNG RGBA 64×64 | SHIP |
| 3.1 | BO6 lockup | `assets/kit/logo-bo6.png` | `img/logo-bo6.png` | aspect-fit in 265×58 zone | PNG RGBA 512 wide | SHIP |
| 3.2 | BO7 lockup | `assets/kit/logo-bo7.png` | `img/logo-bo7.png` | aspect-fit in 265×58 zone | PNG RGBA 512 wide | SHIP |
| 3.3 | MW4 lockup | `assets/kit/logo-mw4.png` | `img/logo-mw4.png` | aspect-fit in 265×58 zone | PNG RGBA 512 wide | SHIP |
| 4.1 | ic-backup | `assets/kit/ic-backup.png` | `img/ic-backup.png` | 14×14 | PNG RGBA 32×32 | SHIP |
| 4.2 | ic-restore | `assets/kit/ic-restore.png` | `img/ic-restore.png` | 14×14 | PNG RGBA 32×32 | SHIP |
| 4.3 | ic-lock | `assets/kit/ic-lock.png` | `img/ic-lock.png` | 14×14 | PNG RGBA 32×32 | SHIP |
| 4.4 | ic-unlock | `assets/kit/ic-unlock.png` | `img/ic-unlock.png` | 14×14 | PNG RGBA 32×32 | SHIP |
| 4.5 | ic-refresh | `assets/kit/ic-refresh.png` | `img/ic-refresh.png` | 14×14 | PNG RGBA 32×32 | SHIP |
| 4.6 | ic-folder | `assets/kit/ic-folder.png` | `img/ic-folder.png` | 14×14 | PNG RGBA 32×32 | SHIP |
| 5.1 | GB flag | `assets/flag-en.png` | `img/flag-en.png` | aspect-fit in 22×15 | PNG ≥160 wide | SHIP |
| 5.2 | BR flag | `assets/flag-pt.png` | `img/flag-pt.png` | aspect-fit in 22×15 | PNG ≥160 wide | SHIP |
| 6.1 | app icon | `src/CodBackupConfig/icon.ico` | `<ApplicationIcon>` | taskbar/titlebar 16-256 | ICO multi-frame | SHIP |
| 7.1 | Hitmarker Condensed Bold | `assets/kit/fonts/HitmarkerCondensed-Bold.ttf` | `font/HitmarkerCondensed-Bold.ttf` | title 12.5 pt, section labels 10 pt | TTF | SHIP |
| 7.2 | other Hitmarker weights | `assets/kit/fonts/Hitmarker*.ttf` (5 files) | not embedded | — | TTF | OPTIONAL |
| 8.1 | grain tile | none (generated at startup) | none | tiled across form background | 96×96 ARGB, built in `GrainFx.cs` | SHIP |
| 9.1 | old wordmarks | `assets/{steam,battlenet,xbox}.png` | none | — | PNG ~240 wide | DEPRECATED |

## 1 · Game covers (cards)

Role: full-bleed backdrop of each game card, center-cropped to the 289×108
card, then darkened by the card scrim (horizontal gradient, base `#0A0B0C`,
alpha stops 248 → 195 at 45 % → 238). The official lockup (slot 3) is drawn
over it, and the bottom band (bottom 36 px) is reserved for the resolved-path
line and the "N ACCOUNTS" line.

Acceptance:
- landscape ~2:1 photographic key art, current source 460×215
- must read under the scrim — test it darkened, raw brightness does not count
- no giant centered title of its own competing with the lockup zone
  (upper-middle-left); some art showing on the right half is desirable
- no watermarks, no store UI chrome, no text smaller than illegible-at-289px
- JPEG q ≥ 80, ≤150 KB

Source: Steam store headers (© Activision — identification only), exact URLs
in SOURCES.md.

## 2 · Platform marks (legend)

Role: the platform key under the cards. `MainForm.AddPlatformLogo` loads
`"platform-" + key + ".png"` (key ∈ `battlenet`, `steam`, `xbox`), draws it
18×18, and puts a lowercase label 24 px to its right. The account list itself
is text-only today; if it ever renders marks, reuse the same files at
14-18 px.

Acceptance:
- pure square icon mark, **no wordmark**, one glyph per brand
- official brand colors: battle.net `#00AEFF`, steam `#E9E6DF` (silver),
  xbox `#52B043`
- centered with ≥12 % safe margin so the 18 px downscale stays clean
- transparent background, no halo, ≤30 KB

Source: simpleicons.org SVGs rendered to transparent PNG.

## 3 · Game lockups (card hero)

Role: replaces the plain-text title on found cards — `GameCard` draws it
aspect-fit into `Rectangle(12, 8, Width-24, Height-50)` of the card (with the
standard 289×108 card that is 265×58, i.e. the logo must read at ~58 px tall).
Fallback: if the logo fails to load, the plain game name is drawn instead —
so a missing file degrades gracefully and must never crash.

Acceptance:
- single-line official lockup, near-white stencil type (`#F2F0EB` family)
  with the official numeral treatment (BO6 orange "6", BO7 orange "7",
  MW4 gold "IV")
- wide single-line aspect (~3.2-3.9 : 1) so it fills the 265×58 zone
- transparent background, **no baked shadow or glow** (the app adds none)
- crisp at 58 px rendered height; ≤150 KB

Note: `logo-mw4.png` is 512×161 (taller) and therefore renders ~184 px wide,
visibly smaller than BO6/BO7 (~223 px). That is accepted official artwork; a
wide-crop variant is an optional improvement, not a defect.

## 4 · Button icons

Role: `LineButton.Icon` — drawn 14×14 at x=10, label centered in the space to
its right. Usage map:

| Button | Icon |
|---|---|
| REFRESH | ic-refresh |
| BACK UP THIS ACCOUNT (BACKUP ACCOUNT) | ic-backup |
| RESTORE A SNAPSHOT (RESTORE SNAPSHOT) | ic-restore |
| LOCK SETTINGS | ic-lock / ic-unlock, **swapped dynamically** with lock state |
| OPEN PLAYERS FOLDER / OPEN BACKUPS FOLDER | ic-folder |

Acceptance (all six judged as **one set**):
- 32×32, minimal stroke/pixel style, one consistent stroke weight across the set
- ink `#E9E6DF`; ic-lock may carry a single `#0159B3` accent detail — the
  only accent permitted in the set
- optically centered with even padding; legible at 14 px with no merged
  strokes; transparent; ≤30 KB each

## 5 · Flags (language switcher)

Role: `FlagButton` (top-right corner, EN/PT toggle) aspect-fits the flag into
a 22×15 box beside the label.

Acceptance:
- correct national flags (GB, BR) per vexillological specifications, flat
  colors, no photo texture
- source ≥160 px wide (current: 160×80 / 160×112), ≤10 KB

Source: flagcdn.com `/w160/` (public domain / CC0).

## 6 · App icon

Shipped file: `src/CodBackupConfig/icon.ico`, wired as `<ApplicationIcon>`
(exe icon, Explorer, taskbar, default window icon). Masters for re-export:
`assets/kit/icon.ico` and `assets/kit/icon-256.png` (256×256 RGBA).

Acceptance:
- multi-frame ICO: **BMP DIB 32bpp frames at 16/24/32/48** (WinForms garbles
  small PNG-encoded ICO frames) + PNG frames at 64/128/256
- recognizable silhouette at 16 px; reads correctly on a dark taskbar
- ≤30 KB

Note: the shipped src icon and the kit master currently differ (different
hashes). Treat the shipped file as canonical; if you replace it, update both
and keep them identical.

## 7 · Typefaces (Hitmarker)

Ship: `HitmarkerCondensed-Bold.ttf` → resource `font/HitmarkerCondensed-Bold.ttf`.
GDI+ family name is exactly `Hitmarker Condensed`. Used **only** for the app
title (`Ui.Head(12.5f)`) and the section labels GAME / ACCOUNTS / BACKUPS
(`Ui.Head(10f)`). Everything else — body, numbers, buttons, list rows,
tooltips — stays Consolas, because account IDs and timestamps need strict
tabular alignment.

Registration is memory-only, no OS install (see `Program.Main` and the kit
README for the recipe): embed as `font/<name>`, pin the bytes with a
`GCHandle` that stays alive for process lifetime, register with **both**
`PrivateFontCollection.AddMemoryFont` and gdi32 `AddFontMemResourceEx`, before
any control is created. If the family fails to register, the app silently
falls back to Consolas Bold — never crash, never show a dialog.

Optional (kit-only, documented roles in `assets/kit/README.md`):
`HitmarkerNormal-{Bold,Regular}.ttf`, `HitmarkerText-{Regular,Medium,Bold}.ttf`.
Not embedded, not loaded. Adding one = new `font/...` csproj entry + one more
registration pass in Program.Main.

License: © Activision — identification/display use in the app. No redrawing or
redistributing the faces outside the exe.

## 8 · Background grain tile (generated, no file)

Not a slot with a file — it is built deterministically at startup in
`src/CodBackupConfig/GrainFx.cs` and painted only in the form's
`OnPaintBackground`. Panels, buttons and cards stay flat on purpose.

Contract (deterministic — same bytes on every run):
- 96×96 `Format32bppArgb` seamless tile
- value noise: 8×8 grid from `Random(1337)`, smoothstep bilinear with grid
  indices wrapped modulo 8 (guarantees seamless tiling)
- ordered dither: standard 4×4 Bayer matrix, normalized [0,1]
- per pixel: `delta = (n - 0.5) * 13 + (bayer - 0.5) * 13`, applied equally to
  R/G/B over base `#0A0B0C` → neutral grey grain, total peak-to-peak swing ≤ 26
  levels (the shipped reading of the requested 5 % noise + 10 % dither)
- consumed through a cached `TextureBrush(WrapMode.Tile)`

Measured acceptance: sampling an empty background strip of a screenshot gives
max−min ≈ 4-30 levels; sampling inside a panel gives zero variance.

To retune loudness: change the two amplitude constants (both 13) in
`BuildTile` only. Seed, grid size, Bayer matrix and wrap logic are fixed by
contract.

## 9 · Deprecated

`assets/steam.png`, `assets/battlenet.png`, `assets/xbox.png` — old wide
white wordmarks (~240 px). Replaced by slot 2; the files were deleted from the repo on 2026-11-09
(no code or csproj reference remained).

## Known open items (as of 2026-11-09)

1. ~~Battle.net legend mark missing~~ — fixed 2026-11-09: `AddPlatformLogo`
   now strips dots from the platform key before the resource lookup, so
   `battle.net` resolves to `img/platform-battlenet.png`. Verified by pixel
   scan of a screenshot (87 blue px in the legend box).
2. ~~Shipped `icon.ico` and kit master differ~~ — resolved 2026-11-09:
   `src/CodBackupConfig/icon.ico` is canonical (shipped); `kit/icon.ico` and
   `kit/icon-256.png` are kept only as the design master in SOURCES.md.
3. MW4 lockup renders smaller because its official aspect is 512×161 —
   optional wide crop (slot 3).
