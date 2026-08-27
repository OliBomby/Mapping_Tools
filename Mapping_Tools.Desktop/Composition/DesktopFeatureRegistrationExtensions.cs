using Avalonia.Controls.Primitives;
using CommunityToolkit.Mvvm.ComponentModel;
using Mapping_Tools.Desktop.Hosting;
using Mapping_Tools.Desktop.Shell;
using Mapping_Tools.Desktop.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using GetStartedViewModel = Mapping_Tools.Desktop.ViewModels.GetStarted.GetStartedViewModel;

namespace Mapping_Tools.Desktop.Composition;

internal static class DesktopFeatureRegistrationExtensions
{
    internal static IServiceCollection AddDesktopFeatures(
        this IServiceCollection services,
        IEnumerable<System.Reflection.Assembly> toolAssemblies)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(toolAssemblies);

        services.AddShellFeature<GetStartedViewModel>(
            "get-started",
            "Get started",
            "Home",
            "Onboarding, bundled changelog, support links, and recent beatmaps.",
            ["home", "help", "changelog", "recent", "faq"]);
        services.AddShellFeature<PreferencesViewModel>(
            "preferences",
            "Preferences",
            "Application",
            "Paths, backup policy, Editor Reader, and application theme.",
            ["settings", "paths", "backups", "editor reader", "theme"],
            horizontalScrollBarVisibility: ScrollBarVisibility.Auto,
            verticalScrollBarVisibility: ScrollBarVisibility.Auto);

        var catalog = ToolDefinitionCatalog.Discover(toolAssemblies);
        catalog.RegisterServices(services);
        services.AddSingleton(catalog);

        services.AddSingleton<IShellFeatureRegistry>(provider =>
            new ShellFeatureRegistry(
                provider.GetServices<ShellFeatureRegistration>()));

        return services;
    }

    private static IServiceCollection AddShellFeature<TViewModel>(
        this IServiceCollection services,
        string id,
        string displayName,
        string category,
        string description,
        IEnumerable<string> searchTerms,
        bool startsSection = false,
        ScrollBarVisibility horizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
        ScrollBarVisibility verticalScrollBarVisibility = ScrollBarVisibility.Disabled)
        where TViewModel : ObservableObject
    {
        services.AddSingleton<TViewModel>();
        services.AddSingleton(provider => new ShellFeatureRegistration(
            id,
            displayName,
            category,
            description,
            searchTerms,
            provider.GetRequiredService<TViewModel>,
            startsSection,
            horizontalScrollBarVisibility,
            verticalScrollBarVisibility));

        return services;
    }
}
