using Mapping_Tools.Application.Localization;
using Mapping_Tools.Application.Tools.HitsoundCopier;
using Mapping_Tools.Cli.Commands;
using Mapping_Tools.Cli.Localization;
using Spectre.Console;

namespace Mapping_Tools.Cli.Tools;

/// <summary>Copies hitsounds from an explicitly chosen source into target beatmaps.</summary>
internal static class HitsoundCopierCommand
{
    /// <summary>Registers source selection, target bindings, and hitsound-copy settings.</summary>
    /// <param name="console">Renders help and supplies the output writer for operation results.</param>
    /// <param name="error">Receives diagnostics; its lifetime belongs to the caller.</param>
    /// <param name="input">Supplies standard input for explicitly interactive operations; its lifetime belongs to the caller.</param>
    /// <returns>A ready-to-register command with inherited root help.</returns>
    internal static ToolCommand Create(IAnsiConsole console, TextWriter error, TextReader input) =>
        ToolCommand.For(
            console, error, input,
            HitsoundCopierToolDefinition.Definition,
            () => new HitsoundCopierServiceOptions(),
            async (context, options) =>
            {
                var result = await context.Get<IHitsoundCopierService>().CopyAsync(
                    options,
                    quickRun: CliOptions.ReloadEditor.GetValue(context.ParseResult),
                    cancellationToken: context.CancellationToken);
                context.Report(result);
                if (result.OutputDirectory is { } outputDirectory)
                    context.Output.WriteLine(ApplicationText.Format(CliStrings.OutputDirectory, outputDirectory));

                return 0;
            },
            options: new Dictionary<string, string>
            {
                [CliOptions.FromCurrent.Name] = CliStrings.FromCurrentHelp,
            },
            prepare: (context, schema) => context.PrepareTargetsAsync(
                schema, nameof(HitsoundCopierServiceOptions.PathTo)),
            import: context => context.ResolveCurrentSourceAsync());
}
