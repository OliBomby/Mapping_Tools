using Mapping_Tools.Application.QuickRun.Models;
using Mapping_Tools.Application.Localization;
using Mapping_Tools.Application.Tools;
using Mapping_Tools.SamplePlugin.Localization;

namespace Mapping_Tools.SamplePlugin;

/// <summary>
///     Provides the discoverable metadata for the sample plugin tool.
/// </summary>
public static class SampleToolDefinition
{
    static SampleToolDefinition()
    {
        TranslationManager.RegisterResources(static culture => SamplePluginStrings.Culture = culture);
    }

    /// <summary>
    ///     Gets the stable sample tool metadata used by shell and QuickRun catalogs.
    /// </summary>
    public static ToolDefinition Definition { get; } = new(
        "sample-plugin",
        "Sample Plugin",
        "A single-run tool that adds a tag to selected beatmaps.",
        ["sample", "plugin", "example", "tag"],
        QuickRunTargets.Always,
        translatedDescriptionGetter: static () => SamplePluginStrings.Sample_Description,
        translatedSearchTermsGetter: static () => SamplePluginStrings.Sample_SearchTerms);
}
