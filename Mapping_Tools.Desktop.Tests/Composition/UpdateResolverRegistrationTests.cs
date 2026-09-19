using Mapping_Tools.Application.Updates.Contracts;
using Mapping_Tools.Desktop.Composition;
using Mapping_Tools.Infrastructure.Updates;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Desktop.Tests.Composition;

[TestClass]
public sealed class UpdateResolverRegistrationTests
{
    [TestMethod]
    public void AddMappingToolsDesktop_WithoutLocalPackage_RegistersGithubResolver()
    {
        // Arrange
        ServiceCollection services = new();
        services.AddMappingToolsDesktop();
        using ServiceProvider provider = services.BuildServiceProvider();

        // Act
        IPackageResolver resolver = provider.GetRequiredService<IPackageResolver>();

        // Assert
        resolver.Should().BeOfType<GithubUpdatePackageResolver>();
    }

    [TestMethod]
    public void AddMappingToolsDesktop_RegistersGatewayUsingApplicationResolver()
    {
        // Arrange
        ServiceCollection services = new();
        services.AddMappingToolsDesktop();
        using ServiceProvider provider = services.BuildServiceProvider();

        // Act
        IUpdateGateway gateway = provider.GetRequiredService<IUpdateGateway>();

        // Assert
        gateway.Should().BeOfType<OnovaUpdateGateway>();
    }

    [TestMethod]
    public void AddMappingToolsDesktop_WithLocalPackage_RegistersLocalResolver()
    {
        // Arrange
        string packagePath = Path.Combine(
            Path.GetTempPath(),
            $"mapping-tools-update-v2.0.0-{Guid.NewGuid():N}.zip");
        File.WriteAllText(packagePath, string.Empty);

        try
        {
            ServiceCollection services = new();
            services.AddMappingToolsDesktop(localUpdatePackagePath: packagePath);
            using ServiceProvider provider = services.BuildServiceProvider();

            // Act
            IPackageResolver resolver = provider.GetRequiredService<IPackageResolver>();

            // Assert
            resolver.Should().BeOfType<LocalUpdatePackageResolver>();
        }
        finally
        {
            File.Delete(packagePath);
        }
    }
}
