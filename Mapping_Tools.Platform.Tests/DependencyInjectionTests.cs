using Mapping_Tools.ApplicationServices.Platform;
using Mapping_Tools.Desktop.Composition;
using Mapping_Tools.Desktop.ViewModels;
using Mapping_Tools.Desktop.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Platform.Tests;

[TestClass]
public sealed class DependencyInjectionTests
{
    [TestMethod]
    public void DesktopCompositionRootRegistersExpectedSingletons()
    {
        ServiceCollection services = new();

        services.AddMappingToolsDesktop();

        Type[] expectedSingletons =
        [
            typeof(MainWindow),
            typeof(MainViewModel),
            typeof(IFilePicker),
            typeof(IClipboardService),
            typeof(IPlatformLauncher),
            typeof(IFileRevealService),
            typeof(IApplicationDirectories)
        ];

        foreach (Type serviceType in expectedSingletons)
        {
            ServiceDescriptor? registration = services.SingleOrDefault(
                descriptor => descriptor.ServiceType == serviceType);

            Assert.IsNotNull(registration, $"{serviceType.Name} is not registered.");
            Assert.AreEqual(
                ServiceLifetime.Singleton,
                registration.Lifetime,
                $"{serviceType.Name} has the wrong lifetime.");
        }
    }

    [TestMethod]
    public void DesktopCompositionRootPassesContainerValidation()
    {
        ServiceCollection services = new();
        services.AddMappingToolsDesktop();

        using ServiceProvider provider = services.BuildServiceProvider(
            new ServiceProviderOptions
            {
                ValidateOnBuild = true,
                ValidateScopes = true
            });

        Assert.IsNotNull(provider);
    }
}
