namespace Mapping_Tools.Application.Updates.Models;

/// <summary>
///     Describes the title and Markdown body of a release published by the update source.
/// </summary>
/// <param name="Title">The release name, when supplied.</param>
/// <param name="Body">The release description in Markdown, when supplied.</param>
public sealed record UpdateReleaseNotes(string? Title, string? Body);
