# Wave 11 - Step 48: default executable switch

Status: implemented 2026-08-18.

Step 48 makes `Mapping_Tools.Desktop` the default development and shipped
frontend after the step-47 parity audit. The legacy WPF project is no longer
part of the shipped solution; the removal and compatibility record is in
step 49.

## Executable and package contract

- The Avalonia project publishes `Mapping Tools.exe` for Windows and the
  extensionless `Mapping Tools` apphost for Linux and macOS. The managed
  payload remains rooted in `Mapping_Tools.Desktop` for every runtime
  identifier.
- The release workflow publishes self-contained Windows x86/x64, Linux x64/arm64,
  and macOS x64/arm64 assets. Canonical
  archive names are `mapping-tools-{windows,linux,osx}-{architecture}.zip`.
  Linux and macOS archives are created on native runners after marking the
  extensionless apphost executable, preserving the Unix mode in the ZIP; the
  macOS apphost is wrapped in a standard `Mapping Tools.app` bundle.
- The historical Windows updater names `release.zip` and `release_x64.zip`
  remain byte-for-byte aliases of the canonical Windows x86/x64 assets. The
  Inno Setup outputs `mapping_tools_installer_x86.exe` and
  `mapping_tools_installer_x64.exe` remain Windows-only and continue to use
  the existing Windows publish directories.
- `tools/validate-release-layout.ps1` checks all six publish directories and
  archives, their platform-specific apphost names, required root-level
  or bundle payload contents, deterministic archive names, Windows compatibility aliases, and
  Windows installers before upload. It rejects nested ZIPs and stale apphost
  names.

## Runtime compatibility

The Windows Avalonia updater continues to use the existing `release.zip` asset
names, Onova staging/replacement behavior, original command-line arguments,
and the current process architecture. The production gateway gives Onova the
actual running/published `Mapping Tools.exe` path explicitly (falling back to
the development assembly path for DLL launches), so installed Windows update
handoff and restart do not depend on the `Mapping_Tools.Desktop.dll` assembly
name. The portable Linux/macOS archives are release downloads; the existing
Windows installer and updater path does not make those platforms installer-
compatible.
Application settings, project JSON, backups, exports, and crash reports remain
under the operating system's local application-data directory (`%LOCALAPPDATA%\Mapping Tools`
on Windows).

The Avalonia entry point now writes the legacy-compatible `crash-log.txt` for
dispatcher, domain, unobserved-task, and startup exceptions. Windows-only
adapters and the Windows Onova updater still report unsupported platforms
through their existing guards. No single-instance mechanism existed in the
pre-cutover startup path, so step 48 does not introduce a new mutex or change
restart semantics.

## Verification contract

The release-layout fixture test covers all six desktop runtime identifiers and
the release workflow validates the concrete publish layout before uploading
assets. Windows installer behavior remains covered separately by the existing
installer sources; no installer is claimed for Linux or macOS.

Avalonia 12.1 sources consulted for the startup exception boundary:

- https://docs.avaloniaui.net/docs/app-development/setting-unhandled-exceptions
- https://docs.avaloniaui.net/api/avalonia/threading/dispatcher
- https://docs.avaloniaui.net/api/avalonia/threading/dispatcherunhandledexceptioneventargs
- https://github.com/AvaloniaUI/Avalonia/tree/12.1.0
