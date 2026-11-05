using Mapping_Tools.Application.Localization;
using Mapping_Tools.Application.Projects.Models;
using Mapping_Tools.Application.Tools.HitsoundStudio;
using Mapping_Tools.Application.Tools.HitsoundStudio.Contracts;
using Mapping_Tools.Application.Tools.HitsoundStudio.Models;
using Mapping_Tools.Cli.Commands;
using Mapping_Tools.Cli.Configuration;
using Mapping_Tools.Cli.Localization;
using Mapping_Tools.Core.HitsoundStuff;
using Mapping_Tools.Infrastructure.Projects;
using Newtonsoft.Json.Linq;
using Spectre.Console;

namespace Mapping_Tools.Cli.Tools;

/// <summary>Imports or reloads sound layers and publishes staged hitsound exports.</summary>
internal static class HitsoundStudioCommand
{
    private const string layer_overrides_key = CliOptions.LAYER_ALIAS;
    private const string import_type_alias = "type";

    private static readonly VersionedProjectJsonSerializer serializer = new();

    /// <summary>Registers layer imports, explicit reloads, export policy, and single base-beatmap binding.</summary>
    /// <param name="console">Renders help and supplies the output writer for operation results.</param>
    /// <param name="error">Receives diagnostics; its lifetime belongs to the caller.</param>
    /// <param name="input">Supplies standard input for explicitly interactive operations; its lifetime belongs to the caller.</param>
    /// <returns>A ready-to-register command with inherited root help.</returns>
    internal static ToolCommand Create(IAnsiConsole console, TextWriter error, TextReader input) =>
        ToolCommand.For(
            console, error, input,
            HitsoundStudioToolDefinition.Definition,
            () => new HitsoundStudioServiceOptions(),
            RunAsync,
            ImportAsync,
            options: new Dictionary<string, string>
            {
                [CliOptions.Import.Name] = CliStrings.ImportHelp,
                [CliOptions.Reload.Name] = CliStrings.ReloadHelp,
                [CliOptions.CleanOutput.Name] = CliStrings.CleanHelp,
            },
            description: CliStrings.ToolDescription_hitsound_studio,
            prepare: (context, schema) => context.PrepareTargetsAsync(
                schema, nameof(HitsoundStudioServiceOptions.BaseBeatmap), single: true));

    /// <summary>Validates samples, exports into staging, publishes outputs, and updates a requested saved project with the sample schema.</summary>
    /// <param name="context">Prepared invocation, including any relative subcommand and CLI-only choices.</param>
    /// <param name="project">Typed Application settings after project loading, imports, and overrides.</param>
    /// <returns>Zero after successful export and any project update.</returns>
    internal static async Task<int> RunAsync(ToolContext context, HitsoundStudioServiceOptions project)
    {
        string destination = project.ExportFolder;
        using var export = context.Files.BeginTransaction(destination);
        context.StagingDirectories.Add(export.StagingDirectory);

        var snapshot = project.Clone();
        var invalid = await context.Get<IHitsoundStudioService>().ValidateSamplesAsync(
            snapshot.HitsoundLayers.Select(layer => layer.SampleArgs).ToArray(), context.CancellationToken);

        if (invalid.Count > 0)
            throw new InvalidDataException(ApplicationText.Format(
                CliStrings.InvalidSamples,
                string.Join(Environment.NewLine, invalid.Values.Select(exception => exception.Message))));

        snapshot.ExportFolder = export.StagingDirectory;
        snapshot.DeleteAllInExportFirst = false;

        var result = await context.Get<IHitsoundStudioService>().ExportAsync(snapshot, cancellationToken: context.CancellationToken);
        var outputFiles = await export.CommitAsync(
            context.CancellationToken,
            CliOptions.Overwrite.GetValue(context.ParseResult),
            CliOptions.CleanOutput.GetValue(context.ParseResult));

        foreach (string path in outputFiles)
            context.ReportFile(path);

        context.Output.WriteLine(ApplicationText.Format(CliStrings.HitsoundSummary, result.LayerCount, result.EventCount, result.SampleCount));
        if (result.OutputDirectory is not null)
            context.Output.WriteLine(ApplicationText.Format(CliStrings.OutputDirectory, destination));

        if (context.GetOption(CliOptions.SaveProject) is { } projectPath)
        {
            project.PreviousSampleSchema = result.Schema;

            var json = JObject.Parse(new VersionedProjectJsonSerializer().Serialize(project));
            string propertyName = nameof(HitsoundStudioServiceOptions.PreviousSampleSchema);
            context.Document[propertyName] = json[propertyName];
            await context.Projects.SaveDocumentAsync(projectPath, context.Document.ToString(), true, context.CancellationToken);
        }

        return 0;
    }

    /// <summary>
    /// Validates output cleanup permission, explicitly reloads saved layers when requested, and prepares ordered
    /// imported layers.
    /// </summary>
    /// <param name="context">Invocation whose mutable document receives imported settings before model option binding.</param>
    /// <returns>A task completing after requested imports and their layer overrides are applied.</returns>
    /// <remarks>Also runs for project-only invocations; tool export and beatmap editing remain in the operation handler.</remarks>
    private static async Task ImportAsync(ToolContext context)
    {
        if (CliOptions.CleanOutput.GetValue(context.ParseResult) && !CliOptions.Overwrite.GetValue(context.ParseResult))
            throw new ArgumentException(CliStrings.CleanRequiresOverwrite);

        var document = context.Document;
        var service = context.HasOption(CliOptions.Import) || CliOptions.Reload.GetValue(context.ParseResult)
            ? context.Get<IHitsoundStudioService>()
            : null;

        if (CliOptions.Reload.GetValue(context.ParseResult))
        {
            var project = serializer.Deserialize<HitsoundStudioServiceOptions>(
                ToolConfigSchema.ForTool(HitsoundStudioToolDefinition.Definition.Id), document.ToString());
            var reloaded = await service!.ReloadAsync(project.HitsoundLayers, context.CancellationToken);
            string propertyName = nameof(HitsoundStudioServiceOptions.HitsoundLayers);
            document[propertyName] = JObject.Parse(serializer.Serialize(
                new HitsoundStudioServiceOptions { HitsoundLayers = reloaded.ToList() }))[propertyName];
        }

        var imports = context.OptionValues(CliOptions.Import);

        if (imports.Count == 0)
            return;

        JArray layers = [];

        foreach (string text in imports)
        {
            var value = TomlValue.Parse(text) as JObject
                ?? throw new ArgumentException(ApplicationText.Format(CliStrings.InvalidObject, CliOptions.Import.Name));
            value.Remove(layer_overrides_key, out var layerOverrides);

            if (value.Remove(import_type_alias, out var type))
                value[OptionSchema.Kebab(nameof(HitsoundStudioImportRequest.ImportType))] = type;

            var normalized = OptionSchema.Normalize(value, typeof(HitsoundStudioImportRequest));
            var request = normalized.ToObject<HitsoundStudioImportRequest>()!;
            var imported = await service!.ImportAsync(request, context.CancellationToken);
            var snapshot = JObject.Parse(serializer.Serialize(new HitsoundStudioServiceOptions { HitsoundLayers = imported.ToList() }));

            foreach (var layer in (JArray)snapshot[nameof(HitsoundStudioServiceOptions.HitsoundLayers)]!)
            {
                if (layerOverrides is not null)
                    ((JObject)layer).Merge(
                        OptionSchema.Normalize(layerOverrides, typeof(HitsoundLayer)),
                        new JsonMergeSettings { MergeArrayHandling = MergeArrayHandling.Replace });

                layers.Add(layer.DeepClone());
            }
        }

        document[nameof(HitsoundStudioServiceOptions.HitsoundLayers)] = layers;
    }
}
