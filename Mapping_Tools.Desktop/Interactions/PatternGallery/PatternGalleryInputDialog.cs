using Avalonia.Controls;
using Mapping_Tools.Core.Tools.PatternGallery.Models;
using Mapping_Tools.Desktop.Views.Dialogs;

namespace Mapping_Tools.Desktop.Interactions.PatternGallery;

/// <summary>Displays the typed Pattern Gallery import forms as owner-modal Avalonia windows.</summary>
public sealed class PatternGalleryInputDialog : IPatternGalleryInputDialog
{
    private readonly Func<Window> owner;

    /// <summary>Creates the dialog adapter.</summary>
    /// <param name="owner">Returns the initialized shell window.</param>
    public PatternGalleryInputDialog(Func<Window> owner)
    {
        this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
    }

    /// <inheritdoc />
    public async Task<PatternGalleryCodeInput?> ShowCodeAsync(string defaultName)
    {
        var viewModel = PatternGalleryInputViewModel.ForCode(defaultName);
        PatternGalleryInputDialogWindow window = new() { DataContext = viewModel };
        viewModel.Close = value => window.Close(value);
        object? result = await window.ShowDialog<object?>(owner());
        return result is PatternGalleryCodeInput input ? input : null;
    }

    /// <inheritdoc />
    public async Task<PatternGalleryFileInput?> ShowFileAsync(string defaultName, string defaultPath)
    {
        var viewModel = PatternGalleryInputViewModel.ForFile(defaultName, defaultPath);
        PatternGalleryInputDialogWindow window = new() { DataContext = viewModel };
        viewModel.Close = value => window.Close(value);
        object? result = await window.ShowDialog<object?>(owner());
        return result is PatternGalleryFileInput input ? input : null;
    }

    /// <inheritdoc />
    public async Task<string?> ShowDetailsAsync(PatternGalleryPattern pattern)
    {
        ArgumentNullException.ThrowIfNull(pattern);
        PatternGalleryDetailsViewModel viewModel = new(pattern);
        PatternGalleryDetailsDialogWindow window = new() { DataContext = viewModel };
        viewModel.Close = value => window.Close(value);
        object? result = await window.ShowDialog<object?>(owner());
        return result as string;
    }
}

