using Mapping_Tools.Application.Updates.Contracts;
using Mapping_Tools.Application.Updates.Models;
using Mapping_Tools.Application.Workspace.Models;
using Mapping_Tools.Desktop.Tests.TestDoubles;
using Mapping_Tools.Desktop.Shell;
using Mapping_Tools.Desktop.ViewModels.GetStarted;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Desktop.Tests.ViewModels;

[TestClass]
public sealed class GetStartedViewModelTests
{
    [TestMethod]
    public void RecentMaps_WhenItemAdded_UpdatesEmptyStateAndRaisesPropertyChanged()
    {
        // Arrange
        var viewModel = CreateViewModel();
        List<string?> changedProperties = [];
        viewModel.PropertyChanged += (_, eventArgs) =>
            changedProperties.Add(eventArgs.PropertyName);

        // Act
        viewModel.RecentMaps.Add(new RecentMapViewModel(
            "map.osu",
            @"C:\Songs\map.osu",
            "today"));

        // Assert
        viewModel.HasNoRecentMaps.Should().BeFalse();
        changedProperties.Should().ContainSingle()
            .Which.Should().Be(nameof(GetStartedViewModel.HasNoRecentMaps));
    }

    [TestMethod]
    public void Changelog_WhenGatewayReturnsMultipleReleases_DisplaysAllInGatewayOrder()
    {
        // Arrange
        using GetStartedViewModel viewModel = CreateViewModel();

        // Act
        ChangelogEntryViewModel[] entries = viewModel.Changelog.ToArray();

        // Assert
        entries.Select(entry => entry.Title).Should().Equal("Version 2.0", "Version 1.0");
        entries.Select(entry => entry.Text).Should().Equal(
            "## Improvements\n\n- A release note fetched from GitHub.",
            "Bug fixes.");
    }

    [TestMethod]
    public void SelectRecentMaps_WithMultipleRows_SetsWorkspaceSelectionInRowOrder()
    {
        // Arrange
        TestBeatmapWorkspace workspace = new();
        using GetStartedViewModel viewModel = new(
            workspace,
            new FakeUpdateGateway(),
            new ImmediateUiDispatcher());
        RecentMapViewModel first = new("one.osu", @"C:\one.osu", "today");
        RecentMapViewModel second = new("two.osu", @"C:\two.osu", "yesterday");

        // Act
        viewModel.SelectRecentMaps([first, second]);

        // Assert
        workspace.SelectedPaths.Should().Equal(@"C:\one.osu", @"C:\two.osu");
        workspace.LastSelectionSource.Should().Be(BeatmapSelectionSource.RecentHistory);
    }

    private static GetStartedViewModel CreateViewModel()
    {
        return new GetStartedViewModel(
            new TestBeatmapWorkspace(),
            new FakeUpdateGateway(),
            new ImmediateUiDispatcher());
    }

    private sealed class FakeUpdateGateway : IUpdateGateway
    {
        public Task<IReadOnlyList<UpdateReleaseNotes>> GetReleaseNotesAsync(
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<UpdateReleaseNotes>>
            ([
                new UpdateReleaseNotes(
                    "Version 2.0",
                    "## Improvements\n\n- A release note fetched from GitHub."),
                new UpdateReleaseNotes("Version 1.0", "Bug fixes.")
            ]);
        }

        public Task<UpdatePackageInfo> CheckForUpdatesAsync(
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task PrepareUpdateAsync(
            Version version,
            IProgress<double> progress,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public void LaunchUpdater(Version version, bool restartAfterUpdate)
        {
            throw new NotSupportedException();
        }

        public void Dispose()
        {
        }
    }

    private sealed class ImmediateUiDispatcher : IUiDispatcher
    {
        public void Post(Action action)
        {
            action();
        }
    }
}
