using CommunityToolkit.Mvvm.ComponentModel;
using Mapping_Tools.Application.Localization;

namespace Mapping_Tools.Desktop.Localization;

/// <summary>Refreshes translated presentation getters while retaining typed editable state and validation rules.</summary>
public abstract class LocalizedObservableValidator : ObservableValidator
{
    /// <summary>Registers a weak language-change observer for this validating presentation model.</summary>
    protected LocalizedObservableValidator()
    {
        var reference = new WeakReference<LocalizedObservableValidator>(this);
        EventHandler? handler = null;
        handler = (_, _) =>
        {
            if (reference.TryGetTarget(out var target)) target.RefreshLocalizedProperties();
            else TranslationManager.LanguageChanged -= handler;
        };
        TranslationManager.LanguageChanged += handler;
    }

    /// <summary>Refreshes translated getters; derived models may also rebuild cached presentation choices.</summary>
    protected virtual void RefreshLocalizedProperties()
    {
        // Revalidate only existing errors so a language change does not introduce untouched-field errors.
        foreach (var property in GetType().GetProperties())
        {
            if (property.GetIndexParameters().Length == 0 && GetErrors(property.Name).Any())
                ValidateProperty(property.GetValue(this), property.Name);
        }

        OnPropertyChanged(string.Empty);
    }
}
