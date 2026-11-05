using Mapping_Tools.Application.Tools.ComboColourStudio;
using Mapping_Tools.Cli.Commands;
using Mapping_Tools.Cli.Localization;
using Mapping_Tools.Infrastructure.Projects;
using Newtonsoft.Json.Linq;
using Spectre.Console;

namespace Mapping_Tools.Cli.Tools;

/// <summary>Imports palettes or colour hax and applies configured combo colours to target maps.</summary>
internal static class ComboColourStudioCommand
{
    private static readonly VersionedProjectJsonSerializer serializer = new();

    /// <summary>Registers colour imports and batch combo-colour application.</summary>
    /// <param name="console">Renders help and supplies the output writer for operation results.</param>
    /// <param name="error">Receives diagnostics; its lifetime belongs to the caller.</param>
    /// <param name="input">Supplies standard input for explicitly interactive operations; its lifetime belongs to the caller.</param>
    /// <returns>A ready-to-register command with inherited root help.</returns>
    internal static ToolCommand Create(IAnsiConsole console, TextWriter error, TextReader input) =>
        ToolCommand.For(
            console, error, input,
            ComboColourStudioToolDefinition.Definition,
            () => new ComboColourServiceOptions(),
            async (context, options) =>
            {
                context.Report(await context.Get<IComboColourStudioService>().ApplyAsync(
                    await context.PathsAsync(), options,
                    quickRun: CliOptions.ReloadEditor.GetValue(context.ParseResult),
                    cancellationToken: context.CancellationToken));

                return 0;
            },
            ImportAsync,
            options: new Dictionary<string, string>
            {
                [CliOptions.FromCurrent.Name] = CliStrings.FromCurrentHelp,
                [CliOptions.From.Name] = CliStrings.FromHelp,
                [CliOptions.ImportColourHax.Name] = CliStrings.ImportColourHaxHelp,
            });

    /// <summary>Resolves an optional current source, then imports palette and colour-hax settings in the requested workflow.</summary>
    /// <param name="context">Invocation whose mutable document receives imported settings before model option binding.</param>
    /// <returns>A task completing after requested imports and their layer overrides are applied.</returns>
    /// <remarks>Also runs for project-only invocations; tool export and beatmap editing remain in the operation handler.</remarks>
    private static async Task ImportAsync(ToolContext context)
    {
        await context.ResolveCurrentSourceAsync();

        if (context.GetOption(CliOptions.From) is { } path)
            context.MergeImportedSettings(JObject.Parse(serializer.Serialize(
                await context.Get<IComboColourStudioService>().ImportComboColoursAsync(path, context.CancellationToken))));

        if (context.GetOption(CliOptions.ImportColourHax) is { } hax)
            context.MergeImportedSettings(JObject.Parse(serializer.Serialize(
                await context.Get<IComboColourStudioService>().ImportColourHaxAsync(hax, 4, context.CancellationToken))));
    }
}
