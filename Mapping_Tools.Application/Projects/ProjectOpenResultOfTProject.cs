namespace Mapping_Tools.Application.Projects;

/// <summary>
///     Carries a successfully opened project together with the path from which it
///     was loaded, allowing the presentation layer to track subsequent saves.
/// </summary>
/// <typeparam name="TProject">The feature-specific project model.</typeparam>
/// <param name="Path">The local file selected by the user.</param>
/// <param name="Project">The deserialized project, not yet installed into presentation state.</param>
public sealed record ProjectOpenResult<TProject>(string Path, TProject Project);

