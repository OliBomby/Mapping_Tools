using System.Diagnostics.CodeAnalysis;
using Avalonia.Controls;
using Avalonia.Interactivity;
using CommunityToolkit.Mvvm.Input;
using Mapping_Tools.Application.Localization;
using Mapping_Tools.Desktop.Plugin;
using Mapping_Tools.Desktop.Shell;
using Mapping_Tools.Desktop.Tests.TestHelpers;
using Material.Icons;
using Material.Icons.Avalonia;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Desktop.Tests.Shell;

[TestClass]
public sealed class ShellMenuHostTests
{
    [TestMethod]
    public void SetRegistrations_AllLocations_PreservesBuiltInsAndRendersNestedCommands()
    {
        // Arrange
        MenuItem file = new() { Header = "File" };
        MenuItem about = new() { Header = "About" };
        MenuItem builtIn = new() { Header = "Built in" };
        file.Items.Add(builtIn);
        Menu menu = new();
        menu.Items.Add(file);
        menu.Items.Add(about);
        ShellMenuHost shell = new(menu, file, about);
        using var host = HeadlessViewHost.Show(menu);
        int executed = 0;
        RelayCommand command = new(() => executed++);

        // Act
        shell.SetRegistrations(
        [
            new(ShellMenuLocation.File, new ShellMenuItem("Folder", command,
                icon: MaterialIconKind.FolderOpen, toolTip: "Open a folder")),
            new(ShellMenuLocation.About, new ShellMenuItem("Verifier", command)),
            new(ShellMenuLocation.MenuBar, new ShellMenuItem("_Extras", items:
                [new ShellMenuItem("Nested", items: [new ShellMenuItem("Launch", command)])])),
        ]);
        var extras = (MenuItem)menu.Items[2]!;
        var nested = (MenuItem)extras.Items[0]!;
        var launch = (MenuItem)nested.Items[0]!;
        launch.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));

        // Assert
        file.Items[0].Should().BeSameAs(builtIn);
        ((MenuItem)file.Items[1]!).Header.Should().Be("Folder");
        ((MenuItem)file.Items[1]!).Icon.Should().BeOfType<MaterialIcon>().Which.Kind.Should().Be(MaterialIconKind.FolderOpen);
        ToolTip.GetTip((MenuItem)file.Items[1]!).Should().Be("Open a folder");
        ((MenuItem)about.Items[0]!).Header.Should().Be("Verifier");
        extras.Header.Should().Be("_Extras");
        extras.Height.Should().Be(35);
        launch.Command.Should().BeSameAs(command);
        executed.Should().Be(1);
    }

    [TestMethod]
    [SuppressMessage("ReSharper", "AccessToModifiedClosure", Justification = "The command deliberately observes changing context while its menu is open.")]
    public void SetRegistrations_CommandAvailabilityChanges_UpdatesEnabledState()
    {
        // Arrange
        MenuItem file = new() { Header = "File" };
        MenuItem about = new() { Header = "About" };
        Menu menu = new();
        menu.Items.Add(file);
        menu.Items.Add(about);
        ShellMenuHost shell = new(menu, file, about);
        bool available = false;
        RelayCommand command = new(() => { }, () => available);
        shell.SetRegistrations([new(ShellMenuLocation.File, new ShellMenuItem("Open folder", command))]);
        using var host = HeadlessViewHost.Show(menu);
        host.Click(file);
        var item = (MenuItem)file.Items[0]!;
        bool initiallyEnabled = item.IsEffectivelyEnabled;

        // Act
        available = true;
        command.NotifyCanExecuteChanged();
        HeadlessViewHost.RunDispatcherJobs();

        // Assert
        initiallyEnabled.Should().BeFalse();
        item.IsEffectivelyEnabled.Should().BeTrue();
    }

    [TestMethod]
    public void SetRegistrations_ReplacedOrCleared_RemovesOnlyPreviousPluginItems()
    {
        // Arrange
        MenuItem file = new();
        MenuItem about = new();
        MenuItem builtIn = new();
        file.Items.Add(builtIn);
        Menu menu = new();
        menu.Items.Add(file);
        menu.Items.Add(about);
        ShellMenuHost shell = new(menu, file, about);
        ShellMenuRegistration registration = new(ShellMenuLocation.File,
            new ShellMenuItem("Folder", new RelayCommand(() => { })));
        shell.SetRegistrations([registration]);
        shell.SetRegistrations([registration]);
        int itemCountAfterReplacement = file.Items.Count;

        // Act
        shell.SetRegistrations([]);

        // Assert
        itemCountAfterReplacement.Should().Be(2);
        file.Items.Should().ContainSingle().Which.Should().BeSameAs(builtIn);
        menu.Items.Should().Equal(file, about);
    }

    [TestMethod]
    public void SetRegistrations_LanguageChanges_RefreshesExistingLabelsAndTooltips()
    {
        // Arrange
        string? previous = TranslationManager.Language;
        TranslationManager.SetLanguage("en");
        MenuItem file = new();
        MenuItem about = new();
        Menu menu = new();
        menu.Items.Add(file);
        menu.Items.Add(about);
        ShellMenuHost shell = new(menu, file, about);
        shell.SetRegistrations([new(ShellMenuLocation.File, new ShellMenuItem("Folder",
            new RelayCommand(() => { }),
            translatedHeaderGetter: () => TranslationManager.Language == "nl" ? "Map" : "Folder",
            translatedToolTipGetter: () => TranslationManager.Language == "nl" ? "Open map" : "Open folder"))]);
        using var host = HeadlessViewHost.Show(menu);
        var item = (MenuItem)file.Items[0]!;

        try
        {
            // Act
            TranslationManager.SetLanguage("nl");
            HeadlessViewHost.RunDispatcherJobs();

            // Assert
            item.Header.Should().Be("Map");
            ToolTip.GetTip(item).Should().Be("Open map");
        }
        finally
        {
            TranslationManager.SetLanguage(previous);
        }
    }
}
