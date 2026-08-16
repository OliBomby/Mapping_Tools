using System.ComponentModel.DataAnnotations;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mapping_Tools.Application.Execution;
using Mapping_Tools.Application.Interactions;
using Mapping_Tools.Application.Interactions.Validation;
using Mapping_Tools.Application.MetadataManager;
using Mapping_Tools.Application.Platform;
using Mapping_Tools.Application.Projects;
using Mapping_Tools.Application.Workspace;
using Mapping_Tools.Core.Classes.BeatmapHelper;
using Mapping_Tools.Core.Tools.MetadataManager;
using Mapping_Tools.Desktop.Shell;

namespace Mapping_Tools.Desktop.ViewModels;

/// <summary>
/// Owns Metadata Manager form state, project persistence, file selection, and execution.
/// </summary>
public sealed partial class MetadataManagerViewModel : SingleRunToolViewModel,
    IShellProjectFeature
{
    private const string OperationId = "metadata-manager";

    private readonly IMetadataManagerService _metadataManager;
    private readonly IFilePicker _filePicker;
    private readonly ICurrentBeatmapLocator _currentBeatmapLocator;
    private readonly IUserNotificationService _notifications;
    private readonly ProjectDefinition<MetadataManagerProject> _definition;

    /// <summary>Gets or sets the beatmap whose metadata should be imported.</summary>
    [ObservableProperty]
    public partial string ImportPath { get; set; } = string.Empty;

    /// <summary>Gets or sets vertical-bar-separated target beatmap paths.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ExportMapCountText))]
    public partial string ExportPath { get; set; } = string.Empty;

    /// <summary>Gets or sets the Unicode artist name.</summary>
    [ObservableProperty]
    [NotifyDataErrorInfo]
    [StringLength(81, ErrorMessage = "Artist names must be 81 characters or fewer.")]
    [NotifyPropertyChangedFor(nameof(IsBeatmapFileNameOverflowVisible))]
    public partial string Artist { get; set; } = string.Empty;

    /// <summary>Gets or sets the ASCII artist name used in generated filenames.</summary>
    [ObservableProperty]
    [NotifyDataErrorInfo]
    [StringLength(81, ErrorMessage = "Artist names must be 81 characters or fewer.")]
    [AsciiOnly]
    [NotifyPropertyChangedFor(nameof(IsBeatmapFileNameOverflowVisible))]
    public partial string RomanisedArtist { get; set; } = string.Empty;

    /// <summary>Gets or sets the Unicode title.</summary>
    [ObservableProperty]
    [NotifyDataErrorInfo]
    [StringLength(81, ErrorMessage = "Titles must be 81 characters or fewer.")]
    [NotifyPropertyChangedFor(nameof(IsBeatmapFileNameOverflowVisible))]
    public partial string Title { get; set; } = string.Empty;

    /// <summary>Gets or sets the ASCII title used in generated filenames.</summary>
    [ObservableProperty]
    [NotifyDataErrorInfo]
    [StringLength(81, ErrorMessage = "Titles must be 81 characters or fewer.")]
    [AsciiOnly]
    [NotifyPropertyChangedFor(nameof(IsBeatmapFileNameOverflowVisible))]
    public partial string RomanisedTitle { get; set; } = string.Empty;

    /// <summary>Gets or sets the mapper name.</summary>
    [ObservableProperty]
    [NotifyDataErrorInfo]
    [StringLength(81, ErrorMessage = "Mapper names must be 81 characters or fewer.")]
    [NotifyPropertyChangedFor(nameof(IsBeatmapFileNameOverflowVisible))]
    public partial string BeatmapCreator { get; set; } = string.Empty;

    /// <summary>Gets or sets the source text recorded in the beatmap.</summary>
    [ObservableProperty]
    [NotifyDataErrorInfo]
    [StringLength(81, ErrorMessage = "Source text must be 81 characters or fewer.")]
    public partial string Source { get; set; } = string.Empty;

    /// <summary>Gets or sets the space-separated beatmap tags.</summary>
    [ObservableProperty]
    [NotifyDataErrorInfo]
    [StringLength(1000, ErrorMessage = "Tags must be 1000 characters or fewer.")]
    [NotifyPropertyChangedFor(nameof(IsTagsOverflowVisible))]
    public partial string Tags { get; set; } = string.Empty;

    /// <summary>Gets or sets whether repeated tags are removed before export.</summary>
    [ObservableProperty]
    public partial bool DoRemoveDuplicateTags { get; set; } = true;

    /// <summary>Gets or sets whether online beatmap and mapset IDs are reset.</summary>
    [ObservableProperty]
    public partial bool ResetIds { get; set; }

    /// <summary>Gets or sets the preview timestamp in milliseconds.</summary>
    [ObservableProperty]
    public partial double PreviewTime { get; set; }

    /// <summary>Gets or sets whether combo and special colours are exported.</summary>
    [ObservableProperty]
    public partial bool UseComboColours { get; set; } = true;

    /// <summary>Gets the ordered combo-colour palette edited by the form.</summary>
    public ObservableCollection<ComboColour> ComboColours { get; } = [];

    /// <summary>Gets the named special colours edited by the form.</summary>
    public ObservableCollection<SpecialColour> SpecialColours { get; } = [];

    /// <summary>Gets the number of non-empty target beatmap paths.</summary>
    public string ExportMapCountText
    {
        get
        {
            int count = string.IsNullOrEmpty(ExportPath)
                ? 0
                : ExportPath.Split('|').Length;
            return count == 1 ? "(1) map total" : $"({count}) maps total";
        }
    }

    /// <summary>Gets whether the generated filename may exceed osu!'s legacy limit.</summary>
    public bool IsBeatmapFileNameOverflowVisible =>
        13 + RomanisedArtist.Length + RomanisedTitle.Length + BeatmapCreator.Length > 255;

    /// <summary>Gets whether the tags exceed the legacy warning thresholds.</summary>
    public bool IsTagsOverflowVisible =>
        Tags.Length > 1024 || Tags.Split(' ').Length > 100;

    /// <summary>Creates a Metadata Manager presentation model.</summary>
    /// <param name="metadataManager">Imports and exports metadata through application ports.</param>
    /// <param name="execution">Coordinates background execution, cancellation, and notifications.</param>
    /// <param name="filePicker">Presents native beatmap file dialogs.</param>
    /// <param name="currentBeatmapLocator">Finds the beatmap currently open in osu!.</param>
    /// <param name="notifications">Publishes project and picker failures.</param>
    /// <param name="directories">Supplies the default export directory.</param>
    public MetadataManagerViewModel(
        IMetadataManagerService metadataManager,
        IToolExecutionService execution,
        IFilePicker filePicker,
        ICurrentBeatmapLocator currentBeatmapLocator,
        IUserNotificationService notifications,
        IApplicationDirectories directories)
        : base(execution, OperationId)
    {
        _metadataManager = metadataManager ?? throw new ArgumentNullException(nameof(metadataManager));
        _filePicker = filePicker ?? throw new ArgumentNullException(nameof(filePicker));
        _currentBeatmapLocator = currentBeatmapLocator ?? throw new ArgumentNullException(nameof(currentBeatmapLocator));
        _notifications = notifications ?? throw new ArgumentNullException(nameof(notifications));
        ArgumentNullException.ThrowIfNull(directories);

        ExportPath = Path.Combine(directories.Exports, "metadata_manager.osu");
        string defaultExportPath = ExportPath;
        _definition = new ProjectDefinition<MetadataManagerProject>(
            "metadataproject.json",
            "Metadata Manager Projects",
            () => CreateDefaultProject(defaultExportPath),
            "metadata-manager-project.json");
    }

    [RelayCommand]
    private async Task BrowseImportAsync()
    {
        await PickBeatmapsAsync(
            "Import metadata from",
            ImportPath,
            allowMultiple: false,
            paths => ImportPath = paths[0]);
    }

    [RelayCommand]
    private async Task UseCurrentImportAsync()
    {
        string? path = await _currentBeatmapLocator.FindCurrentBeatmapAsync();
        if (!string.IsNullOrWhiteSpace(path))
        {
            ImportPath = path;
        }
    }

    [RelayCommand]
    private async Task ImportAsync()
    {
        try
        {
            string exportPath = ExportPath;
            MetadataManagerOptions options = await _metadataManager.ImportAsync(ImportPath);
            options.ImportPath = ImportPath;
            options.ExportPath = exportPath;
            Install(options);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception exception)
        {
            await PublishFailureAsync(
                "Metadata could not be imported",
                "The selected beatmap could not be read.",
                exception);
        }
    }

    [RelayCommand]
    private async Task BrowseExportAsync()
    {
        await PickBeatmapsAsync(
            "Export metadata to",
            FirstExportPathOrNull(),
            allowMultiple: true,
            paths => ExportPath = string.Join('|', paths));
    }

    [RelayCommand]
    private async Task UseCurrentExportAsync()
    {
        string? path = await _currentBeatmapLocator.FindCurrentBeatmapAsync();
        if (!string.IsNullOrWhiteSpace(path))
        {
            ExportPath = path;
        }
    }

    [RelayCommand]
    private void AddComboColour()
    {
        if (ComboColours.Count >= 8)
        {
            return;
        }

        RgbaColour colour = ComboColours.Count == 0
            ? RgbaColour.FromRgb(255, 255, 255)
            : ComboColours[^1].Color;
        ComboColours.Add(new ComboColour(colour));
    }

    [RelayCommand]
    private void RemoveComboColour()
    {
        if (ComboColours.Count > 0)
        {
            ComboColours.RemoveAt(ComboColours.Count - 1);
        }
    }

    [RelayCommand]
    private void AddSpecialColour()
    {
        RgbaColour colour = SpecialColours.Count == 0
            ? RgbaColour.FromRgb(255, 255, 255)
            : SpecialColours[^1].Color;
        SpecialColours.Add(new SpecialColour(colour));
    }

    [RelayCommand]
    private void RemoveSpecialColour()
    {
        if (SpecialColours.Count > 0)
        {
            SpecialColours.RemoveAt(SpecialColours.Count - 1);
        }
    }

    /// <inheritdoc/>
    protected override bool PrepareRun()
    {
        ValidateAllProperties();
        return !HasErrors;
    }

    /// <inheritdoc/>
    protected override async Task RunCoreAsync()
    {
        MetadataManagerOptions options = Snapshot();
        await Execution.ExecuteAsync(
            new ToolExecutionRequest<MetadataManagerResult>(
                OperationId,
                "Metadata Manager",
                async context =>
                {
                    context.ReportProgress(0, "Preparing metadata export");
                    MetadataManagerResult result = await _metadataManager.ExportAsync(
                        options,
                        new Progress<double>(value => context.ReportProgress(value, "Exporting metadata")),
                        context.CancellationToken);
                    return new ToolExecutionOutput<MetadataManagerResult>(
                        result,
                        $"Successfully exported metadata to {result.ProcessedCount} beatmap(s)!");
                }),
            CreateProgress());
    }

    IProjectDefinition IShellProjectFeature.ProjectDefinition => _definition;

    object IShellProjectFeature.Snapshot() => Snapshot();

    void IShellProjectFeature.Install(object project) =>
        Install((MetadataManagerProject)project);

    partial void OnDoRemoveDuplicateTagsChanged(bool value)
    {
        if (value)
        {
            Tags = MetadataManagerEngine.NormalizeTags(Tags);
        }
    }

    partial void OnTagsChanged(string value)
    {
        if (DoRemoveDuplicateTags)
        {
            string normalized = MetadataManagerEngine.NormalizeTags(value);
            if (normalized != value)
            {
                Tags = normalized;
            }
        }
    }

    private MetadataManagerProject Snapshot() => new()
    {
        ImportPath = ImportPath,
        ExportPath = ExportPath,
        Artist = Artist,
        RomanisedArtist = RomanisedArtist,
        Title = Title,
        RomanisedTitle = RomanisedTitle,
        BeatmapCreator = BeatmapCreator,
        Source = Source,
        Tags = Tags,
        DoRemoveDuplicateTags = DoRemoveDuplicateTags,
        ResetIds = ResetIds,
        PreviewTime = PreviewTime,
        UseComboColours = UseComboColours,
        ComboColours = ComboColours.Select(colour => new ComboColour(colour.Color)).ToList(),
        SpecialColours = SpecialColours
            .Select(colour => new SpecialColour(colour.Color, colour.Name ?? string.Empty))
            .ToList()
    };

    private void Install(MetadataManagerOptions options)
    {
        ValidateProject(options);
        ImportPath = options.ImportPath;
        ExportPath = options.ExportPath;
        Artist = options.Artist;
        RomanisedArtist = options.RomanisedArtist;
        Title = options.Title;
        RomanisedTitle = options.RomanisedTitle;
        BeatmapCreator = options.BeatmapCreator;
        Source = options.Source;
        DoRemoveDuplicateTags = options.DoRemoveDuplicateTags;
        Tags = options.Tags;
        ResetIds = options.ResetIds;
        PreviewTime = options.PreviewTime;
        UseComboColours = options.UseComboColours;

        ComboColours.Clear();
        foreach (ComboColour colour in options.ComboColours)
        {
            ComboColours.Add(new ComboColour(colour.Color));
        }

        SpecialColours.Clear();
        foreach (SpecialColour colour in options.SpecialColours)
        {
            SpecialColours.Add(new SpecialColour(colour.Color, colour.Name ?? string.Empty));
        }
    }

    private async Task PickBeatmapsAsync(
        string title,
        string? suggestedStartLocation,
        bool allowMultiple,
        Action<IReadOnlyList<string>> apply)
    {
        try
        {
            IReadOnlyList<string> paths = await _filePicker.PickOpenFilesAsync(
                new OpenFilePickerRequest
                {
                    Title = title,
                    SuggestedStartLocation = suggestedStartLocation,
                    AllowMultiple = allowMultiple,
                    Filters = [CommonFilePickerFilters.Beatmaps]
                });
            if (paths.Count > 0)
            {
                apply(paths);
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception exception)
        {
            await PublishFailureAsync(
                "Could not select beatmaps",
                "The file picker could not return local beatmap paths.",
                exception);
        }
    }

    private Task PublishFailureAsync(string title, string message, Exception exception) =>
        _notifications.PublishAsync(new UserNotification(
            UserNotificationSeverity.Error,
            title,
            message,
            exception));

    private string? FirstExportPathOrNull()
    {
        string? path = ExportPath
            .Split('|', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault();
        return string.IsNullOrWhiteSpace(path) ? null : Path.GetDirectoryName(path);
    }

    private static MetadataManagerProject CreateDefaultProject(string exportPath) => new()
    {
        ExportPath = exportPath
    };

    private static void ValidateProject(MetadataManagerOptions options)
    {
        if (options is null ||
            options.ComboColours is null ||
            options.SpecialColours is null ||
            options.ComboColours.Any(colour => colour is null) ||
            options.SpecialColours.Any(colour => colour is null) ||
            options.SpecialColours.Any(colour => string.IsNullOrWhiteSpace(colour.Name)) ||
            !double.IsFinite(options.PreviewTime))
        {
            throw new InvalidDataException("The Metadata Manager project is incomplete.");
        }
    }
}
