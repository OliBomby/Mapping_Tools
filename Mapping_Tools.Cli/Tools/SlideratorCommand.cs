using Mapping_Tools.Application.Tools.Sliderator;
using Mapping_Tools.Application.Tools.Sliderator.Contracts;
using Mapping_Tools.Application.Tools.Sliderator.Models;
using Mapping_Tools.Cli.Commands;
using Mapping_Tools.Cli.Configuration;
using Mapping_Tools.Cli.Localization;
using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.MathUtil;
using Mapping_Tools.Core.Tools.Sliderator.Models;
using Mapping_Tools.Infrastructure.Projects;
using Newtonsoft.Json.Linq;
using Spectre.Console;

namespace Mapping_Tools.Cli.Tools;

/// <summary>Imports selected sliders and orchestrates existing per-slider service calls entirely within the CLI.</summary>
internal static class SlideratorCommand
{
    /// <summary>Registers source-value overrides, explicit selection, and per-slider execution.</summary>
    /// <param name="console">Renders help and supplies the output writer for operation results.</param>
    /// <param name="error">Receives diagnostics; its lifetime belongs to the caller.</param>
    /// <param name="input">Supplies standard input for explicitly interactive operations; its lifetime belongs to the caller.</param>
    /// <returns>A ready-to-register command with inherited root help.</returns>
    internal static ToolCommand Create(IAnsiConsole console, TextWriter error, TextReader input) =>
        ToolCommand.For(
            console, error, input,
            SlideratorToolDefinition.Definition,
            () => new SlideratorServiceOptions { ExportModeSetting = SlideratorExportMode.Override },
            RunAsync,
            options: new Dictionary<string, string>
            {
                [CliOptions.Selection.Name] = CliStrings.SelectionOptionHelp,
                [CliOptions.TimeCode.Name] = CliStrings.TimeCodeHelp,
            },
            description: CliStrings.ToolDescription_sliderator,
            prepare: PrepareAsync);

    /// <summary>Imports each map once, snapshots its selected sources, and calls the unchanged Application service for each slider.</summary>
    /// <param name="context">Prepared invocation supplying targets, selection, services, and cancellation.</param>
    /// <param name="project">Reusable graph and generation settings; cloned for every source slider.</param>
    /// <returns>Zero after reporting the number of transformed sources for each map.</returns>
    /// <remarks>
    /// Each service call saves independently. After the first save, subsequent calls read the saved file so a stale
    /// live editor snapshot cannot erase earlier results. An empty selection reports zero without saving.
    /// </remarks>
    private static async Task<int> RunAsync(ToolContext context, SlideratorServiceOptions project)
    {
        var service = context.Get<ISlideratorService>();
        var explicitOptions = context.Document[CliDocumentKeys.SOURCE_OVERRIDES]?.Values<string>().OfType<string>().ToHashSet() ?? [];
        VersionedProjectJsonSerializer serializer = new();
        string settings = serializer.Serialize(project);

        foreach (string path in await context.PathsAsync())
        {
            var imported = await service.ImportAsync(path, context.Selection(), context.TimeCode, context.CancellationToken);
            var sources = imported.Sliders.Select(slider => slider.DeepCopy()).ToArray();
            bool preferLiveEditor = imported.PreferLiveEditor;

            foreach (var source in sources)
            {
                context.CancellationToken.ThrowIfCancellationRequested();
                var options = serializer.Deserialize<SlideratorServiceOptions>(settings);
                ConfigureSource(options, source, imported.GlobalSv, explicitOptions);

                await service.RunAsync(path, options, source, CliOptions.ReloadEditor.GetValue(context.ParseResult),
                    cancellationToken: context.CancellationToken, preferLiveEditor: preferLiveEditor);

                // Continue from our saved edits rather than the unchanged editor snapshot.
                preferLiveEditor = false;
            }

            context.Report(sources.Length);
        }

        return 0;
    }

    /// <summary>Derives timing and dimensions from one source unless the corresponding setting was explicitly overridden.</summary>
    /// <param name="options">Cloned settings whose graph is rescaled in place to the effective duration.</param>
    /// <param name="source">Snapshot of the originally imported slider, unaffected by earlier saves.</param>
    /// <param name="globalSv">The source beatmap's base slider multiplier.</param>
    /// <param name="explicitOptions">Application property names recorded as explicit CLI or saved CLI overrides.</param>
    private static void ConfigureSource(SlideratorServiceOptions options, HitObject source, double globalSv, ISet<string> explicitOptions)
    {
        if (!explicitOptions.Contains(nameof(SlideratorEngineOptions.GlobalSv)))
            options.GlobalSv = globalSv;

        if (!explicitOptions.Contains(nameof(SlideratorEngineOptions.BeatsPerMinute)))
            options.BeatsPerMinute = source.UnInheritedTimingPoint is { MpB: > 0 } timing ? 60000 / timing.MpB : 180;

        if (!explicitOptions.Contains(nameof(SlideratorEngineOptions.PixelLength)))
            options.PixelLength = source.PixelLength;

        if (!explicitOptions.Contains(nameof(SlideratorEngineOptions.ExportTime)))
            options.ExportTime = source.Time;

        if (!explicitOptions.Contains(nameof(SlideratorEngineOptions.GraphBeats)))
            options.GraphBeats = source.TemporalLength * options.BeatsPerMinute / 60000;

        var graph = options.GraphState;
        double width = graph.MaxX - graph.MinX;

        if (width > 0)
            foreach (var anchor in graph.Anchors)
                anchor.Pos = new Vector2((anchor.Pos.X - graph.MinX) / width * options.GraphBeats, anchor.Pos.Y);

        graph.MinX = 0;
        graph.MaxX = options.GraphBeats;
    }

    /// <summary>
    /// Captures explicit source-value overrides and saved selection before project saving, then resolves targets and
    /// validates selection for actual runs.
    /// </summary>
    /// <param name="context">Invocation with merged settings and CLI arguments.</param>
    /// <param name="schema">Bindings used to identify explicit model options.</param>
    /// <returns>A task completing after the document is ready for typed deserialization and project saving.</returns>
    private static async Task PrepareAsync(ToolContext context, OptionSchema schema)
    {
        var args = context;
        var document = context.Document;

        if (args.GetOption(CliOptions.Selection) is { } selection)
            document[CliDocumentKeys.SELECTION] = selection;

        if (args.GetOption(CliOptions.TimeCode) is { } timeCode)
            document[CliDocumentKeys.TIME_CODE] = timeCode;

        var explicitValues = document[CliDocumentKeys.SOURCE_OVERRIDES]?.Values<string>().OfType<string>().ToHashSet() ?? [];

        foreach (var (option, _) in args.ExplicitOptions)
            if (schema.Options.TryGetValue(option, out var entry)
                && entry.Path is nameof(SlideratorEngineOptions.ExportTime)
                    or nameof(SlideratorEngineOptions.BeatsPerMinute)
                    or nameof(SlideratorEngineOptions.PixelLength)
                    or nameof(SlideratorEngineOptions.GraphBeats)
                    or nameof(SlideratorEngineOptions.GlobalSv))
                explicitValues.Add(entry.Path);

        document[CliDocumentKeys.SOURCE_OVERRIDES] = new JArray(explicitValues.Order(StringComparer.Ordinal));
        document[CliDocumentKeys.IMPORT_MODE_SETTING] = document[CliDocumentKeys.SELECTION]
            ?? document[CliDocumentKeys.IMPORT_MODE_SETTING];
        document[CliDocumentKeys.TIME_CODE_SETTING] = document[CliDocumentKeys.TIME_CODE]
            ?? document[CliDocumentKeys.TIME_CODE_SETTING];
        document.Remove(CliDocumentKeys.LOADED_HIT_OBJECTS);

        await context.PrepareTargetsAsync(schema);

        if (!CliOptions.NoRun.GetValue(args.ParseResult))
            context.Selection();
    }
}
