# Mapping Tools sample plugin

Use this project as a starting point for your own plugin. It adds a tool that appends a tag to selected beatmaps,
plus a shell menu with folder-opening actions.

A plugin DLL can contain multiple tools and shell extensions. Mapping Tools discovers their attributed classes
at startup; there is no manifest or central registration list.

## Build and install

Use the .NET 10 SDK and the same Mapping Tools source revision as the host you will run. From the repository root:

```powershell
dotnet build Mapping_Tools.SamplePlugin/Mapping_Tools.SamplePlugin.csproj -c Release
```

The packaged output is in `Mapping_Tools.SamplePlugin/bin/Release/plugin/`.

1. In Mapping Tools, choose **About > Open Mapping Tools folder** and open `Plugins`.
   On Windows, this is `%APPDATA%\Mapping Tools\Plugins`.
2. Close Mapping Tools and copy the packaged output into that folder. Put the plugin DLL directly in `Plugins`,
   keeping the language resource subfolders beside it.
3. Restart Mapping Tools. **Sample Plugin** should appear in both navigation and the menu bar.

Keep the sample's packaging setup: the host supplies Mapping Tools, Avalonia, and MVVM assemblies. Package any
additional dependencies yourself. Updating a plugin requires another restart.

## Make it your own

Copy this project to a sibling directory in the repository, then rename the project, namespace, and AXAML/resource
references together. These files show the main pieces:

| File | Purpose |
| --- | --- |
| [SampleToolRegistration.cs](SampleToolRegistration.cs) | Registers the tool's metadata, view, view model, and services. |
| [SampleToolDefinition.cs](SampleToolDefinition.cs) | Defines its ID, name, search terms, and QuickRun support. |
| [SampleToolViewModel.cs](SampleToolViewModel.cs) and [view](SampleToolView.axaml) | Present settings and run the tool. |
| [SampleToolService.cs](SampleToolService.cs) | Edits beatmaps through the shared backup-aware gateway. |
| [SampleToolProject.cs](SampleToolProject.cs) and [schema](SampleToolConfigSchema.cs) | Save settings and migrate older configurations. |
| [SampleShellExtension.cs](SampleShellExtension.cs) | Adds a menu independently of the tool. |

Each tool needs a public `[MappingToolDefinition]` class implementing `IMappingToolDefinition`, with a public
parameterless constructor. Register dependencies in `RegisterServices`; the host injects them into the tool's
singleton view model. Follow the view-locator naming convention: `YourToolViewModel` maps to `YourToolView`, including
`ViewModels` → `Views` namespace segments. The view also needs a public parameterless constructor.

Give your tool its own tool ID, configuration schema ID, and project filenames. IDs must be unique across loaded
tools and remain stable after release. Use the same schema in the registration and project definition; the sample
shows a migration to version 2.

Project persistence (`IShellProjectFeature<T>`) and QuickRun (`IQuickRun`) are optional. Remove the corresponding
implementation, metadata, and UI when you do not need them. For beatmap edits, follow the sample's
`SingleRunToolViewModel` and `IBeatmapEditingGateway` flow for progress, cancellation, backups, and reload behavior.

## Add a shell menu

A public `[ShellExtension]` class implementing `IShellExtension` can register menus without defining a tool.
It needs a public parameterless constructor and a `RegisterServices` method; see
[SampleShellExtension.cs](SampleShellExtension.cs) for a complete example.

Register each `ShellMenuRegistration` as a singleton factory. It pairs a `ShellMenuLocation` with a `ShellMenuItem`:
`File` and `About` append to existing menus; `MenuBar` adds a top-level action or submenu. Resolve dependencies inside
the factory, after service registration is complete.

A menu item contains either an `ICommand` or child items. Use `RelayCommand` or `AsyncRelayCommand` for actions;
`CanExecute` controls availability. Icons, tooltips, access keys, and translated label getters are supported.
Menus remain available on Get started, Preferences, and every tool screen.

## Check your plugin

After installing, open each tool screen and try its menus across navigation. Test edits on disposable beatmaps,
including backups and cancellation. Check QuickRun, project save/load, undo, and language changes where supported.

If the plugin is missing, check its folder, host revision, attribute, interface, and constructor. Discovery failures
appear in debugger `Trace` output; tool activation logs are in `Logs` alongside `Plugins`. A view-name mismatch or
missing service registration can prevent a discovered tool from opening.
