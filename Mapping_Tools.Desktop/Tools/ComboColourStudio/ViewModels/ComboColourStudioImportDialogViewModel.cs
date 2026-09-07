using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mapping_Tools.Application.Platform.FilePicker;
using Mapping_Tools.Application.Workspace.Contracts;

namespace Mapping_Tools.Desktop.Tools.ComboColourStudio.ViewModels;

/// <summary>Owns the path and actions for the Combo Colour Studio beatmap import dialog.</summary>
public sealed partial class ComboColourStudioImportDialogViewModel : ObservableObject
{
    private readonly ICurrentBeatmapLocator currentBeatmap;
    private readonly IFilePicker filePicker;

    /// <summary>Creates an import dialog with an optional initial beatmap path.</summary>
    /// <param name="initialPath">The path initially shown in the dialog.</param>
    /// <param name="currentBeatmap">Locates the beatmap currently open in osu!.</param>
    /// <param name="filePicker">Presents the native beatmap file picker.</param>
    public ComboColourStudioImportDialogViewModel(
        string? initialPath,
        ICurrentBeatmapLocator currentBeatmap,
        IFilePicker filePicker)
    {
        this.currentBeatmap = currentBeatmap ?? throw new ArgumentNullException(nameof(currentBeatmap));
        this.filePicker = filePicker ?? throw new ArgumentNullException(nameof(filePicker));
        Path = initialPath ?? string.Empty;
        AcceptCommand = new RelayCommand(Accept);
        CancelCommand = new RelayCommand(() => Close(null));
        UseCurrentCommand = new AsyncRelayCommand(UseCurrentAsync);
        BrowseCommand = new AsyncRelayCommand(BrowseAsync);
    }

    /// <summary>Gets or sets the beatmap path being imported.</summary>
    [ObservableProperty]
    public partial string Path { get; set; }

    /// <summary>Gets or sets the latest path or editor lookup error.</summary>
    [ObservableProperty]
    public partial string Error { get; private set; } = string.Empty;

    /// <summary>Gets the command that validates and accepts the path.</summary>
    public IRelayCommand AcceptCommand { get; }

    /// <summary>Gets the command that closes the dialog without importing.</summary>
    public IRelayCommand CancelCommand { get; }

    /// <summary>Gets the command that fills the path from the current osu! beatmap.</summary>
    public IAsyncRelayCommand UseCurrentCommand { get; }

    /// <summary>Gets the command that opens the native beatmap picker.</summary>
    public IAsyncRelayCommand BrowseCommand { get; }

    /// <summary>Gets or sets the callback used to close the modal dialog.</summary>
    internal Action<string?> Close { get; set; } = _ => { };

    private void Accept()
    {
        Error = string.Empty;
        if (string.IsNullOrWhiteSpace(Path))
        {
            Error = "A beatmap path is required.";
            return;
        }

        Close(Path.Trim());
    }

    private async Task UseCurrentAsync()
    {
        try
        {
            Path = await currentBeatmap.FindCurrentBeatmapAsync();
            Error = string.Empty;
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception exception)
        {
            Error = exception.Message;
        }
    }

    private async Task BrowseAsync()
    {
        try
        {
            var paths = await filePicker.PickOpenFilesAsync(new OpenFilePickerRequest
            {
                Title = "Select beatmap to import",
                AllowMultiple = false,
                Filters = [CommonFilePickerFilters.BeatmapsAndStoryboards],
            });

            if (paths.Count > 0)
            {
                Path = paths[0];
                Error = string.Empty;
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception exception)
        {
            Error = exception.Message;
        }
    }
}
