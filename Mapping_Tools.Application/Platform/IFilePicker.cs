namespace Mapping_Tools.ApplicationServices.Platform;

public interface IFilePicker
{
    bool CanOpenFiles { get; }

    bool CanSaveFiles { get; }

    bool CanPickFolders { get; }

    Task<IReadOnlyList<string>> PickOpenFilesAsync(
        OpenFilePickerRequest request,
        CancellationToken cancellationToken = default);

    Task<string?> PickSaveFileAsync(
        SaveFilePickerRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> PickFoldersAsync(
        OpenFolderPickerRequest request,
        CancellationToken cancellationToken = default);
}
