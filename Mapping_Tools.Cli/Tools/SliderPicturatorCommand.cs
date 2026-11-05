using Mapping_Tools.Application.Tools.SliderPicturator;
using Mapping_Tools.Cli.Commands;
using Mapping_Tools.Cli.Configuration;
using Mapping_Tools.Cli.Localization;
using Mapping_Tools.Core.Tools.SliderPicturator.Models;
using Spectre.Console;

namespace Mapping_Tools.Cli.Tools;

/// <summary>Builds slider pictures from image settings and optionally reads a live selected source slider.</summary>
internal static class SliderPicturatorCommand
{
    /// <summary>Registers picturation settings and discards saved preview geometry before execution.</summary>
    /// <param name="console">Renders help and supplies the output writer for operation results.</param>
    /// <param name="error">Receives diagnostics; its lifetime belongs to the caller.</param>
    /// <param name="input">Supplies standard input for explicitly interactive operations; its lifetime belongs to the caller.</param>
    /// <returns>A ready-to-register command with inherited root help.</returns>
    internal static ToolCommand Create(IAnsiConsole console, TextWriter error, TextReader input) =>
        ToolCommand.For(
            console, error, input,
            SliderPicturatorToolDefinition.Definition,
            () => new SliderPicturatorServiceOptions(),
            async (context, options) =>
            {
                string path = await context.OnePathAsync();

                if (CliOptions.UseSelectedSlider.GetValue(context.ParseResult))
                    options.SelectedSlider = await context.Get<ISliderPicturatorService>().GetSelectedSliderAsync(
                        path, context.CancellationToken);

                context.Report(await context.Get<ISliderPicturatorService>().PicturateAsync(
                    path, options,
                    quickRun: CliOptions.ReloadEditor.GetValue(context.ParseResult),
                    cancellationToken: context.CancellationToken));

                return 0;
            },
            options: new Dictionary<string, string>
            {
                [CliOptions.UseSelectedSlider.Name] = CliStrings.UseSelectedSliderHelp,
            },
            prepare: PrepareAsync);

    /// <summary>
    /// Discards saved preview geometry and resolves target paths; live selected geometry is requested separately during
    /// execution.
    /// </summary>
    /// <param name="context">Invocation with merged settings and CLI arguments.</param>
    /// <param name="schema">Bindings used to identify explicit model options.</param>
    /// <returns>A task completing after the document is ready for typed deserialization and project saving.</returns>
    private static Task PrepareAsync(ToolContext context, OptionSchema schema)
    {
        context.Document.Remove(nameof(SliderPicturatorEngineOptions.SelectedSlider));

        return context.PrepareTargetsAsync(schema);
    }
}
