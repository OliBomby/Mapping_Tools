# Wave 0 test ownership

Wave 0 fixture data is colocated with the test project that consumes it. Shared inputs are
copied only when both a domain test and an application or infrastructure test need an
independent test-project output.

| Test project | Ownership | Current contents |
|---|---|---|
| `Mapping_Tools.Core.Tests` | Framework-neutral domain tests | Beatmaps, storyboard data, and core expected outputs under `Resources` |
| `Mapping_Tools.Application.Tests` | Application services and tool workflows | Beatmaps, mapsets, transformation records/outputs, geometry-dashboard records, and their referenced inputs under `Fixtures` |
| `Mapping_Tools.Infrastructure.Tests` | Filesystem, persistence, audio, and platform adapters | Settings, project JSON, pattern collections, geometry persistence, audio, and platform-failure scenarios under `Fixtures` |
| `Mapping_Tools.Architecture.Tests` | Cross-project dependency policy | Core/Application source, package, and project-reference guardrails |
| `Mapping_Tools.Desktop.Tests` | Avalonia presentation and shell behavior | View-model, control, and frontend tests without migration fixture ownership |

As Wave 1 moves a production type into Core, its pure tests move from the legacy suite to
`Mapping_Tools.Core.Tests` in the same change. A test remains in a frontend project when it
directly needs that frontend or a platform integration. Tests must not be copied into both
projects without an independent output-path reason.
