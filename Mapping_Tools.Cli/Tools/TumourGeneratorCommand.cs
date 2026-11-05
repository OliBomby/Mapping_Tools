using Mapping_Tools.Application.Tools.TumourGenerator;
using Mapping_Tools.Application.Tools.TumourGenerator.Models;
using Mapping_Tools.Cli.Commands;
using Mapping_Tools.Cli.Configuration;
using Spectre.Console;

namespace Mapping_Tools.Cli.Tools;

/// <summary>Applies configured tumour layers to selected objects while discarding Desktop preview geometry.</summary>
internal static class TumourGeneratorCommand
{
    /// <summary>Registers tumour-layer options, selection, and batch execution.</summary>
    /// <param name="console">Renders help and supplies the output writer for operation results.</param>
    /// <param name="error">Receives diagnostics; its lifetime belongs to the caller.</param>
    /// <param name="input">Supplies standard input for explicitly interactive operations; its lifetime belongs to the caller.</param>
    /// <returns>A ready-to-register command with inherited root help.</returns>
    internal static ToolCommand Create(IAnsiConsole console, TextWriter error, TextReader input) =>
        ToolCommand.For(
            console, error, input,
            TumourGeneratorToolDefinition.Definition,
            () => new TumourGeneratorServiceOptions(),
            async (context, options) =>
            {
                context.Selection();
                context.Report(await context.Get<ITumourGeneratorService>().RunAsync(
                    await context.PathsAsync(), options,
                    quickRun: CliOptions.ReloadEditor.GetValue(context.ParseResult),
                    cancellationToken: context.CancellationToken));

                return 0;
            },
            prepare: PrepareAsync);

    /// <summary>Discards Desktop preview geometry and resolves the actual target beatmaps.</summary>
    /// <param name="context">Invocation with merged settings and CLI arguments.</param>
    /// <param name="schema">Bindings used to identify explicit model options.</param>
    /// <returns>A task completing after the document is ready for typed deserialization and project saving.</returns>
    private static Task PrepareAsync(ToolContext context, OptionSchema schema)
    {
        context.Document.Remove(CliDocumentKeys.PREVIEW_HIT_OBJECT);

        return context.PrepareTargetsAsync(schema);
    }
}
