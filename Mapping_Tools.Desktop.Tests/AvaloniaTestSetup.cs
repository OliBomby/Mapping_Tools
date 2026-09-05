using Avalonia;
using Avalonia.Headless;
using Mapping_Tools.Desktop;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Desktop.Tests;

[TestClass]
public sealed class AvaloniaTestSetup
{
    [AssemblyInitialize]
    public static void Initialize(TestContext _)
    {
        SynchronizationContext? synchronizationContext = SynchronizationContext.Current;
        try
        {
            AppBuilder.Configure<App>()
                .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = true })
                .SetupWithoutStarting();
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(synchronizationContext);
        }
    }
}
