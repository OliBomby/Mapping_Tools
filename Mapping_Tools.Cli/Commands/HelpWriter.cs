using System.CommandLine;
using System.CommandLine.Help;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using Mapping_Tools.Application.Tools.GeometryDashboard;
using Mapping_Tools.Application.Tools.PatternGallery;
using Mapping_Tools.Application.Tools.Sliderator;
using Mapping_Tools.Cli.Configuration;
using Mapping_Tools.Cli.Localization;
using Mapping_Tools.Cli.Tools;
using Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObjectGenerators;
using Newtonsoft.Json.Linq;
using Spectre.Console;

namespace Mapping_Tools.Cli.Commands;

/// <summary>Decorates System.CommandLine-generated help with terminal colors, localized notes, examples, and generator information.</summary>
/// <param name="console">Receives rendered help; its detected width controls library wrapping.</param>
/// <remarks>One root help action serves the whole tree. The selected command's ancestors provide tool context for nested help.</remarks>
internal sealed class HelpWriter(IAnsiConsole console) : SynchronousCommandLineAction
{
    private const string options_heading = "Options:";
    private const string example_resource_prefix = "Example.";

    /// <inheritdoc />
    /// <remarks>Help remains available even when required arguments are missing or other supplied options are invalid.</remarks>
    public override bool ClearsParseErrors => true;

    /// <inheritdoc />
    public override int Invoke(ParseResult parseResult)
    {
        var command = parseResult.CommandResult.Command;
        bool root = command is RootCommand;
        var ancestor = parseResult.CommandResult;
        List<string> subcommands = [];

        while (ancestor.Command is not ToolCommand && ancestor.Parent is CommandResult parent)
        {
            subcommands.Insert(0, ancestor.Command.Name);
            ancestor = parent;
        }

        var tool = ancestor.Command as ToolCommand;
        var schema = tool?.Schema;

        TextWriter original = parseResult.InvocationConfiguration.Output;
        using StringWriter text = new();
        parseResult.InvocationConfiguration.Output = text;

        try
        {
            new HelpAction { MaxWidth = Math.Min(console.Profile.Width, 110) }.Invoke(parseResult);
        }
        finally
        {
            parseResult.InvocationConfiguration.Output = original;
        }

        using StringReader reader = new(text.ToString());
        bool inOptions = false;
        int? descriptionColumn = null;

        while (reader.ReadLine() is { } line)
        {
            if (line.EndsWith(':'))
            {
                inOptions = line.Trim() == options_heading;
                descriptionColumn = null;

                var heading = new Style(Color.Cyan, decoration: Decoration.Bold);
                console.Write(new Text(line + Environment.NewLine, heading));

                continue;
            }

            if (inOptions && IsOptionLine(line))
            {
                descriptionColumn = FindDescriptionColumn(line);

                if (descriptionColumn is { } column)
                    WriteOptionLine(line, column);
                else
                    console.Write(new Text(line + Environment.NewLine, new Style(Color.White)));

                continue;
            }

            if (inOptions && descriptionColumn is { } continuationColumn)
            {
                WriteDescriptionContinuation(line, continuationColumn);

                continue;
            }

            console.Write(new Text(line + Environment.NewLine, Style.Plain));
        }

        string[] notes = Notes(command, root, tool, schema, string.Join(' ', subcommands)).ToArray();

        if (notes.Length > 0)
        {
            Heading(CliStrings.Notes);
            console.WriteLine(string.Join(Environment.NewLine + Environment.NewLine, notes));
        }

        if (tool?.Name == GeometryDashboardToolDefinition.Definition.Id)
        {
            Heading(CliStrings.Generators);
            console.WriteLine(CliStrings.GeneratorsHelp);

            var generators = typeof(RelevantObjectsGenerator).Assembly.GetTypes()
                .Where(type => !type.IsAbstract && typeof(RelevantObjectsGenerator).IsAssignableFrom(type)
                               && type.GetConstructor(Type.EmptyTypes) is not null)
                .Select(type => (RelevantObjectsGenerator)Activator.CreateInstance(type)!)
                .OrderBy(generator => generator.Id, StringComparer.Ordinal);

            foreach (var generator in generators)
                console.WriteLine($"  {generator.Id} ($kind = {generator.Settings.Id})");
        }

        string? example = root ? CliStrings.Example_root
            : tool is not null ? CliStrings.ResourceManager.GetString(
                example_resource_prefix + string.Join('.', new[] { tool.Name }.Concat(subcommands)), CliStrings.Culture)
            : command.Name switch
            {
                ConfigCommand.COMMAND => CliStrings.Config_Examples,
                ConfigCommand.SHOW => CliStrings.Config_Example_show,
                ConfigCommand.GET => CliStrings.Config_Example_get,
                ConfigCommand.SET => CliStrings.Config_Example_set,
                ConfigCommand.UNSET => CliStrings.Config_Example_unset,
                ConfigCommand.RESET => CliStrings.Config_Example_reset,
                ConfigCommand.PATH => CliStrings.Config_Example_path,
                _ => null,
            };

        if (example is not null)
        {
            Heading(CliStrings.Examples);
            console.WriteLine(example);
        }

        return 0;
    }

    private static IEnumerable<string> Notes(Command command, bool root, ToolCommand? tool, OptionSchema? schema, string subcommand)
    {
        if (root)
            yield return CliStrings.RootHelp;

        if (tool is not null && schema is not null
            && (tool.Name != PatternGalleryToolDefinition.Definition.Id || subcommand == PatternGalleryPatternCommands.EXPORT))
        {
            if (schema.Options.Values.Any(entry => entry.Default is JObject or JArray))
                yield return CliStrings.CompoundHelp;

            if (schema.Options.Values.Any(entry => entry.Default is JArray))
                yield return CliStrings.ListHelp;
        }

        if (tool?.Name == SlideratorToolDefinition.Definition.Id)
            yield return CliStrings.SlideratorHelp;

        if (tool?.Name == GeometryDashboardToolDefinition.Definition.Id)
            yield return CliStrings.GeometryHelp;

        if (tool is null && command.Name is ConfigCommand.COMMAND or ConfigCommand.GET or ConfigCommand.SHOW)
            yield return CliStrings.Config_DefaultsHelp;

        if (tool is null && command.Name is ConfigCommand.COMMAND or ConfigCommand.SET)
            yield return CliStrings.Config_EditingHelp;
    }

    private void Heading(string text)
    {
        console.WriteLine();
        console.Write(new Text(text + Environment.NewLine, new Style(Color.Cyan, decoration: Decoration.Bold)));
    }

    private void WriteOptionLine(string line, int descriptionColumn)
    {
        console.Write(new Text(line[..descriptionColumn], new Style(Color.White)));
        console.Write(new Text(line[descriptionColumn..] + Environment.NewLine, new Style(Color.Green)));
    }

    private void WriteDescriptionContinuation(string line, int descriptionColumn)
    {
        int split = Math.Min(descriptionColumn, line.Length);
        console.Write(new Text(line[..split], new Style(Color.White)));
        console.Write(new Text(line[split..] + Environment.NewLine, new Style(Color.Green)));
    }

    private static bool IsOptionLine(string line)
    {
        int firstText = line.IndexOfAnyExcept(' ');

        return firstText >= 0 && line[firstText] == '-';
    }

    private static int? FindDescriptionColumn(string line)
    {
        int index = 0;

        while (index < line.Length && char.IsWhiteSpace(line[index]))
            index++;

        for (; index < line.Length; index++)
        {
            if (!char.IsWhiteSpace(line[index]))
                continue;

            int end = index;

            while (end < line.Length && char.IsWhiteSpace(line[end]))
                end++;

            if (end - index >= 2 && end < line.Length)
                return end;

            index = end - 1;
        }

        return null;
    }
}
