# Wave 11 - Step 48: default executable switch

Status: implemented 2026-08-18.

Step 48 makes `Mapping_Tools.Desktop` the default development and shipped
frontend after the step-47 parity audit. Step 49 remains out of scope: the WPF
project, WPF packages, and WPF source remain buildable and are not selected by
any default launch or installer path.

## Executable and package contract

- The Avalonia project keeps the user-facing `Mapping Tools.exe` published
  apphost name, version, icon, and Windows application manifest while
  publishing from its own `net10.0/win-x86` and `net10.0/win-x64` directories.
- `release.zip` and `release_x64.zip`, both Inno installers, the VS Code launch
  profile, the VS Code build/watch/publish tasks, and the Avalonia project
  launch profile all target `Mapping_Tools.Desktop`. The VS Code prelaunch
  build intentionally builds only the default frontend; solution-wide builds
  remain the CI and transition verification path.
- The release workflow also publishes WPF from the unchanged legacy project as
  `legacy-wpf_x86.zip` and `legacy-wpf_x64.zip`. These archives are documented
  fallback artifacts and are not included in the primary installer payload.
- `tools/validate-release-layout.ps1` checks both architecture-specific primary
  and fallback outputs, matching file versions, both installer outputs, the
  user-facing executable names, and the required root-level contents of all
  four release archives before upload. It rejects nested ZIPs and cross-frontend
  assemblies so a fallback archive cannot silently become the shipped payload.

## Runtime compatibility

The Avalonia updater continues to use the existing `release.zip` asset names,
Onova staging/replacement behavior, original command-line arguments, and the
current process architecture. The production gateway gives Onova the actual
running/published `Mapping Tools.exe` path explicitly (falling back to the
development assembly path for DLL launches), so installed update handoff and
restart do not depend on the `Mapping_Tools.Desktop.dll` assembly name.
Application settings, project JSON, backups, exports, and crash reports remain
under `%LOCALAPPDATA%\Mapping Tools`.

The Avalonia entry point now writes the legacy-compatible `crash-log.txt` for
dispatcher, domain, unobserved-task, and startup exceptions. Windows-only
adapters and the Windows Onova updater still report unsupported platforms
through their existing guards. No single-instance mechanism existed in the
pre-cutover startup path, so step 48 does not introduce a new mutex or change
restart semantics.

## Verification contract

`DefaultExecutableTests` guards the solution ordering and the development,
release, installer, and WPF-fallback references. The release workflow validates
the concrete publish layout before uploading assets. Both frontend projects
remain in the solution and continue to be built and tested during the
transition.

Avalonia 12.1 sources consulted for the startup exception boundary:

- https://docs.avaloniaui.net/docs/app-development/setting-unhandled-exceptions
- https://docs.avaloniaui.net/api/avalonia/threading/dispatcher
- https://docs.avaloniaui.net/api/avalonia/threading/dispatcherunhandledexceptioneventargs
- https://github.com/AvaloniaUI/Avalonia/tree/12.1.0
