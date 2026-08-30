using System.Collections.ObjectModel;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mapping_Tools.Application.Execution.ToolExecution;
using Mapping_Tools.Application.Execution.ToolExecution.Models;
using Mapping_Tools.Application.Execution.UserNotification;
using Mapping_Tools.Application.Execution.UserNotification.Models;
using Mapping_Tools.Application.Platform.FilePicker;
using Mapping_Tools.Application.Projects.Contracts;
using Mapping_Tools.Application.Projects.Models;
using Mapping_Tools.Application.QuickRun.Contracts;
using Mapping_Tools.Desktop.Settings.Models;
using Mapping_Tools.Application.Tools;
using Mapping_Tools.Application.Tools.SliderPicturator;
using Mapping_Tools.Application.Workspace.Contracts;
using Mapping_Tools.Application.Workspace.Models;
using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.Images;
using Mapping_Tools.Core.Tools.SliderPicturator;
using Mapping_Tools.Desktop.Shell;
using Mapping_Tools.Desktop.Tools.SliderPicturator.Models;
using Mapping_Tools.Desktop.Utilities;
using Mapping_Tools.Desktop.ViewModels;

namespace Mapping_Tools.Desktop.Tools.SliderPicturator.ViewModels;

/// <summary>Owns Slider Picturator state, preview generation, project persistence, and tool execution.</summary>
public sealed partial class SliderPicturatorViewModel : SingleRunToolViewModel, IQuickRun, IShellProjectFeature<SliderPicturatorProject>,
    IShellFeatureActivation
{
    private readonly ICurrentBeatmapLocator currentBeatmap;

    private readonly ProjectDefinition<SliderPicturatorProject> definition = new(
        "sliderpicturatorproject.json", "Slider Picturator Projects", static () => new SliderPicturatorProject(),
        "slider-picturator-project.json",
        ToolConfigSchema.ForTool(SliderPicturatorToolDefinition.Definition.Id));

    private readonly IFilePicker filePicker;
    private readonly IImageFileService images;
    private readonly IUserNotificationService notifications;
    private readonly ISliderPicturatorService picturator;
    private readonly DesktopApplicationSettings settings;
    private readonly IBeatmapWorkspace workspace;
    private CancellationTokenSource? colorRefreshCancellation;
    private CancellationTokenSource? imageLoadCancellation;
    private bool isActive;
    private CancellationTokenSource? previewCancellation;
    private Bitmap? previewImage;
    private RgbaImage? sourceImage;

    /// <summary>Creates the Slider Picturator presentation model.</summary>
    /// <param name="picturator">Runs the framework-independent operation.</param>
    /// <param name="images">Decodes source images for preview generation.</param>
    /// <param name="filePicker">Presents the native image picker.</param>
    /// <param name="execution">Coordinates execution, cancellation, and completion messages.</param>
    /// <param name="currentBeatmap">Finds the current osu! beatmap.</param>
    /// <param name="workspace">Supplies shell-selected paths.</param>
    /// <param name="settings">Supplies the legacy Always QuickRun setting.</param>
    /// <param name="notifications">Publishes picker and preview failures.</param>
    public SliderPicturatorViewModel(ISliderPicturatorService picturator, IImageFileService images,
        IFilePicker filePicker, IToolExecutionService execution, ICurrentBeatmapLocator currentBeatmap,
        IBeatmapWorkspace workspace, DesktopApplicationSettings settings, IUserNotificationService notifications)
        : base(execution, SliderPicturatorToolDefinition.Definition)
    {
        this.picturator = picturator ?? throw new ArgumentNullException(nameof(picturator));
        this.images = images ?? throw new ArgumentNullException(nameof(images));
        this.filePicker = filePicker ?? throw new ArgumentNullException(nameof(filePicker));
        this.currentBeatmap = currentBeatmap ?? throw new ArgumentNullException(nameof(currentBeatmap));
        this.workspace = workspace ?? throw new ArgumentNullException(nameof(workspace));
        this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
        this.notifications = notifications ?? throw new ArgumentNullException(nameof(notifications));
    }

    /// <summary>Gets the supported GPU viewport-size choices in legacy order.</summary>
    public IReadOnlyList<long> ViewportSizes { get; } = [16384, 32768];

    /// <summary>Gets the current beatmap palette.</summary>
    public ObservableCollection<RgbaColour> AvailableColors { get; } = [];

    /// <summary>Gets whether the map palette selector is visible.</summary>
    public bool ShouldShowCcPicker => UseMapComboColors;

    /// <summary>Gets whether the manual track colour picker is visible.</summary>
    public bool ShouldShowPalette => !UseMapComboColors;

    /// <summary>Gets the current recoloured preview bitmap.</summary>
    public Bitmap? PreviewImage
    {
        get => previewImage;
        private set
        {
            if (ReferenceEquals(previewImage, value)) return;

            var previous = previewImage;
            previewImage = value;
            OnPropertyChanged();
            previous?.Dispose();
        }
    }

    /// <summary>Gets whether a preview calculation is active.</summary>
    [ObservableProperty]
    public partial bool IsProcessingPreview { get; set; }

    /// <summary>Gets or sets the GPU viewport-size choice.</summary>
    [ObservableProperty]
    public partial long ViewportSize { get; set; } = 32768;

    /// <summary>Gets or sets the image quality.</summary>
    [ObservableProperty]
    public partial int Quality { get; set; } = 1;

    /// <summary>Gets or sets the estimated segment count.</summary>
    [ObservableProperty]
    public partial long SegmentCount { get; set; }

    /// <summary>Gets or sets the image vertical resolution.</summary>
    [ObservableProperty]
    public partial double YResolution { get; set; } = 1080;

    /// <summary>Gets or sets the slider start X coordinate.</summary>
    [ObservableProperty]
    public partial double SliderStartX { get; set; } = 256;

    /// <summary>Gets or sets the slider start Y coordinate.</summary>
    [ObservableProperty]
    public partial double SliderStartY { get; set; } = 192;

    /// <summary>Gets or sets the image start X coordinate.</summary>
    [ObservableProperty]
    public partial double ImageStartX { get; set; }

    /// <summary>Gets or sets the image start Y coordinate.</summary>
    [ObservableProperty]
    public partial double ImageStartY { get; set; }

    /// <summary>Gets or sets whether map combo colours supply the track colour.</summary>
    [ObservableProperty]
    public partial bool UseMapComboColors { get; set; }

    /// <summary>Gets or sets the selected combo colour.</summary>
    [ObservableProperty]
    public partial RgbaColour ComboColor { get; set; } = RgbaColour.FromRgb(0, 0, 0);

    /// <summary>Gets or sets the effective track colour.</summary>
    [ObservableProperty]
    public partial RgbaColour CurrentTrackColor { get; set; } = RgbaColour.White;

    /// <summary>Gets or sets the manually selected track colour.</summary>
    [ObservableProperty]
    public partial RgbaColour TrackColorPickerColor { get; set; } = RgbaColour.White;

    /// <summary>Gets or sets the border colour.</summary>
    [ObservableProperty]
    public partial RgbaColour BorderColor { get; set; } = RgbaColour.White;

    /// <summary>Gets or sets the generated start time.</summary>
    [ObservableProperty]
    public partial double TimeCode { get; set; }

    /// <summary>Gets or sets the generated duration.</summary>
    [ObservableProperty]
    public partial double Duration { get; set; } = 1;

    /// <summary>Gets or sets the selected image path.</summary>
    [ObservableProperty]
    public partial string PictureFile { get; set; } = string.Empty;

    /// <summary>Gets or sets whether transparent black can represent black pixels.</summary>
    [ObservableProperty]
    public partial bool BlackOn { get; set; } = true;

    /// <summary>Gets or sets whether the border colour can represent pixels.</summary>
    [ObservableProperty]
    public partial bool BorderOn { get; set; } = true;

    /// <summary>Gets or sets whether red participates in matching.</summary>
    [ObservableProperty]
    public partial bool RedOn { get; set; } = true;

    /// <summary>Gets or sets whether green participates in matching.</summary>
    [ObservableProperty]
    public partial bool GreenOn { get; set; } = true;

    /// <summary>Gets or sets whether blue participates in matching.</summary>
    [ObservableProperty]
    public partial bool BlueOn { get; set; } = true;

    /// <summary>Gets or sets whether alpha participates in matching.</summary>
    [ObservableProperty]
    public partial bool AlphaOn { get; set; } = true;

    /// <summary>Gets or sets whether generated map colours are persisted.</summary>
    [ObservableProperty]
    public partial bool SetBeatmapColors { get; set; } = true;

    /// <summary>Gets the transient slider whose sliderball path should be preserved.</summary>
    [ObservableProperty]
    public partial HitObject? SelectedSlider { get; set; }

    /// <inheritdoc />
    public async Task RunQuickAsync(CancellationToken cancellationToken)
    {
        string? path = await currentBeatmap.FindCurrentBeatmapAsync(cancellationToken).ConfigureAwait(false);
        await RunWithStateAsync(() => RunPathAsync(path, true, cancellationToken));
    }

    /// <inheritdoc />
    public void Activate()
    {
        if (isActive) return;

        isActive = true;
        workspace.SelectionChanged += OnWorkspaceSelectionChanged;
        _ = RefreshColorsAsync();
        if (sourceImage is not null) _ = GeneratePreviewAsync();
    }

    /// <inheritdoc />
    public void Deactivate()
    {
        if (!isActive) return;

        isActive = false;
        workspace.SelectionChanged -= OnWorkspaceSelectionChanged;
        imageLoadCancellation?.Cancel();
        colorRefreshCancellation?.Cancel();
        previewCancellation?.Cancel();
        IsProcessingPreview = false;
        PreviewImage = null;
    }

    ProjectDefinition<SliderPicturatorProject> IShellProjectFeature<SliderPicturatorProject>.ProjectDefinition => definition;

    SliderPicturatorProject IShellProjectFeature<SliderPicturatorProject>.Snapshot()
    {
        return Snapshot();
    }

    void IShellProjectFeature<SliderPicturatorProject>.Install(SliderPicturatorProject project)
    {
        Install(project);
    }

    /// <summary>Opens the legacy image filter and loads the selected image.</summary>
    [RelayCommand]
    private async Task BrowseAsync()
    {
        try
        {
            var paths = await filePicker.PickOpenFilesAsync(new OpenFilePickerRequest
            {
                Title = "Select an image", AllowMultiple = false, Filters =
                [
                    new FilePickerFilter(
                        "All Image Files", ["*.bmp", "*.jpg", "*.jpeg", "*.png", "*.gif", "*.tif", "*.tiff", "*.ico"]),
                ],
            });
            if (paths.Count > 0) PictureFile = paths[0];
        }
        catch (OperationCanceledException) { }
        catch (Exception exception) { await PublishFailureAsync("Could not select image", "The selected file was not a valid local image.", exception); }
    }

    /// <summary>Imports the first selected slider from the current editor.</summary>
    [RelayCommand]
    private async Task ImportAsync()
    {
        string? path = await currentBeatmap.FindCurrentBeatmapAsync();
        if (string.IsNullOrWhiteSpace(path)) return;
        try { SelectedSlider = await picturator.GetSelectedSliderAsync(path); }
        catch (Exception exception) { await PublishFailureAsync("Could not import slider", "The selected hit object could not be read.", exception); }
    }

    /// <summary>Removes the imported slider so duration uses the explicit field.</summary>
    [RelayCommand]
    private void Remove()
    {
        SelectedSlider = null;
    }

    private async Task RefreshColorsAsync()
    {
        colorRefreshCancellation?.Cancel();
        CancellationTokenSource cancellation = new();
        colorRefreshCancellation = cancellation;
        try
        {
            string? path = await currentBeatmap.FindCurrentBeatmapAsync(cancellation.Token);
            if (string.IsNullOrWhiteSpace(path)) return;
            var colours = await picturator.GetAvailableColorsAsync(path, cancellation.Token);
            cancellation.Token.ThrowIfCancellationRequested();
            AvailableColors.Clear();
            foreach (var colour in colours) AvailableColors.Add(colour);
            if (AvailableColors.Count > 0 && UseMapComboColors && !AvailableColors.Contains(ComboColor)) ComboColor = AvailableColors[0];
        }
        catch (OperationCanceledException) { }
        catch (Exception exception) { await PublishFailureAsync("Could not read map colours", "The current beatmap palette could not be loaded.", exception); }
        finally
        {
            if (ReferenceEquals(colorRefreshCancellation, cancellation)) colorRefreshCancellation = null;

            cancellation.Dispose();
        }
    }

    /// <inheritdoc />
    protected override async Task RunCoreAsync()
    {
        string? path = await currentBeatmap.FindCurrentBeatmapAsync();
        if (string.IsNullOrWhiteSpace(path)) path = workspace.SelectedPaths.FirstOrDefault();
        await RunPathAsync(path, settings.AlwaysQuickRun, CancellationToken.None);
    }

    partial void OnPictureFileChanged(string value)
    {
        _ = LoadPreviewAsync(value);
    }

    partial void OnQualityChanged(int value)
    {
        _ = GeneratePreviewAsync();
    }

    partial void OnUseMapComboColorsChanged(bool value)
    {
        CurrentTrackColor = value ? ComboColor : TrackColorPickerColor;
        OnPropertyChanged(nameof(ShouldShowCcPicker));
        OnPropertyChanged(nameof(ShouldShowPalette));
        _ = GeneratePreviewAsync();
    }

    partial void OnComboColorChanged(RgbaColour value)
    {
        if (UseMapComboColors) CurrentTrackColor = value;
        _ = GeneratePreviewAsync();
    }

    partial void OnCurrentTrackColorChanged(RgbaColour value)
    {
        _ = GeneratePreviewAsync();
    }

    partial void OnTrackColorPickerColorChanged(RgbaColour value)
    {
        if (!UseMapComboColors) CurrentTrackColor = value;
    }

    partial void OnBorderColorChanged(RgbaColour value)
    {
        _ = GeneratePreviewAsync();
    }

    partial void OnBlackOnChanged(bool value)
    {
        _ = GeneratePreviewAsync();
    }

    partial void OnBorderOnChanged(bool value)
    {
        _ = GeneratePreviewAsync();
    }

    partial void OnRedOnChanged(bool value)
    {
        _ = GeneratePreviewAsync();
    }

    partial void OnGreenOnChanged(bool value)
    {
        _ = GeneratePreviewAsync();
    }

    partial void OnBlueOnChanged(bool value)
    {
        _ = GeneratePreviewAsync();
    }

    partial void OnAlphaOnChanged(bool value)
    {
        _ = GeneratePreviewAsync();
    }

    partial void OnSelectedSliderChanged(HitObject? value)
    {
        _ = GeneratePreviewAsync();
    }

    private async Task RunPathAsync(string? path, bool quick, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(path)) return;
        var options = Snapshot();
        var execution = await Execution.ExecuteAsync(new ToolExecutionRequest<SliderPicturatorResult>(Tool.Id, Tool.DisplayName,
            async context =>
            {
                var result = await picturator.PicturateAsync(path, options,
                    new Progress<double>(value => context.ReportProgress(value, "Generating slider picture")),
                    context.CancellationToken);
                return new ToolExecutionOutput<SliderPicturatorResult>(
                    result,
                    quick ? null : "Done!",
                    quick);
            }), CreateProgress(), cancellationToken);
        if (execution.Status == ToolExecutionStatus.Succeeded && execution.Value is { } result)
            SegmentCount = result.SegmentCount;
    }

    private async Task LoadPreviewAsync(string path)
    {
        imageLoadCancellation?.Cancel();
        CancellationTokenSource cancellation = new();
        imageLoadCancellation = cancellation;
        previewCancellation?.Cancel();
        sourceImage = null;
        PreviewImage = null;

        if (string.IsNullOrWhiteSpace(path))
        {
            sourceImage = null;
            PreviewImage = null;
            cancellation.Dispose();
            imageLoadCancellation = null;
            return;
        }

        try
        {
            var image = await images.LoadAsync(path, cancellation.Token);
            cancellation.Token.ThrowIfCancellationRequested();
            if (!string.Equals(PictureFile, path, StringComparison.Ordinal)) return;

            sourceImage = image;
            await GeneratePreviewAsync();
        }
        catch (OperationCanceledException) { }
        catch (Exception exception)
        {
            if (string.Equals(PictureFile, path, StringComparison.Ordinal))
            {
                sourceImage = null;
                PreviewImage = null;
                await PublishFailureAsync("Could not load image", "Not a valid image file.", exception);
            }
        }
        finally
        {
            if (ReferenceEquals(imageLoadCancellation, cancellation)) imageLoadCancellation = null;

            cancellation.Dispose();
        }
    }

    private async Task GeneratePreviewAsync()
    {
        if (sourceImage is null) return;
        previewCancellation?.Cancel();
        CancellationTokenSource cancellation = new();
        previewCancellation = cancellation;
        var token = cancellation.Token;
        IsProcessingPreview = true;
        try
        {
            SliderPicturatorProject options = Snapshot();
            options.BackgroundColor = RgbaColour.FromArgb(0, 0, 0, 0);
            var sourceImage = this.sourceImage
                              ?? throw new InvalidOperationException("The preview source image was cleared.");
            (RgbaImage image, long segments) result = await Task.Run(
                () => SliderPicturatorEngine.Recolor(sourceImage, options),
                token);
            token.ThrowIfCancellationRequested();
            PreviewImage = RgbaImageBitmapFactory.Create(result.image);
            SegmentCount = result.segments;
        }
        catch (OperationCanceledException) { }
        catch (Exception exception) { await PublishFailureAsync("Preview generation failed", "The image could not be picturated.", exception); }
        finally
        {
            if (ReferenceEquals(previewCancellation, cancellation))
            {
                previewCancellation = null;
                IsProcessingPreview = false;
            }

            cancellation.Dispose();
        }
    }

    private void OnWorkspaceSelectionChanged(object? sender, BeatmapSelectionChangedEventArgs eventArgs)
    {
        _ = RefreshColorsAsync();
    }

    private SliderPicturatorProject Snapshot()
    {
        return new SliderPicturatorProject
        {
            ViewportSize = ViewportSize, Quality = Quality, SegmentCount = SegmentCount, YResolution = YResolution,
            SliderStartX = SliderStartX, SliderStartY = SliderStartY, ImageStartX = ImageStartX, ImageStartY = ImageStartY,
            UseMapComboColors = UseMapComboColors, ComboColor = ComboColor, CurrentTrackColor = CurrentTrackColor,
            TrackColorPickerColor = TrackColorPickerColor, BorderColor = BorderColor, TimeCode = TimeCode, Duration = Duration,
            PictureFile = PictureFile, BlackOn = BlackOn, BorderOn = BorderOn, RedOn = RedOn, GreenOn = GreenOn,
            BlueOn = BlueOn, AlphaOn = AlphaOn, SetBeatmapColors = SetBeatmapColors, SelectedSlider = SelectedSlider,
        };
    }

    private void Install(SliderPicturatorProject project)
    {
        ArgumentNullException.ThrowIfNull(project);
        ViewportSize = project.ViewportSize;
        Quality = project.Quality;
        SegmentCount = project.SegmentCount;
        YResolution = project.YResolution;
        SliderStartX = project.SliderStartX;
        SliderStartY = project.SliderStartY;
        ImageStartX = project.ImageStartX;
        ImageStartY = project.ImageStartY;
        UseMapComboColors = project.UseMapComboColors;
        ComboColor = project.ComboColor;
        CurrentTrackColor = project.CurrentTrackColor;
        TrackColorPickerColor = project.TrackColorPickerColor;
        BorderColor = project.BorderColor;
        TimeCode = project.TimeCode;
        Duration = project.Duration;
        PictureFile = project.PictureFile ?? string.Empty;
        BlackOn = project.BlackOn;
        BorderOn = project.BorderOn;
        RedOn = project.RedOn;
        GreenOn = project.GreenOn;
        BlueOn = project.BlueOn;
        AlphaOn = project.AlphaOn;
        SetBeatmapColors = project.SetBeatmapColors;
        SelectedSlider = project.SelectedSlider;
    }

    private Task PublishFailureAsync(string title, string message, Exception exception)
    {
        return notifications.PublishAsync(
            new UserNotification(UserNotificationSeverity.Error, title, message, exception));
    }
}
