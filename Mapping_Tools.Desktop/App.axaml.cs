using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Mapping_Tools.Desktop.ViewModels;
using Mapping_Tools.Desktop.Views;
using Microsoft.Extensions.DependencyInjection;

namespace Mapping_Tools.Desktop;

public partial class App : Application {
    public override void Initialize() {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted() {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop) {
            desktop.MainWindow = new MainWindow {
                DataContext = Program.AppHost.Services.GetRequiredService<MainWindowViewModel>(),
            };
        } else if (ApplicationLifetime is ISingleViewApplicationLifetime singleView) {
            // Mobile/Web: set the *MainView* instead of a window.
            singleView.MainView = new MainWindow {
                DataContext = Program.AppHost.Services.GetRequiredService<MainWindowViewModel>(),
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}