# Wave 0 behavioral baseline register

This register is the review surface for legacy destructive behavior. The machine-readable records live under `tests/fixtures/wave0/transformations`; each record names the seed input, project/options fixture, expected output location, and capture status.

Wave 0 automation guarantees that every destructive feature has a stable record and that all versioned fixture hashes remain unchanged. It does not substitute for mapper approval. A record becomes trusted only after a user compares the recorded legacy output with the stated options and changes its status from `captured` to `accepted` with reviewer/date evidence.

The initial records intentionally use the existing compact beatmap fixtures. Feature-specific before/after outputs should replace the seed expected files when each legacy workflow is captured. Never run the capture against a live Songs directory: copy the seed into a disposable run directory first.

## Acceptance checklist

1. Run the named legacy feature against a disposable copy of the seed input.
2. Use the exact project/options fixture recorded in the baseline.
3. Replace the baseline's expected output with the legacy result.
4. Record the legacy Mapping Tools version, output SHA-256, reviewer, date, and any known legacy defect.
5. Mark the baseline `accepted` only after inspecting the semantic change and confirming recovery from the untouched seed.

The Wave 0 user gate remains open until every baseline is accepted. CI checks coverage and drift; user acceptance supplies trust.
