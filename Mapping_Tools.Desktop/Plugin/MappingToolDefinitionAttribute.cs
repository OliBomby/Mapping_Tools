namespace Mapping_Tools.Desktop.Plugin;

/// <summary>
///     Marks a parameterless definition type as a discoverable Mapping Tools
///     plugin contribution.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class MappingToolDefinitionAttribute : Attribute
{
}
