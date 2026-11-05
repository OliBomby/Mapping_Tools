using Mapping_Tools.Application.Tools.PropertyTransformer;
using Mapping_Tools.Cli.Commands;
using Spectre.Console;

namespace Mapping_Tools.Cli.Tools;

/// <summary>Applies property transformations to explicitly selected objects across target beatmaps.</summary>
internal static class PropertyTransformerCommand
{
    /// <summary>Registers transformation settings and delegates the complete target list to the service.</summary>
    /// <param name="console">Renders help and supplies the output writer for operation results.</param>
    /// <param name="error">Receives diagnostics; its lifetime belongs to the caller.</param>
    /// <param name="input">Supplies standard input for explicitly interactive operations; its lifetime belongs to the caller.</param>
    /// <returns>A ready-to-register command with inherited root help.</returns>
    internal static ToolCommand Create(IAnsiConsole console, TextWriter error, TextReader input) =>
        ToolCommand.For(
            console, error, input,
            PropertyTransformerToolDefinition.Definition,
            () => new PropertyTransformerServiceOptions(),
            async (context, options) =>
            {
                context.Report(await context.Get<IPropertyTransformerService>().TransformAsync(
                    await context.PathsAsync(), options,
                    quickRun: CliOptions.ReloadEditor.GetValue(context.ParseResult),
                    cancellationToken: context.CancellationToken));

                return 0;
            });
}
