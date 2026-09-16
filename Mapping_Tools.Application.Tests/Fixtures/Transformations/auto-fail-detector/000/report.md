# Auto-fail Detector legacy semantic report

- Legacy version: 1.12.30
- Capture date: 2026-07-19
- Positive fixture: `IOSYS - Endless Tewi-ma Park (Lanturn) [Tewi 2B Expert Edition].osu`
- Positive result: `20 unloading objects detected and 63 potential unloading objects detected!`
- Negative control: `ComplicatedTestMap.osu`
- Negative result: `No auto-fail detected.`
- Backup: not applicable (analysis-only operation)

## Fixture characteristics

| Item | Positive fixture |
|---|---:|
| Hit objects | 440 |
| Sliders | 215 |
| Circles | 223 |
| Spinners | 2 |
| Redlines | 1 |
| Greenlines | 8 |
| Approach rate | 8 |
| Overall difficulty | 5 |

The compact seed correctly exercises the clean path, but the real-world 2B map is the behavioral baseline because it exercises both confirmed and potential unloading detection. Default AR/OD, 9 ms physics-update leniency, and no automatic fix were used. Neither input was modified.

Status: accepted by Olivier on 2026-07-19.
