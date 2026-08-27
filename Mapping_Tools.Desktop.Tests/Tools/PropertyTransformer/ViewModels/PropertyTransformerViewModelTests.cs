using Mapping_Tools.Application.Execution.ToolExecution;
using Mapping_Tools.Application.Execution.UserNotification;
using Mapping_Tools.Application.Settings.Models;
using Mapping_Tools.Application.Tools.PropertyTransformer;
using Mapping_Tools.Desktop.Tools.PropertyTransformer.ViewModels;
using Mapping_Tools.Core.Tools.PropertyTransformer;
using Mapping_Tools.Desktop.Converters;
using Mapping_Tools.Desktop.Tests.TestDoubles;
using Mapping_Tools.Desktop.ViewModels;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Desktop.Tests.Tools.PropertyTransformer.ViewModels;

[TestClass]
public sealed class PropertyTransformerViewModelTests
{
    [TestMethod]
    public void SyncTimeFields_WhenMultiplierChanges_SynchronizesOnlyTimeFields()
    {
        // Arrange
        var viewModel = Create();
        viewModel.SyncTimeFields = true;

        // Act
        viewModel.BookmarkTimeMultiplier = 2;
        viewModel.BookmarkTimeOffset = 125;

        // Assert
        viewModel.TimingpointOffsetMultiplier.Should().Be(2);
        viewModel.HitObjectTimeMultiplier.Should().Be(2);
        viewModel.SbSampleTimeMultiplier.Should().Be(2);
        viewModel.VideoTimeMultiplier.Should().Be(2);
        viewModel.TimingpointOffsetOffset.Should().Be(125);
        viewModel.PreviewTimeOffset.Should().Be(125);
        viewModel.TimingpointBpmMultiplier.Should().Be(1);
        viewModel.HitObjectVolumeOffset.Should().Be(0);
    }

    [TestMethod]
    public void ResetCommand_WithConfiguredValues_RestoresLegacyDefaults()
    {
        // Arrange
        var viewModel = Create();
        viewModel.SyncTimeFields = true;
        viewModel.TimingpointOffsetMultiplier = 2;
        viewModel.TimingpointBpmOffset = 50;
        viewModel.MatchFilter = [100];
        viewModel.EnableFilters = true;

        // Act
        viewModel.ResetCommand.Execute(null);

        // Assert
        viewModel.TimingpointOffsetMultiplier.Should().Be(1);
        viewModel.TimingpointBpmOffset.Should().Be(0);
        viewModel.HitObjectTimeMultiplier.Should().Be(1);
        viewModel.BookmarkTimeOffset.Should().Be(0);
        viewModel.MatchFilter.Length.Should().Be(1);
        viewModel.MatchFilter[0].Should().Be(100);
        viewModel.EnableFilters.Should().BeTrue();
        viewModel.SyncTimeFields.Should().BeTrue();
    }

    [TestMethod]
    public async Task RunCommand_WithWorkspaceSelection_PassesSnapshotToServiceAndResetsProgress()
    {
        // Arrange
        RecordingPropertyTransformer service = new();
        TestBeatmapWorkspace workspace = new();
        workspace.SetSelection(["first.osu", "second.osb"]);
        var viewModel = Create(service, workspace);
        viewModel.BookmarkTimeOffset = 5;
        viewModel.ClipProperties = true;

        // Act
        await viewModel.RunCommand.ExecuteAsync(null);

        // Assert
        service.Paths.Should().Equal("first.osu", "second.osb");
        service.Options.Should().NotBeNull();
        service.Options!.BookmarkTimeOffset.Should().Be(5);
        service.Options.ClipProperties.Should().BeTrue();
        viewModel.Progress.Should().Be(0);
        viewModel.IsRunning.Should().BeFalse();
    }

    private static PropertyTransformerViewModel Create(
        RecordingPropertyTransformer? service = null,
        TestBeatmapWorkspace? workspace = null)
    {
        return new PropertyTransformerViewModel(
            service ?? new RecordingPropertyTransformer(),
            new ToolExecutionService(
                new UserNotificationService(),
                new RecordingEditorReloadService(),
                new ApplicationSettings(),
                TimeProvider.System),
            workspace ?? new TestBeatmapWorkspace());
    }

    private sealed class RecordingPropertyTransformer : IPropertyTransformerService
    {
        public IReadOnlyList<string>? Paths { get; private set; }

        public PropertyTransformerEngineOptions? Options { get; private set; }

        public Task<PropertyTransformerResult> TransformAsync(
            IReadOnlyList<string> paths,
            PropertyTransformerServiceOptions options,
            IProgress<double>? progress = null,
            CancellationToken cancellationToken = default)
        {
            Paths = paths;
            Options = options;
            progress?.Report(1);
            return Task.FromResult(new PropertyTransformerResult(paths));
        }
    }

}
