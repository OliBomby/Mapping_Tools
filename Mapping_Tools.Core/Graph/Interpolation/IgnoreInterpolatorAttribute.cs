namespace Mapping_Tools.Core.Graph.Interpolation;

/// <summary>Marks an interpolator that is not offered in the user-facing selection menu.</summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class IgnoreInterpolatorAttribute : Attribute;

