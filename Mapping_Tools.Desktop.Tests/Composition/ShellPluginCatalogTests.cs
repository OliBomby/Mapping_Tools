using System.Reflection;
using CommunityToolkit.Mvvm.Input;
using Mapping_Tools.Desktop.Composition;
using Mapping_Tools.Desktop.Plugin;
using Mapping_Tools.Desktop.Shell;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Desktop.Tests.Composition;

[TestClass]
public sealed class ShellPluginCatalogTests
{
    [TestMethod]
    public void Discover_ShellOnlyAssembly_RegistersMenusWithoutTools()
    {
        // Arrange
        ServiceCollection services = new();
        var assembly = new PluginAssembly(typeof(TestShellPlugin), typeof(UnmarkedShellPlugin));

        // Act
        services.AddDesktopFeatures([assembly, assembly]);
        using var provider = services.BuildServiceProvider();
        var contributions = provider.GetServices<ShellMenuContribution>().ToArray();
        var catalog = provider.GetRequiredService<ToolDefinitionCatalog>();

        // Assert
        catalog.Definitions.Should().BeEmpty();
        contributions.Should().ContainSingle();
        contributions[0].Item.Header.Should().Be("Folders");
        provider.GetRequiredService<IShellFeatureRegistry>().Features.Select(feature => feature.Id)
            .Should().Equal("get-started", "preferences");
        provider.GetServices<ShellMenuContribution>().Single().Should().BeSameAs(contributions[0]);
    }

    [TestMethod]
    public void Discover_BrokenExternalAssembly_LogsFailureAndLoadsOtherPlugins()
    {
        // Arrange
        var broken = new PluginAssembly(typeof(TestShellPlugin), typeof(ThrowingShellPlugin));
        List<(Assembly Assembly, Exception Exception)> failures = [];

        // Act
        var catalog = ShellPluginCatalog.Discover([broken, new PluginAssembly(typeof(TestShellPlugin))],
            (assembly, exception) => failures.Add((assembly, exception)));

        // Assert
        catalog.Plugins.Should().ContainSingle().Which.Should().BeOfType<TestShellPlugin>();
        failures.Should().ContainSingle().Which.Assembly.Should().BeSameAs(broken);
    }

    [TestMethod]
    public void Discover_UnloadableExternalAssembly_LogsFailureAndContinues()
    {
        // Arrange
        var broken = new UnloadableAssembly();
        List<Assembly> failures = [];

        // Act
        var catalog = ShellPluginCatalog.Discover([broken, new PluginAssembly(typeof(TestShellPlugin))],
            (assembly, _) => failures.Add(assembly));

        // Assert
        catalog.Plugins.Should().ContainSingle();
        failures.Should().Equal(broken);
    }

    [TestMethod]
    public void RegisterServices_MultiplePlugins_OrdersTypesAndDefersFactoriesUntilResolution()
    {
        // Arrange
        ServiceCollection services = new();
        var catalog = ShellPluginCatalog.Discover(
            [new PluginAssembly(typeof(TestShellPlugin), typeof(AnotherShellPlugin))]);
        catalog.RegisterServices(services);
        int executed = 0;
        services.AddSingleton(new RelayCommand(() => executed++));
        using var provider = services.BuildServiceProvider();

        // Act
        var contributions = provider.GetServices<ShellMenuContribution>().ToArray();
        contributions[0].Item.Command!.Execute(null);

        // Assert
        contributions.Select(contribution => contribution.Item.Header).Should().Equal("First", "Folders");
        executed.Should().Be(1);
    }

    private sealed class PluginAssembly(params Type[] types) : Assembly
    {
        public override Type[] GetTypes() => types;

        public override AssemblyName GetName(bool copiedName) => new("ShellOnlyTestPlugin");
    }

    private sealed class UnloadableAssembly : Assembly
    {
        public override Type[] GetTypes() => throw new ReflectionTypeLoadException([], []);
    }

    [ShellPlugin]
    private sealed class TestShellPlugin : IShellPlugin
    {
        public void RegisterServices(IServiceCollection services)
        {
            services.AddSingleton(_ => new ShellMenuContribution(ShellMenuLocation.File,
                new ShellMenuItem("Folders", new RelayCommand(() => { }))));
        }
    }

    [ShellPlugin]
    private sealed class AnotherShellPlugin : IShellPlugin
    {
        public void RegisterServices(IServiceCollection services)
        {
            services.AddSingleton(provider => new ShellMenuContribution(ShellMenuLocation.File,
                new ShellMenuItem("First", provider.GetRequiredService<RelayCommand>())));
        }
    }

    [ShellPlugin]
    private sealed class ThrowingShellPlugin : IShellPlugin
    {
        public ThrowingShellPlugin() => throw new InvalidOperationException("Broken plugin.");

        public void RegisterServices(IServiceCollection services) => throw new NotSupportedException();
    }

    private sealed class UnmarkedShellPlugin : IShellPlugin
    {
        public void RegisterServices(IServiceCollection services) => throw new NotSupportedException();
    }
}
