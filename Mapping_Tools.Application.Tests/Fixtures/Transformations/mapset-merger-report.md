# Mapset Merger legacy semantic report

- Legacy version: 1.12.30
- Capture date: 2026-07-18
- Result: `Successfully merged 2 mapsets!`
- Exported beatmaps: 2
- Exported assets: 12
- Backup: not applicable (export-only operation)

## Semantic comparison

The Normal and Expert inputs were emitted as separate beatmaps. Their audio and background references were rewritten to `Wave0-A` and `Wave0-B` subfolders respectively. Shared custom hitsound names were collision-resolved by assigning the second mapset the `2` suffix, producing paired samples such as `soft-hitclap.wav` and `soft-hitclap2.wav`.

The exact beatmap outputs are versioned. The output manifest also records hashes for the larger audio/image files without duplicating those media files in the repository.

Status: accepted by Olivier on 2026-07-18.
