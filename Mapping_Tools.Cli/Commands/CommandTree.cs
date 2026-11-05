using System.CommandLine;
using System.CommandLine.Help;
using Mapping_Tools.Cli.Localization;
using Mapping_Tools.Cli.Tools;
using Spectre.Console;

namespace Mapping_Tools.Cli.Commands;

/// <summary>Composes the root command and explicitly registers the commands returned by each tool factory.</summary>
/// <param name="console">Renders help and supplies the output writer for operation results.</param>
/// <param name="error">Receives diagnostics; its lifetime belongs to the caller.</param>
/// <param name="input">Supplies standard input for explicitly interactive operations; its lifetime belongs to the caller.</param>
/// <remarks>The root's native recursive help option is customized once and inherited by every descendant.</remarks>
internal sealed class CommandTree(IAnsiConsole console, TextWriter error, TextReader input)
{
    /// <summary>Builds a fresh tree without executing tools or reading invocation configuration.</summary>
    /// <returns>The ready-to-parse root command, including config and manual update commands.</returns>
    internal RootCommand Create()
    {
        RootCommand root = new(CliStrings.RootDescription);
        root.Directives.Clear();
        CommandOptions.Add(root, CommandOptions.Common.Where(pair =>
            pair.Key == CliOptions.Config.Name || pair.Key == CliOptions.NoConfig.Name), recursive: true);

        var helpAction = new HelpWriter(console);
        var help = root.Options.OfType<HelpOption>().Single();
        help.Description = CliStrings.HelpHelp;
        help.Action = helpAction;
        root.SetAction(result => helpAction.Invoke(result));

        root.Subcommands.Add(AutoFailCommand.Create(console, error, input));
        root.Subcommands.Add(ComboColourStudioCommand.Create(console, error, input));
        root.Subcommands.Add(GeometryCommand.Create(console, error, input));
        root.Subcommands.Add(HitsoundCopierCommand.Create(console, error, input));
        root.Subcommands.Add(HitsoundPreviewHelperCommand.Create(console, error, input));
        root.Subcommands.Add(HitsoundStudioCommand.Create(console, error, input));
        root.Subcommands.Add(MapCleanerCommand.Create(console, error, input));
        root.Subcommands.Add(MapsetMergerCommand.Create(console, error, input));
        root.Subcommands.Add(MetadataManagerCommand.Create(console, error, input));
        root.Subcommands.Add(PatternGalleryCommand.Create(console, error, input));
        root.Subcommands.Add(PropertyTransformerCommand.Create(console, error, input));
        root.Subcommands.Add(RhythmGuideCommand.Create(console, error, input));
        root.Subcommands.Add(SlideratorCommand.Create(console, error, input));
        root.Subcommands.Add(SliderCompletionatorCommand.Create(console, error, input));
        root.Subcommands.Add(SliderMergerCommand.Create(console, error, input));
        root.Subcommands.Add(SliderPicturatorCommand.Create(console, error, input));
        root.Subcommands.Add(TimingCopierCommand.Create(console, error, input));
        root.Subcommands.Add(TimingHelperCommand.Create(console, error, input));
        root.Subcommands.Add(TumourGeneratorCommand.Create(console, error, input));

        root.Subcommands.Add(ConfigCommand.Create(console));
        root.Subcommands.Add(UpdateCommand.Create(console));

        return root;
    }
}
