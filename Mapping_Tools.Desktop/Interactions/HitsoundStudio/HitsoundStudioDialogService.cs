using Mapping_Tools.Application.Platform.FilePicker;
using Mapping_Tools.Application.Tools.HitsoundStudio.Models;
using Mapping_Tools.Desktop.Platform;
using Mapping_Tools.Desktop.Views.Dialogs;
using DesktopHitsoundStudioProject = Mapping_Tools.Desktop.Models.HitsoundStudioProject;

namespace Mapping_Tools.Desktop.Interactions.HitsoundStudio;

/// <summary>Shows the Hitsound Studio forms in the shell DialogHost.</summary>
public sealed class HitsoundStudioDialogService : IHitsoundStudioDialogService
{
    private readonly IFilePicker filePicker;

    /// <summary>Creates the dialog adapter.</summary>
    /// <param name="filePicker">Presents the native file and folder pickers used by the forms.</param>
    public HitsoundStudioDialogService(IFilePicker filePicker)
    {
        this.filePicker = filePicker ?? throw new ArgumentNullException(nameof(filePicker));
    }

    /// <inheritdoc />
    public async Task<HitsoundStudioImportRequest?> ShowImportAsync(
        string defaultName,
        CancellationToken cancellationToken = default)
    {
        HitsoundStudioImportDialogViewModel viewModel = new(defaultName, filePicker);
        HitsoundStudioImportDialog dialog = new() { DataContext = viewModel };
        viewModel.Close = value => DialogHostInteraction.Close(
            DialogHostInteraction.RootIdentifier,
            value);
        object? result = await DialogHostInteraction.ShowAsync(
            dialog,
            DialogHostInteraction.RootIdentifier,
            cancellationToken).ConfigureAwait(false);
        return result as HitsoundStudioImportRequest;
    }

    /// <inheritdoc />
    public async Task<DesktopHitsoundStudioProject?> ShowExportAsync(
        DesktopHitsoundStudioProject project,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(project);
        HitsoundStudioExportDialogViewModel viewModel = new(project, filePicker);
        HitsoundStudioExportDialog dialog = new() { DataContext = viewModel };
        viewModel.Close = value => DialogHostInteraction.Close(
            DialogHostInteraction.RootIdentifier,
            value);
        object? result = await DialogHostInteraction.ShowAsync(
            dialog,
            DialogHostInteraction.RootIdentifier,
            cancellationToken).ConfigureAwait(false);
        return result as DesktopHitsoundStudioProject;
    }
}
