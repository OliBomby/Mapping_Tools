using Mapping_Tools.Application.Tools.HitsoundPreviewHelper;
using Mapping_Tools.Cli.Commands;
using Spectre.Console;

namespace Mapping_Tools.Cli.Tools;

/// <summary>Applies Hitsound Preview Helper settings to a batch of target beatmaps.</summary>
internal static class HitsoundPreviewHelperCommand
{
    /// <summary>Registers configured preview-helper items and delegates batch application to the service.</summary>
    /// <param name="console">Renders help and supplies the output writer for operation results.</param>
    /// <param name="error">Receives diagnostics; its lifetime belongs to the caller.</param>
    /// <param name="input">Supplies standard input for explicitly interactive operations; its lifetime belongs to the caller.</param>
    /// <returns>A ready-to-register command with inherited root help.</returns>
    internal static ToolCommand Create(IAnsiConsole console, TextWriter error, TextReader input) =>
        ToolCommand.For(
            console, error, input,
            HitsoundPreviewHelperToolDefinition.Definition,
            () => new HitsoundPreviewHelperServiceOptions(),
            async (context, options) =>
            {
                context.Report(await context.Get<IHitsoundPreviewHelperService>().ApplyAsync(
                    await context.PathsAsync(), options,
                    quickRun: CliOptions.ReloadEditor.GetValue(context.ParseResult),
                    cancellationToken: context.CancellationToken));

                return 0;
            });
}
