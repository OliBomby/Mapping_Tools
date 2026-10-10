using Mapping_Tools.Core.Annotations;

namespace Mapping_Tools.Desktop.Services.Undo;

/// <summary>Marks editable view-model state for project or dialog history.</summary>
[AttributeUsage(AttributeTargets.Property)]
[MeansImplicitUse]
public sealed class UndoableAttribute : Attribute;
