# Map Cleaner legacy semantic report

- Legacy version: 1.12.30
- Capture date: 2026-07-18
- Result: `Successfully removed 16 greenlines and resnapped 20 objects!`
- Warnings/errors: none
- Seed SHA-256: `E19766C9D9DE19CE9C9BC89F850BE2B1E86D3FBD57832F025031EAC6B53C9EE8`
- Output SHA-256: `33040289283130D5E417D4303EA198DD19FD07412D3985216D25C5B1A0521C57`
- Backup SHA-256: `E19766C9D9DE19CE9C9BC89F850BE2B1E86D3FBD57832F025031EAC6B53C9EE8`

## Semantic comparison

| Item | Before | After | Difference |
|---|---:|---:|---:|
| Timing points | 831 | 815 | -16 |
| Hit objects | 924 | 924 | 0 |
| Bookmarks | 20 | 20 | 0 |

The operation changed 20 bookmark timestamps. Exact-line comparison found 128 removed and 112 added timing-point lines, and 844 removed/added hit-object lines; these broad textual changes are consistent with resnapping while preserving the hit-object count. The backup is byte-identical to the seed.

Status: accepted by Olivier on 2026-07-18.
