using Mapping_Tools.Application.Updates.Contracts;
using Mapping_Tools.Desktop.Composition;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Desktop.Tests.Composition;

[TestClass]
public sealed class UpdateGatewayRegistrationTests
{
    [TestMethod]
    public void AddMappingToolsDesktop_RegistersVelopackGateway()
    {
        // Arrange
        ServiceCollection services = new();
        services.AddMappingToolsDesktop();
        using var provider = services.BuildServiceProvider();

        // Act
        var descriptor = services.Single(service => service.ServiceType == typeof(IUpdateGateway));

        // Assert
        descriptor.ImplementationFactory.Should().NotBeNull();
    }

    [TestMethod]
    public void AddMappingToolsDesktop_WithLocalFeed_RegistersVelopackGateway()
    {
        // Arrange
        string feedDirectory = Path.Combine(
            Path.GetTempPath(),
            $"mapping-tools-feed-{Guid.NewGuid():N}");
        string feedPath = Path.Combine(feedDirectory, "releases.win-x64.json");
        Directory.CreateDirectory(feedDirectory);

        try
        {
            ServiceCollection services = new();
            services.AddMappingToolsDesktop(localUpdatePackagePath: feedPath);
            using var provider = services.BuildServiceProvider();

            // Act
            var descriptor = services.Single(service => service.ServiceType == typeof(IUpdateGateway));

            // Assert
            descriptor.ImplementationFactory.Should().NotBeNull();
        }
        finally
        {
            if (Directory.Exists(feedDirectory)) Directory.Delete(feedDirectory, recursive: true);
        }
    }
}
