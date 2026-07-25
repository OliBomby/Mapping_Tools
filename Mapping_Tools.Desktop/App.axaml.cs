using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Mapping_Tools.Desktop.Platform;
using Mapping_Tools.Desktop.ViewModels;
using Mapping_Tools.Desktop.Views;
using Mapping_Tools.Infrastructure.Files;
using Mapping_Tools.Infrastructure.Platform;

namespace Mapping_Tools.Desktop;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            MainWindow mainWindow = new()
            {
                DataContext = new MainViewModel(),
            };

            PlatformServices = new DesktopPlatformServices(
                new AvaloniaFilePicker(() => mainWindow.StorageProvider),
                new AvaloniaClipboardService(() => mainWindow.Clipboard),
                new AvaloniaPlatformLauncher(() => mainWindow.Launcher),
                new WindowsFileRevealService(),
                new ApplicationDirectories());

            desktop.MainWindow = mainWindow;
        }

        base.OnFrameworkInitializationCompleted();
    }

    public DesktopPlatformServices? PlatformServices { get; private set; }
}
