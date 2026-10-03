using System.ComponentModel;

namespace Mapping_Tools.Desktop.Localization;

/// <summary>Represents a selectable UI language, displaying each explicit language's native name.</summary>
public sealed class LanguageOption : INotifyPropertyChanged
{
    /// <summary>Creates a language choice.</summary>
    /// <param name="code">The resource culture code, or null for the system default.</param>
    /// <param name="nativeName">The native language name; ignored for the system default.</param>
    public LanguageOption(string? code, string nativeName)
    {
        Code = code;
        NativeName = nativeName;
    }

    /// <summary>Gets the persisted language code; null follows the system.</summary>
    public string? Code { get; }

    /// <summary>Gets this language's native name.</summary>
    public string NativeName { get; }

    /// <summary>Gets the display name, translating only the system-default option.</summary>
    public string Name => Code is null ? DesktopStrings.Shell_SystemDefault : NativeName;

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    internal void Refresh() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Name)));
}
