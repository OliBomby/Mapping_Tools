# Metadata Manager legacy semantic report

- Legacy version: 1.12.30
- Capture date: 2026-07-18
- Result: `Successfully exported metadata to 1 beatmap!`
- Target seed SHA-256: `9DDBE49058712E1211DCEAA52BCBE3D8FF903562EEF5DD0587945407439AC341`
- Output SHA-256: `0F9EF22EEC9843B684C7D2EB76433E3356025F5186CBBA0313BD2724373B26E2`
- Backup SHA-256: `9DDBE49058712E1211DCEAA52BCBE3D8FF903562EEF5DD0587945407439AC341`

## Semantic comparison

| Item | Before | After |
|---|---|---|
| Artist / romanised artist | Test Artist | Wave Zero Artist |
| Title / romanised title | Test Title | Wave Zero Metadata Baseline |
| Creator | OliBomby | Fixture Mapper |
| Source | empty | Wave 0 |
| Tags | test | wave zero metadata fixture |
| Preview time | -1 | 12345 |
| Beatmap ID / set ID | existing values | 0 / -1 |
| Combo colours | existing palette | `#FF3366`, `#33CCFF`, `#FFCC33` |
| Timing points | 9 | 9 |
| Hit objects | 17 | 17 |

The manager removed the duplicate `wave` tag, reset the online IDs, replaced the combo palette, and renamed the file to `Wave Zero Artist - Wave Zero Metadata Baseline (Fixture Mapper) [complicated].osu`. Timing and hit objects were retained. The backup is byte-identical to the target seed.

Status: accepted by Olivier on 2026-07-18.
