using Avalonia.Controls;
using Avalonia;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using DialogHostAvalonia;
using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Desktop.Tests.TestHelpers;
using Mapping_Tools.Desktop.Tools.PatternGallery.Controls;
using Mapping_Tools.Desktop.Tools.PatternGallery.ViewModels;
using Mapping_Tools.Desktop.Tools.PatternGallery.Views;
using Mapping_Tools.Desktop.Views;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace Mapping_Tools.Desktop.Tests.Tools.PatternGallery.Views;

[TestClass]
public sealed class PatternGalleryViewTests
{
    [TestMethod]
    public async Task SingleCirclePatternThumbnail_WhenRealized_UsesAvailablePreviewBounds()
    {
        // Arrange
        PatternGalleryViewTestHarness harness = PatternGalleryViewTestHarness.Create(patternCount: 1);
        PatternGalleryItemViewModel item = harness.ViewModel.Groups.SelectMany(group => group.Patterns).Single();
        Beatmap beatmap = new();
        beatmap.HitObjects.Add(DecodeHitObject("256,192,100,1,0"));
        PatternGalleryView view = new() { DataContext = harness.ViewModel };
        using HeadlessViewHost host = HeadlessViewHost.Show(view, width: 800, height: 600);

        // Act
        item.SetThumbnail(beatmap);
        await HeadlessViewHost.DrainAsync(() => view.GetVisualDescendants().OfType<PatternThumbnailControl>()
            .Any(control => ReferenceEquals(control.Beatmap, beatmap)));
        PatternThumbnailControl thumbnail = view.GetVisualDescendants().OfType<PatternThumbnailControl>().Single();
        DrawingGroup drawing = new();

        using (DrawingContext context = drawing.Open())
            thumbnail.Render(context);
        (GeometryDrawing Drawing, Rect Bounds)[] circleGeometryDrawings = EnumerateGeometryDrawings(drawing)
            .Where(drawingCommand => !IsThumbnailBackground(drawingCommand.Drawing, thumbnail))
            .ToArray();

        // Assert
        thumbnail.IsVisible.Should().BeTrue();
        thumbnail.Bounds.Width.Should().BeGreaterThan(0);
        thumbnail.Bounds.Height.Should().BeGreaterThan(0);
        Control availableBounds = thumbnail.GetVisualAncestors().OfType<Control>().First();
        thumbnail.Bounds.Width.Should().BeLessThanOrEqualTo(availableBounds.Bounds.Width);
        thumbnail.Bounds.Height.Should().BeLessThanOrEqualTo(availableBounds.Bounds.Height);
        thumbnail.ClipToBounds.Should().BeTrue();
        thumbnail.Beatmap.Should().BeSameAs(beatmap);
        thumbnail.Beatmap!.HitObjects.Should().ContainSingle().Which.IsCircle.Should().BeTrue();
        circleGeometryDrawings.Should().NotBeEmpty();
        circleGeometryDrawings.Select(drawingCommand => drawingCommand.Bounds)
            .Should().OnlyContain(bounds => IsFiniteAndContained(bounds, thumbnail.Bounds.Size));
    }

    [TestMethod]
    public async Task SingleSliderPatternThumbnail_WhenRealized_PreparesVisibleSliderPath()
    {
        // Arrange
        PatternGalleryViewTestHarness harness = PatternGalleryViewTestHarness.Create(patternCount: 1);
        PatternGalleryItemViewModel item = harness.ViewModel.Groups.SelectMany(group => group.Patterns).Single();
        Beatmap beatmap = new();
        HitObject slider = DecodeHitObject("32,64,100,2,0,L|200:64,1,168");
        beatmap.HitObjects.Add(slider);
        PatternGalleryView view = new() { DataContext = harness.ViewModel };
        using HeadlessViewHost host = HeadlessViewHost.Show(view, width: 800, height: 600);

        // Act
        item.SetThumbnail(beatmap);
        await HeadlessViewHost.DrainAsync(() => view.GetVisualDescendants().OfType<PatternThumbnailControl>()
            .Any(control => ReferenceEquals(control.Beatmap, beatmap) && control.SliderPathPoints is not null));
        PatternThumbnailControl thumbnail = view.GetVisualDescendants().OfType<PatternThumbnailControl>().Single();
        DrawingGroup drawing = new();

        using (DrawingContext context = drawing.Open())
            thumbnail.Render(context);
        (GeometryDrawing Drawing, Rect Bounds)[] sliderGeometryDrawings = EnumerateGeometryDrawings(drawing)
            .Where(drawingCommand => !IsThumbnailBackground(drawingCommand.Drawing, thumbnail))
            .ToArray();
        (GeometryDrawing Drawing, Rect Bounds)[] sliderBodyDrawings = sliderGeometryDrawings
            .Where(drawingCommand => drawingCommand.Drawing.Brush is null && drawingCommand.Drawing.Pen is not null)
            .ToArray();

        // Assert
        thumbnail.IsVisible.Should().BeTrue();
        thumbnail.Bounds.Width.Should().BeGreaterThan(0);
        thumbnail.Bounds.Height.Should().BeGreaterThan(0);
        Control availableBounds = thumbnail.GetVisualAncestors().OfType<Control>().First();
        thumbnail.Bounds.Width.Should().BeLessThanOrEqualTo(availableBounds.Bounds.Width);
        thumbnail.Bounds.Height.Should().BeLessThanOrEqualTo(availableBounds.Bounds.Height);
        thumbnail.Beatmap.Should().BeSameAs(beatmap);
        thumbnail.SliderPathPoints.Should().ContainKey(slider);
        thumbnail.SliderPathPoints![slider].Should().HaveCountGreaterThan(1);
        sliderBodyDrawings.Should().HaveCount(2,
            "the slider path is rendered with both its outer and inner strokes");
        sliderGeometryDrawings.Select(drawingCommand => drawingCommand.Bounds)
            .Should().OnlyContain(bounds => IsFiniteAndContained(bounds, thumbnail.Bounds.Size));
    }

    private static IEnumerable<(GeometryDrawing Drawing, Rect Bounds)> EnumerateGeometryDrawings(
        Drawing drawing,
        Matrix? transform = null)
    {
        Matrix currentTransform = transform ?? Matrix.Identity;

        if (drawing is GeometryDrawing geometryDrawing && geometryDrawing.Geometry is { } geometry)
        {
            Rect geometryBounds = geometryDrawing.Pen is { } pen
                ? geometry.GetRenderBounds(pen)
                : geometry.Bounds;
            yield return (geometryDrawing, TransformBounds(geometryBounds, currentTransform));
        }

        if (drawing is not DrawingGroup drawingGroup) yield break;

        Matrix groupTransform = drawingGroup.Transform?.Value ?? Matrix.Identity;
        Matrix childTransform = groupTransform * currentTransform;
        foreach (Drawing child in drawingGroup.Children)
            foreach (var descendant in EnumerateGeometryDrawings(child, childTransform))
                yield return descendant;
    }

    private static Rect TransformBounds(Rect bounds, Matrix transform)
    {
        Point topLeft = transform.Transform(bounds.TopLeft);
        Point topRight = transform.Transform(bounds.TopRight);
        Point bottomLeft = transform.Transform(bounds.BottomLeft);
        Point bottomRight = transform.Transform(bounds.BottomRight);
        double left = Math.Min(Math.Min(topLeft.X, topRight.X), Math.Min(bottomLeft.X, bottomRight.X));
        double top = Math.Min(Math.Min(topLeft.Y, topRight.Y), Math.Min(bottomLeft.Y, bottomRight.Y));
        double right = Math.Max(Math.Max(topLeft.X, topRight.X), Math.Max(bottomLeft.X, bottomRight.X));
        double bottom = Math.Max(Math.Max(topLeft.Y, topRight.Y), Math.Max(bottomLeft.Y, bottomRight.Y));
        return new Rect(left, top, right - left, bottom - top);
    }

    private static bool IsThumbnailBackground(GeometryDrawing drawing, PatternThumbnailControl thumbnail)
    {
        return drawing.Geometry is RectangleGeometry rectangle
               && rectangle.Rect.Equals(new Rect(thumbnail.Bounds.Size))
               && Equals(drawing.Brush, Brushes.Black)
               && drawing.Pen is null;
    }

    private static bool IsFiniteAndContained(Rect bounds, Size viewport)
    {
        return double.IsFinite(bounds.X)
               && double.IsFinite(bounds.Y)
               && double.IsFinite(bounds.Width)
               && double.IsFinite(bounds.Height)
               && bounds.Width > 0
               && bounds.Height > 0
               && bounds.X >= 0
               && bounds.Y >= 0
               && bounds.Right <= viewport.Width
               && bounds.Bottom <= viewport.Height;
    }

    [TestMethod]
    public async Task PatternDoubleClick_WhenAnotherPatternWasSelected_ExportsOnlyClickedPatternOnce()
    {
        // Arrange
        PatternGalleryViewTestHarness harness = PatternGalleryViewTestHarness.Create(patternCount: 2);
        PatternGalleryViewModel viewModel = harness.ViewModel;
        PatternGalleryItemViewModel[] items = viewModel.Groups.SelectMany(group => group.Patterns).ToArray();
        PatternGalleryView view = new() { DataContext = viewModel };
        using HeadlessViewHost host = HeadlessViewHost.Show(view);
        TextBlock selectedPattern = FindPatternName(view, items[1]);
        TextBlock clickedPattern = FindPatternName(view, items[0]);
        host.Click(selectedPattern);

        // Act
        host.DoubleClick(clickedPattern);
        await HeadlessViewHost.DrainAsync(() => harness.Gallery.ExportCount == 1 && !viewModel.IsRunning);

        // Assert
        harness.Gallery.ExportCount.Should().Be(1);
        harness.Gallery.TargetPath.Should().Be("current.osu");
        harness.Gallery.QuickRun.Should().BeTrue();
        harness.Gallery.ExportedPatterns.Should().ContainSingle().Which.Should().BeSameAs(items[0].Pattern);
        items[0].IsSelected.Should().BeTrue();
        items[1].IsSelected.Should().BeFalse();
    }

    [TestMethod]
    [SuppressMessage("ReSharper", "AccessToDisposedClosure", Justification = "The property-change handler is removed before its timeout source is disposed.")]
    public async Task SearchBox_WhenQueryChangesSeveralTimes_ShowsOnlyFinalQueryResults()
    {
        // Arrange
        PatternGalleryViewTestHarness harness = PatternGalleryViewTestHarness.Create(patternCount: 120);
        PatternGalleryViewModel viewModel = harness.ViewModel;
        PatternGalleryView view = new() { DataContext = viewModel };
        using HeadlessViewHost host = HeadlessViewHost.Show(view);
        TextBox search = view.GetVisualDescendants().OfType<TextBox>()
            .Single(textBox => textBox.PlaceholderText == "Search");

        // Act
        host.Click(search);
        host.TypeText("Pattern 0");
        host.PressKey(Key.A, RawInputModifiers.Control, PhysicalKey.A, "a");
        host.TypeText("Pattern 119");
        string searchFilterAfterTyping = viewModel.SearchFilter;
        bool hasFinalResults() => viewModel.Groups.SelectMany(group => group.Patterns)
            .Select(item => item.Name)
            .SequenceEqual(["Pattern 119"]);
        using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(5));
        PropertyChangedEventHandler onResultsChanged = (_, _) =>
        {
            if (hasFinalResults()) timeout.Cancel();
        };
        viewModel.PropertyChanged += onResultsChanged;
        SynchronizationContext? synchronizationContext = SynchronizationContext.Current;
        try
        {
            if (!hasFinalResults()) Dispatcher.UIThread.MainLoop(timeout.Token);
        }
        catch (OperationCanceledException) when (hasFinalResults())
        {
        }
        finally
        {
            viewModel.PropertyChanged -= onResultsChanged;
            SynchronizationContext.SetSynchronizationContext(synchronizationContext);
        }
        await HeadlessViewHost.DrainAsync(() => VisiblePatternNames(view).SequenceEqual(["Pattern 119"]));

        // Assert
        searchFilterAfterTyping.Should().Be("Pattern 119");
        viewModel.SearchFilter.Should().Be("Pattern 119");
        viewModel.Groups.SelectMany(group => group.Patterns)
            .Select(item => item.Name)
            .Should().Equal("Pattern 119");
        VisiblePatternNames(view).Should().Equal("Pattern 119");
    }

    [TestMethod]
    public async Task RenameCollectionCommand_AcceptingNames_UpdatesDisplayAndFolderMetadata()
    {
        // Arrange
        PatternGalleryViewTestHarness harness = PatternGalleryViewTestHarness.Create();
        PatternGalleryViewModel viewModel = harness.ViewModel;
        PatternGalleryView view = new() { DataContext = viewModel };
        MainWindow window = new();
        using HeadlessViewHost host = HeadlessViewHost.ShowWindow(window);
        DialogHost dialogHost = window.GetVisualDescendants().OfType<DialogHost>().Single();
        dialogHost.Content = view;
        HeadlessViewHost.RunDispatcherJobs();

        // Act
        Task renameTask = viewModel.RenameCollectionCommand.ExecuteAsync(null);
        await HeadlessViewHost.DrainAsync(() => dialogHost.IsOpen);
        TextBox[] fields = window.GetVisualDescendants().OfType<TextBox>()
            .Where(textBox => textBox.IsVisible && textBox.Bounds.Width > 0)
            .ToArray();
        TextBox newName = fields.Single(textBox => textBox.Text == "Collection");
        TextBox newFolder = fields.Single(textBox => textBox.Text == "Default");
        host.Click(newName);
        host.PressKey(Key.A, RawInputModifiers.Control, PhysicalKey.A, "a");
        host.TypeText("Renamed collection");
        host.Click(newFolder);
        host.PressKey(Key.A, RawInputModifiers.Control, PhysicalKey.A, "a");
        host.TypeText("renamed-folder");
        host.Click(window.GetVisualDescendants().OfType<Button>()
            .Single(button => button.Content?.ToString() == "ACCEPT"));
        await renameTask;

        // Assert
        await HeadlessViewHost.DrainAsync(() => view.GetVisualDescendants().OfType<TextBlock>()
            .Any(textBlock => textBlock.IsVisible && textBlock.Text == "Renamed collection"));
        viewModel.CollectionName.Should().Be("Renamed collection");
        viewModel.Project.CollectionName.Should().Be("Renamed collection");
        viewModel.Project.FileHandler.CollectionFolderName.Should().Be("renamed-folder");
        view.GetVisualDescendants().OfType<TextBlock>()
            .Should().ContainSingle(textBlock => textBlock.IsVisible && textBlock.Text == "Renamed collection");
        harness.RenamedFolderName().Should().Be("renamed-folder");
    }

    [TestMethod]
    public void PatternGallery_WhenManyPatternsAreLoaded_UsesFiniteScrollingAndRealizesVisibleItems()
    {
        // Arrange
        PatternGalleryViewTestHarness harness = PatternGalleryViewTestHarness.Create(patternCount: 250);
        PatternGalleryView view = new() { DataContext = harness.ViewModel };
        using HeadlessViewHost host = HeadlessViewHost.Show(view, width: 800, height: 600);
        ScrollViewer galleryScroll = view.GetVisualDescendants().OfType<ScrollViewer>()
            .Single(scrollViewer => scrollViewer.Content is ItemsControl
                                    && ReferenceEquals(scrollViewer.DataContext, harness.ViewModel));
        double maximumOffset = galleryScroll.Extent.Height - galleryScroll.Viewport.Height;

        // Act
        galleryScroll.Offset = new Vector(0, maximumOffset);
        HeadlessViewHost.RunDispatcherJobs();
        int realizedItems = view.GetVisualDescendants().OfType<ListBoxItem>()
            .Count(item => item.IsVisible && item.Bounds.Width > 0 && item.Bounds.Height > 0);
        PatternGalleryItemViewModel lastItem = harness.ViewModel.Groups.SelectMany(group => group.Patterns).Last();
        TextBlock lastItemName = FindPatternName(view, lastItem);

        host.Click(lastItemName);

        // Assert
        maximumOffset.Should().BeGreaterThan(0);
        galleryScroll.Offset.Y.Should().BeGreaterThan(0);
        realizedItems.Should().BeGreaterThan(0).And.BeLessThan(60);
        lastItem.IsSelected.Should().BeTrue();
        harness.ViewModel.Groups.SelectMany(group => group.Patterns).Should().HaveCount(250);
    }

    private static TextBlock FindPatternName(PatternGalleryView view, PatternGalleryItemViewModel item)
    {
        return view.GetVisualDescendants().OfType<TextBlock>()
            .Single(textBlock => ReferenceEquals(textBlock.DataContext, item)
                                 && textBlock.Text == item.Name
                                 && textBlock.IsVisible
                                 && textBlock.Bounds.Width > 0);
    }

    private static string[] VisiblePatternNames(PatternGalleryView view)
    {
        return view.GetVisualDescendants().OfType<TextBlock>()
            .Where(textBlock => textBlock.IsVisible
                                && textBlock.Bounds.Width > 0
                                && textBlock.DataContext is PatternGalleryItemViewModel)
            .Select(textBlock => textBlock.Text ?? string.Empty)
            .ToArray();
    }
}
