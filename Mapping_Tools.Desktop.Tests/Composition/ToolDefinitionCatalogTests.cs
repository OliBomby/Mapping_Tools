using System.Reflection;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using Mapping_Tools.Application.Tools;
using Mapping_Tools.Desktop.Composition;
using Mapping_Tools.Desktop.Plugin;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Desktop.Tests.Composition;

[TestClass]
public sealed class ToolDefinitionCatalogTests
{
    [TestMethod]
    public void Discover_ExternalAssemblyWithAttributedDefinition_RegistersItsToolAndServices()
    {
        // Arrange
        ServiceCollection services = new();

        // Act
        var catalog = ToolDefinitionCatalog.Discover(
            [typeof(ToolDefinitionCatalogTests).Assembly]);
        catalog.RegisterServices(services);

        // Assert
        catalog.Definitions.Should().ContainSingle(definition => definition.Definition.Id == "external-test");
        catalog.Definitions.Single().ConfigSchema.Id.Should().Be("mapping-tools.tool.external-test");
        services.Should().Contain(descriptor => descriptor.ServiceType == typeof(ExternalPluginService));
    }

    [TestMethod]
    public void Discover_UnloadablePluginAssembly_LogsFailureAndContinuesWithOtherAssemblies()
    {
        // Arrange
        UnloadablePluginAssembly unloadableAssembly = new();
        List<(Assembly Assembly, Exception Exception)> failures = [];

        // Act
        var catalog = ToolDefinitionCatalog.Discover(
            [unloadableAssembly, typeof(ToolDefinitionCatalogTests).Assembly],
            (assembly, exception) => failures.Add((assembly, exception)));

        // Assert
        catalog.Definitions.Should().ContainSingle(definition => definition.Definition.Id == "external-test");
        failures.Should().ContainSingle();
        failures[0].Assembly.Should().BeSameAs(unloadableAssembly);
        failures[0].Exception.Message.Should().Contain("Could not inspect tool definitions");
    }

    private sealed class UnloadablePluginAssembly : Assembly
    {
        public override Type[] GetTypes()
        {
            throw new ReflectionTypeLoadException(
                [],
                [new TypeLoadException("The plugin references an unavailable type.")]);
        }

        public override AssemblyName GetName(bool copiedName)
        {
            return new AssemblyName("Mapping_Tools.UnloadablePlugin");
        }
    }

    [MappingToolDefinition]
    public sealed class ExternalToolRegistration : IMappingToolDefinition
    {
        public ToolScrollBarVisibility HorizontalScrollBarVisibility => ToolScrollBarVisibility.Disabled;

        public ToolScrollBarVisibility VerticalScrollBarVisibility => ToolScrollBarVisibility.Disabled;

        public ToolDefinition Definition { get; } = new(
            "external-test",
            "External Test",
            "Test plugin registration.",
            ["plugin"]);

        public Type ViewModelType => typeof(ExternalPluginViewModel);

        public Type ViewType => typeof(ExternalPluginView);

        public void RegisterServices(IServiceCollection services)
        {
            services.AddSingleton<ExternalPluginService>();
        }
    }

    public sealed class ExternalPluginViewModel : ObservableObject
    {
    }

    public sealed class ExternalPluginView : UserControl
    {
    }

    public sealed class ExternalPluginService
    {
    }
}
