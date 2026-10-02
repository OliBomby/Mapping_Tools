using Avalonia.Controls;
using Avalonia.VisualTree;
using Mapping_Tools.Desktop.Tests.TestDoubles;
using Mapping_Tools.Desktop.Tests.TestHelpers;
using Mapping_Tools.Desktop.Tools.PatternGallery.Models;
using Mapping_Tools.Desktop.Tools.PatternGallery.ViewModels;
using Mapping_Tools.Desktop.Tools.PatternGallery.Views;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Desktop.Tests.Tools.PatternGallery.Views;

[TestClass]
public sealed class PatternGalleryImportDialogViewTests
{
    [TestMethod]
    public void CodeImportAcceptButton_WithValidFields_ReturnsEnteredCode()
    {
        // Arrange
        PatternGalleryCodeImportViewModel viewModel = new("Pattern")
        {
            HitObjects = "1,2,3,4,5,1,0,0:0:0:0:",
            TimingPoints = "0,500",
            GlobalSv = 1.25,
        };
        object? result = null;
        viewModel.Close = value => result = value;
        PatternGalleryCodeImportDialog view = new() { DataContext = viewModel };
        using HeadlessViewHost host = HeadlessViewHost.Show(view, height: 340);
        Button accept = FindAction(host, "ACCEPT");

        // Act
        host.Click(accept);

        // Assert
        PatternGalleryCodeInput input = result.Should().BeOfType<PatternGalleryCodeInput>().Subject;
        input.Name.Should().Be("Pattern");
        input.HitObjects.Should().Be("1,2,3,4,5,1,0,0:0:0:0:");
        input.TimingPoints.Should().Be("0,500");
        input.GlobalSv.Should().Be(1.25);
    }

    [TestMethod]
    public void CodeImportAcceptButton_WithBlankName_LeavesDialogOpenAndShowsValidation()
    {
        // Arrange
        PatternGalleryCodeImportViewModel viewModel = new("Pattern");
        int closeCount = 0;
        viewModel.Close = _ => closeCount++;
        PatternGalleryCodeImportDialog view = new() { DataContext = viewModel };
        using HeadlessViewHost host = HeadlessViewHost.Show(view);
        TextBox name = view.GetVisualDescendants().OfType<TextBox>().Single(textBox =>
            ToolTip.GetTip(textBox)?.ToString() == "The name for the pattern.");

        // Act
        host.Click(name);
        host.PressKey(Avalonia.Input.Key.A, Avalonia.Input.RawInputModifiers.Control,
            Avalonia.Input.PhysicalKey.A, "a");
        host.TypeText(" ");
        host.Click(FindAction(host, "ACCEPT"));

        // Assert
        closeCount.Should().Be(0);
        name.Text.Should().Be(" ");
        DataValidationErrors.GetHasErrors(name).Should().BeTrue();
        DataValidationErrors.GetErrors(name).Should().Contain("A pattern name is required.");
    }

    [TestMethod]
    public void CodeImportDialog_WithLongCode_KeepsAcceptAndCancelReachable()
    {
        // Arrange
        PatternGalleryCodeImportViewModel viewModel = new("Pattern")
        {
            HitObjects = string.Join(Environment.NewLine, Enumerable.Repeat("1,2,3,4,5,1,0,0:0:0:0:", 80)),
            TimingPoints = string.Join(Environment.NewLine, Enumerable.Repeat("0,500", 80)),
        };
        object? result = null;
        viewModel.Close = value => result = value;
        PatternGalleryCodeImportDialog view = new() { DataContext = viewModel };
        using HeadlessViewHost host = HeadlessViewHost.Show(view, height: 340);
        Button accept = FindAction(host, "ACCEPT");
        Button cancel = FindAction(host, "CANCEL");

        // Act
        host.Click(accept);

        // Assert
        accept.IsVisible.Should().BeTrue();
        accept.Bounds.Height.Should().BeGreaterThan(0);
        cancel.IsVisible.Should().BeTrue();
        cancel.Bounds.Height.Should().BeGreaterThan(0);
        result.Should().BeOfType<PatternGalleryCodeInput>();
    }

    [TestMethod]
    public void FileImportAcceptButton_WithValidPath_ReturnsSelectedPath()
    {
        // Arrange
        PatternGalleryFileImportViewModel viewModel = new(
            "Pattern",
            "C:/Maps/pattern.osu",
            new TestFilePicker(),
            new TestCurrentBeatmapDialogService(),
            new TestBeatmapWorkspace());
        object? result = null;
        viewModel.Close = value => result = value;
        PatternGalleryFileImportDialog view = new() { DataContext = viewModel };
        using HeadlessViewHost host = HeadlessViewHost.Show(view);

        // Act
        host.Click(FindAction(host, "ACCEPT"));

        // Assert
        PatternGalleryFileInput input = result.Should().BeOfType<PatternGalleryFileInput>().Subject;
        input.FilePath.Should().Be("C:/Maps/pattern.osu");
    }

    [TestMethod]
    public void CollectionRenameAcceptButton_WithTwoNames_ReturnsBothNames()
    {
        // Arrange
        PatternGalleryCollectionRenameViewModel viewModel = new("Collection", "Folder");
        object? result = null;
        viewModel.Close = value => result = value;
        PatternGalleryCollectionRenameDialog view = new() { DataContext = viewModel };
        using HeadlessViewHost host = HeadlessViewHost.Show(view);
        TextBox[] fields = host.Window.GetVisualDescendants().OfType<TextBox>()
            .Where(textBox => textBox.IsVisible && textBox.Bounds.Width > 0)
            .ToArray();
        TextBox newName = fields.Single(textBox => textBox.Text == "Collection");
        TextBox newFolder = fields.Single(textBox => textBox.Text == "Folder");

        // Act
        host.Click(newName);
        host.PressKey(Avalonia.Input.Key.A, Avalonia.Input.RawInputModifiers.Control,
            Avalonia.Input.PhysicalKey.A, "a");
        host.TypeText("Renamed collection");
        host.Click(newFolder);
        host.PressKey(Avalonia.Input.Key.A, Avalonia.Input.RawInputModifiers.Control,
            Avalonia.Input.PhysicalKey.A, "a");
        host.TypeText("renamed-folder");
        host.Click(FindAction(host, "ACCEPT"));

        // Assert
        PatternGalleryCollectionRenameInput input = result.Should().BeOfType<PatternGalleryCollectionRenameInput>().Subject;
        input.NewName.Should().Be("Renamed collection");
        input.NewFolderName.Should().Be("renamed-folder");
    }

    [TestMethod]
    public void CollectionRenameCancelButton_DismissesWithoutChangingNames()
    {
        // Arrange
        PatternGalleryCollectionRenameViewModel viewModel = new("Collection", "Folder");
        object? result = new();
        viewModel.Close = value => result = value;
        PatternGalleryCollectionRenameDialog view = new() { DataContext = viewModel };
        using HeadlessViewHost host = HeadlessViewHost.Show(view);

        // Act
        host.Click(FindAction(host, "CANCEL"));

        // Assert
        result.Should().BeNull();
        viewModel.NewName.Should().Be("Collection");
        viewModel.NewFolderName.Should().Be("Folder");
    }

    private static Button FindAction(HeadlessViewHost host, string content)
    {
        return host.Window.GetVisualDescendants().OfType<Button>()
                   .Single(button => string.Equals(button.Content?.ToString(), content, StringComparison.Ordinal));
    }
}
