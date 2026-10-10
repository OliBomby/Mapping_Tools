using Avalonia.VisualTree;
using Mapping_Tools.Desktop.Controls.FeatureLoading;
using Mapping_Tools.Desktop.Tests.TestHelpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Desktop.Tests.Controls.FeatureLoading;

[TestClass]
public sealed class FeatureContentHostTests
{
    [TestMethod]
    public async Task Feature_WhenReturningToCachedFeature_PreservesStateAndRemainsInteractive()
    {
        // Arrange
        var firstFeature = new FirstFeatureViewModel();
        var secondFeature = new SecondFeatureViewModel();
        FeatureContentHost host = new() { Feature = firstFeature };
        using HeadlessViewHost window = HeadlessViewHost.Show(host);
        await HeadlessViewHost.DrainAsync(() => host.GetVisualDescendants().OfType<FirstFeatureView>().Any());
        FirstFeatureView firstView = host.GetVisualDescendants().OfType<FirstFeatureView>().Single();
        Avalonia.Controls.TextBox input = firstView.GetVisualDescendants()
            .OfType<Avalonia.Controls.TextBox>().Single();
        input.Text = "saved value";

        // Act
        host.Feature = secondFeature;
        await HeadlessViewHost.DrainAsync(() => host.GetVisualDescendants().OfType<SecondFeatureView>().Any());
        host.Feature = firstFeature;
        await HeadlessViewHost.DrainAsync(() => firstView.IsVisible);
        host.Feature = secondFeature;
        await HeadlessViewHost.DrainAsync(() => host.GetVisualDescendants().OfType<SecondFeatureView>().Single().IsVisible);
        host.Feature = firstFeature;
        await HeadlessViewHost.DrainAsync(() => firstView.IsVisible);
        Avalonia.Controls.Button button = firstView.GetVisualDescendants()
            .OfType<Avalonia.Controls.Button>().Single();
        window.Click(button);

        // Assert
        host.GetVisualDescendants().OfType<FirstFeatureView>().Should().ContainSingle()
            .Which.Should().BeSameAs(firstView);
        input.Text.Should().Be("saved value");
        firstFeature.ActionCount.Should().Be(1);
        firstView.IsHitTestVisible.Should().BeTrue();
    }

    [TestMethod]
    public async Task Feature_WhenConstructionIsPending_ShowsLoadingUntilLatestFeatureIsReady()
    {
        // Arrange
        FeatureContentHost host = new();
        using HeadlessViewHost window = HeadlessViewHost.Show(host);

        // Act
        host.Feature = new FirstFeatureViewModel();
        FeatureLoadingSkeleton loading = host.GetVisualDescendants().OfType<FeatureLoadingSkeleton>().Single();
        bool loadingWasVisible = loading.IsEffectivelyVisible;
        host.Feature = new SecondFeatureViewModel();
        await HeadlessViewHost.DrainAsync(() => host.GetVisualDescendants().OfType<SecondFeatureView>().Any());

        // Assert
        loadingWasVisible.Should().BeTrue();
        host.GetVisualDescendants().OfType<FeatureLoadingSkeleton>().Should().BeEmpty();
        host.GetVisualDescendants().OfType<SecondFeatureView>().Should().ContainSingle()
            .Which.IsVisible.Should().BeTrue();
    }

    [TestMethod]
    public async Task Feature_WhenNavigatingAwayBeforeConstruction_DoesNotPublishStaleView()
    {
        // Arrange
        FeatureContentHost host = new();
        using HeadlessViewHost window = HeadlessViewHost.Show(host);

        // Act
        host.Feature = new FirstFeatureViewModel();
        host.Feature = new SecondFeatureViewModel();
        await HeadlessViewHost.DrainAsync(() => host.GetVisualDescendants().OfType<SecondFeatureView>().Any());

        // Assert
        host.GetVisualDescendants().OfType<SecondFeatureView>().Single().IsVisible.Should().BeTrue();
        host.GetVisualDescendants().OfType<FirstFeatureView>().Should().BeEmpty();
    }

}
