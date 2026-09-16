# Slider Merger legacy semantic report

- Legacy version: 1.12.30
- Capture date: 2026-07-19
- Result: `Successfully merged 16 sliders!`
- Seed SHA-256: `9DDBE49058712E1211DCEAA52BCBE3D8FF903562EEF5DD0587945407439AC341`
- Output SHA-256: `0EF86314B44A53B84F546D64A716D19516B18EDDD074BB228503B59E90C4FA49`
- Backup SHA-256: `9DDBE49058712E1211DCEAA52BCBE3D8FF903562EEF5DD0587945407439AC341`

## Semantic comparison

| Target item | Before | After | Difference |
|---|---:|---:|---:|
| Hit objects | 17 | 3 | -14 |
| Sliders | 7 | 2 | -5 |
| Circles | 9 | 0 | -9 |
| Spinners | 1 | 1 | 0 |
| Timing points | 9 | 9 | 0 |

With effectively unlimited leniency and linear gap connections, the 12 eligible objects before the spinner became one 2,218.35 px Bézier slider. The four eligible objects after the spinner became one 947.07 px Bézier slider. The spinner split the two merge groups and remained unchanged. The legacy message counts the 16 source sliders/circles participating in merges. The backup is byte-identical to the seed.

Status: accepted by Olivier on 2026-07-19.
