namespace Mapping_Tools.SamplePlugin;

/// <summary>
///     Stores the sample tool's single persisted setting.
/// </summary>
public sealed class SampleToolProject
{
    /// <summary>
    ///     Gets or sets the one whitespace-free tag that the tool appends to a beatmap.
    /// </summary>
    public string Tag { get; set; } = "sample-plugin";
}
