using Avalonia.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Desktop.Tests.TestHelpers;

[TestClass]
public sealed class HeadlessViewHostTests
{
    [TestMethod]
    public void DrainAsync_WhenConditionNeverCompletesWithAvaloniaContext_ThrowsBoundedTimeout()
    {
        // Arrange
        SynchronizationContext? previousContext = SynchronizationContext.Current;
        AvaloniaSynchronizationContext.InstallIfNeeded();
        try
        {
            // Act
            Action act = () => HeadlessViewHost.DrainAsync(
                static () => false,
                TimeSpan.FromMilliseconds(25)).GetAwaiter().GetResult();

            // Assert
            act.Should().Throw<TimeoutException>();
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(previousContext);
        }
    }

    [TestMethod]
    public void PumpDispatcherUntil_WhenConditionCompletes_RestoresCallerContext()
    {
        // Arrange
        SynchronizationContext? previousContext = SynchronizationContext.Current;
        AvaloniaSynchronizationContext.InstallIfNeeded();
        SynchronizationContext? avaloniaContext = SynchronizationContext.Current;

        try
        {
            // Act
            HeadlessViewHost.PumpDispatcherUntil(static () => true, TimeSpan.FromSeconds(1));

            // Assert
            SynchronizationContext.Current.Should().BeSameAs(avaloniaContext);
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(previousContext);
        }
    }

    [TestMethod]
    public void PumpDispatcherUntil_WhenConditionNeverCompletesWithAvaloniaContext_ThrowsBoundedTimeoutAndRestoresContext()
    {
        // Arrange
        SynchronizationContext? previousContext = SynchronizationContext.Current;
        AvaloniaSynchronizationContext.InstallIfNeeded();
        SynchronizationContext? avaloniaContext = SynchronizationContext.Current;

        try
        {
            // Act
            Action pump = () => HeadlessViewHost.PumpDispatcherUntil(
                static () => false,
                TimeSpan.FromMilliseconds(25));

            // Assert
            pump.Should().Throw<TimeoutException>();
            SynchronizationContext.Current.Should().BeSameAs(avaloniaContext);
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(previousContext);
        }
    }

    [TestMethod]
    public void PumpDispatcherUntil_WhenTimeoutIsNotPositive_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        // Act
        Action pump = () => HeadlessViewHost.PumpDispatcherUntil(
            static () => false,
            TimeSpan.Zero);

        // Assert
        pump.Should().Throw<ArgumentOutOfRangeException>();
    }
}
