namespace Mapping_Tools.Core.Annotations;

/// <summary>
/// Marks an attribute as indicating that any symbol it decorates is used implicitly.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class MeansImplicitUseAttribute : Attribute
{
}
