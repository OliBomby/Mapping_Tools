using CommunityToolkit.Mvvm.ComponentModel;
using Mapping_Tools.Application.Localization;

namespace Mapping_Tools.Desktop.Localization;

/// <summary>Refreshes presentation getters when text language changes without retaining abandoned view models.</summary>
public abstract class LocalizedObservableObject : ObservableObject
{
    /// <summary>Registers a weak language-change observer for this presentation model.</summary>
    protected LocalizedObservableObject()
    {
        var reference = new WeakReference<LocalizedObservableObject>(this);
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
        OnPropertyChanged(string.Empty);
    }
}
