namespace Mapping_Tools.Application.Projects;

/// <summary>
///     Carries a successfully opened project across the heterogeneous shell boundary.
/// </summary>
/// <param name="Path">The local file selected by the user.</param>
/// <param name="Project">The deserialized project, not yet installed into presentation state.</param>
public sealed record ProjectOpenResult(string Path, object Project);
