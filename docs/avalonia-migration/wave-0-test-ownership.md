# Wave 0 test ownership

Wave 0 keeps the legacy WPF suite runnable while creating explicit homes for migration tests.

| Test project | Ownership | Current contents |
|---|---|---|
| `Mapping_Tools.Core.Tests` | Framework-neutral domain tests and versioned fixture contracts | Fixture catalog completeness, stable IDs, hashes, destructive-feature baseline coverage, migrated C2 mathematics tests, and C1 storyboard round-trip/error tests |
| `Mapping_Tools.Architecture.Tests` | Cross-project dependency policy | Core/Application source, package, and project-reference guardrails |
| `Mapping_Tools_Tests` | Legacy WPF characterization and integration | Existing beatmap, math, slider, tumour, serialization, listener, converter, and project tests |

As Wave 1 moves a production type into Core, its pure tests move from `Mapping_Tools_Tests` to `Mapping_Tools.Core.Tests` in the same change. A test remains in the legacy project when it directly needs WPF, WinForms, the legacy executable, or a Windows integration. Tests must not be copied into both projects.

The fixture contract deliberately lives in `Mapping_Tools.Core.Tests`: the files and hashes are frontend-independent migration inputs. Behavioral comparisons against legacy implementations remain in `Mapping_Tools_Tests` until the protected production code moves.
