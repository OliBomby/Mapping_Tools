using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;
using Mapping_Tools.Desktop.Tests.TestHelpers;
using Mapping_Tools.Desktop.Localization;
using Mapping_Tools.Desktop.Tests.Tools.GeometryDashboard.ViewModels;
using Mapping_Tools.Desktop.Tools.GeometryDashboard.ViewModels;
using Mapping_Tools.Desktop.Tools.GeometryDashboard.Views;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Desktop.Tests.Tools.GeometryDashboard.Views;

[TestClass]
public sealed class GeometryDashboardGeneratorSettingsWindowTests
{
    [TestMethod]
    public void AddPredicate_ConfigureRulesAndApply_CopiesSelectedPredicate()
    {
        // Arrange
        using GeometryDashboardViewModel dashboard = GeometryDashboardViewModelTestFactory.CreateViewModel();
        var generator = dashboard.Generators[0];
        int originalPredicateCount = generator.Settings.InputPredicate.Predicates.Count;
        GeometryDashboardGeneratorSettingsDialogViewModel viewModel = new(generator.Settings);
        GeometryDashboardGeneratorSettingsWindow window = new() { DataContext = viewModel };
        viewModel.Close = result => window.Close(result);
        using HeadlessViewHost host = HeadlessViewHost.ShowWindow(window);
        Button addPredicate = window.GetVisualDescendants().OfType<Button>()
            .Single(button => Equals(ToolTip.GetTip(button), DesktopStrings.GeometryDashboard_AddPredicateTip));
        Button apply = window.GetVisualDescendants().OfType<Button>()
            .Single(button => Equals(button.Content, DesktopStrings.GeometryDashboard_Apply));

        // Act
        host.Click(addPredicate);
        var predicate = viewModel.InputPredicateRows.Last();
        ListBoxItem needSelected = window.GetVisualDescendants().OfType<ListBoxItem>()
            .Single(item => ReferenceEquals(item.DataContext, predicate)
                            && Equals(ToolTip.GetTip(item), DesktopStrings.GeometryDashboard_NeedSelected));
        host.Click(needSelected);
        TextBox minimumRelevancy = window.GetVisualDescendants().OfType<TextBox>()
            .Single(textBox => ReferenceEquals(textBox.DataContext, predicate)
                               && Equals(ToolTip.GetTip(textBox), DesktopStrings.GeometryDashboard_MinRelevancyTip));
        host.Click(minimumRelevancy);
        host.PressKey(Key.A, RawInputModifiers.Control, PhysicalKey.A, "a");
        host.TypeText("0.8");
        host.Click(apply);

        // Assert
        viewModel.InputPredicateRows.Should().HaveCount(originalPredicateCount + 1);
        generator.Settings.InputPredicate.Predicates.Should().HaveCount(originalPredicateCount + 1);
        generator.Settings.InputPredicate.Predicates.Last().NeedSelected.Should().BeTrue();
        generator.Settings.InputPredicate.Predicates.Last().MinRelevancy.Should().Be(0.8);
    }
}
