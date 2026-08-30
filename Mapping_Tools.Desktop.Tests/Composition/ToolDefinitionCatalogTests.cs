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
        ToolDefinitionCatalog catalog = ToolDefinitionCatalog.Discover(
            [typeof(ToolDefinitionCatalogTests).Assembly]);
        catalog.RegisterServices(services);

        // Assert
        catalog.Definitions.Should().ContainSingle(definition => definition.Definition.Id == "external-test");
        catalog.Definitions.Single().ConfigSchema.Id.Should().Be("mapping-tools.tool.external-test");
        services.Should().Contain(descriptor => descriptor.ServiceType == typeof(ExternalPluginService));
    }

    [MappingToolDefinition]
    public sealed class ExternalToolRegistration : IMappingToolDefinition
    {
        public string Category => "Tools";

        public bool StartsSection => false;

        public ToolScrollBarVisibility HorizontalScrollBarVisibility => ToolScrollBarVisibility.Disabled;

        public ToolScrollBarVisibility VerticalScrollBarVisibility => ToolScrollBarVisibility.Disabled;

        public int Order => 1_000;

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
