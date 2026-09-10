using System.ComponentModel.DataAnnotations;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mapping_Tools.Application.Platform.FilePicker;
using Mapping_Tools.Application.Workspace.Contracts;

namespace Mapping_Tools.Desktop.Tools.PatternGallery.Interactions;

/// <summary>Owns the pattern-file import dialog state and picker actions.</summary>
public sealed partial class PatternGalleryFileImportViewModel : ObservableValidator
{
    private readonly ICurrentBeatmapLocator currentBeatmap;
    private readonly IFilePicker filePicker;

    /// <summary>Creates a source-file import form.</summary>
    /// <param name="defaultName">The suggested display name.</param>
    /// <param name="defaultPath">The selected source path.</param>
    /// <param name="filePicker">Presents the native pattern-file picker.</param>
    /// <param name="currentBeatmap">Locates the beatmap currently open in osu!.</param>
    public PatternGalleryFileImportViewModel(
        string defaultName,
        string defaultPath,
        IFilePicker filePicker,
        ICurrentBeatmapLocator currentBeatmap)
    {
        this.filePicker = filePicker ?? throw new ArgumentNullException(nameof(filePicker));
        this.currentBeatmap = currentBeatmap ?? throw new ArgumentNullException(nameof(currentBeatmap));
        Name = defaultName;
        FilePath = defaultPath;
        AcceptCommand = new RelayCommand(Accept);
        CancelCommand = new RelayCommand(() => Close(null));
        BrowseCommand = new AsyncRelayCommand(BrowseAsync);
        UseCurrentCommand = new AsyncRelayCommand(UseCurrentAsync);
    }

    /// <summary>Gets or sets the pattern display name.</summary>
    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required(ErrorMessage = "A pattern name is required.")]
    public partial string Name { get; set; }

    /// <summary>Gets or sets the source pattern file path.</summary>
    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required(ErrorMessage = "A pattern file path is required.")]
    public partial string FilePath { get; set; }

    /// <summary>Gets or sets the optional time-code filter.</summary>
    [ObservableProperty]
    public partial string Filter { get; set; } = string.Empty;

    /// <summary>Gets or sets the optional lower time bound in milliseconds.</summary>
    [ObservableProperty]
    public partial double StartTime { get; set; } = -1;

    /// <summary>Gets or sets the optional upper time bound in milliseconds.</summary>
    [ObservableProperty]
    public partial double EndTime { get; set; } = -1;

    /// <summary>Gets the command that validates and accepts the form.</summary>
    public IRelayCommand AcceptCommand { get; }

    /// <summary>Gets the command that dismisses the form.</summary>
    public IRelayCommand CancelCommand { get; }

    /// <summary>Gets the command that opens the pattern-file picker.</summary>
    public IAsyncRelayCommand BrowseCommand { get; }

    /// <summary>Gets the command that fills the path from the current osu! beatmap.</summary>
    public IAsyncRelayCommand UseCurrentCommand { get; }

    /// <summary>Gets or sets the dialog-close callback installed by the adapter.</summary>
    internal Action<object?> Close { get; set; } = _ => { };

    private void Accept()
    {
        ValidateAllProperties();
        if (HasErrors) return;

        Close(new PatternGalleryFileInput(Name, FilePath, Filter, StartTime, EndTime));
    }

    private async Task UseCurrentAsync()
    {
        FilePath = await currentBeatmap.FindCurrentBeatmapAsync();
    }

    private async Task BrowseAsync()
    {
        var selected = await filePicker.PickOpenFilesAsync(new OpenFilePickerRequest
        {
            Title = "Import pattern file",
            AllowMultiple = false,
            Filters = [CommonFilePickerFilters.Beatmaps],
        });
        if (selected.Count > 0) FilePath = selected[0];
    }

}
