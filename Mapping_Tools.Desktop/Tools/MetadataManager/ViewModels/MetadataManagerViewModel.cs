using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mapping_Tools.Application.Execution.ToolExecution;
using Mapping_Tools.Application.Execution.ToolExecution.Models;
using Mapping_Tools.Application.Execution.UserNotification;
using Mapping_Tools.Application.Execution.UserNotification.Models;
using Mapping_Tools.Application.Platform;
using Mapping_Tools.Application.Platform.FilePicker;
using Mapping_Tools.Application.Projects.Models;
using Mapping_Tools.Application.Tools.MetadataManager;
using Mapping_Tools.Application.Workspace.Contracts;
using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.Tools.MetadataManager;
using Mapping_Tools.Desktop.Services.Dialogs;
using Mapping_Tools.Desktop.Shell;
using Mapping_Tools.Desktop.Services.Undo;
using Mapping_Tools.Desktop.Localization;
using Mapping_Tools.Desktop.Tools.ComboColourStudio.ViewModels.Adapters;
using Mapping_Tools.Desktop.Tools.MetadataManager.Models;
using Mapping_Tools.Desktop.Validation;
using Mapping_Tools.Desktop.ViewModels;

using Mapping_Tools.Application.Localization;

namespace Mapping_Tools.Desktop.Tools.MetadataManager.ViewModels;

/// <summary>
///     Owns Metadata Manager form state, project persistence, file selection, and execution.
/// </summary>
public sealed partial class MetadataManagerViewModel : SingleRunToolViewModel,
    IShellProjectFeature<MetadataManagerProject>
{
    /// <inheritdoc />
    public IProjectUndoHistory? UndoHistory { get; set; }

    private readonly ICurrentBeatmapDialogService currentBeatmapService;
    private readonly ProjectDefinition<MetadataManagerProject> definition;
    private readonly IFilePicker filePicker;

    private readonly IMetadataManagerService metadataManager;
    private readonly IUserNotificationService notifications;
    private readonly IBeatmapWorkspace workspace;

    /// <summary>Creates a Metadata Manager presentation model.</summary>
    /// <param name="metadataManager">Imports and exports metadata through application ports.</param>
    /// <param name="execution">Coordinates background execution, cancellation, and notifications.</param>
    /// <param name="filePicker">Presents native beatmap file dialogs.</param>
    /// <param name="currentBeatmapService">Fetches the current beatmap and presents lookup feedback.</param>
    /// <param name="workspace">Supplies the shared default beatmap picker location.</param>
    /// <param name="notifications">Publishes project and picker failures.</param>
    /// <param name="directories">Supplies the default export directory.</param>
    public MetadataManagerViewModel(
        IMetadataManagerService metadataManager,
        IToolExecutionService execution,
        IFilePicker filePicker,
        ICurrentBeatmapDialogService currentBeatmapService,
        IBeatmapWorkspace workspace,
        IUserNotificationService notifications,
        IApplicationDirectories directories)
        : base(execution, MetadataManagerToolDefinition.Definition)
    {
        this.metadataManager = metadataManager ?? throw new ArgumentNullException(nameof(metadataManager));
        this.filePicker = filePicker ?? throw new ArgumentNullException(nameof(filePicker));
        this.currentBeatmapService = currentBeatmapService
                                     ?? throw new ArgumentNullException(nameof(currentBeatmapService));
        this.workspace = workspace ?? throw new ArgumentNullException(nameof(workspace));
        this.notifications = notifications ?? throw new ArgumentNullException(nameof(notifications));
        ArgumentNullException.ThrowIfNull(directories);

        ExportPath = Path.Combine(directories.Exports, "metadata_manager.osu");
        string defaultExportPath = ExportPath;
        definition = new ProjectDefinition<MetadataManagerProject>(
            "metadataproject.json",
            "Metadata Manager Projects",
            () => CreateDefaultProject(defaultExportPath),
            "metadata-manager-project.json",
            ToolConfigSchema.ForTool(MetadataManagerToolDefinition.Definition.Id));
    }

    /// <summary>Gets or sets the beatmap whose metadata should be imported.</summary>
    [ObservableProperty]
    [Undoable]
    public partial string ImportPath { get; set; } = string.Empty;

    /// <summary>Gets or sets vertical-bar-separated target beatmap paths.</summary>
    [ObservableProperty]
    [Undoable]
    [NotifyPropertyChangedFor(nameof(ExportMapCountText))]
    public partial string ExportPath { get; set; }

    /// <summary>Gets or sets the Unicode artist name.</summary>
    [ObservableProperty]
    [Undoable]
    [NotifyDataErrorInfo]
    [StringLength(81, ErrorMessageResourceType = typeof(DesktopStrings), ErrorMessageResourceName = nameof(DesktopStrings.MetadataManager_Validation_ArtistMaxLength))]
    [NotifyPropertyChangedFor(nameof(IsBeatmapFileNameOverflowVisible))]
    public partial string Artist { get; set; } = string.Empty;

    /// <summary>Gets or sets the ASCII artist name used in generated filenames.</summary>
    [ObservableProperty]
    [Undoable]
    [NotifyDataErrorInfo]
    [StringLength(81, ErrorMessageResourceType = typeof(DesktopStrings), ErrorMessageResourceName = nameof(DesktopStrings.MetadataManager_Validation_ArtistMaxLength))]
    [AsciiOnly]
    [NotifyPropertyChangedFor(nameof(IsBeatmapFileNameOverflowVisible))]
    public partial string RomanisedArtist { get; set; } = string.Empty;

    /// <summary>Gets or sets the Unicode title.</summary>
    [ObservableProperty]
    [Undoable]
    [NotifyDataErrorInfo]
    [StringLength(81, ErrorMessageResourceType = typeof(DesktopStrings), ErrorMessageResourceName = nameof(DesktopStrings.MetadataManager_Validation_TitleMaxLength))]
    [NotifyPropertyChangedFor(nameof(IsBeatmapFileNameOverflowVisible))]
    public partial string Title { get; set; } = string.Empty;

    /// <summary>Gets or sets the ASCII title used in generated filenames.</summary>
    [ObservableProperty]
    [Undoable]
    [NotifyDataErrorInfo]
    [StringLength(81, ErrorMessageResourceType = typeof(DesktopStrings), ErrorMessageResourceName = nameof(DesktopStrings.MetadataManager_Validation_TitleMaxLength))]
    [AsciiOnly]
    [NotifyPropertyChangedFor(nameof(IsBeatmapFileNameOverflowVisible))]
    public partial string RomanisedTitle { get; set; } = string.Empty;

    /// <summary>Gets or sets the mapper name.</summary>
    [ObservableProperty]
    [Undoable]
    [NotifyDataErrorInfo]
    [StringLength(81, ErrorMessageResourceType = typeof(DesktopStrings), ErrorMessageResourceName = nameof(DesktopStrings.MetadataManager_Validation_CreatorMaxLength))]
    [NotifyPropertyChangedFor(nameof(IsBeatmapFileNameOverflowVisible))]
    public partial string BeatmapCreator { get; set; } = string.Empty;

    /// <summary>Gets or sets the source text recorded in the beatmap.</summary>
    [ObservableProperty]
    [Undoable]
    [NotifyDataErrorInfo]
    [StringLength(81, ErrorMessageResourceType = typeof(DesktopStrings), ErrorMessageResourceName = nameof(DesktopStrings.MetadataManager_Validation_SourceMaxLength))]
    public partial string Source { get; set; } = string.Empty;

    /// <summary>Gets or sets the space-separated beatmap tags.</summary>
    [ObservableProperty]
    [Undoable]
    [NotifyDataErrorInfo]
    [StringLength(1000, ErrorMessageResourceType = typeof(DesktopStrings), ErrorMessageResourceName = nameof(DesktopStrings.MetadataManager_Validation_TagsMaxLength))]
    [NotifyPropertyChangedFor(nameof(IsTagsOverflowVisible))]
    public partial string Tags { get; set; } = string.Empty;

    /// <summary>Gets or sets whether repeated tags are removed before export.</summary>
    [ObservableProperty]
    [Undoable]
    public partial bool DoRemoveDuplicateTags { get; set; } = true;

    /// <summary>Gets or sets whether online beatmap and mapset IDs are reset.</summary>
    [ObservableProperty]
    [Undoable]
    public partial bool ResetIds { get; set; }

    /// <summary>Gets or sets the preview timestamp in milliseconds.</summary>
    [ObservableProperty]
    [Undoable]
    public partial double PreviewTime { get; set; }

    /// <summary>Gets or sets whether combo and special colours are exported.</summary>
    [ObservableProperty]
    [Undoable]
    public partial bool UseComboColours { get; set; } = true;

    /// <summary>Gets the ordered combo-colour palette edited by the form.</summary>
    [Undoable]
    public ObservableCollection<ObservableComboColour> ComboColours { get; } = [];

    /// <summary>Gets the named special colours edited by the form.</summary>
    [Undoable]
    public ObservableCollection<ObservableSpecialColour> SpecialColours { get; } = [];

    /// <summary>Gets the number of non-empty target beatmap paths.</summary>
    public string ExportMapCountText
    {
        get
        {
            int count = string.IsNullOrEmpty(ExportPath)
                ? 0
                : ExportPath.Split('|').Length;
            return count == 1
                ? DesktopStrings.Common_MapCountOne
                : ApplicationText.Format(DesktopStrings.Common_MapCountMany, count);
        }
    }

    /// <summary>Gets whether the generated filename may exceed osu!'s legacy limit.</summary>
    public bool IsBeatmapFileNameOverflowVisible =>
        13 + RomanisedArtist.Length + RomanisedTitle.Length + BeatmapCreator.Length > 255;

    /// <summary>Gets whether the tags exceed the legacy warning thresholds.</summary>
    public bool IsTagsOverflowVisible =>
        Tags.Length > 1024 || Tags.Split(' ').Length > 100;

    ProjectDefinition<MetadataManagerProject> IShellProjectFeature<MetadataManagerProject>.ProjectDefinition => definition;

    MetadataManagerProject IShellProjectFeature<MetadataManagerProject>.Snapshot()
    {
        return Snapshot();
    }

    void IShellProjectFeature<MetadataManagerProject>.Install(MetadataManagerProject project)
    {
        Install(project);
    }

    [RelayCommand]
    private async Task BrowseImportAsync()
    {
        await PickBeatmapsAsync(
            DesktopStrings.MetadataManager_ImportMetadataFrom,
            workspace.GetBeatmapPickerStartLocation(Path.GetDirectoryName(ImportPath)),
            false,
            paths => ImportPath = paths[0]);
    }

    [RelayCommand]
    private async Task UseCurrentImportAsync()
    {
        string? path = await currentBeatmapService.FetchAsync();
        if (path is not null) ImportPath = path;
    }

    [RelayCommand]
    private async Task ImportAsync()
    {
        using var edit = UndoHistory?.BeginEdit();
        try
        {
            string exportPath = ExportPath;
            var imported = await metadataManager.ImportAsync(ImportPath);
            Install(new MetadataManagerProject
            {
                ImportPath = ImportPath,
                ExportPath = exportPath,
                Artist = imported.Artist,
                RomanisedArtist = imported.RomanisedArtist,
                Title = imported.Title,
                RomanisedTitle = imported.RomanisedTitle,
                BeatmapCreator = imported.BeatmapCreator,
                Source = imported.Source,
                Tags = imported.Tags,
                DoRemoveDuplicateTags = imported.DoRemoveDuplicateTags,
                ResetIds = imported.ResetIds,
                PreviewTime = imported.PreviewTime,
                UseComboColours = imported.UseComboColours,
                ComboColours = imported.ComboColours,
                SpecialColours = imported.SpecialColours,
            });
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception exception)
        {
            await PublishFailureAsync(
                DesktopStrings.MetadataManager_ImportFailureTitle,
                DesktopStrings.MetadataManager_ImportFailureMessage,
                exception);
        }
    }

    [RelayCommand]
    private async Task BrowseExportAsync()
    {
        await PickBeatmapsAsync(
            DesktopStrings.MetadataManager_ExportMetadataTo,
            GetExportPickerStartLocation(),
            true,
            paths => ExportPath = string.Join('|', paths));
    }

    [RelayCommand]
    private async Task UseCurrentExportAsync()
    {
        string? path = await currentBeatmapService.FetchAsync();
        if (path is not null) ExportPath = path;
    }

    [RelayCommand]
    private void AddComboColour()
    {
        using var edit = UndoHistory?.BeginEdit();
        if (ComboColours.Count >= 8) return;

        var colour = ComboColours.Count == 0
            ? RgbaColour.FromRgb(255, 255, 255)
            : ComboColours[^1].Color;
        ComboColours.Add(new ObservableComboColour(new ComboColour(colour)));
    }

    [RelayCommand]
    private void RemoveComboColour()
    {
        using var edit = UndoHistory?.BeginEdit();
        if (ComboColours.Count > 0) ComboColours.RemoveAt(ComboColours.Count - 1);
    }

    [RelayCommand]
    private void AddSpecialColour()
    {
        using var edit = UndoHistory?.BeginEdit();
        var colour = SpecialColours.Count == 0
            ? RgbaColour.FromRgb(255, 255, 255)
            : SpecialColours[^1].Color;
        SpecialColours.Add(new ObservableSpecialColour(new SpecialColour(colour)));
    }

    [RelayCommand]
    private void RemoveSpecialColour()
    {
        using var edit = UndoHistory?.BeginEdit();
        if (SpecialColours.Count > 0) SpecialColours.RemoveAt(SpecialColours.Count - 1);
    }

    /// <inheritdoc />
    protected override async Task RunCoreAsync()
    {
        var options = Snapshot();
        await Execution.ExecuteAsync(
            new ToolExecutionRequest<MetadataManagerResult>(
                Tool.Id,
                Tool.DisplayName,
                async context =>
                {
                    context.ReportProgress(0, DesktopStrings.MetadataManager_PreparingExport);
                    var result = await metadataManager.ExportAsync(
                        options,
                        new Progress<double>(value => context.ReportProgress(value, DesktopStrings.MetadataManager_Exporting)),
                        context.CancellationToken);
                    return new ToolExecutionOutput<MetadataManagerResult>(
                        result,
                        result.ProcessedCount == 1
                            ? ApplicationText.Format(DesktopStrings.MetadataManager_ExportResultOne, result.ProcessedCount)
                            : ApplicationText.Format(DesktopStrings.MetadataManager_ExportResultMany, result.ProcessedCount));
                }),
            CreateProgress());
    }

    partial void OnDoRemoveDuplicateTagsChanged(bool value)
    {
        if (value) Tags = MetadataManagerEngine.NormalizeTags(Tags);
    }

    partial void OnTagsChanged(string value)
    {
        if (DoRemoveDuplicateTags)
        {
            string normalized = MetadataManagerEngine.NormalizeTags(value);
            if (normalized != value) Tags = normalized;
        }
    }

    private MetadataManagerProject Snapshot()
    {
        return new MetadataManagerProject
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
            ComboColours = ComboColours.Select(colour => colour.Snapshot()).ToList(),
            SpecialColours = SpecialColours
                .Select(colour => colour.Snapshot())
                .ToList(),
        };
    }

    private void Install(MetadataManagerProject options)
    {
        ArgumentNullException.ThrowIfNull(options);
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
        foreach (var colour in options.ComboColours)
            ComboColours.Add(new ObservableComboColour(new ComboColour(colour.Color)));

        SpecialColours.Clear();
        foreach (var colour in options.SpecialColours)
            SpecialColours.Add(new ObservableSpecialColour(new SpecialColour(colour.Color, colour.Name ?? string.Empty)));
    }

    private async Task PickBeatmapsAsync(
        string title,
        string? suggestedStartLocation,
        bool allowMultiple,
        Action<IReadOnlyList<string>> apply)
    {
        try
        {
            var paths = await filePicker.PickOpenFilesAsync(
                new OpenFilePickerRequest
                {
                    Title = title,
                    SuggestedStartLocation = suggestedStartLocation,
                    AllowMultiple = allowMultiple,
                    Filters = [CommonFilePickerFilters.Beatmaps],
                });
            if (paths.Count > 0) apply(paths);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception exception)
        {
            await PublishFailureAsync(
                DesktopStrings.Common_SelectBeatmapsFailureTitle,
                DesktopStrings.Common_SelectBeatmapsFailureMessage,
                exception);
        }
    }

    private Task PublishFailureAsync(string title, string message, Exception exception)
    {
        return notifications.PublishAsync(new UserNotification(
            UserNotificationSeverity.Error,
            title,
            message,
            exception));
    }

    private string? GetExportPickerStartLocation()
    {
        string? importDirectory = Path.GetDirectoryName(ImportPath);
        return string.IsNullOrWhiteSpace(importDirectory)
            ? workspace.GetBeatmapPickerStartLocation(Path.GetDirectoryName(ExportPath))
            : importDirectory;
    }

    private static MetadataManagerProject CreateDefaultProject(string exportPath)
    {
        return new MetadataManagerProject
        {
            ExportPath = exportPath,
        };
    }
}
