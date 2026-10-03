using System.Globalization;

namespace Mapping_Tools.Application.Localization;

/// <summary>Chooses the language used for text resources without changing formatting or parsing cultures.</summary>
public static class TranslationManager
{
    private static readonly CultureInfo systemCulture = CultureInfo.CurrentUICulture;
    private static string? language;
    private static event Action<CultureInfo>? resourceCultureChanged;

    static TranslationManager()
    {
        ApplicationStrings.Culture = Culture;
    }

    /// <summary>Raised after the selected text language changes.</summary>
    public static event EventHandler? LanguageChanged;

    /// <summary>Gets the explicit language code, or null to follow the startup system UI language.</summary>
    public static string? Language => language;

    /// <summary>Gets the culture used exclusively for translation lookup.</summary>
    public static CultureInfo Culture { get; private set; } = systemCulture;

    /// <summary>Connects a generated resource class's Culture setter to text-language selection.</summary>
    /// <param name="applyCulture">Sets the generated resource class's Culture property. Register once before using that class.</param>
    /// <remarks>The current culture is applied immediately, and subsequent updates run before language-change notifications.</remarks>
    public static void RegisterResources(Action<CultureInfo> applyCulture)
    {
        ArgumentNullException.ThrowIfNull(applyCulture);
        resourceCultureChanged += applyCulture;
        applyCulture(Culture);
    }

    /// <summary>Selects a text language. Unsupported or invalid codes use English; null follows the system.</summary>
    /// <param name="code">A supported language code such as en, nl, ru, zh-Hans, zh-Hant, or ja, or null for the system default.</param>
    public static void SetLanguage(string? code)
    {
        string? normalized = string.IsNullOrWhiteSpace(code) ? null : Normalize(code);
        CultureInfo culture = normalized is null ? systemCulture : CultureInfo.GetCultureInfo(normalized);
        if (language == normalized && Culture.Equals(culture)) return;

        language = normalized;
        Culture = culture;
        ApplicationStrings.Culture = culture;
        resourceCultureChanged?.Invoke(culture);
        LanguageChanged?.Invoke(null, EventArgs.Empty);
    }

    private static string Normalize(string code)
    {
        try
        {
            var culture = CultureInfo.GetCultureInfo(code);
            string languageCode = culture.TwoLetterISOLanguageName;
            if (languageCode == "zh")
            {
                return culture.Name.Contains("Hant", StringComparison.OrdinalIgnoreCase)
                    || culture.Name is "zh-TW" or "zh-HK" or "zh-MO"
                    ? "zh-Hant"
                    : "zh-Hans";
            }

            for (CultureInfo candidate = culture;
                 !candidate.Equals(CultureInfo.InvariantCulture);
                 candidate = candidate.Parent)
            {
                if (ApplicationStrings.ResourceManager.GetResourceSet(candidate, true, false) is not null)
                    return candidate.Name;
            }

            return "en";
        }
        catch (CultureNotFoundException)
        {
            return "en";
        }
    }
}
