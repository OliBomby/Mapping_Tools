using Avalonia.Platform.Storage;
using Mapping_Tools.Application.Platform.FilePicker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Mapping_Tools.Desktop.Services.Platform;

/// <summary>
///     Maps portable picker requests to Avalonia storage-provider dialogs and
///     rejects selections that cannot be represented as local filesystem paths.
/// </summary>
public sealed class AvaloniaFilePicker : IFilePicker
{
    private readonly Func<IStorageProvider?> storageProviderAccessor;
    private readonly ILogger<AvaloniaFilePicker> logger;

    /// <summary>
    ///     Creates an adapter that resolves the storage provider lazily from a top-level window.
    /// </summary>
    /// <param name="storageProviderAccessor">Returns the current storage provider, if initialized.</param>
    /// <param name="logger">Records picker requests and selected paths.</param>
    public AvaloniaFilePicker(Func<IStorageProvider?> storageProviderAccessor, ILogger<AvaloniaFilePicker>? logger = null)
    {
        this.storageProviderAccessor = storageProviderAccessor
                                       ?? throw new ArgumentNullException(nameof(storageProviderAccessor));
        this.logger = logger ?? NullLogger<AvaloniaFilePicker>.Instance;
    }

    /// <inheritdoc />
    public bool CanOpenFiles => storageProviderAccessor()?.CanOpen == true;

    /// <inheritdoc />
    public bool CanSaveFiles => storageProviderAccessor()?.CanSave == true;

    /// <inheritdoc />
    public bool CanPickFolders => storageProviderAccessor()?.CanPickFolder == true;

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> PickOpenFilesAsync(
        OpenFilePickerRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        logger.LogInformation("Open file picker: {Title}; start {Start}; multiple {Multiple}", request.Title, request.SuggestedStartLocation, request.AllowMultiple);
        var provider = GetProvider(provider => provider.CanOpen, "open files");
        var startLocation = await GetStartLocationAsync(
            provider,
            request.SuggestedStartLocation,
            cancellationToken);

        var files = await provider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = request.Title,
            AllowMultiple = request.AllowMultiple,
            SuggestedStartLocation = startLocation,
            FileTypeFilter = MapFilters(request.Filters),
        });

        cancellationToken.ThrowIfCancellationRequested();
        var paths = GetLocalPaths(files);
        logger.LogInformation("Open file picker {Title} returned {Count} paths: {Paths}", request.Title, paths.Count, string.Join(" | ", paths));
        return paths;
    }

    /// <inheritdoc />
    public async Task<string?> PickSaveFileAsync(
        SaveFilePickerRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        logger.LogInformation("Save file picker: {Title}; start {Start}; suggested {Name}", request.Title, request.SuggestedStartLocation, request.SuggestedFileName);
        var provider = GetProvider(provider => provider.CanSave, "save files");
        var startLocation = await GetStartLocationAsync(
            provider,
            request.SuggestedStartLocation,
            cancellationToken);

        var file = await provider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = request.Title,
            SuggestedStartLocation = startLocation,
            SuggestedFileName = request.SuggestedFileName,
            DefaultExtension = request.DefaultExtension,
            ShowOverwritePrompt = request.ShowOverwritePrompt,
            FileTypeChoices = MapFilters(request.Filters),
        });

        cancellationToken.ThrowIfCancellationRequested();
        string? path = file is null ? null : GetLocalPath(file);
        logger.LogInformation("Save file picker {Title} returned {Path}", request.Title, path);
        return path;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> PickFoldersAsync(
        OpenFolderPickerRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        logger.LogInformation("Folder picker: {Title}; start {Start}; multiple {Multiple}", request.Title, request.SuggestedStartLocation, request.AllowMultiple);
        var provider = GetProvider(provider => provider.CanPickFolder, "pick folders");
        var startLocation = await GetStartLocationAsync(
            provider,
            request.SuggestedStartLocation,
            cancellationToken);

        var folders = await provider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = request.Title,
            AllowMultiple = request.AllowMultiple,
            SuggestedStartLocation = startLocation,
        });

        cancellationToken.ThrowIfCancellationRequested();
        var paths = GetLocalPaths(folders);
        logger.LogInformation("Folder picker {Title} returned {Count} paths: {Paths}", request.Title, paths.Count, string.Join(" | ", paths));
        return paths;
    }

    internal static IReadOnlyList<FilePickerFileType> MapFilters(IReadOnlyList<FilePickerFilter> filters)
    {
        ArgumentNullException.ThrowIfNull(filters);

        return filters
            .Select(filter => new FilePickerFileType(filter.Name)
            {
                Patterns = filter.Patterns,
                MimeTypes = filter.MimeTypes,
                AppleUniformTypeIdentifiers = filter.AppleUniformTypeIdentifiers,
            })
            .ToArray();
    }

    private IStorageProvider GetProvider(Func<IStorageProvider, bool> capability, string operation)
    {
        var provider = storageProviderAccessor();
        if (provider is null || !capability(provider))
            throw new PlatformNotSupportedException(
                $"The current platform does not support the ability to {operation}.");

        return provider;
    }

    private static async Task<IStorageFolder?> GetStartLocationAsync(
        IStorageProvider provider,
        string? path,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(path)) return null;

        cancellationToken.ThrowIfCancellationRequested();
        var folder = await provider.TryGetFolderFromPathAsync(path);
        cancellationToken.ThrowIfCancellationRequested();
        return folder;
    }

    private static IReadOnlyList<string> GetLocalPaths<T>(IReadOnlyList<T> items)
        where T : IStorageItem
    {
        return items.Select(item => GetLocalPath(item)).ToArray();
    }

    private static string GetLocalPath(IStorageItem item)
    {
        return item.TryGetLocalPath()
               ?? throw new IOException(
                   $"The selected storage item '{item.Name}' does not expose a local filesystem path.");
    }
}
