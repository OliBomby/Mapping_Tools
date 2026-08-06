using CommunityToolkit.Mvvm.ComponentModel;
using Mapping_Tools.Application.QuickRun;
using Mapping_Tools.Desktop.Hosting;
using Mapping_Tools.Desktop.Shell;
using Mapping_Tools.Desktop.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace Mapping_Tools.Desktop.Composition;

internal static class DesktopFeatureRegistrationExtensions
{
    public static IServiceCollection AddDesktopFeatures(this IServiceCollection services)
    {
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
            ["settings", "paths", "backups", "editor reader", "theme"]);
        services.AddMappingTool<RhythmGuideViewModel>(
            "rhythm-guide",
            "Rhythm Guide",
            "Make a beatmap with circles from the rhythm of multiple maps.",
            ["rhythm", "hitsound", "guide", "reference"],
            startsSection: true);
        services.AddMappingTool<AutoFailDetectorViewModel>(
            "auto-fail-detector",
            "Auto-fail Detector",
            "Detect incorrect object loading in overlapping patterns.",
            ["auto fail", "2b", "unloading", "objects"],
            quickRunTargets: QuickRunTargets.Always,
            quickRun: static (viewModel, cancellationToken) =>
                viewModel.RunQuickAsync(cancellationToken));
        services.AddMappingTool<MapCleanerViewModel>(
            "map-cleaner",
            "Map Cleaner",
            "Rebuild useful greenlines and optionally resnap map content.",
            ["clean", "greenline", "resnap", "samples"],
            quickRunTargets: QuickRunTargets.Always,
            quickRun: static (viewModel, cancellationToken) =>
                viewModel.RunQuickAsync(cancellationToken));

        services.AddSingleton<IShellFeatureRegistry>(provider =>
            new ShellFeatureRegistry(
                provider.GetServices<ShellFeatureRegistration>()));

        return services;
    }

    public static IServiceCollection AddShellFeature<TViewModel>(
        this IServiceCollection services,
        string id,
        string displayName,
        string category,
        string description,
        IEnumerable<string> searchTerms,
        bool startsSection = false)
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
            startsSection));

        return services;
    }

    public static IServiceCollection AddMappingTool<TViewModel>(
        this IServiceCollection services,
        string id,
        string displayName,
        string description,
        IEnumerable<string> searchTerms,
        bool startsSection = false,
        QuickRunTargets? quickRunTargets = null,
        Func<TViewModel, CancellationToken, Task>? quickRun = null)
        where TViewModel : ObservableObject
    {
        services.AddShellFeature<TViewModel>(
            id,
            displayName,
            "Tools",
            description,
            searchTerms,
            startsSection);

        if (quickRunTargets is not null && quickRun is not null)
        {
            services.AddSingleton(provider => new MappingToolQuickRunRegistration(
                id,
                displayName,
                quickRunTargets.Value,
                cancellationToken => quickRun(
                    provider.GetRequiredService<TViewModel>(),
                    cancellationToken)));
        }

        return services;
    }
}
