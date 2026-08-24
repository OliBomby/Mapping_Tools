using Mapping_Tools.Application.Settings;
using Mapping_Tools.Application.Tools;

namespace Mapping_Tools.Application.QuickRun;

/// <summary>
///     Distinguishes successful QuickRun dispatch from configuration, live-editor,
///     and command failures without requiring a view-owned completion event.
/// </summary>
public enum QuickRunStatus
{
    /// <summary>
    ///     The resolved command callback completed successfully.
    /// </summary>
    Executed,

    /// <summary>
    ///     Current-tool routing was requested before the shell selected a capable command.
    /// </summary>
    NoCurrentCommand,

    /// <summary>
    ///     A persisted Smart QuickRun tool name no longer exists in the command catalog.
    /// </summary>
    CommandNotFound,

    /// <summary>
    ///     Smart routing could not inspect the currently open osu! beatmap.
    /// </summary>
    EditorUnavailable,

    /// <summary>
    ///     Live selection discovery or the resolved command threw an exception.
    /// </summary>
    Failed,
}

