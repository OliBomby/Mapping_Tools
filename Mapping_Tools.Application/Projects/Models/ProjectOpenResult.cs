namespace Mapping_Tools.Application.Projects.Models;

/// <summary>
///     Carries a successfully opened, strongly typed project together with the
///     path from which it was loaded.
/// </summary>
/// <typeparam name="TProject">The feature-specific project model.</typeparam>
/// <param name="Path">The local file selected by the user.</param>
/// <param name="Project">The deserialized project, not yet installed into presentation state.</param>
public sealed record ProjectOpenResult<TProject>(string Path, TProject Project);
