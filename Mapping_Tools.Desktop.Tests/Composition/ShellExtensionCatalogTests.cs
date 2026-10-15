using System.Reflection;
using CommunityToolkit.Mvvm.Input;
using Mapping_Tools.Desktop.Composition;
using Mapping_Tools.Desktop.Plugin;
using Mapping_Tools.Desktop.Shell;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Desktop.Tests.Composition;

[TestClass]
public sealed class ShellExtensionCatalogTests
{
    [TestMethod]
    public void Discover_ShellOnlyAssembly_RegistersMenusWithoutTools()
    {
        // Arrange
        ServiceCollection services = new();
        var assembly = new PluginAssembly(typeof(TestShellExtension), typeof(UnmarkedShellExtension));

        // Act
        services.AddDesktopFeatures([assembly, assembly]);
        using var provider = services.BuildServiceProvider();
        var registrations = provider.GetServices<ShellMenuRegistration>().ToArray();
        var catalog = provider.GetRequiredService<ToolDefinitionCatalog>();

        // Assert
        catalog.Definitions.Should().BeEmpty();
        registrations.Should().ContainSingle();
        registrations[0].Item.Header.Should().Be("Folders");
        provider.GetRequiredService<IShellFeatureRegistry>().Features.Select(feature => feature.Id)
            .Should().Equal("get-started", "preferences");
        provider.GetServices<ShellMenuRegistration>().Single().Should().BeSameAs(registrations[0]);
    }

    [TestMethod]
    public void Discover_BrokenExternalAssembly_LogsFailureAndLoadsOtherExtensions()
    {
        // Arrange
        var broken = new PluginAssembly(typeof(TestShellExtension), typeof(ThrowingShellExtension));
        List<(Assembly Assembly, Exception Exception)> failures = [];

        // Act
        var catalog = ShellExtensionCatalog.Discover([broken, new PluginAssembly(typeof(TestShellExtension))],
            (assembly, exception) => failures.Add((assembly, exception)));

        // Assert
        catalog.Extensions.Should().ContainSingle().Which.Should().BeOfType<TestShellExtension>();
        failures.Should().ContainSingle().Which.Assembly.Should().BeSameAs(broken);
    }

    [TestMethod]
    public void Discover_UnloadableExternalAssembly_LogsFailureAndContinues()
    {
        // Arrange
        var broken = new UnloadableAssembly();
        List<Assembly> failures = [];

        // Act
        var catalog = ShellExtensionCatalog.Discover([broken, new PluginAssembly(typeof(TestShellExtension))],
            (assembly, _) => failures.Add(assembly));

        // Assert
        catalog.Extensions.Should().ContainSingle();
        failures.Should().Equal(broken);
    }

    [TestMethod]
    public void RegisterServices_MultipleExtensions_OrdersTypesAndDefersFactoriesUntilResolution()
    {
        // Arrange
        ServiceCollection services = new();
        var catalog = ShellExtensionCatalog.Discover(
            [new PluginAssembly(typeof(TestShellExtension), typeof(AnotherShellExtension))]);
        catalog.RegisterServices(services);
        int executed = 0;
        services.AddSingleton(new RelayCommand(() => executed++));
        using var provider = services.BuildServiceProvider();

        // Act
        var registrations = provider.GetServices<ShellMenuRegistration>().ToArray();
        registrations[0].Item.Command!.Execute(null);

        // Assert
        registrations.Select(registration => registration.Item.Header).Should().Equal("First", "Folders");
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

    [ShellExtension]
    private sealed class TestShellExtension : IShellExtension
    {
        public void RegisterServices(IServiceCollection services)
        {
            services.AddSingleton(_ => new ShellMenuRegistration(ShellMenuLocation.File,
                new ShellMenuItem("Folders", new RelayCommand(() => { }))));
        }
    }

    [ShellExtension]
    private sealed class AnotherShellExtension : IShellExtension
    {
        public void RegisterServices(IServiceCollection services)
        {
            services.AddSingleton(provider => new ShellMenuRegistration(ShellMenuLocation.File,
                new ShellMenuItem("First", provider.GetRequiredService<RelayCommand>())));
        }
    }

    [ShellExtension]
    private sealed class ThrowingShellExtension : IShellExtension
    {
        public ThrowingShellExtension() => throw new InvalidOperationException("Broken plugin.");

        public void RegisterServices(IServiceCollection services) => throw new NotSupportedException();
    }

    private sealed class UnmarkedShellExtension : IShellExtension
    {
        public void RegisterServices(IServiceCollection services) => throw new NotSupportedException();
    }
}
