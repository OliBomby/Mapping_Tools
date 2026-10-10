using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Headless;
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
            AppBuilder.Configure<HeadlessTestApplication>()
                .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = true })
                .SetupWithClassicDesktopLifetime([]);
            IClassicDesktopStyleApplicationLifetime lifetime =
                (IClassicDesktopStyleApplicationLifetime)global::Avalonia.Application.Current!.ApplicationLifetime!;
            lifetime.ShutdownMode = ShutdownMode.OnExplicitShutdown;
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(synchronizationContext);
        }
    }
}
