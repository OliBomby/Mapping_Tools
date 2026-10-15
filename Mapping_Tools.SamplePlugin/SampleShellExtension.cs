using Mapping_Tools.Desktop.Localization;
using Mapping_Tools.Desktop.Plugin;
using Mapping_Tools.Desktop.ViewModels;
using Material.Icons;
using Microsoft.Extensions.DependencyInjection;

namespace Mapping_Tools.SamplePlugin;

/// <summary>Demonstrates an always-available shell menu, independently of the sample mapping tool.</summary>
[ShellExtension]
public sealed class SampleShellExtension : IShellExtension
{
    /// <inheritdoc />
    public void RegisterServices(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddSingleton(provider =>
        {
            var workspace = provider.GetRequiredService<BeatmapWorkspaceViewModel>();
            return new ShellMenuRegistration(ShellMenuLocation.MenuBar, new ShellMenuItem(
                "_Sample Plugin",
                items:
                [
                    new ShellMenuItem("Open Mapping Tools folder", workspace.OpenApplicationFolderCommand,
                        icon: MaterialIconKind.FolderOpen,
                        translatedHeaderGetter: static () => DesktopStrings.Shell_MenuOpenMappingToolsFolder),
                    new ShellMenuItem("Open backups folder", workspace.OpenBackupsFolderCommand,
                        icon: MaterialIconKind.FolderOpen,
                        translatedHeaderGetter: static () => DesktopStrings.Shell_MenuOpenBackupsFolder),
                ]));
        });
    }
}
