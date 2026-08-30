# Mapping Tools sample plugin

This project is a small complete external tool for the Avalonia desktop
version of Mapping Tools. It demonstrates the pieces a plugin needs:

- an attributed `IMappingToolDefinition` implementation;
- a `SingleRunToolViewModel` with one `IShellProjectFeature<T>` setting;
- a backup-aware edit through `IBeatmapEditingGateway`; and
- an Avalonia `Control` view whose name matches the view model name.

The sample appends one configured tag to the `[Metadata]` `Tags` field of the
beatmaps selected in the Mapping Tools shell. It is intentionally small, but
still uses the normal Mapping Tools execution, backup, cancellation, and
reload boundaries. QuickRun uses the shell's first selected beatmap and is the
only path that requests an automatic osu! reload.

## Build

From the repository root, run:

```powershell
dotnet build Mapping_Tools.SamplePlugin/Mapping_Tools.SamplePlugin.csproj -c Release
```

The build packages the drop-in artifact at:

```text
Mapping_Tools.SamplePlugin/bin/Release/plugin/Mapping_Tools.SamplePlugin.dll
```

Copy that single DLL into the `Plugins` directory in the Mapping Tools app-data
folder (`%LOCALAPPDATA%\Mapping Tools\Plugins`), then restart Mapping Tools.
The folder is created automatically when Mapping Tools starts. For a Debug
build, use the equivalent `bin/Debug/plugin` directory.

The plugin references the host's public `Mapping_Tools.Desktop.Plugin` API and
the host's Avalonia/MVVM assemblies. Build it against the same Mapping Tools
source revision as the executable that will load it.
