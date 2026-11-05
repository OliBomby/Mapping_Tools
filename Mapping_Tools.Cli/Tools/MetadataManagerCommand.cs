using Mapping_Tools.Application.BeatmapEditing.Contracts;
using Mapping_Tools.Application.Tools.MetadataManager;
using Mapping_Tools.Cli.Commands;
using Mapping_Tools.Cli.Localization;
using Mapping_Tools.Infrastructure.Projects;
using Newtonsoft.Json.Linq;
using Spectre.Console;

namespace Mapping_Tools.Cli.Tools;

/// <summary>Imports metadata, applies explicit overrides, and exports prepared fields to target beatmaps.</summary>
internal static class MetadataManagerCommand
{
    private static readonly VersionedProjectJsonSerializer serializer = new();

    /// <summary>Registers source imports and multi-target metadata export.</summary>
    /// <param name="console">Renders help and supplies the output writer for operation results.</param>
    /// <param name="error">Receives diagnostics; its lifetime belongs to the caller.</param>
    /// <param name="input">Supplies standard input for explicitly interactive operations; its lifetime belongs to the caller.</param>
    /// <returns>A ready-to-register command with inherited root help.</returns>
    internal static ToolCommand Create(IAnsiConsole console, TextWriter error, TextReader input) =>
        ToolCommand.For(
            console, error, input,
            MetadataManagerToolDefinition.Definition,
            () => new MetadataManagerServiceOptions(),
            async (context, options) =>
            {
                context.Report(await context.Get<IMetadataManagerService>().ExportAsync(
                    options, cancellationToken: context.CancellationToken));

                if (CliOptions.ReloadEditor.GetValue(context.ParseResult))
                    await context.Get<IEditorReloadService>().ReloadAsync(context.CancellationToken);

                return 0;
            },
            ImportAsync,
            options: new Dictionary<string, string>
            {
                [CliOptions.FromCurrent.Name] = CliStrings.FromCurrentHelp,
                [CliOptions.From.Name] = CliStrings.FromHelp,
            },
            prepare: (context, schema) => context.PrepareTargetsAsync(
                schema, nameof(MetadataManagerServiceOptions.ExportPath)));

    /// <summary>Resolves an optional current source and merges imported metadata before explicit CLI overrides.</summary>
    /// <param name="context">Invocation whose mutable document receives imported settings before model option binding.</param>
    /// <returns>A task completing after requested imports and their layer overrides are applied.</returns>
    /// <remarks>Also runs for project-only invocations; tool export and beatmap editing remain in the operation handler.</remarks>
    private static async Task ImportAsync(ToolContext context)
    {
        await context.ResolveCurrentSourceAsync();

        if (context.GetOption(CliOptions.From) is not { } path)
            return;

        var imported = await context.Get<IMetadataManagerService>().ImportAsync(path, context.CancellationToken);
        context.MergeImportedSettings(JObject.Parse(serializer.Serialize(imported)));
    }
}
