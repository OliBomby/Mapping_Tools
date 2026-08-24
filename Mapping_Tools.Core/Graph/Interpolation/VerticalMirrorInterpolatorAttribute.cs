using System.ComponentModel;
using Mapping_Tools.Core.Graph.Interpolation.Interpolators;

namespace Mapping_Tools.Core.Graph.Interpolation;

/// <summary>Marks an interpolator whose direction can be mirrored vertically by the editor.</summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class VerticalMirrorInterpolatorAttribute : Attribute;

