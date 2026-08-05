using Mapping_Tools.Application.Settings;
using Mapping_Tools.Desktop.ViewModels;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Platform.Tests;

[TestClass]
public sealed class GetStartedViewModelTests
{
    [TestMethod]
    public void RecentMaps_WhenItemAdded_UpdatesEmptyStateAndRaisesPropertyChanged()
    {
        // Arrange
        GetStartedViewModel viewModel = CreateViewModel();
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

    private static GetStartedViewModel CreateViewModel() =>
        new(new ApplicationSettings());
}
