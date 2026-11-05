using Mapping_Tools.Application.Tools.SliderCompletionator;
using Mapping_Tools.Cli.Commands;
using Spectre.Console;

namespace Mapping_Tools.Cli.Tools;

/// <summary>Completes selected sliders using Application batch behavior and shared editor-time bounds.</summary>
internal static class SliderCompletionatorCommand
{
    /// <summary>Registers selection and completion settings for one batch service call.</summary>
    /// <param name="console">Renders help and supplies the output writer for operation results.</param>
    /// <param name="error">Receives diagnostics; its lifetime belongs to the caller.</param>
    /// <param name="input">Supplies standard input for explicitly interactive operations; its lifetime belongs to the caller.</param>
    /// <returns>A ready-to-register command with inherited root help.</returns>
    internal static ToolCommand Create(IAnsiConsole console, TextWriter error, TextReader input) =>
        ToolCommand.For(
            console, error, input,
            SliderCompletionatorToolDefinition.Definition,
            () => new SliderCompletionatorServiceOptions(),
            async (context, options) =>
            {
                context.Selection();
                context.Report(await context.Get<ISliderCompletionatorService>().CompleteAsync(
                    await context.PathsAsync(), options,
                    quickRun: CliOptions.ReloadEditor.GetValue(context.ParseResult),
                    cancellationToken: context.CancellationToken));

                return 0;
            });
}
