using System.Reflection;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using CommunityToolkit.Mvvm.ComponentModel;
using Mapping_Tools.Application.QuickRun.Contracts;
using Mapping_Tools.Application.Tools;
using Mapping_Tools.Desktop.Hosting;
using Mapping_Tools.Desktop.Plugin;
using Mapping_Tools.Desktop.Shell;
using Microsoft.Extensions.DependencyInjection;

namespace Mapping_Tools.Desktop.Composition;

internal sealed class ToolDefinitionCatalog
{
    private ToolDefinitionCatalog(IReadOnlyList<IMappingToolDefinition> definitions)
    {
        Definitions = definitions;
    }

    internal IReadOnlyList<IMappingToolDefinition> Definitions { get; }

    internal static ToolDefinitionCatalog Discover(IEnumerable<Assembly> assemblies)
    {
        ArgumentNullException.ThrowIfNull(assemblies);

        var definitions = assemblies
            .Distinct()
            .SelectMany(DiscoverAssembly)
            .OrderBy(definition => definition.Order)
            .ThenBy(definition => definition.Definition.Id, StringComparer.OrdinalIgnoreCase)
            .ThenBy(definition => definition.GetType().Assembly.GetName().Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var schemaIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var definition in definitions)
        {
            if (!ids.Add(definition.Definition.Id))
                throw new InvalidOperationException(
                    $"Tool id '{definition.Definition.Id}' is registered more than once.");

            if (!schemaIds.Add(definition.ConfigSchema.Id))
                throw new InvalidOperationException(
                    $"Configuration schema '{definition.ConfigSchema.Id}' is registered more than once.");
        }

        return new ToolDefinitionCatalog(definitions);
    }

    internal void RegisterServices(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        foreach (var registration in Definitions)
        {
            registration.RegisterServices(services);
            services.AddSingleton(registration.ViewModelType);
            services.AddSingleton(provider => new ShellFeatureRegistration(
                registration.Definition.Id,
                registration.Definition.DisplayName,
                registration.Category,
                registration.Definition.Description,
                registration.Definition.SearchTerms,
                () => (ObservableObject)provider.GetRequiredService(registration.ViewModelType),
                registration.StartsSection,
                ToAvaloniaScrollBarVisibility(registration.HorizontalScrollBarVisibility),
                ToAvaloniaScrollBarVisibility(registration.VerticalScrollBarVisibility)));

            if (registration.Definition.QuickRunTargets is not null)
                services.AddSingleton(provider => new MappingToolQuickRunRegistration(
                    registration.Definition,
                    cancellationToken => (provider.GetRequiredService(registration.ViewModelType) as IQuickRun
                                           ?? throw new InvalidOperationException(
                                               $"Feature '{registration.Definition.Id}' declares QuickRun but "
                                               + $"{registration.ViewModelType.Name} does not implement IQuickRun."))
                        .RunQuickAsync(cancellationToken)));
        }
    }

    private static IEnumerable<IMappingToolDefinition> DiscoverAssembly(Assembly assembly)
    {
        foreach (var type in GetLoadableTypes(assembly))
        {
            if (type.IsAbstract
                || type.ContainsGenericParameters
                || !type.IsDefined(typeof(MappingToolDefinitionAttribute), inherit: false)
                || !typeof(IMappingToolDefinition).IsAssignableFrom(type))
                continue;

            if (type.GetConstructor(Type.EmptyTypes) is null)
                throw new InvalidOperationException(
                    $"Tool definition '{type.FullName}' must have a parameterless constructor.");

            if (Activator.CreateInstance(type) is not IMappingToolDefinition definition)
                throw new InvalidOperationException(
                    $"Tool definition '{type.FullName}' could not be instantiated.");

            Validate(definition, type);
            yield return definition;
        }
    }

    private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException exception)
        {
            throw new InvalidOperationException(
                $"Could not inspect tool definitions in assembly '{assembly.FullName}'.",
                exception);
        }
    }

    private static void Validate(IMappingToolDefinition definition, Type definitionType)
    {
        if (definition.Definition is null)
            throw new InvalidOperationException($"Tool definition '{definitionType.FullName}' returned no metadata.");

        if (definition.ConfigSchema is null)
            throw new InvalidOperationException(
                $"Tool definition '{definitionType.FullName}' returned no configuration schema.");

        if (definition.ViewModelType is null)
            throw new InvalidOperationException(
                $"Tool definition '{definitionType.FullName}' returned no view-model type.");

        if (definition.ViewType is null)
            throw new InvalidOperationException(
                $"Tool definition '{definitionType.FullName}' returned no view type.");

        if (!typeof(ObservableObject).IsAssignableFrom(definition.ViewModelType)
            || definition.ViewModelType.IsAbstract
            || definition.ViewModelType.ContainsGenericParameters)
            throw new InvalidOperationException(
                $"Tool definition '{definitionType.FullName}' must expose a concrete ObservableObject view model type.");

        if (!typeof(Control).IsAssignableFrom(definition.ViewType)
            || definition.ViewType.IsAbstract
            || definition.ViewType.ContainsGenericParameters)
            throw new InvalidOperationException(
                $"Tool definition '{definitionType.FullName}' must expose a concrete Avalonia Control view type.");
    }

    private static ScrollBarVisibility ToAvaloniaScrollBarVisibility(
        ToolScrollBarVisibility visibility)
    {
        return visibility switch
        {
            ToolScrollBarVisibility.Auto => ScrollBarVisibility.Auto,
            ToolScrollBarVisibility.Disabled => ScrollBarVisibility.Disabled,
            ToolScrollBarVisibility.Hidden => ScrollBarVisibility.Hidden,
            ToolScrollBarVisibility.Visible => ScrollBarVisibility.Visible,
            _ => throw new ArgumentOutOfRangeException(nameof(visibility), visibility, null)
        };
    }
}
