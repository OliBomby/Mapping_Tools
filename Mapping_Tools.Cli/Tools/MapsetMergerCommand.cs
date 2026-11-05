using Mapping_Tools.Application.Tools.MapsetMerger;
using Mapping_Tools.Application.Tools.MapsetMerger.Contracts;
using Mapping_Tools.Application.Tools.MapsetMerger.Models;
using Mapping_Tools.Cli.Commands;
using Mapping_Tools.Cli.Configuration;
using Mapping_Tools.Cli.Localization;
using Spectre.Console;

namespace Mapping_Tools.Cli.Tools;

/// <summary>Merges mapsets through the Application service's export transaction.</summary>
internal static class MapsetMergerCommand
{
    /// <summary>Registers mapset inputs, output cleanup policy, and staged export handling.</summary>
    /// <param name="console">Renders help and supplies the output writer for operation results.</param>
    /// <param name="error">Receives diagnostics; its lifetime belongs to the caller.</param>
    /// <param name="input">Supplies standard input for explicitly interactive operations; its lifetime belongs to the caller.</param>
    /// <returns>A ready-to-register command with inherited root help.</returns>
    internal static ToolCommand Create(IAnsiConsole console, TextWriter error, TextReader input) =>
        ToolCommand.For(
            console, error, input,
            MapsetMergerToolDefinition.Definition,
            () => new MapsetMergerServiceOptions(),
            async (context, options) =>
            {
                var result = await context.Get<IMapsetMergerService>().MergeAsync(
                    options,
                    cancellationToken: context.CancellationToken,
                    overwrite: CliOptions.Overwrite.GetValue(context.ParseResult),
                    clearExisting: CliOptions.CleanOutput.GetValue(context.ParseResult));

                foreach (string path in result.OutputFiles)
                    context.ReportFile(path);

                context.Report(result);

                return 0;
            },
            options: new Dictionary<string, string>
            {
                [CliOptions.CleanOutput.Name] = CliStrings.CleanHelp,
            },
            prepare: PrepareAsync);

    /// <summary>Requires explicit overwrite permission for output cleanup before resolving any saved or explicit targets.</summary>
    /// <param name="context">Invocation with merged settings and CLI arguments.</param>
    /// <param name="schema">Bindings used to identify explicit model options.</param>
    /// <returns>A task completing after the document is ready for typed deserialization and project saving.</returns>
    private static Task PrepareAsync(ToolContext context, OptionSchema schema)
    {
        if (CliOptions.CleanOutput.GetValue(context.ParseResult) && !CliOptions.Overwrite.GetValue(context.ParseResult))
            throw new ArgumentException(CliStrings.CleanRequiresOverwrite);

        return context.PrepareTargetsAsync(schema);
    }
}
