using System.ComponentModel;
using System.Runtime.CompilerServices;
using Mapping_Tools.Application.Localization;

namespace Mapping_Tools.Desktop.Localization;

/// <summary>Notifies existing text bindings after all generated resources switch to the selected language.</summary>
public sealed class LocalizationState : INotifyPropertyChanged
{
    private LocalizationState()
    {
        TranslationManager.RegisterResources(static culture => DesktopStrings.Culture = culture);
        TranslationManager.LanguageChanged += (_, _) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LanguageVersion)));
    }

    [ModuleInitializer]
    internal static void InitializeResourceCulture()
    {
        _ = Instance;
    }

    /// <summary>Gets the process-lifetime language notification source for live text bindings.</summary>
    public static LocalizationState Instance { get; } = new();

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>Gets the selected text culture name observed by translation bindings.</summary>
    public string LanguageVersion => TranslationManager.Culture.Name;
}
