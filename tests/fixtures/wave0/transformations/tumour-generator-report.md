# Tumour Generator 2 legacy semantic report

- Legacy version: 1.12.30
- Capture date: 2026-07-19
- Result: `Successfully generated tumours on 7 sliders!`
- Seed SHA-256: `9DDBE49058712E1211DCEAA52BCBE3D8FF903562EEF5DD0587945407439AC341`
- Output SHA-256: `BE26A9D99683DD8F31713DA20B27E34449F3510CD06AFC2B375D28A37EF8BF55`
- Backup SHA-256: `9DDBE49058712E1211DCEAA52BCBE3D8FF903562EEF5DD0587945407439AC341`

## Semantic comparison

| Target item | Before | After | Difference |
|---|---:|---:|---:|
| Sliders | 7 | 7 | 0 |
| Total slider curve points | 31 | 58 | +27 |
| Hit objects | 17 | 17 | 0 |
| Redlines | 5 | 5 | 0 |
| Greenlines | 4 | 6 | +2 |

The real-world single triangle layer was applied to every slider, adding 27 reconstructed curve points and adjusting affected pixel lengths. With `Fix SV` enabled, two additional inherited points preserved the resulting temporal slider lengths. The backup is byte-identical to the seed.

Status: accepted by Olivier on 2026-07-19.
