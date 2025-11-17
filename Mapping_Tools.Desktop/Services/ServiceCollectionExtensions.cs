using Mapping_Tools.Application.Persistence;
using Mapping_Tools.Desktop.ViewModels;
using Mapping_Tools.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Mapping_Tools.Desktop.Services;

public static class ServiceCollectionExtensions {
    public static IServiceCollection AddInfrastructure(this IServiceCollection s, IConfiguration cfg) {
        // concrete repos, EF/LiteDB/HTTP, file stores, logging sinks, etc.
        // e.g., s.AddScoped<IOrderRepository, SqlOrderRepository>();
        //       s.AddSingleton<IUiStateStore, JsonUiStateStore>();
        s.AddSingleton<IStateStore, JsonStateStore>();
        return s;
    }

    public static IServiceCollection AddPresentation(this IServiceCollection s) {
        // Avalonia VM/View registrations, navigation, dialogs

        // if (OperatingSystem.IsWindows()) {
        //     s.AddSingleton<IFileDialogService, FileDialogServiceWin>();
        // } else if (OperatingSystem.IsLinux()) {
        //     s.AddSingleton<IFileDialogService, FileDialogServiceLinux>();
        // } else if (OperatingSystem.IsMacOS()) {
        //     s.AddSingleton<IFileDialogService, FileDialogServiceMac>();
        // }

        s.AddHostedService<UpdateChecker>();

        s.AddSingleton<NavigationService>();
        s.AddSingleton<MainWindowViewModel>();
        
        s.AddTransient<HomeViewModel>();
        s.AddTransient<SettingsViewModel>();

        return s;
    }
}