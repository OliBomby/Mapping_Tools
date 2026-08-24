using Mapping_Tools.Application.Settings;
using Mapping_Tools.Application.Tools;

namespace Mapping_Tools.Application.QuickRun;

/// <summary>
///     Identifies the live osu! selection sizes for which a command may be offered
///     as an explicit Smart QuickRun target.
/// </summary>
[Flags]
public enum QuickRunTargets
{
    /// <summary>
    ///     The editor has no selected hit objects.
    /// </summary>
    NoSelection = 1,

    /// <summary>
    ///     The editor has exactly one selected hit object.
    /// </summary>
    SingleSelection = 1 << 1,

    /// <summary>
    ///     The editor has at least two selected hit objects.
    /// </summary>
    MultipleSelection = 1 << 2,

    /// <summary>
    ///     The command accepts either one or multiple selected hit objects.
    /// </summary>
    AnySelection = SingleSelection | MultipleSelection,

    /// <summary>
    ///     The command is suitable for every live selection size.
    /// </summary>
    Always = NoSelection | SingleSelection | MultipleSelection,
}

