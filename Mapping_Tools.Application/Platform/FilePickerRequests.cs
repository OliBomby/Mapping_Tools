namespace Mapping_Tools.ApplicationServices.Platform;

public sealed record OpenFilePickerRequest
{
    public string? Title { get; init; }

    public string? SuggestedStartLocation { get; init; }

    public bool AllowMultiple { get; init; }

    public IReadOnlyList<FilePickerFilter> Filters { get; init; } = [];
}

public sealed record SaveFilePickerRequest
{
    public string? Title { get; init; }

    public string? SuggestedStartLocation { get; init; }

    public string? SuggestedFileName { get; init; }

    public string? DefaultExtension { get; init; }

    public bool ShowOverwritePrompt { get; init; } = true;

    public IReadOnlyList<FilePickerFilter> Filters { get; init; } = [];
}

public sealed record OpenFolderPickerRequest
{
    public string? Title { get; init; }

    public string? SuggestedStartLocation { get; init; }

    public bool AllowMultiple { get; init; }
}
