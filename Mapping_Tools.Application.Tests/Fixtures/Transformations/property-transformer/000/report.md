# Property Transformer legacy semantic report

- Legacy version: 1.12.30
- Capture date: 2026-07-18
- Result: `Done!`
- Seed SHA-256: `E19766C9D9DE19CE9C9BC89F850BE2B1E86D3FBD57832F025031EAC6B53C9EE8`
- Output SHA-256: `7DD458E8AF4924ABEDD285D41A5668C65CB5C473087EC0B9DB006F8F9D57A0F5`
- Backup SHA-256: `E19766C9D9DE19CE9C9BC89F850BE2B1E86D3FBD57832F025031EAC6B53C9EE8`

## Semantic comparison

| Item | Before | After | Difference |
|---|---:|---:|---:|
| Bookmarks | 20 | 20 | every timestamp +5 ms |
| Timing points | 831 | 831 | 0 |
| Hit objects | 924 | 924 | 0 |

The operation changed every bookmark by exactly the configured +5 ms. The beatmap serializer also normalized whitespace around several General and Editor property separators and inserted one blank line; these are textual changes without changed values. The backup is byte-identical to the seed.

Status: accepted by Olivier on 2026-07-18.
