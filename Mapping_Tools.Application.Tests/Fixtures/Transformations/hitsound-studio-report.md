# Hitsound Studio legacy semantic report

- Legacy version: 1.12.30
- Capture date: 2026-07-18
- Result: `Number of sample indices: 1, Number of samples: 1, Number of greenlines: 1`
- Base beatmap SHA-256: `9DDBE49058712E1211DCEAA52BCBE3D8FF903562EEF5DD0587945407439AC341`
- Source sample SHA-256: `08965C7225457205B4D8CA9EACE5195CEE1CE77D7BA548F41DFC7F1E0DC33E23`
- Output beatmap SHA-256: `2DC96A9639E1E78F0931A02D50B6A7453018AED968FC28C81BF84DAEAA54AB99`
- Exported sample SHA-256: `08965C7225457205B4D8CA9EACE5195CEE1CE77D7BA548F41DFC7F1E0DC33E23`
- Backup: not applicable (export-only operation)

## Semantic comparison

| Output item | Count |
|---|---:|
| Redlines retained from base | 5 |
| Generated greenlines | 1 |
| Generated hit objects | 5 |
| Generated sample indices | 1 |
| Exported samples | 1 |

The single layer produced normal hitsounds at `15`, `334`, `526`, `942`, and `2109` ms. All events share custom index 1, so one inherited timing point and one `normal-hitnormal.wav` sample were emitted. The PCM export is byte-identical to the source WAV. Opening the export folder on completion is part of the observed legacy behavior.

Status: accepted by Olivier on 2026-07-18.
