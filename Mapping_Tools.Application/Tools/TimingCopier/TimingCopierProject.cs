using Mapping_Tools.Core.Tools.TimingCopier.Models;

namespace Mapping_Tools.Application.Tools.TimingCopier;

/// <summary>
///     Represents the complete Timing Copier project persisted by the shell.
/// </summary>
public sealed class TimingCopierProject : TimingCopierOptions
{
    /// <summary>Gets or sets the beatmap whose timing is copied.</summary>
    public string ImportPath { get; set; } = string.Empty;

    /// <summary>Gets or sets vertical-bar-separated beatmap targets.</summary>
    public string ExportPath { get; set; } = string.Empty;

}
