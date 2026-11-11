# UI Asset Kit — Cod Backup Configs

Ready-to-embed asset kit prepared for `src/CodBackupConfig/` (`.csproj` `EmbeddedResource`, 100% loaded from memory at runtime — **zero temp files** written to disk).

## Contents

### 1. Application Icon
| File | Format / Frames | Use |
|---|---|---|
| `icon.ico` | Multi-size `.ico`: **BMP DIB 32bpp** (`16, 24, 32, 48`) + **PNG** (`64, 128, 256`) | `<ApplicationIcon>` in `.csproj` (exe icon, Explorer, taskbar) — referenced directly from this path, the single canonical copy. Also embedded as `img/icon.ico` for the runtime window/header icon in `MainForm`. |

> **Why BMP DIB for 16–48px:** WinForms `System.Drawing.Icon` garbles small PNG-encoded ICO frames. Keeping `16..48` as classic 32-bit DIB guarantees sharp taskbar, Explorer, and titlebar rendering.

### 2. Official Platform Icons (`64x64` transparent PNG)
Pure square icon marks (replacing the clipped/wide wordmarks in `assets/*.png`):
- `platform-battlenet.png` — Battle.net swirl (`#00AEFF`, Simple Icons)
- `platform-steam.png` — Steam emblem (`#E9E6DF`, Simple Icons)
- `platform-xbox.png` — Xbox sphere mark (`#52B043`, Simple Icons)

Recommended display size in Accounts list & legend: `16x16` or `18x18` (with `Img.Fit` fixed to contain inside bounds).

### 3. Official Call of Duty Game Logos (`512px` wide max, transparent PNG, dark-UI ready)
- `logo-bo6.png` — Call of Duty: Black Ops 6 (recolored `#F2F0EB` + official orange `6`)
- `logo-bo7.png` — Call of Duty: Black Ops 7 (recolored `#F2F0EB` + official orange `7`)
- `logo-mw4.png` — Call of Duty: Modern Warfare 4 (official white + gold `4`)

Source: Wikimedia Commons official logos (`Call_of_Duty_-_Black_Ops_6_Logo.svg`, `CoDBlackOps7_Logo.svg`, `CODMW4_LOGO_PRIMARY.webp`).

### 4. Minimalist UI Action Icons (`32x32` transparent PNG)
Stroke `#E9E6DF` (with `#0159B3` accent detail on lock):
- `ic-backup.png`
- `ic-restore.png`
- `ic-lock.png`
- `ic-unlock.png`
- `ic-refresh.png`
- `ic-folder.png`

Recommended draw size on buttons: `14x14` or `16x16` (`InterpolationMode.HighQualityBicubic`).

### 5. Official Activision Hitmarker Fonts (`fonts/*.ttf`)
Downloaded from Activision's official brand portal (`https://brand.callofduty.com/typography/typeface` -> `https://www.callofduty.com/cdn/brand/HitmarkerFonts.zip`).

| File | Verified GDI+ Family Name | Shipped role (v1.11.10) |
|---|---|---|
| `HitmarkerCondensed-Bold.ttf` | `Hitmarker Condensed` | Window title, section headers (`GAME`, `ACCOUNTS`, `BACKUPS`), button caps |
| `HitmarkerNormal-Bold.ttf` | `Hitmarker Normal` | Game names on the account cards |
| `HitmarkerNormal-Regular.ttf` | `Hitmarker Normal` | Registered spare (normal-weight name text) |
| `HitmarkerText-Regular.ttf` | `Hitmarker Text` | Labels, hints, body text, status, tooltips |
| `HitmarkerText-Medium.ttf` | `Hitmarker Text Medium` | Registered, no UI slot yet |
| `HitmarkerText-Bold.ttf` | `Hitmarker Text` | Bold body-text runs |

### 6. Intro Animation (`intro.gif`)
38-frame 1280×720 GIF, ~2.1 s via its `PropertyItem(0x5100)` frame delays. Content:
the app shield/lock emblem, “COD BACKUP CONFIGS” wordmark and the tagline
“LOCAL PROFILE VAULT // COPY-ONLY // READ-ONLY LOCK” over dimmed COD key art;
both edges fade in/out on `#0A0B0C` so it blends into the app background with
no visible seam. Embedded as `img/intro.gif` and played **once, in memory**, at
startup by a fullscreen double-buffered overlay (`IntroOverlay`, MainForm) —
zero files written, no looping. Skippable with a mouse click, `Esc` or `Space`.

> **WinForms Memory-Only Loading Note (Zero Temp Files / No OS Install Needed):**
> the working recipe is `src/CodBackupConfig/FontHost.cs` — all six files are
> embedded as `font/<name>.ttf`, pinned with `GCHandle` (kept alive for the
> whole run) and fed to `GdipPrivateAddMemoryFont` on a collection made with
> `GdipNewPrivateFontCollection` (a `new Bitmap(1, 1)` warm-up initializes the
> GDI+ session first); the same pinned bytes are then mirrored into GDI via
> `AddFontMemResourceEx`, because `TextRenderer`/`Label`/`TextBox`/checkbox
> text resolve families through GDI, which cannot see a GDI+ private
> collection. Each family handle from `GdipCreateFontFamilyFromName`
> is wrapped into a managed `FontFamily` via reflection (`_nativeFamily` +
> `_createDefaultOnFail`). `Ui.Face(...)` silently falls back to Consolas on
> any miss or substitution; Consolas also stays reserved for numeric account
> IDs and timestamps so columns remain strictly tabular/aligned.

## Licensing Notice
- **MIT License** applies to the application source code (`src/`).
- **Hitmarker** typeface (`© Activision`), Call of Duty game logos/covers (`© Activision`), and platform trademarks (Battle.net, Steam, Xbox) are proprietary marks of their respective owners, used strictly for visual identification in this unofficial utility.
