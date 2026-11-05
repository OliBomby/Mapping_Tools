using Mapping_Tools.Application.BeatmapEditing.Contracts;
using Mapping_Tools.Application.Tools.TimingCopier;
using Mapping_Tools.Cli.Commands;
using Mapping_Tools.Cli.Localization;
using Spectre.Console;

namespace Mapping_Tools.Cli.Tools;

/// <summary>Copies timing from a chosen source into target beatmaps.</summary>
internal static class TimingCopierCommand
{
    /// <summary>Registers source selection and multi-target timing-copy settings.</summary>
    /// <param name="console">Renders help and supplies the output writer for operation results.</param>
    /// <param name="error">Receives diagnostics; its lifetime belongs to the caller.</param>
    /// <param name="input">Supplies standard input for explicitly interactive operations; its lifetime belongs to the caller.</param>
    /// <returns>A ready-to-register command with inherited root help.</returns>
    internal static ToolCommand Create(IAnsiConsole console, TextWriter error, TextReader input) =>
        ToolCommand.For(
            console, error, input,
            TimingCopierToolDefinition.Definition,
            () => new TimingCopierServiceOptions(),
            async (context, options) =>
            {
                context.Report(await context.Get<ITimingCopierService>().CopyAsync(
                    options, cancellationToken: context.CancellationToken));

                if (CliOptions.ReloadEditor.GetValue(context.ParseResult))
                    await context.Get<IEditorReloadService>().ReloadAsync(context.CancellationToken);

                return 0;
            },
            options: new Dictionary<string, string>
            {
                [CliOptions.FromCurrent.Name] = CliStrings.FromCurrentHelp,
            },
            prepare: (context, schema) => context.PrepareTargetsAsync(
                schema, nameof(TimingCopierServiceOptions.ExportPath)),
            import: context => context.ResolveCurrentSourceAsync());
}
