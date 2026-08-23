using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mapping_Tools.Application.Platform;

namespace Mapping_Tools.Desktop.ViewModels;

/// <summary>Editable presentation state for one Mapset Merger source row.</summary>
public sealed partial class MapsetMergerItemViewModel : ObservableObject
{
    private readonly IFilePicker _filePicker;

    /// <summary>Creates a source row with the supplied initial values.</summary>
    /// <param name="filePicker">Presents the folder picker used by Browse.</param>
    /// <param name="name">The initial output name.</param>
    /// <param name="path">The initial source directory.</param>
    public MapsetMergerItemViewModel(IFilePicker filePicker, string name = "", string path = "")
    {
        _filePicker = filePicker ?? throw new ArgumentNullException(nameof(filePicker));
        Name = name;
        Path = path;
    }

    /// <summary>Gets or sets the output folder and reference prefix.</summary>
    [ObservableProperty]
    public partial string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the source mapset directory.</summary>
    [ObservableProperty]
    public partial string Path { get; set; } = string.Empty;

    /// <summary>Gets or sets whether this row is selected for removal.</summary>
    [ObservableProperty]
    public partial bool IsSelected { get; set; }

    /// <summary>Lets the user replace this row's source directory.</summary>
    [RelayCommand]
    private async Task BrowseAsync()
    {
        var paths = await _filePicker.PickFoldersAsync(new OpenFolderPickerRequest
        {
            Title = "Select mapset",
            SuggestedStartLocation = Directory.Exists(Path) ? Path : null,
            AllowMultiple = false,
        });
        string? path = paths.FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(path)) Path = path;
    }
}
