namespace Mapping_Tools.Application.Platform.FilePicker;

/// <summary>
///     Describes one portable native-file-dialog filter using the metadata formats
///     required by Windows/Linux patterns, MIME-aware platforms, and macOS.
/// </summary>
public sealed class FilePickerFilter
{
    /// <summary>
    ///     Creates a normalized, case-insensitive file filter.
    /// </summary>
    /// <param name="name">The label shown by the native picker.</param>
    /// <param name="patterns">Filename patterns such as <c>*.osu</c>.</param>
    /// <param name="mimeTypes">Optional MIME types for platforms that support them.</param>
    /// <param name="appleUniformTypeIdentifiers">Optional Apple uniform type identifiers.</param>
    /// <exception cref="ArgumentException">The name is blank or no non-blank pattern is supplied.</exception>
    public FilePickerFilter(
        string name,
        IEnumerable<string> patterns,
        IEnumerable<string>? mimeTypes = null,
        IEnumerable<string>? appleUniformTypeIdentifiers = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(patterns);

        string[] patternArray = CleanValues(patterns);
        if (patternArray.Length == 0) throw new ArgumentException("At least one file pattern is required.", nameof(patterns));

        Name = name.Trim();
        Patterns = patternArray;
        MimeTypes = CleanValues(mimeTypes ?? []);
        AppleUniformTypeIdentifiers = CleanValues(appleUniformTypeIdentifiers ?? []);
    }

    /// <summary>
    ///     Gets the trimmed display label.
    /// </summary>
    public string Name { get; }

    /// <summary>
    ///     Gets distinct, trimmed filename patterns.
    /// </summary>
    public IReadOnlyList<string> Patterns { get; }

    /// <summary>
    ///     Gets distinct, trimmed MIME types.
    /// </summary>
    public IReadOnlyList<string> MimeTypes { get; }

    /// <summary>
    ///     Gets distinct, trimmed Apple uniform type identifiers.
    /// </summary>
    public IReadOnlyList<string> AppleUniformTypeIdentifiers { get; }

    private static string[] CleanValues(IEnumerable<string> values)
    {
        return values
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
