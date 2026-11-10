# Asset Sources

One line per file. All images verified by magic bytes (JPEG `FFD8` / PNG `89504E47`)
and visual inspection. Proprietary marks are used for identification only, in an
unofficial utility that is not sold.

## Shipped (embedded in the exe)

| File | Source | Notes |
|---|---|---|
| `mw4.jpg` | https://shared.fastly.steamstatic.com/store_item_assets/steam/apps/4435490/0c492be3204835d71e8b454b6eb1817f8ff68c0d/header.jpg?t=1793941334 | Steam store header, appid 4435490. © Activision. |
| `bo7.jpg` | https://shared.fastly.steamstatic.com/store_item_assets/steam/apps/3606480/d7041a15f572f7702d5f4bc97e498cd3e1cc62e2/header.jpg?t=1789747703 | Steam store header, appid 3606480. © Activision. |
| `bo6.jpg` | https://shared.fastly.steamstatic.com/store_item_assets/steam/apps/2933620/header.jpg | Steam store header, appid 2933620. © Activision. |
| `kit/platform-battlenet.png` | https://simpleicons.org/icons/battlenet.svg | Rendered to 64×64 transparent PNG, brand color `#00AEFF`. © Activision Blizzard trademark. |
| `kit/platform-steam.png` | https://simpleicons.org/icons/steam.svg | Rendered to 64×64 transparent PNG, neutral `#E9E6DF` on dark UI. © Valve trademark. |
| `kit/platform-xbox.png` | https://simpleicons.org/icons/xbox.svg | Rendered to 64×64 transparent PNG, brand color `#52B043`. © Microsoft trademark. |
| `kit/logo-bo6.png` | https://commons.wikimedia.org/wiki/File:Call_of_Duty_-_Black_Ops_6_Logo.svg | Official lockup rendered 512 wide, wordmark recolored `#F2F0EB`, orange `6` kept. © Activision. |
| `kit/logo-bo7.png` | https://commons.wikimedia.org/wiki/File:CoDBlackOps7_Logo.svg | Official lockup rendered 512 wide, wordmark recolored `#F2F0EB`, orange `7` kept. © Activision. |
| `kit/logo-mw4.png` | https://commons.wikimedia.org/wiki/File:CODMW4_LOGO_PRIMARY.webp | Official lockup rendered 512 wide, original white + gold `IV` kept. © Activision. |
| `kit/ic-backup.png` … `kit/ic-folder.png` | handmade | Six 32×32 action icons, stroke `#E9E6DF`, accent detail `#0159B3`. Original to this repo. |
| `kit/fonts/Hitmarker*.ttf` (six faces) | https://brand.callofduty.com/typography/typeface → https://www.callofduty.com/cdn/brand/HitmarkerFonts.zip | Official Activision typeface; every shipped face is embedded in the exe and registered in a GDI+ private collection at startup — memory-only, no install, no registry. © Activision. |
| `flag-en.png` | https://flagcdn.com/w160/gb.png | flagcdn.com, CC0. 160×80. |
| `flag-pt.png` | https://flagcdn.com/w160/br.png | flagcdn.com, CC0. 160×112. |
| `kit/icon.ico` | handmade | Shipped `<ApplicationIcon>` (BMP DIB 16–48 + PNG 64–256), referenced directly by the csproj. `kit/icon-256.png` is embedded separately and becomes the runtime window icon. |

## Generated at runtime (no source file)

Background film grain: deterministic 96×96 tile built in `src/CodBackupConfig/GrainFx.cs`
(seed 1337, 8×8 value noise + 4×4 Bayer, total swing ≤ 26 levels). Same input always
produces the same pixels.

## Removed (replaced by the kit)

| File | Was | Replaced by |
|---|---|---|
| `steam.png` | white wordmark from Wikimedia `Steam_logo.svg` | `kit/platform-steam.png` |
| `battlenet.png` | white wordmark from Wikimedia `Battle.net_logo.svg` | `kit/platform-battlenet.png` |
| `xbox.png` | white wordmark from Wikimedia `Xbox_wordmark.svg` | `kit/platform-xbox.png` |

Download dates: 2026-11-09 (covers, flags, wordmarks) and kit assembly the same day.
SVG→PNG conversion done locally with sharp (temp tooling removed afterwards).
