using Mapping_Tools.Application.Platform;
using Mapping_Tools.Application.Platform.FilePicker;
using Mapping_Tools.Desktop.Platform;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Desktop.Tests.Platform;

[TestClass]
public sealed class AvaloniaFilePickerTests
{
    [TestMethod]
    public void Capabilities_WithoutTopLevel_AreFalse()
    {
        // Arrange
        // Act
        AvaloniaFilePicker picker = new(() => null);

        // Assert
        picker.CanOpenFiles.Should().BeFalse();
        picker.CanSaveFiles.Should().BeFalse();
        picker.CanPickFolders.Should().BeFalse();
    }

    [TestMethod]
    public async Task PickOpenFilesAsync_WithoutTopLevel_ThrowsPlatformNotSupportedException()
    {
        // Arrange
        AvaloniaFilePicker picker = new(() => null);

        // Act
        Func<Task> act1 = () => picker.PickOpenFilesAsync(new OpenFilePickerRequest());

        // Assert
        await act1.Should().ThrowAsync<PlatformNotSupportedException>();
    }

    [TestMethod]
    public async Task PickFoldersAsync_WithPreCancelledToken_ThrowsWithoutPlatformAccess()
    {
        // Arrange
        bool accessed = false;
        AvaloniaFilePicker picker = new(() =>
        {
            accessed = true;
            return null;
        });
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();

        // Act
        Func<Task> act2 = () => picker.PickFoldersAsync(
            new OpenFolderPickerRequest(),
            cancellation.Token);

        // Assert
        await act2.Should().ThrowAsync<OperationCanceledException>();

        accessed.Should().BeFalse();
    }

    [TestMethod]
    public void MapFilters_WithCrossPlatformIdentifiers_MapsAllValues()
    {
        // Arrange
        FilePickerFilter filter = new(
            "Audio",
            ["*.wav", "*.ogg"],
            ["audio/wav", "audio/ogg"],
            ["com.microsoft.waveform-audio"]);

        // Act
        var mapped = AvaloniaFilePicker.MapFilters([filter]).Single();

        // Assert
        mapped.Name.Should().Be("Audio");
        mapped.Patterns!.ToArray().Should().Equal(filter.Patterns.ToArray());
        mapped.MimeTypes!.ToArray().Should().Equal(filter.MimeTypes.ToArray());
        mapped.AppleUniformTypeIdentifiers!.ToArray().Should().Equal(filter.AppleUniformTypeIdentifiers.ToArray());
    }
}
