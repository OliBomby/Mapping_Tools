# Hitsound Copier legacy semantic report

- Legacy version: 1.12.30
- Capture date: 2026-07-18
- Result: `Done!`
- Target seed SHA-256: `9DDBE49058712E1211DCEAA52BCBE3D8FF903562EEF5DD0587945407439AC341`
- Source SHA-256: `E19766C9D9DE19CE9C9BC89F850BE2B1E86D3FBD57832F025031EAC6B53C9EE8`
- Output SHA-256: `6B75E2932AED26F1C73B290362B4BA1355FD297D2B63526828365AE08C11F2FE`
- Backup SHA-256: `9DDBE49058712E1211DCEAA52BCBE3D8FF903562EEF5DD0587945407439AC341`

## Semantic comparison

| Target item | Before | After | Difference |
|---|---:|---:|---:|
| Redlines | 5 | 5 | 0 |
| Greenlines | 4 | 799 | +795 |
| Hit objects | 17 | 17 | 0 |
| Objects with nonzero hitsound flags | 14 | 0 | -14 |

Overwrite mode copied the source sampleset, custom-index, and volume state into the target timeline. It retained the target's five redlines and all 17 target objects. No source hitsound event matched a target event within the configured 5.5 ms leniency, so the target's object hitsound flags were reset. Storyboard sample copying and slider-end muting were disabled. The backup is byte-identical to the target seed.

Status: accepted by Olivier on 2026-07-18.
