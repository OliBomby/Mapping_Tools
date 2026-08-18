# Wave 11 — Step 49 legacy WPF/WinForms removal

Status: implemented 2026-08-18  
Scope: final removal of the obsolete WPF/WinForms frontend after the step-47 parity audit and step-48 Avalonia cutover validation.

Step 49 was executed with explicit approval to complete the remaining migration. The Avalonia frontend was already the default executable, and the release/parity checks were run before removal. The release contract remains the shipped Avalonia `Mapping Tools.exe` in `release.zip` and `release_x64.zip` plus the existing x86/x64 installer layout.

## Removed

- `Mapping_Tools/Mapping_Tools.csproj` and the complete legacy WPF/WinForms application tree, including its XAML views, code-behind, converters, custom controls, updater window, and WPF-only adapters.
- `Mapping_Tools_Tests/Mapping_Tools_Tests.csproj` and the legacy WPF characterization/integration test tree.
- Both legacy projects and their solution configurations/references.
- Legacy WPF package references and release fallback artifacts: WPF publishes, `legacy-wpf_x86.zip`, `legacy-wpf_x64.zip`, validator parameters, workflow steps, and release uploads.
- Obsolete linked Desktop assets and shared fixture links that depended on the deleted projects; the assets and fixtures now live in the Avalonia/fixture-owned locations.

## Retained compatibility and release behavior

- `Mapping_Tools.Desktop` remains the only UI project and continues to publish the user-facing executable as `Mapping Tools.exe`.
- Core, Application, and Infrastructure logic remains in place, with no UI framework dependency introduced into those layers.
- `LegacyProjectJsonSerializer` and its `Mapping Tools` / `Mapping_Tools.*` type aliases remain active so existing feature project JSON continues to load and round-trip.
- Existing settings paths, project JSON, pattern collections, `.osu`/`.osb` files, backups, exports, crash logs, and user data under `%LOCALAPPDATA%\Mapping Tools` remain supported.
- Onova update discovery, published executable metadata, command-line handoff, restart behavior, archive names, installer layout, and rollback to the previous Avalonia release remain unchanged.
- Live Infrastructure dependencies such as `System.Drawing.Common`, NAudio, Overlay.NET, and Onova remain because they serve retained framework-neutral or Windows integration contracts; only WPF-only project dependencies were removed.

## Validation record

- Before removal: step-47 parity audit passed 632 relevant cases; Avalonia and WPF builds were clean; the pre-removal release-layout validator and `git diff --check` passed.
- After removal: the solution contains only Avalonia Desktop and framework-neutral projects/tests; the final Release build completed with 0 errors and 157 existing non-CS1591 warnings; the full Release matrix passed 621 tests in 5 test projects, including 9 architecture tests; project persistence tests cover migrated core type aliases.
- Independent final review also restored CI coverage for all five surviving test projects, made updater progress publication complete before preparation completion, and ignored stale tool-progress callbacks after a run resets or a newer run starts. The complete Release matrix passed again after these fixes.
- Release publish completed for win-x86 and win-x64 with `Mapping Tools.exe` version 1.12.30. Fresh `release.zip` and `release_x64.zip` archives passed the primary-only release-layout validator, including executable rename, required Desktop assemblies, archive-root layout, and absence of the removed WPF assembly.
- `git diff --check` passed. Installer source paths and executables were checked for both RIDs; local Inno Setup (`ISCC.exe`) was unavailable, so compilation could not run in this environment.

No commit was created for this migration step.
