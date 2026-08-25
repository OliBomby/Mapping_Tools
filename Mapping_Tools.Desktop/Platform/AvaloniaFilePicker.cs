using Avalonia.Platform.Storage;
using Mapping_Tools.Application.Platform.FilePicker;

namespace Mapping_Tools.Desktop.Platform;

/// <summary>
///     Maps portable picker requests to Avalonia storage-provider dialogs and
///     rejects selections that cannot be represented as local filesystem paths.
/// </summary>
public sealed class AvaloniaFilePicker : IFilePicker
{
    private readonly Func<IStorageProvider?> storageProviderAccessor;

    /// <summary>
    ///     Creates an adapter that resolves the storage provider lazily from a top-level window.
    /// </summary>
    /// <param name="storageProviderAccessor">Returns the current storage provider, if initialized.</param>
    public AvaloniaFilePicker(Func<IStorageProvider?> storageProviderAccessor)
    {
        this.storageProviderAccessor = storageProviderAccessor
                                       ?? throw new ArgumentNullException(nameof(storageProviderAccessor));
    }

    /// <summary>
    ///     <inheritdoc />
    public bool CanOpenFiles => storageProviderAccessor()?.CanOpen == true;

    /// <summary>
    ///     <inheritdoc />
    public bool CanSaveFiles => storageProviderAccessor()?.CanSave == true;

    /// <summary>
    ///     <inheritdoc />
    public bool CanPickFolders => storageProviderAccessor()?.CanPickFolder == true;

    /// <summary>
    ///     <inheritdoc />
    public async Task<IReadOnlyList<string>> PickOpenFilesAsync(
        OpenFilePickerRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

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
        return GetLocalPaths(files);
    }

    /// <summary>
    ///     <inheritdoc />
    public async Task<string?> PickSaveFileAsync(
        SaveFilePickerRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

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
        return file is null ? null : GetLocalPath(file);
    }

    /// <summary>
    ///     <inheritdoc />
    public async Task<IReadOnlyList<string>> PickFoldersAsync(
        OpenFolderPickerRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

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
        return GetLocalPaths(folders);
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
