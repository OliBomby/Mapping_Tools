using Mapping_Tools.Desktop.Platform;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Platform.Tests;

[TestClass]
public sealed class AvaloniaClipboardServiceTests
{
    [TestMethod]
    public async Task ReadTextAsync_WithoutTopLevel_ThrowsInvalidOperationException()
    {
        // Arrange
        AvaloniaClipboardService clipboard = new(() => null);

        // Act
        Func<Task> act1 = () => clipboard.ReadTextAsync();

        // Assert
        await act1.Should().ThrowAsync<InvalidOperationException>();
    }

    [TestMethod]
    public async Task ClearAsync_WithPreCancelledToken_ThrowsWithoutPlatformAccess()
    {
        // Arrange
        bool accessed = false;
        AvaloniaClipboardService clipboard = new(() =>
        {
            accessed = true;
            return null;
        });
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();

        // Act
        Func<Task> act2 = () => clipboard.ClearAsync(cancellation.Token);

        // Assert
        await act2.Should().ThrowAsync<OperationCanceledException>();

        accessed.Should().BeFalse();
    }
}
