using Mapping_Tools.Core.Tools.PatternGallery.Models;
using Mapping_Tools.Desktop.Platform;
using Mapping_Tools.Desktop.Views.Dialogs;
using PatternGalleryInputDialogView = Mapping_Tools.Desktop.Views.Dialogs.PatternGalleryInputDialog;

namespace Mapping_Tools.Desktop.Interactions.PatternGallery;

/// <summary>Displays the typed Pattern Gallery forms in the shell DialogHost.</summary>
public sealed class PatternGalleryInputDialog : IPatternGalleryInputDialog
{
    /// <summary>Creates the dialog adapter.</summary>
    public PatternGalleryInputDialog()
    {
    }

    /// <inheritdoc />
    public async Task<PatternGalleryCodeInput?> ShowCodeAsync(string defaultName)
    {
        var viewModel = PatternGalleryInputViewModel.ForCode(defaultName);
        PatternGalleryInputDialogView dialog = new() { DataContext = viewModel };
        viewModel.Close = value => DialogHostInteraction.Close(
            DialogHostInteraction.RootIdentifier,
            value);
        object? result = await DialogHostInteraction.ShowAsync(
            dialog,
            DialogHostInteraction.RootIdentifier);
        return result is PatternGalleryCodeInput input ? input : null;
    }

    /// <inheritdoc />
    public async Task<PatternGalleryFileInput?> ShowFileAsync(string defaultName, string defaultPath)
    {
        var viewModel = PatternGalleryInputViewModel.ForFile(defaultName, defaultPath);
        PatternGalleryInputDialogView dialog = new() { DataContext = viewModel };
        viewModel.Close = value => DialogHostInteraction.Close(
            DialogHostInteraction.RootIdentifier,
            value);
        object? result = await DialogHostInteraction.ShowAsync(
            dialog,
            DialogHostInteraction.RootIdentifier);
        return result is PatternGalleryFileInput input ? input : null;
    }

    /// <inheritdoc />
    public async Task<string?> ShowDetailsAsync(PatternGalleryPattern pattern)
    {
        ArgumentNullException.ThrowIfNull(pattern);
        PatternGalleryDetailsViewModel viewModel = new(pattern);
        PatternGalleryDetailsDialog dialog = new() { DataContext = viewModel };
        viewModel.Close = value => DialogHostInteraction.Close(
            DialogHostInteraction.RootIdentifier,
            value);
        object? result = await DialogHostInteraction.ShowAsync(
            dialog,
            DialogHostInteraction.RootIdentifier);
        return result as string;
    }
}
