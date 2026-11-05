using Mapping_Tools.Application.Tools.MapCleaner;
using Mapping_Tools.Cli.Commands;
using Spectre.Console;

namespace Mapping_Tools.Cli.Tools;

/// <summary>Runs Map Cleaner as one Application batch while reporting individual saved files.</summary>
internal static class MapCleanerCommand
{
    /// <summary>Registers cleanup settings and delegates the full target list to the cleanup service.</summary>
    /// <param name="console">Renders help and supplies the output writer for operation results.</param>
    /// <param name="error">Receives diagnostics; its lifetime belongs to the caller.</param>
    /// <param name="input">Supplies standard input for explicitly interactive operations; its lifetime belongs to the caller.</param>
    /// <returns>A ready-to-register command with inherited root help.</returns>
    internal static ToolCommand Create(IAnsiConsole console, TextWriter error, TextReader input) =>
        ToolCommand.For(
            console, error, input,
            MapCleanerToolDefinition.Definition,
            () => new MapCleanerServiceOptions(),
            async (context, options) =>
            {
                context.Report(await context.Get<IMapCleanerService>().CleanAsync(
                    await context.PathsAsync(), options.MapCleanerArgs,
                    quickRun: CliOptions.ReloadEditor.GetValue(context.ParseResult),
                    cancellationToken: context.CancellationToken));

                return 0;
            });
}
