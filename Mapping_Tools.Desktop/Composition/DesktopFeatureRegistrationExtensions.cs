using System.Reflection;
using Avalonia.Controls.Primitives;
using CommunityToolkit.Mvvm.ComponentModel;
using Mapping_Tools.Desktop.Shell;
using Mapping_Tools.Desktop.ViewModels;
using Mapping_Tools.Desktop.ViewModels.GetStarted;
using Microsoft.Extensions.DependencyInjection;

using Mapping_Tools.Desktop.Localization;

namespace Mapping_Tools.Desktop.Composition;

internal static class DesktopFeatureRegistrationExtensions
{
    internal static IServiceCollection AddDesktopFeatures(
        this IServiceCollection services,
        IEnumerable<Assembly> toolAssemblies)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(toolAssemblies);

        services.AddShellFeature<GetStartedViewModel>(
            "get-started",
            "Get started",
            "Home",
            "Onboarding, GitHub release notes, support links, and recent beatmaps.",
            ["home", "help", "changelog", "recent", "faq"],
            translatedDisplayNameGetter: static () => DesktopStrings.Shell_GetStarted,
            translatedDescriptionGetter: static () => DesktopStrings.Shell_HomeDescription,
            translatedSearchTermsGetter: static () => DesktopStrings.Shell_HomeSearchTerms);
        services.AddShellFeature<PreferencesViewModel>(
            "preferences",
            "Preferences",
            "Application",
            "Paths, backup policy, Editor Reader, and application theme.",
            ["settings", "paths", "backups", "editor reader", "theme"],
            ScrollBarVisibility.Auto,
            ScrollBarVisibility.Auto,
            translatedDisplayNameGetter: static () => DesktopStrings.Shell_Preferences,
            translatedDescriptionGetter: static () => DesktopStrings.Shell_PreferencesDescription,
            translatedSearchTermsGetter: static () => DesktopStrings.Shell_PreferencesSearchTerms);

        var catalog = ToolDefinitionCatalog.Discover(toolAssemblies);
        catalog.RegisterServices(services);
        services.AddSingleton(catalog);

        services.AddSingleton<IShellFeatureRegistry>(provider =>
            new ShellFeatureRegistry(
                provider.GetServices<ShellFeatureRegistration>()));

        return services;
    }

    private static void AddShellFeature<TViewModel>(this IServiceCollection services,
        string id,
        string displayName,
        string category,
        string description,
        IEnumerable<string> searchTerms,
        ScrollBarVisibility horizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
        ScrollBarVisibility verticalScrollBarVisibility = ScrollBarVisibility.Disabled,
        Func<string>? translatedDisplayNameGetter = null,
        Func<string>? translatedDescriptionGetter = null,
        Func<string>? translatedSearchTermsGetter = null)
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
            horizontalScrollBarVisibility,
            verticalScrollBarVisibility,
            translatedDisplayNameGetter: translatedDisplayNameGetter,
            translatedDescriptionGetter: translatedDescriptionGetter,
            translatedSearchTermsGetter: translatedSearchTermsGetter));
    }
}
