namespace Mapping_Tools.ApplicationServices.Platform;

public sealed class FilePickerFilter
{
    public FilePickerFilter(
        string name,
        IEnumerable<string> patterns,
        IEnumerable<string>? mimeTypes = null,
        IEnumerable<string>? appleUniformTypeIdentifiers = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(patterns);

        string[] patternArray = CleanValues(patterns);
        if (patternArray.Length == 0)
        {
            throw new ArgumentException("At least one file pattern is required.", nameof(patterns));
        }

        Name = name.Trim();
        Patterns = patternArray;
        MimeTypes = CleanValues(mimeTypes ?? []);
        AppleUniformTypeIdentifiers = CleanValues(appleUniformTypeIdentifiers ?? []);
    }

    public string Name { get; }

    public IReadOnlyList<string> Patterns { get; }

    public IReadOnlyList<string> MimeTypes { get; }

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
