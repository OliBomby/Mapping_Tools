namespace Mapping_Tools.Desktop.Tests;

/// <summary>Provides desktop lifetime services without starting the production host or native message loop.</summary>
public sealed class HeadlessTestApplication : App
{
    /// <inheritdoc />
    public override void OnFrameworkInitializationCompleted()
    {
    }
}
