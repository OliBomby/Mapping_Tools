using Mapping_Tools.ApplicationServices.Platform;
using Mapping_Tools.Desktop.Platform;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Platform.Tests;

[TestClass]
public sealed class AvaloniaFilePickerTests
{
    [TestMethod]
    public void CapabilitiesAreFalseBeforeTopLevelIsAvailable()
    {
        AvaloniaFilePicker picker = new(() => null);

        Assert.IsFalse(picker.CanOpenFiles);
        Assert.IsFalse(picker.CanSaveFiles);
        Assert.IsFalse(picker.CanPickFolders);
    }

    [TestMethod]
    public async Task UnsupportedPickerOperationHasExplicitFailure()
    {
        AvaloniaFilePicker picker = new(() => null);

        await Assert.ThrowsExceptionAsync<PlatformNotSupportedException>(
            () => picker.PickOpenFilesAsync(new OpenFilePickerRequest()));
    }

    [TestMethod]
    public async Task PreCancelledPickerOperationDoesNotAccessPlatform()
    {
        bool accessed = false;
        AvaloniaFilePicker picker = new(() =>
        {
            accessed = true;
            return null;
        });
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();

        await Assert.ThrowsExceptionAsync<OperationCanceledException>(
            () => picker.PickFoldersAsync(
                new OpenFolderPickerRequest(),
                cancellation.Token));

        Assert.IsFalse(accessed);
    }

    [TestMethod]
    public void FiltersMapAllCrossPlatformIdentifiers()
    {
        FilePickerFilter filter = new(
            "Audio",
            ["*.wav", "*.ogg"],
            ["audio/wav", "audio/ogg"],
            ["com.microsoft.waveform-audio"]);

        var mapped = AvaloniaFilePicker.MapFilters([filter]).Single();

        Assert.AreEqual("Audio", mapped.Name);
        CollectionAssert.AreEqual(filter.Patterns.ToArray(), mapped.Patterns!.ToArray());
        CollectionAssert.AreEqual(filter.MimeTypes.ToArray(), mapped.MimeTypes!.ToArray());
        CollectionAssert.AreEqual(
            filter.AppleUniformTypeIdentifiers.ToArray(),
            mapped.AppleUniformTypeIdentifiers!.ToArray());
    }
}
