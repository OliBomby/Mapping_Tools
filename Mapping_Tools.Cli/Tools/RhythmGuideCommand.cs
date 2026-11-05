using Mapping_Tools.Application.BeatmapEditing.Contracts;
using Mapping_Tools.Application.Tools.RhythmGuide;
using Mapping_Tools.Cli.Commands;
using Mapping_Tools.Core.Tools.RhythmGuide.Models;
using Spectre.Console;

namespace Mapping_Tools.Cli.Tools;

/// <summary>Generates rhythm guides while enforcing export collision and backup policies.</summary>
internal static class RhythmGuideCommand
{
    /// <summary>Registers guide settings and binds CLI targets into the nested Application run options.</summary>
    /// <param name="console">Renders help and supplies the output writer for operation results.</param>
    /// <param name="error">Receives diagnostics; its lifetime belongs to the caller.</param>
    /// <param name="input">Supplies standard input for explicitly interactive operations; its lifetime belongs to the caller.</param>
    /// <returns>A ready-to-register command with inherited root help.</returns>
    internal static ToolCommand Create(IAnsiConsole console, TextWriter error, TextReader input) =>
        ToolCommand.For(
            console, error, input,
            RhythmGuideToolDefinition.Definition,
            () => new RhythmGuideServiceOptions(),
            async (context, options) =>
            {
                if (options.GuideGeneratorArgs.ExportMode == RhythmGuideExportMode.NewMap)
                    context.CheckOutput(options.GuideGeneratorArgs.ExportPath);

                context.Report(await context.Get<IRhythmGuideService>().GenerateAsync(
                    options.GuideGeneratorArgs, context.CancellationToken));

                if (CliOptions.ReloadEditor.GetValue(context.ParseResult))
                    await context.Get<IEditorReloadService>().ReloadAsync(context.CancellationToken);

                return 0;
            },
            prepare: (context, schema) => context.PrepareTargetsAsync(schema, "GuideGeneratorArgs.Paths"));
}
