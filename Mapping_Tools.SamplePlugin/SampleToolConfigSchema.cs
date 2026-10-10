using System.Text.Json.Nodes;
using Mapping_Tools.Application.Projects.Contracts;
using Mapping_Tools.Application.Projects.Models;

namespace Mapping_Tools.SamplePlugin;

/// <summary>
///     Defines the sample plugin's stable configuration identity and version history.
/// </summary>
public static class SampleToolConfigSchema
{
    /// <summary>
    ///     Gets the schema used by the sample tool's project persistence.
    /// </summary>
    public static ToolConfigSchema Definition { get; } = new(
        "mapping-tools.tool.sample-plugin",
        [new AddDefaultTagMigration()]);

    private sealed class AddDefaultTagMigration : IConfigMigration
    {
        int IConfigMigration.ToVersion => 2;

        void IConfigMigration.Apply(JsonObject document)
        {
            ArgumentNullException.ThrowIfNull(document);
            document["Tag"] ??= "sample-plugin";
        }
    }
}
