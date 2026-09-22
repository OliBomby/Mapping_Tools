using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mapping_Tools.Application.Platform.FilePicker;
using Mapping_Tools.Application.Workspace.Contracts;
using Mapping_Tools.Desktop.Services.Dialogs;

namespace Mapping_Tools.Desktop.Tools.ComboColourStudio.ViewModels;

/// <summary>Owns the path and actions for the Combo Colour Studio beatmap import dialog.</summary>
public sealed partial class ComboColourStudioImportDialogViewModel : ObservableObject
{
    private readonly ICurrentBeatmapDialogService currentBeatmapService;
    private readonly IFilePicker filePicker;
    private readonly IBeatmapWorkspace workspace;

    /// <summary>Creates an import dialog with an optional initial beatmap path.</summary>
    /// <param name="initialPath">The path initially shown in the dialog.</param>
    /// <param name="currentBeatmapService">Fetches the current beatmap and presents lookup feedback.</param>
    /// <param name="workspace">Supplies the shared default beatmap picker location.</param>
    /// <param name="filePicker">Presents the native beatmap file picker.</param>
    public ComboColourStudioImportDialogViewModel(
        string? initialPath,
        ICurrentBeatmapDialogService currentBeatmapService,
        IBeatmapWorkspace workspace,
        IFilePicker filePicker)
    {
        this.currentBeatmapService = currentBeatmapService
                                     ?? throw new ArgumentNullException(nameof(currentBeatmapService));
        this.workspace = workspace ?? throw new ArgumentNullException(nameof(workspace));
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
        if (string.IsNullOrWhiteSpace(Path))
            return;

        Close(Path.Trim());
    }

    private async Task UseCurrentAsync()
    {
        string? path = await currentBeatmapService.FetchAsync();
        if (path is not null)
            Path = path;
    }

    private async Task BrowseAsync()
    {
        var paths = await filePicker.PickOpenFilesAsync(new OpenFilePickerRequest
        {
            Title = "Select beatmap to import",
            SuggestedStartLocation = workspace.GetBeatmapPickerStartLocation(
                System.IO.Path.GetDirectoryName(Path)),
            AllowMultiple = false,
            Filters = [CommonFilePickerFilters.BeatmapsAndStoryboards],
        });

        if (paths.Count > 0) Path = paths[0];
    }
}
