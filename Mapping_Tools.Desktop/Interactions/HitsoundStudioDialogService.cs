using System.Globalization;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mapping_Tools.Application.Platform;
using Mapping_Tools.Application.Tools.HitsoundStudio;
using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.HitsoundStuff;
using Mapping_Tools.Desktop.Views.Dialogs;

namespace Mapping_Tools.Desktop.Interactions;

/// <summary>Shows the Hitsound Studio import and export forms as owner-modal windows.</summary>
public sealed class HitsoundStudioDialogService : IHitsoundStudioDialogService
{
    private readonly IFilePicker filePicker;
    private readonly Func<Window> owner;

    /// <summary>Creates the dialog adapter.</summary>
    /// <param name="owner">Returns the active shell window.</param>
    public HitsoundStudioDialogService(Func<Window> owner, IFilePicker filePicker)
    {
        this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
        this.filePicker = filePicker ?? throw new ArgumentNullException(nameof(filePicker));
    }

    /// <inheritdoc />
    public async Task<HitsoundStudioImportRequest?> ShowImportAsync(
        string defaultName,
        CancellationToken cancellationToken = default)
    {
        HitsoundStudioImportDialogViewModel viewModel = new(defaultName, filePicker);
        HitsoundStudioImportDialogWindow window = new() { DataContext = viewModel };
        viewModel.Close = value => window.Close(value);
        object? result = await window.ShowDialog<object?>(owner()).WaitAsync(cancellationToken).ConfigureAwait(false);
        return result as HitsoundStudioImportRequest;
    }

    /// <inheritdoc />
    public async Task<HitsoundStudioProject?> ShowExportAsync(
        HitsoundStudioProject project,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(project);
        HitsoundStudioExportDialogViewModel viewModel = new(project, filePicker);
        HitsoundStudioExportDialogWindow window = new() { DataContext = viewModel };
        viewModel.Close = value => window.Close(value);
        object? result = await window.ShowDialog<object?>(owner()).WaitAsync(cancellationToken).ConfigureAwait(false);
        return result as HitsoundStudioProject;
    }
}

