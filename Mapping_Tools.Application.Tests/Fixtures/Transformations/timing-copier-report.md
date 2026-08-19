# Timing Copier legacy semantic report

- Legacy version: 1.12.30
- Capture date: 2026-07-18
- Result: `Successfully copied timing to 1 beatmap!`
- Target seed SHA-256: `9DDBE49058712E1211DCEAA52BCBE3D8FF903562EEF5DD0587945407439AC341`
- Source SHA-256: `E19766C9D9DE19CE9C9BC89F850BE2B1E86D3FBD57832F025031EAC6B53C9EE8`
- Output SHA-256: `014BE30D61715042DE03222EFE5FB4232460BAFBB4197CE5E8F8E5CB0C48C51D`
- Backup SHA-256: `9DDBE49058712E1211DCEAA52BCBE3D8FF903562EEF5DD0587945407439AC341`

## Semantic comparison

| Target item | Before | After | Difference |
|---|---:|---:|---:|
| Redlines | 5 | 1 | -4 |
| Greenlines | 4 | 8 | +4 |
| Total timing points | 9 | 9 | 0 |
| Hit objects | 17 | 17 | 0 |

The source contained one redline. The target's previous redlines were replaced by the source timing; four became inherited timing points, preserving the total timing-point count. All 17 target objects were retained and resnapped using `1/3, 1/4`. The backup is byte-identical to the target seed.

Status: accepted by Olivier on 2026-07-18.
