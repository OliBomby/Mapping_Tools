namespace Mapping_Tools.Application.QuickRun.Models;

/// <summary>
///     Reports the command selected by QuickRun together with any diagnostic that
///     prevented it from completing.
/// </summary>
/// <param name="Status">The terminal dispatch outcome.</param>
/// <param name="CommandId">The resolved stable command key, when resolution reached a command.</param>
/// <param name="Exception">The live-read or command failure captured for diagnostics.</param>
public sealed record QuickRunResult(
    QuickRunStatus Status,
    string? CommandId = null,
    Exception? Exception = null);

