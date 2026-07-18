# Rhythm Guide legacy semantic report

- Legacy version: 1.12.30
- Capture date: 2026-07-18
- Result: `Done!`
- Target seed SHA-256: `9DDBE49058712E1211DCEAA52BCBE3D8FF903562EEF5DD0587945407439AC341`
- Source SHA-256: `E19766C9D9DE19CE9C9BC89F850BE2B1E86D3FBD57832F025031EAC6B53C9EE8`
- Output SHA-256: `3BFB3DCDB9021B8DD82FDBDC31B0EC6BBFB30E3F65384F0CD7097F9B6160E394`
- Backup SHA-256: `E19766C9D9DE19CE9C9BC89F850BE2B1E86D3FBD57832F025031EAC6B53C9EE8`

## Semantic comparison

| Target item | Before | After | Difference |
|---|---:|---:|---:|
| Redlines | 5 | 5 | 0 |
| Greenlines | 4 | 4 | 0 |
| Hit objects | 17 | 1,344 | +1,327 |
| Circles | 9 | 1,336 | +1,327 |
| New-combo objects | 7 | 1,334 | +1,327 |

`HitsoundEvents` selection appended one circle for each qualifying source timeline event. `NC everything` marked all 1,327 generated circles as new combos. The original 17 target objects and all nine timing points were retained.

The legacy AddToMap path backs up the input source instead of the mutated export target. The observed `source.osu` backup is byte-identical to the source fixture; this known legacy behavior is recorded for parity and deferred from Wave 0.

Status: accepted by Olivier on 2026-07-18.
