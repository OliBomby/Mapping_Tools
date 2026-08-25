using Mapping_Tools.Application.Workspace.Models;
using Mapping_Tools.Desktop.Tests.TestDoubles;
using Mapping_Tools.Desktop.ViewModels.GetStarted;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using GetStartedViewModel = Mapping_Tools.Desktop.ViewModels.GetStarted.GetStartedViewModel;

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
    public void SelectRecentMaps_WithMultipleRows_SetsWorkspaceSelectionInRowOrder()
    {
        // Arrange
        TestBeatmapWorkspace workspace = new();
        using GetStartedViewModel viewModel = new(workspace);
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
        return new GetStartedViewModel(new TestBeatmapWorkspace());
    }
}
