# Slider Completionator legacy semantic report

- Legacy version: 1.12.30
- Capture date: 2026-07-19
- Result: `Successfully completed 7 sliders!`
- Seed SHA-256: `9DDBE49058712E1211DCEAA52BCBE3D8FF903562EEF5DD0587945407439AC341`
- Output SHA-256: `D7A646118B8EE2E3A1C51B92D2E6A40B447E004522AA2C9FEEF0BB29B003B078`
- Backup SHA-256: `9DDBE49058712E1211DCEAA52BCBE3D8FF903562EEF5DD0587945407439AC341`

## Semantic comparison

| Target item | Before | After | Difference |
|---|---:|---:|---:|
| Sliders | 7 | 7 | 0 |
| Hit objects | 17 | 17 | 0 |
| Redlines | 5 | 5 | 0 |
| Greenlines | 4 | 7 | +3 |

All slider anchors were moved so the effective curve ended at 75% of the full path. Their pixel lengths and inherited velocities were recalculated for a duration of 1.5 beats. Three additional inherited points were required where simultaneous or nearby sliders used distinct calculated velocities. The backup is byte-identical to the seed.

Status: accepted by Olivier on 2026-07-19.
