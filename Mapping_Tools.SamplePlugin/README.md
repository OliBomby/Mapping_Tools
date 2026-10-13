# Mapping Tools sample plugin

This project is a small complete external tool for the Avalonia desktop version of Mapping Tools. It demonstrates the
pieces a plugin needs:

- an attributed `IMappingToolDefinition` implementation;
- a `SingleRunToolViewModel` with one `IShellProjectFeature<T>` setting;
- a tool-owned `ToolConfigSchema` with an example configuration migration;
- a backup-aware edit through `IBeatmapEditingGateway`; and
- an Avalonia `Control` view whose name matches the view model name.

The sample appends one configured tag to the `[Metadata]` `Tags` field of the beatmaps selected in the Mapping Tools
shell. It is intentionally small, but still uses the normal Mapping Tools execution, backup, cancellation, and reload
boundaries. QuickRun uses the shell's first selected beatmap and is the only path that requests an automatic osu!
reload.

## Build

From the repository root, run:

```powershell
dotnet build Mapping_Tools.SamplePlugin/Mapping_Tools.SamplePlugin.csproj -c Release
```

The build packages the drop-in artifact at:

```text
Mapping_Tools.SamplePlugin/bin/Release/plugin/Mapping_Tools.SamplePlugin.dll
```

Copy that single DLL into the `Plugins` directory in the Mapping Tools app-data folder
(`%LOCALAPPDATA%\Mapping Tools\Plugins`), then restart Mapping Tools. The folder is created automatically when Mapping
Tools starts. For a Debug build, use the equivalent `bin/Debug/plugin` directory.

Plugin configuration schemas live in the plugin assembly and are exposed from
`IMappingToolDefinition.ConfigSchema`. Implement `IConfigMigration` for each contiguous target version after version 1,
then pass the same schema to the tool's `ProjectDefinition<T>`. The host writes the schema id and version into
`$schema` and `$version`, and runs only migrations belonging to that schema.

The plugin references the host's public `Mapping_Tools.Desktop.Plugin` API and the host's Avalonia/MVVM assemblies.
Build it against the same Mapping Tools source revision as the executable that will load it.

## Shell plugins

Plugins can extend shell menus without defining a mapping tool. Mark a public, parameterless `IShellPlugin`
implementation with `[ShellPlugin]`. Its `RegisterServices` method registers plugin services and singleton
`ShellMenuContribution` factories. The host discovers it in the same `Plugins` folder as tool definitions.
One DLL can contain either kind of entry point or both; a shell-only plugin needs no tool metadata, view,
view model, project configuration, or QuickRun registration.

`SampleShellPlugin.cs` adds a Sample Plugin menu with folder-opening actions. It is independent of
`SampleToolRegistration` and remains available on Get started, Preferences, and every mapping tool,
including while a tool is loading or fails to load.

For example, this is a complete shell-only entry point using an existing shell command:

```csharp
using Mapping_Tools.Desktop.Plugin;
using Mapping_Tools.Desktop.ViewModels;
using Microsoft.Extensions.DependencyInjection;

[ShellPlugin]
public sealed class FolderPlugin : IShellPlugin
{
    public void RegisterServices(IServiceCollection services)
    {
        services.AddSingleton(provider => new ShellMenuContribution(
            ShellMenuLocation.File,
            new ShellMenuItem("Open Mapping Tools folder",
                provider.GetRequiredService<BeatmapWorkspaceViewModel>().OpenApplicationFolderCommand)));
    }
}
```

`ShellMenuLocation.File` and `About` append to the corresponding built-in menu. `MenuBar` appends a new
top-level action or submenu after the built-in menus. Locations do not depend on translated labels.
Assemblies load in filename order, entry points in type-name order, and contributions retain service-registration
order. Multiple plugins can contribute to the same menu.

A `ShellMenuItem` supplies either an `ICommand` action or non-empty `items` for a nested submenu. Optional
Material icons, tooltips, and underscore access keys are supported. Avalonia observes the command's
`CanExecute` and `CanExecuteChanged`, so context-sensitive commands can stay visible and become disabled
when their prerequisites are missing. Use `RelayCommand` or `AsyncRelayCommand` for plugin-owned actions.
Resolve dependencies inside contribution factories after service registration is complete. Singleton services
that implement `IDisposable` or `IAsyncDisposable` are disposed by the host at shutdown.

Plugins can resolve `IBeatmapWorkspace` for selected maps, `IPlatformLauncher` to open folders, files, or URLs,
and `IDialogService` or plugin-owned Avalonia windows for other interfaces. For launching a particular third-party
executable, register a plugin-owned platform adapter. Commands should handle expected failures and report
them through `IUserNotificationService`, as the existing shell commands do.

For localization, pass `translatedHeaderGetter` and `translatedToolTipGetter`, and register plugin resources with
`TranslationManager.RegisterResources`. The host refreshes these getters on language changes. Contributions
are built once for the session; updating a plugin requires a restart. Menu contributions add actions and submenus;
they do not expose the shell's visual tree as a supported mutation API.

The host uses Avalonia's standard [menu behavior](https://docs.avaloniaui.net/controls/menus/menu)
and command handling verified against the [12.1.0 MenuItem source](https://raw.githubusercontent.com/AvaloniaUI/Avalonia/12.1.0/src/Avalonia.Controls/MenuItem.cs).
