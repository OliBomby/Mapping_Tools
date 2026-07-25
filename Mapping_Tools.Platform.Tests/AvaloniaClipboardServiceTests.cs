using Mapping_Tools.Desktop.Platform;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Platform.Tests;

[TestClass]
public sealed class AvaloniaClipboardServiceTests
{
    [TestMethod]
    public async Task MissingTopLevelHasExplicitFailure()
    {
        AvaloniaClipboardService clipboard = new(() => null);

        await Assert.ThrowsExceptionAsync<InvalidOperationException>(
            () => clipboard.ReadTextAsync());
    }

    [TestMethod]
    public async Task PreCancelledOperationDoesNotAccessPlatform()
    {
        bool accessed = false;
        AvaloniaClipboardService clipboard = new(() =>
        {
            accessed = true;
            return null;
        });
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();

        await Assert.ThrowsExceptionAsync<OperationCanceledException>(
            () => clipboard.ClearAsync(cancellation.Token));

        Assert.IsFalse(accessed);
    }
}
