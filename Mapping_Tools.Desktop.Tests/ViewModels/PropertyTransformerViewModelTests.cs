using CommunityToolkit.Mvvm.Input;
using Mapping_Tools.Application.BeatmapEditing;
using Mapping_Tools.Application.Execution;
using Mapping_Tools.Application.PropertyTransformer;
using Mapping_Tools.Application.Settings;
using Mapping_Tools.Application.Workspace;
using Mapping_Tools.Core.Tools.PropertyTransformer;
using Mapping_Tools.Desktop.Converters;
using Mapping_Tools.Desktop.Tests.TestDoubles;
using Mapping_Tools.Desktop.ViewModels;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Desktop.Tests.ViewModels;

[TestClass]
public sealed class PropertyTransformerViewModelTests
{
    [TestMethod]
    public void SyncTimeFields_WhenMultiplierChanges_SynchronizesOnlyTimeFields()
    {
        // Arrange
        PropertyTransformerViewModel viewModel = Create();
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
        PropertyTransformerViewModel viewModel = Create();
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
        PropertyTransformerViewModel viewModel = Create(service, workspace);
        viewModel.BookmarkTimeOffset = 5;
        viewModel.ClipProperties = true;

        // Act
        await ((IAsyncRelayCommand)viewModel.RunCommand).ExecuteAsync(null);

        // Assert
        service.Paths.Should().Equal("first.osu", "second.osb");
        service.Options.Should().NotBeNull();
        service.Options!.BookmarkTimeOffset.Should().Be(5);
        service.Options.ClipProperties.Should().BeTrue();
        viewModel.Progress.Should().Be(0);
        viewModel.IsRunning.Should().BeFalse();
    }

    [TestMethod]
    public void DoubleArrayToStringConverter_WithInvariantValues_RoundTripsText()
    {
        // Arrange
        DoubleArrayToStringConverter converter = new();

        // Act
        object text = converter.Convert(
            new[] { 1.25, 2.5 },
            typeof(string),
            null,
            System.Globalization.CultureInfo.GetCultureInfo("nl-NL"));
        object values = converter.ConvertBack(
            text,
            typeof(double[]),
            null,
            System.Globalization.CultureInfo.GetCultureInfo("nl-NL"));

        // Assert
        text.Should().Be("1.25, 2.5");
        values.Should().BeEquivalentTo(new[] { 1.25, 2.5 });
    }

    private static PropertyTransformerViewModel Create(
        RecordingPropertyTransformer? service = null,
        TestBeatmapWorkspace? workspace = null)
    {
        return new PropertyTransformerViewModel(
            service ?? new RecordingPropertyTransformer(),
            new ToolExecutionService(
                new UserNotificationService(),
                new StubReloadService(),
                new ApplicationSettings(),
                TimeProvider.System),
            workspace ?? new TestBeatmapWorkspace());
    }

    private sealed class RecordingPropertyTransformer : IPropertyTransformerService
    {
        public IReadOnlyList<string>? Paths { get; private set; }

        public PropertyTransformerOptions? Options { get; private set; }

        public Task<PropertyTransformerResult> TransformAsync(
            IReadOnlyList<string> paths,
            PropertyTransformerOptions options,
            IProgress<double>? progress = null,
            CancellationToken cancellationToken = default)
        {
            Paths = paths;
            Options = options;
            progress?.Report(100);
            return Task.FromResult(new PropertyTransformerResult(paths));
        }
    }

    private sealed class StubReloadService : IEditorReloadService
    {
        public Task ReloadAsync(CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
