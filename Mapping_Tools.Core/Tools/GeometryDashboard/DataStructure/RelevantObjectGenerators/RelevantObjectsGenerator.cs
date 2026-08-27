using System.Reflection;
using Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObjectGenerators.Allocation;
using Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObjectGenerators.GeneratorTypes;

namespace Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObjectGenerators;

/// <summary>Base contract for a reflection-discovered geometry generator.</summary>
public abstract class RelevantObjectsGenerator
{
    private MethodInfo[]? generatorMethods;

    /// <summary>Creates a generator with default settings.</summary>
    protected RelevantObjectsGenerator()
    {
        Settings = new GeneratorSettings(this);
    }

    /// <summary>Creates a generator with supplied settings.</summary>
    /// <param name="settings">The settings instance to retain.</param>
    protected RelevantObjectsGenerator(GeneratorSettings settings)
    {
        Settings = settings;
    }

    /// <summary>Gets the mutable settings used by this generator.</summary>
    public GeneratorSettings Settings { get; }

    /// <summary>Gets the stable display name retained for project compatibility.</summary>
    public abstract string Name { get; }

    /// <summary>Gets the stable explanatory text retained for the desktop frontend.</summary>
    public abstract string Tooltip { get; }

    /// <summary>Gets the catalog group for this generator.</summary>
    public abstract GeneratorType GeneratorType { get; }

    /// <summary>Gets how generated timestamps are calculated from parent objects.</summary>
    public virtual GeneratorTemporalPositioning TemporalPositioning => GeneratorTemporalPositioning.Average;

    /// <summary>Gets methods marked as generator operations in declaration/reflection order.</summary>
    /// <returns>The discovered generator methods.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no operation is marked.</exception>
    public MethodInfo[] GetGeneratorMethods()
    {
        if (generatorMethods is not null) return generatorMethods;

        generatorMethods = GetType().GetMethods()
            .Where(method => method.GetCustomAttribute<RelevantObjectsGeneratorMethodAttribute>() is not null)
            .ToArray();
        if (generatorMethods.Length == 0) throw new InvalidOperationException($"Type {GetType()} does not have any generator method.");

        return generatorMethods;
    }

    /// <summary>Gets the parameter types required by a generator method.</summary>
    /// <param name="generatorMethodInfo">The marked method.</param>
    /// <returns>The method parameter types in invocation order.</returns>
    public static Type[] GetDependencies(MethodInfo generatorMethodInfo)
    {
        return generatorMethodInfo.GetParameters().Select(o => o.ParameterType).ToArray();
    }

    /// <summary>Gets the declared return type of a generator method.</summary>
    /// <param name="generatorMethodInfo">The marked method.</param>
    /// <returns>The return type.</returns>
    public static Type GetReturnType(MethodInfo generatorMethodInfo)
    {
        return generatorMethodInfo.ReturnType;
    }
}
