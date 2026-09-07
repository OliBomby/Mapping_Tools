using CommunityToolkit.Mvvm.Input;
using Mapping_Tools.Application.BeatmapEditing.Contracts;
using Mapping_Tools.Application.BeatmapEditing.Models;
using Mapping_Tools.Application.Execution.ToolExecution;
using Mapping_Tools.Application.Execution.UserNotification;
using Mapping_Tools.Application.Execution.UserNotification.Models;
using Mapping_Tools.Application.Settings.Models;
using Mapping_Tools.Application.Tools.ComboColourStudio;
using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.Tools.ComboColourStudio.Models;
using Mapping_Tools.Desktop.Tests.TestDoubles;
using Mapping_Tools.Desktop.Tools.ComboColourStudio.ViewModels;
using Mapping_Tools.Desktop.ViewModels;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Desktop.Tests.Tools.ComboColourStudio.ViewModels;

[TestClass]
public sealed class ComboColourStudioViewModelTests
{
    [TestMethod]
    public void AddColourPointCommand_AfterAddingPaletteColour_SelectsPointAndAddsSequence()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.AddComboColourCommand.Execute(null);

        // Act
        ((IRelayCommand)viewModel.AddColourPointCommand).Execute(null);
        viewModel.SelectedSequenceColour = viewModel.ComboColours[0];
        viewModel.AddSequenceColourCommand.Execute(viewModel.SelectedColourPoint);

        // Assert
        viewModel.Project.ComboColours.Should().ContainSingle();
        viewModel.Project.ColourPoints.Should().ContainSingle();
        viewModel.SelectedColourPoint.Should().NotBeNull();
        viewModel.SelectedColourPoint!.Model.Time.Should().Be(viewModel.Project.ColourPoints[0].Time);
        viewModel.SelectedColourPoint.ColourSequence.Should().ContainSingle();
    }

    [TestMethod]
    public async Task AddColourPointAtEditorTimeCommand_WithLiveEditorTimestamp_AddsPointAtThatTime()
    {
        // Arrange
        const double editorTime = 1234;
        var liveReader = new RecordingLiveBeatmapReader(new LiveBeatmapSnapshot(
            "C:/Songs/map/map.osu",
            [],
            [],
            [],
            0,
            1.4,
            1,
            5,
            4,
            editorTime));
        var viewModel = CreateViewModel(liveReader);

        // Act
        await viewModel.AddColourPointAtEditorTimeCommand.ExecuteAsync(null);

        // Assert
        viewModel.Project.ColourPoints.Should().ContainSingle()
            .Which.Time.Should().Be(editorTime);
    }

    [TestMethod]
    public async Task AddColourPointAtEditorTimeCommand_WhenEditorIsUnavailable_PublishesErrorWithoutAddingPoint()
    {
        // Arrange
        UserNotificationService notifications = new();
        List<UserNotification> published = [];
        notifications.Published += (_, eventArgs) => published.Add(eventArgs.Notification);
        var viewModel = CreateViewModel(
            new RecordingLiveBeatmapReader((LiveBeatmapSnapshot?)null),
            notifications);

        // Act
        await viewModel.AddColourPointAtEditorTimeCommand.ExecuteAsync(null);

        // Assert
        published.Should().ContainSingle();
        published[0].Severity.Should().Be(UserNotificationSeverity.Error);
        published[0].Message.Should().Contain("Open osu!");
        viewModel.Project.ColourPoints.Should().BeEmpty();
    }

    [TestMethod]
    public async Task AddColourPointAtEditorTimeCommand_WhenEditorReaderThrows_PublishesErrorWithoutAddingPoint()
    {
        // Arrange
        Exception failure = new InvalidOperationException("osu! is not running.");
        UserNotificationService notifications = new();
        List<UserNotification> published = [];
        notifications.Published += (_, eventArgs) => published.Add(eventArgs.Notification);
        var viewModel = CreateViewModel(
            new RecordingLiveBeatmapReader(failure),
            notifications);

        // Act
        await viewModel.AddColourPointAtEditorTimeCommand.ExecuteAsync(null);

        // Assert
        UserNotification notification = published.Should().ContainSingle().Which;
        notification.Severity.Should().Be(UserNotificationSeverity.Error);
        notification.Exception.Should().BeSameAs(failure);
        viewModel.Project.ColourPoints.Should().BeEmpty();
    }

    [TestMethod]
    public void RemoveColourPointCommand_WithMultipleSelectedPoints_RemovesEverySelectedPoint()
    {
        // Arrange
        var viewModel = CreateViewModel();
        ((IRelayCommand)viewModel.AddColourPointCommand).Execute(null);
        ((IRelayCommand)viewModel.AddColourPointCommand).Execute(null);
        ((IRelayCommand)viewModel.AddColourPointCommand).Execute(null);
        var points = viewModel.ColourPoints.ToArray();
        viewModel.SetSelectedColourPoints([points[0], points[2]]);

        // Act
        ((IRelayCommand)viewModel.RemoveColourPointCommand).Execute(null);

        // Assert
        viewModel.ColourPoints.Should().ContainSingle().Which.Should().Be(points[1]);
        viewModel.Project.ColourPoints.Should().ContainSingle();
        viewModel.SelectedColourPoint.Should().Be(points[1]);
    }

    [TestMethod]
    public void RemoveColourPointCommand_WithoutSelection_RemovesLastPoint()
    {
        // Arrange
        var viewModel = CreateViewModel();
        ((IRelayCommand)viewModel.AddColourPointCommand).Execute(null);
        ((IRelayCommand)viewModel.AddColourPointCommand).Execute(null);
        var points = viewModel.ColourPoints.ToArray();
        viewModel.SetSelectedColourPoints([]);

        // Act
        ((IRelayCommand)viewModel.RemoveColourPointCommand).Execute(null);

        // Assert
        viewModel.ColourPoints.Should().ContainSingle().Which.Should().Be(points[0]);
    }

    [TestMethod]
    public void RemoveSequenceColourAt_WithRepeatedPaletteReference_RemovesRequestedOccurrence()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.AddComboColourCommand.Execute(null);
        viewModel.AddComboColourCommand.Execute(null);
        ((IRelayCommand)viewModel.AddColourPointCommand).Execute(null);
        viewModel.SelectedSequenceColour = viewModel.ComboColours[0];
        viewModel.AddSequenceColourCommand.Execute(viewModel.SelectedColourPoint);
        viewModel.SelectedSequenceColour = viewModel.ComboColours[1];
        viewModel.AddSequenceColourCommand.Execute(viewModel.SelectedColourPoint);
        viewModel.SelectedSequenceColour = viewModel.ComboColours[0];
        viewModel.AddSequenceColourCommand.Execute(viewModel.SelectedColourPoint);

        // Act
        viewModel.RemoveSequenceColourAt(2);

        // Assert
        viewModel.SelectedColourPoint!.ColourSequence
            .Select(colour => colour.Name)
            .Should()
            .Equal("Combo1", "Combo2");
    }

    [TestMethod]
    public void AddSequenceColour_WithExplicitPaletteColour_AppendsThatColour()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.AddComboColourCommand.Execute(null);
        viewModel.AddComboColourCommand.Execute(null);
        ((IRelayCommand)viewModel.AddColourPointCommand).Execute(null);
        var point = viewModel.SelectedColourPoint!;
        var colour = viewModel.ComboColours[1];

        // Act
        viewModel.AddSequenceColour(point, colour);

        // Assert
        point.ColourSequence.Should().ContainSingle().Which.Should().BeSameAs(colour);
        point.ColourSequence[0].Name.Should().Be("Combo2");
    }

    private static ComboColourStudioViewModel CreateViewModel(
        ILiveBeatmapReader? liveReader = null,
        IUserNotificationService? notifications = null)
    {
        IUserNotificationService notificationService = notifications ?? new UserNotificationService();
        return new ComboColourStudioViewModel(
            new StubComboColourStudioService(),
            new ToolExecutionService(
                notificationService,
                new RecordingEditorReloadService(),
                new ApplicationSettings(),
                TimeProvider.System),
            notificationService,
            new TestBeatmapWorkspace(),
            new RecordingCurrentBeatmapLocator(),
            liveReader ?? new RecordingLiveBeatmapReader((LiveBeatmapSnapshot?)null),
            new TestFilePicker());
    }

    private sealed class StubComboColourStudioService : IComboColourStudioService
    {
        public Task<ComboColourEngineOptions> ImportComboColoursAsync(
            string path,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new ComboColourEngineOptions());

        public Task<ComboColourEngineOptions> ImportColourHaxAsync(
            string path,
            int maxBurstLength,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new ComboColourEngineOptions { MaxBurstLength = maxBurstLength });
        }

        public Task<ComboColourStudioRunResult> ApplyAsync(
            IReadOnlyList<string> paths,
            ComboColourServiceOptions project,
            IProgress<double>? progress = null,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new ComboColourStudioRunResult(paths.Count));
        }
    }

}
