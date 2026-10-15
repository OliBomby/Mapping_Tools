using Mapping_Tools.Core.Annotations;

namespace Mapping_Tools.Desktop.Plugin;

/// <summary>Marks a parameterless <see cref="IShellExtension" /> implementation for discovery in the Plugins folder.</summary>
[MeansImplicitUse]
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class ShellExtensionAttribute : Attribute;
