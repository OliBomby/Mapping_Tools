using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.VisualTree;
using Mapping_Tools.Application.Execution.ToolExecution;
using Mapping_Tools.Application.Execution.UserNotification;
using Mapping_Tools.Application.Execution.UserNotification.Models;
using Mapping_Tools.Application.Tools.SliderPicturator;
using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.Images;
using Mapping_Tools.Desktop.Models;
using Mapping_Tools.Desktop.Localization;
using Mapping_Tools.Desktop.Controls;
using Mapping_Tools.Desktop.Services.Undo;
using Mapping_Tools.Desktop.Tests.TestDoubles;
using Mapping_Tools.Desktop.Tests.TestHelpers;
using Mapping_Tools.Desktop.Tools.SliderPicturator.Models;
using Mapping_Tools.Desktop.Tools.SliderPicturator.ViewModels;
using Mapping_Tools.Desktop.Tools.SliderPicturator.Views;
using Mapping_Tools.Infrastructure.Projects;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Desktop.Tests.Tools.SliderPicturator.Views;

[TestClass]
public sealed class SliderPicturatorViewTests
{
    [TestMethod]
    public void ViewActivated_WithSelectedBeatmap_DoesNotFetchItAsImageOrSliderPath()
    {
        // Arrange
        TestBeatmapWorkspace workspace = new() { QuickRunPath = "selected.osu" };
        workspace.SetSelection(["selected.osu"]);
        TestFilePicker picker = new();
        RecordingImageFileService images = new();
        RecordingPicturator picturator = new();
        SliderPicturatorViewModel viewModel = CreateViewModel(workspace, picker, images, picturator);
        SliderPicturatorView view = new() { DataContext = viewModel };
        using HeadlessViewHost host = HeadlessViewHost.Show(view, height: 1200);

        // Act
        viewModel.Activate();

        // Assert
        images.LoadedPaths.Should().BeEmpty();
        viewModel.PictureFile.Should().BeEmpty();
        viewModel.PreviewImage.Should().BeNull();
        picturator.ColorPaths.Should().BeEmpty();
        picturator.SelectedSliderLookupCount.Should().Be(0);
    }

    [TestMethod]
    public async Task UseMapComboColors_WhenSelectedThroughControls_UsesChosenMapColour()
    {
        // Arrange
        RgbaColour mapColour = RgbaColour.FromRgb(11, 22, 33);
        RecordingPicturator picturator = new() { AvailableColors = [mapColour, RgbaColour.FromRgb(44, 55, 66)] };
        TestBeatmapWorkspace workspace = new() { QuickRunPath = "selected.osu" };
        workspace.SetSelection(["selected.osu"]);
        SliderPicturatorViewModel viewModel = CreateViewModel(workspace, new TestFilePicker(), new RecordingImageFileService(), picturator);
        SliderPicturatorView view = new() { DataContext = viewModel };
        using HeadlessViewHost host = HeadlessViewHost.Show(view, height: 1200);
        CheckBox useMapColors = view.GetVisualDescendants().OfType<CheckBox>()
            .Single(checkBox => checkBox.Content?.ToString() == DesktopStrings.SliderPicturator_UseMapComboColors);

        // Act
        host.Click(useMapColors, new Point(8, useMapColors.Bounds.Height / 2));
        viewModel.Activate();
        await HeadlessViewHost.DrainAsync(() => viewModel.AvailableColors.Contains(mapColour));
        ComboBox palette = view.GetVisualDescendants().OfType<ComboBox>()
            .Single(comboBox => comboBox.ItemsSource is System.Collections.ObjectModel.ObservableCollection<RgbaColour>);
        host.Click(palette);
        host.PressKey(Key.Down, RawInputModifiers.None, PhysicalKey.ArrowDown, "ArrowDown");
        host.PressKey(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, "Enter");
        Button run = view.GetVisualDescendants().OfType<ToolRunButton>()
            .Single().GetVisualDescendants().OfType<Button>().Single();
        host.Click(run);
        await HeadlessViewHost.DrainAsync(() => picturator.PicturateCount == 1 && !viewModel.IsRunning);

        // Assert
        useMapColors.IsChecked.Should().BeTrue();
        palette.SelectedItem.Should().Be(picturator.AvailableColors[1]);
        viewModel.ComboColor.Should().Be(picturator.AvailableColors[1]);
        viewModel.CurrentTrackColor.Should().Be(picturator.AvailableColors[1]);
        picturator.ColorPaths.Should().Contain("selected.osu");
        palette.IsVisible.Should().BeTrue();
        viewModel.ShouldShowPalette.Should().BeFalse();
        picturator.LastOptions.Should().NotBeNull();
        picturator.LastOptions!.CurrentTrackColor.Should().Be(picturator.AvailableColors[1]);
        picturator.PicturateCount.Should().Be(1);
    }

    [DataTestMethod]
    [DataRow(16, 8)]
    [DataRow(800, 1200)]
    public async Task BrowseButton_WhenImageSelected_ShowsScaledPreviewWithinView(int imageWidth, int imageHeight)
    {
        // Arrange
        TestFilePicker picker = new() { OpenFiles = ["picture.png"] };
        RecordingImageFileService images = new(imageWidth, imageHeight);
        SliderPicturatorViewModel viewModel = CreateViewModel(picker: picker, images: images);
        SliderPicturatorView view = new() { DataContext = viewModel };
        using HeadlessViewHost host = HeadlessViewHost.Show(view, width: 900, height: 1000);
        Button browse = view.GetVisualDescendants().OfType<Button>()
            .Single(button => button.Content?.ToString() == "Browse");

        // Act
        host.Click(browse);
        await HeadlessViewHost.DrainAsync(() => viewModel.PreviewImage is not null && !viewModel.IsProcessingPreview);
        Image preview = view.GetVisualDescendants().OfType<Image>().Single();

        // Assert
        picker.LastOpenRequest.Should().NotBeNull();
        picker.LastOpenRequest!.AllowMultiple.Should().BeFalse();
        images.LoadedPaths.Should().ContainSingle().Which.Should().Be("picture.png");
        preview.IsEffectivelyVisible.Should().BeTrue();
        preview.Bounds.Width.Should().BeGreaterThan(0);
        preview.Bounds.Height.Should().BeGreaterThan(0);
        preview.Source.Should().BeSameAs(viewModel.PreviewImage);
        viewModel.PreviewImage!.PixelSize.Width.Should().Be(imageWidth);
        viewModel.PreviewImage.PixelSize.Height.Should().Be(imageHeight);
        preview.Bounds.Width.Should().BeLessThanOrEqualTo(view.Bounds.Width);
        preview.Bounds.Height.Should().BeLessThanOrEqualTo(view.Bounds.Height);
    }

    [TestMethod]
    public async Task TransparentImageBrowse_PreviewBitmapRetainsAlphaChannel()
    {
        // Arrange
        TestFilePicker picker = new() { OpenFiles = ["transparent.png"] };
        RecordingImageFileService images = new(1) { PixelData = [255, 255, 255, 0] };
        SliderPicturatorViewModel viewModel = CreateViewModel(picker: picker, images: images);
        SliderPicturatorView view = new() { DataContext = viewModel };
        using HeadlessViewHost host = HeadlessViewHost.Show(view);
        Button browse = view.GetVisualDescendants().OfType<Button>()
            .Single(button => button.Content?.ToString() == "Browse");

        // Act
        host.Click(browse);
        await HeadlessViewHost.DrainAsync(() => viewModel.PreviewImage is not null && !viewModel.IsProcessingPreview);
        Image preview = view.GetVisualDescendants().OfType<Image>().Single();
        WriteableBitmap bitmap = (WriteableBitmap)preview.Source!;
        using ILockedFramebuffer framebuffer = bitmap.Lock();

        // Assert
        preview.IsEffectivelyVisible.Should().BeTrue();
        preview.Source.Should().BeSameAs(viewModel.PreviewImage);
        bitmap.PixelSize.Should().Be(new PixelSize(1, 1));
        framebuffer.Format.Should().Be(PixelFormat.Rgba8888);
        framebuffer.AlphaFormat.Should().NotBe(AlphaFormat.Opaque);
    }

    [TestMethod]
    public async Task BrowseButton_WhenPickerIsCancelled_PreservesCurrentImageAndPreview()
    {
        // Arrange
        TestFilePicker picker = new();
        RecordingImageFileService images = new();
        SliderPicturatorViewModel viewModel = CreateViewModel(picker: picker, images: images);
        viewModel.PictureFile = "previous.png";
        SliderPicturatorView view = new() { DataContext = viewModel };
        using HeadlessViewHost host = HeadlessViewHost.Show(view, height: 1200);
        await HeadlessViewHost.DrainAsync(() => viewModel.PreviewImage is not null && !viewModel.IsProcessingPreview);
        var previousPreview = viewModel.PreviewImage;
        Image preview = view.GetVisualDescendants().OfType<Image>().Single();
        preview.Source.Should().BeSameAs(previousPreview);
        Button browse = view.GetVisualDescendants().OfType<Button>()
            .Single(button => button.Content?.ToString() == "Browse");

        // Act
        host.Click(browse);

        // Assert
        viewModel.PictureFile.Should().Be("previous.png");
        viewModel.PreviewImage.Should().BeSameAs(previousPreview);
        preview.Source.Should().BeSameAs(previousPreview);
        images.LoadedPaths.Should().ContainSingle().Which.Should().Be("previous.png");
    }

    [TestMethod]
    public async Task BrowseButton_WhenImageCannotLoad_ClearsPreviewAndPublishesFailure()
    {
        // Arrange
        TestFilePicker picker = new() { OpenFiles = ["invalid.png"] };
        RecordingImageFileService images = new();
        UserNotificationService notifications = new();
        List<UserNotification> published = [];
        notifications.Published += (_, args) => published.Add(args.Notification);
        SliderPicturatorViewModel viewModel = CreateViewModel(picker: picker, images: images, notifications: notifications);
        viewModel.PictureFile = "previous.png";
        SliderPicturatorView view = new() { DataContext = viewModel };
        using HeadlessViewHost host = HeadlessViewHost.Show(view, height: 1200);
        await HeadlessViewHost.DrainAsync(() => viewModel.PreviewImage is not null && !viewModel.IsProcessingPreview);
        Image preview = view.GetVisualDescendants().OfType<Image>().Single();
        var previousPreview = viewModel.PreviewImage;
        preview.Source.Should().BeSameAs(previousPreview);
        InvalidDataException failure = new("not an image");
        images.ExceptionToThrow = failure;
        picker.OpenFiles = ["invalid.png"];
        Button browse = view.GetVisualDescendants().OfType<Button>()
            .Single(button => button.Content?.ToString() == "Browse");

        // Act
        host.Click(browse);
        await HeadlessViewHost.DrainAsync(() => published.Count > 0 && !viewModel.IsProcessingPreview);

        // Assert
        viewModel.PictureFile.Should().Be("invalid.png");
        viewModel.PreviewImage.Should().BeNull();
        preview.Source.Should().BeNull();
        published.Should().ContainSingle().Which.Title.Should().Be("Could not load image");
        published[0].Severity.Should().Be(UserNotificationSeverity.Error);
        published[0].Exception.Should().BeSameAs(failure);
        published[0].Exception!.Message.Should().Be("not an image");
    }

    [TestMethod]
    public void QualitySlider_EditAndUndo_PreservesNonUndoableDerivedSegmentCount()
    {
        // Arrange
        SliderPicturatorViewModel viewModel = CreateViewModel();
        viewModel.SegmentCount = 42;
        ProjectUndoHistory<SliderPicturatorProject> history = new(viewModel, new VersionedProjectJsonSerializer());
        viewModel.UndoHistory = history;
        SliderPicturatorView view = new() { DataContext = viewModel };
        using HeadlessViewHost host = HeadlessViewHost.Show(view, height: 1200);
        Window window = host.Window;
        ProjectUndoWindowInput.Attach(window, () => history);
        Slider quality = view.GetVisualDescendants().OfType<Slider>().Single();
        quality.IsVisible.Should().BeTrue();
        quality.Bounds.Width.Should().BeGreaterThan(10);

        // Act
        host.Drag(quality,
            new Point(8, quality.Bounds.Height / 2),
            new Point(quality.Bounds.Width - 8, quality.Bounds.Height / 2));
        double editedQuality = quality.Value;
        string visibleSegmentCount = view.GetVisualDescendants().OfType<TextBlock>()
            .Single(block => block.Text == viewModel.SegmentCountLabel).Text!;
        host.Click(view.GetVisualDescendants().OfType<Button>().First());
        host.PressKey(Key.Z, RawInputModifiers.Control, PhysicalKey.Z, "z");

        // Assert
        editedQuality.Should().BeGreaterThan(1);
        viewModel.Quality.Should().Be(1);
        viewModel.SegmentCount.Should().Be(42);
        view.GetVisualDescendants().OfType<TextBlock>()
            .Single(block => block.Text == viewModel.SegmentCountLabel).Text
            .Should().Be(visibleSegmentCount);
    }

    private static SliderPicturatorViewModel CreateViewModel(
        TestBeatmapWorkspace? workspace = null,
        TestFilePicker? picker = null,
        RecordingImageFileService? images = null,
        RecordingPicturator? picturator = null,
        UserNotificationService? notifications = null)
    {
        notifications ??= new UserNotificationService();
        return new SliderPicturatorViewModel(
            picturator ?? new RecordingPicturator(),
            images ?? new RecordingImageFileService(),
            picker ?? new TestFilePicker(),
            new ToolExecutionService(notifications, TimeProvider.System),
            workspace ?? new TestBeatmapWorkspace(),
            new DesktopApplicationSettings(),
            notifications);
    }

    private sealed class RecordingPicturator : ISliderPicturatorService
    {
        public IReadOnlyList<RgbaColour> AvailableColors { get; init; } = [];

        public List<string> ColorPaths { get; } = [];

        public SliderPicturatorServiceOptions? LastOptions { get; private set; }

        public int PicturateCount { get; private set; }

        public int SelectedSliderLookupCount { get; private set; }

        public Task<SliderPicturatorResult> PicturateAsync(string path, SliderPicturatorServiceOptions options,
            bool quickRun = false, IProgress<double>? progress = null, CancellationToken cancellationToken = default)
        {
            PicturateCount++;
            LastOptions = options;
            return Task.FromResult(new SliderPicturatorResult(path, 42));
        }

        public Task<IReadOnlyList<RgbaColour>> GetAvailableColorsAsync(string path, CancellationToken cancellationToken = default)
        {
            ColorPaths.Add(path);
            return Task.FromResult(AvailableColors);
        }

        public Task<HitObject?> GetSelectedSliderAsync(string path, CancellationToken cancellationToken = default)
        {
            SelectedSliderLookupCount++;
            return Task.FromResult<HitObject?>(null);
        }
    }

    private sealed class RecordingImageFileService : IImageFileService
    {
        private readonly int imageWidth;
        private readonly int imageHeight;

        public RecordingImageFileService(int imageWidth = 2, int imageHeight = 1)
        {
            this.imageWidth = imageWidth;
            this.imageHeight = imageHeight;
        }

        public List<string> LoadedPaths { get; } = [];

        public Exception? ExceptionToThrow { get; set; }

        public byte[]? PixelData { get; set; }

        public Task<RgbaImage> LoadAsync(string path, CancellationToken cancellationToken = default)
        {
            LoadedPaths.Add(path);
            if (ExceptionToThrow is not null) throw ExceptionToThrow;
            byte[] pixels = PixelData ?? new byte[checked(imageWidth * imageHeight * 4)];
            if (PixelData is null)
            {
                for (int pixel = 0; pixel < imageWidth * imageHeight; pixel++)
                {
                    pixels[pixel * 4] = 255;
                    pixels[pixel * 4 + 3] = 255;
                }
            }

            return Task.FromResult(new RgbaImage(imageWidth, imageHeight, pixels));
        }
    }
}
