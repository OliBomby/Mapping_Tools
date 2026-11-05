using Mapping_Tools.Application.Tools.SliderMerger;
using Mapping_Tools.Cli.Commands;
using Spectre.Console;

namespace Mapping_Tools.Cli.Tools;

/// <summary>Merges selected sliders across the supplied beatmaps using the Application batch service.</summary>
internal static class SliderMergerCommand
{
    /// <summary>Registers explicit object selection and slider-merging settings.</summary>
    /// <param name="console">Renders help and supplies the output writer for operation results.</param>
    /// <param name="error">Receives diagnostics; its lifetime belongs to the caller.</param>
    /// <param name="input">Supplies standard input for explicitly interactive operations; its lifetime belongs to the caller.</param>
    /// <returns>A ready-to-register command with inherited root help.</returns>
    internal static ToolCommand Create(IAnsiConsole console, TextWriter error, TextReader input) =>
        ToolCommand.For(
            console, error, input,
            SliderMergerToolDefinition.Definition,
            () => new SliderMergerServiceOptions(),
            async (context, options) =>
            {
                context.Selection();
                context.Report(await context.Get<ISliderMergerService>().MergeAsync(
                    await context.PathsAsync(), options,
                    quickRun: CliOptions.ReloadEditor.GetValue(context.ParseResult),
                    cancellationToken: context.CancellationToken));

                return 0;
            });
}
