using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.VisualTree;
using Mapping_Tools.Application.Localization;
using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Desktop.Controls;
using Mapping_Tools.Desktop.Tests.TestHelpers;
using Mapping_Tools.Desktop.Tests.Tools.GeometryDashboard.ViewModels;
using Mapping_Tools.Desktop.Tests.Tools.HitsoundStudio.ViewModels;
using Mapping_Tools.Desktop.Tests.Tools.PatternGallery.Views;
using Mapping_Tools.Desktop.Tools.ComboColourStudio.Views;
using Mapping_Tools.Desktop.Tools.GeometryDashboard.ViewModels;
using Mapping_Tools.Desktop.Tools.GeometryDashboard.Views;
using Mapping_Tools.Desktop.Tools.HitsoundStudio.Views;
using Mapping_Tools.Desktop.Tools.PatternGallery.ViewModels;
using Mapping_Tools.Desktop.Tools.PatternGallery.Views;
using System.Globalization;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Desktop.Tests.Localization;

[TestClass]
[DoNotParallelize]
public sealed class LocalizedViewRegressionTests
{
    [TestMethod]
    public void TableHeaders_LanguageChanges_RenderTranslatedLabelsInExistingViews()
    {
        // Arrange
        string? previous = TranslationManager.Language;
        TranslationManager.SetLanguage("en");
        using var dashboard = GeometryDashboardViewModelTestFactory.CreateViewModel();
        GeometryDashboardSavestatesViewModel saves = new(dashboard.Project, _ => { }, () => { });
        GeometryDashboardSavestatesWindow saveWindow = new() { DataContext = saves };
        using var saveHost = HeadlessViewHost.ShowWindow(saveWindow);
        using var studio = HitsoundStudioViewModelTestFactory.CreateMixedSelection().ViewModel;
        HitsoundStudioView studioView = new() { DataContext = studio };
        using var studioHost = HeadlessViewHost.Show(studioView);
        var saveTable = saveWindow.GetVisualDescendants().OfType<MaterialGridListView>().Single();
        var layerTable = studioView.GetVisualDescendants().OfType<MaterialGridListView>().Single();

        try
        {
            // Act
            TranslationManager.SetLanguage("nl");
            HeadlessViewHost.RunDispatcherJobs();

            // Assert
            saveTable.Columns.Take(2).Select(column => column.Header).Should().Equal("Naam", "Sneltoets");
            layerTable.Columns.Select(column => column.Header).Should().Equal(
                "Naam", "Aantal", "Sampleset", "Hitsound", "Samplepad");
            saveTable.GetVisualDescendants().OfType<TextBlock>().Select(block => block.Text)
                .Should().Contain("Sneltoets");
            layerTable.GetVisualDescendants().OfType<TextBlock>().Select(block => block.Text)
                .Should().Contain("Samplepad");
        }
        finally
        {
            TranslationManager.SetLanguage(previous);
        }
    }

    [TestMethod]
    public void GeneratorGroupHeading_Dutch_RendersOneCountWithOriginalColors()
    {
        // Arrange
        string? previous = TranslationManager.Language;
        TranslationManager.SetLanguage("nl");
        using var dashboard = GeometryDashboardViewModelTestFactory.CreateViewModel();
        GeometryDashboardView view = new() { DataContext = dashboard };
        using var host = HeadlessViewHost.Show(view, height: 1200);

        try
        {
            // Act
            var headings = view.GetVisualDescendants().OfType<Expander>()
                .Where(expander => expander.DataContext is GeometryDashboardGeneratorGroupViewModel)
                .Select(expander => (Group: (GeometryDashboardGeneratorGroupViewModel)expander.DataContext!,
                    Blocks: ((StackPanel)expander.Header!).Children.OfType<TextBlock>().ToArray()))
                .ToArray();

            // Assert
            headings.Should().NotBeEmpty();
            foreach (var heading in headings)
            {
                heading.Blocks.Select(block => block.Text).Should().Equal(
                    heading.Group.Name, heading.Group.ItemCount.ToString(CultureInfo.InvariantCulture),
                    heading.Group.ItemCount == 1 ? " item" : " items");
                heading.Blocks[1].Foreground.Should().BeAssignableTo<ISolidColorBrush>().Which.Color.Should().Be(Colors.Green);
                heading.Blocks[1].FontWeight.Should().Be(FontWeight.Bold);
                heading.Blocks[2].Foreground.Should().BeAssignableTo<ISolidColorBrush>().Which.Color.Should().Be(Colors.Silver);
                heading.Blocks[2].FontStyle.Should().Be(FontStyle.Italic);
            }
        }
        finally
        {
            TranslationManager.SetLanguage(previous);
        }
    }

    [DataTestMethod]
    [DataRow("en", 1, " item")]
    [DataRow("en", 3, " items")]
    [DataRow("nl", 1, " item")]
    [DataRow("nl", 3, " items")]
    public void PatternGroupHeading_LanguageChanges_RetainsOneCountWithOriginalColors(
        string language, int count, string suffix)
    {
        // Arrange
        string? previous = TranslationManager.Language;
        TranslationManager.SetLanguage(language == "en" ? "nl" : "en");
        PatternGalleryViewTestHarness harness = PatternGalleryViewTestHarness.Create(patternCount: count);
        PatternGalleryView view = new() { DataContext = harness.ViewModel };
        using var host = HeadlessViewHost.Show(view);

        try
        {
            // Act
            TranslationManager.SetLanguage(language);
            HeadlessViewHost.RunDispatcherJobs();
            Expander heading = view.GetVisualDescendants().OfType<Expander>()
                .Single(expander => expander.DataContext is PatternGalleryGroupViewModel);
            var group = (PatternGalleryGroupViewModel)heading.DataContext!;
            TextBlock[] blocks = ((StackPanel)heading.Header!).Children.OfType<TextBlock>().ToArray();

            // Assert
            blocks.Select(block => block.Text).Should().Equal(group.Name, count.ToString(CultureInfo.InvariantCulture), suffix);
            blocks[1].Foreground.Should().BeAssignableTo<ISolidColorBrush>().Which.Color.Should().Be(Colors.Green);
            blocks[1].FontWeight.Should().Be(FontWeight.Bold);
            blocks[2].Foreground.Should().BeAssignableTo<ISolidColorBrush>().Which.Color.Should().Be(Colors.Silver);
            blocks[2].FontStyle.Should().Be(FontStyle.Italic);
        }
        finally
        {
            TranslationManager.SetLanguage(previous);
        }
    }

    [TestMethod]
    public void GeneratorButtons_LanguageChanges_DisplayCompleteDutchLabels()
    {
        // Arrange
        string? previous = TranslationManager.Language;
        TranslationManager.SetLanguage("en");
        using var dashboard = GeometryDashboardViewModelTestFactory.CreateViewModel();
        GeometryDashboardView view = new() { DataContext = dashboard };
        using var host = HeadlessViewHost.Show(view, height: 1200);

        try
        {
            // Act
            TranslationManager.SetLanguage("nl");
            HeadlessViewHost.RunDispatcherJobs();
            var buttons = view.GetVisualDescendants().OfType<Button>()
                .Where(button => Equals(button.Content, "Configureren")).ToArray();

            // Assert
            buttons.Should().NotBeEmpty();
            foreach (Button button in buttons)
            {
                TextBlock label = button.GetVisualDescendants().OfType<TextBlock>()
                    .Single(block => block.Text == "Configureren");
                double textWidth = label.TextLayout.TextLines.Max(line => line.WidthIncludingTrailingWhitespace);
                label.Bounds.Width.Should().BeGreaterThanOrEqualTo(textWidth);
            }
        }
        finally
        {
            TranslationManager.SetLanguage(previous);
        }
    }

    [TestMethod]
    public void DefaultSampleSet_LanguageChanges_PreservesAutoLabelAndEnumValue()
    {
        // Arrange
        string? previous = TranslationManager.Language;
        TranslationManager.SetLanguage("en");
        using var studio = HitsoundStudioViewModelTestFactory.CreateMixedSelection().ViewModel;
        studio.DefaultSample.SampleSet = SampleSet.None;
        HitsoundStudioView view = new() { DataContext = studio };
        using var host = HeadlessViewHost.Show(view, height: 1100);
        ComboBox picker = view.GetVisualDescendants().OfType<ComboBox>()
            .Single(combo => ReferenceEquals(combo.ItemsSource, studio.DefaultSampleSets));
        string[] initialText = picker.GetVisualDescendants().OfType<TextBlock>().Select(block => block.Text ?? "").ToArray();

        try
        {
            // Act
            TranslationManager.SetLanguage("nl");
            HeadlessViewHost.RunDispatcherJobs();

            // Assert
            initialText.Should().Contain("Auto");
            picker.GetVisualDescendants().OfType<TextBlock>().Select(block => block.Text).Should().Contain("Auto");
            picker.SelectedItem.Should().Be(SampleSet.None);
            studio.DefaultSample.SampleSet.Should().Be(SampleSet.None);
        }
        finally
        {
            TranslationManager.SetLanguage(previous);
        }
    }

    [TestMethod]
    public void ImportDialog_Dutch_RetainsOuterPaddingAndGapAboveActions()
    {
        // Arrange
        string? previous = TranslationManager.Language;
        TranslationManager.SetLanguage("nl");
        ComboColourStudioImportDialog view = new();
        using var host = HeadlessViewHost.Show(view);

        try
        {
            // Act
            Grid layout = (Grid)view.Content!;
            var field = layout.Children.OfType<Grid>().Single();
            var actions = layout.Children.OfType<StackPanel>().Single();

            // Assert
            layout.Margin.Should().Be(new Thickness(8));
            (actions.Bounds.Y - field.Bounds.Bottom).Should().BeGreaterThanOrEqualTo(16);
            actions.Bounds.Bottom.Should().BeLessThanOrEqualTo(layout.Bounds.Height);
        }
        finally
        {
            TranslationManager.SetLanguage(previous);
        }
    }
}
