namespace Mapping_Tools.Infrastructure.Updates;

/// <summary>
///     Holds the release title and long description shown before installation.
/// </summary>
/// <param name="Title">The release name, when supplied.</param>
/// <param name="Body">The release description, when supplied.</param>
public sealed record UpdateReleaseNotes(string? Title, string? Body);

