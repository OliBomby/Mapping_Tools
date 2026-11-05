using System.CommandLine;
using Mapping_Tools.Application.Localization;
using Mapping_Tools.Application.Tools.PatternGallery;
using Mapping_Tools.Application.Tools.PatternGallery.Models;
using Mapping_Tools.Cli.Commands;
using Mapping_Tools.Cli.Configuration;
using Mapping_Tools.Cli.Localization;
using Mapping_Tools.Core.Tools.PatternGallery.Models;
using Mapping_Tools.Infrastructure.Projects;
using Newtonsoft.Json.Linq;
using Spectre.Console;

namespace Mapping_Tools.Cli.Tools;

/// <summary>Registers Pattern Gallery library and pattern operations for the CLI.</summary>
internal static class PatternGalleryCommand
{
    private static readonly VersionedProjectJsonSerializer serializer = new();

    private static readonly Application.Projects.Models.ToolConfigSchema schema =
        Application.Projects.Models.ToolConfigSchema.ForTool(PatternGalleryToolDefinition.Definition.Id);

    private static readonly IReadOnlyDictionary<string, string> options = new Dictionary<string, string>
    {
        [CliOptions.Library.Name] = CliStrings.LibraryHelp,
        [CliOptions.Collection.Name] = CliStrings.CollectionHelp,
        [CliOptions.Pattern.Name] = CliStrings.PatternHelp,
        [CliOptions.AllPatterns.Name] = CliStrings.AllPatternsHelp,
        [CliOptions.Name.Name] = CliStrings.NameHelp,
        [CliOptions.Output.Name] = CliStrings.OutputHelp,
        [CliOptions.Source.Name] = CliStrings.SourceHelp,
        [CliOptions.Merge.Name] = CliStrings.MergeHelp,
        [CliOptions.ObjectCode.Name] = CliStrings.ObjectCodeHelp,
        [CliOptions.TimingCode.Name] = CliStrings.TimingCodeHelp,
        [CliOptions.Selection.Name] = CliStrings.SelectionOptionHelp,
        [CliOptions.TimeCode.Name] = CliStrings.TimeCodeHelp,
        [CliOptions.FolderName.Name] = CliStrings.FolderNameHelp,
        [CliOptions.Group.Name] = CliStrings.GroupHelp,
        [CliOptions.GlobalSv.Name] = CliStrings.GlobalSvImportHelp,
        [CliOptions.GameMode.Name] = CliStrings.GameModeImportHelp,
        [CliOptions.StartTime.Name] = CliStrings.StartTimeHelp,
        [CliOptions.EndTime.Name] = CliStrings.EndTimeHelp,
    };

    /// <summary>Creates the Pattern Gallery root command with its native subcommand tree.</summary>
    /// <param name="console">Renders help and supplies the output writer for operation results.</param>
    /// <param name="error">Receives diagnostics; its lifetime belongs to the caller.</param>
    /// <param name="input">Supplies standard input for explicitly interactive operations; its lifetime belongs to the caller.</param>
    /// <returns>A ready-to-register command with inherited root help.</returns>
    internal static ToolCommand Create(IAnsiConsole console, TextWriter error, TextReader input) =>
        ToolCommand.For(
            console,
            error,
            input,
            PatternGalleryToolDefinition.Definition,
            () => new PatternGalleryServiceOptions(),
            (_, _) => throw new ArgumentException(CliStrings.SubcommandHelp),
            options: options,
            description: CliStrings.ToolDescription_pattern_gallery,
            configure: Configure,
            prepare: PrepareAsync);

    /// <summary>Creates an operation command and binds its handler to the shared tool execution lifecycle.</summary>
    /// <param name="tool">The owning tool command and its Application model bindings.</param>
    /// <param name="name">The operation's single command segment.</param>
    /// <param name="run">The operation handler for this command.</param>
    /// <param name="optionNames">CLI-only options supported by this operation.</param>
    /// <returns>The configured native command.</returns>
    internal static Command Operation(
        ToolCommand tool,
        string name,
        Func<ToolContext, PatternGalleryServiceOptions, Task<int>> run,
        params string[] optionNames)
    {
        var names = optionNames.ToHashSet(StringComparer.Ordinal);
        Command command = new(name);

        CommandOptions.Add(command, tool.CliOptionDescriptions.Where(pair => names.Contains(pair.Key)));
        command.SetAction((result, token) => tool.ExecuteAsync(result, token, run));

        return command;
    }

    /// <summary>Loads a validated collection project from its resolved library paths.</summary>
    /// <param name="context">The current operation context.</param>
    /// <param name="paths">Resolved collection paths.</param>
    /// <returns>The collection project.</returns>
    internal static PatternGalleryServiceOptions Read(ToolContext context, PatternGalleryCollectionPaths paths)
    {
        if (!context.Files.FileExists(paths.ProjectFile))
            throw new IOException(ApplicationText.Format(CliStrings.CollectionMissing, paths.Collection));

        var project = serializer.Deserialize<PatternGalleryServiceOptions>(schema, context.Files.ReadAllText(paths.ProjectFile));

        if (project.FileHandler.CollectionFolderName != Path.GetFileName(paths.Collection))
            throw new InvalidDataException(CliStrings.CollectionIdentity);

        return project;
    }

    /// <summary>Saves a collection project using its Application project schema.</summary>
    /// <param name="context">The current operation context.</param>
    /// <param name="paths">Resolved collection paths.</param>
    /// <param name="project">Collection settings to persist.</param>
    /// <returns>A task that completes when the project is saved.</returns>
    internal static Task SaveAsync(
        ToolContext context,
        PatternGalleryCollectionPaths paths,
        PatternGalleryServiceOptions project) =>
        context.Get<Application.Projects.Contracts.IProjectStore>().SaveAsync(
            schema, paths.ProjectFile, project, context.CancellationToken);

    /// <summary>Serializes a collection project for a Pattern Gallery archive.</summary>
    /// <param name="project">Collection settings to serialize.</param>
    /// <returns>Versioned project JSON.</returns>
    internal static string Serialize(PatternGalleryServiceOptions project) => serializer.Serialize(schema, project);

    /// <summary>Deserializes Pattern Gallery settings from an archive project document.</summary>
    /// <param name="json">Versioned project JSON.</param>
    /// <returns>Collection settings.</returns>
    internal static PatternGalleryServiceOptions Deserialize(string json) => serializer.Deserialize<PatternGalleryServiceOptions>(schema, json);

    private static void Configure(ToolCommand command, IAnsiConsole console)
    {
        CommandOptions.Add(command, command.CliOptionDescriptions.Where(pair => pair.Key == CliOptions.Library.Name), recursive: true);

        command.SetAction((result, token) => CliOptions.NoRun.GetValue(result)
            ? command.ExecuteAsync(result, token)
            : Task.FromResult(new HelpWriter(console).Invoke(result)));

        command.Subcommands.Add(PatternGalleryCollectionCommands.Create(command, console));
        PatternGalleryPatternCommands.Register(command);
    }

    private static async Task PrepareAsync(ToolContext context, OptionSchema optionSchema)
    {
        if (context.GetOption(CliOptions.Selection) is { } selection)
            context.Document[CliDocumentKeys.SELECTION] = selection;

        if (context.GetOption(CliOptions.TimeCode) is { } timeCode)
            context.Document[CliDocumentKeys.TIME_CODE] = timeCode;

        await context.PrepareTargetsAsync(optionSchema);

        if (context.GetOption(CliOptions.Collection) is { } collection)
            context.Document[nameof(PatternGalleryEngineOptions.FileHandler)]![nameof(PatternGalleryCollectionMetadata.CollectionFolderName)] = collection;

        if (context.GetOption(CliOptions.Library) is { } library)
            context.Document[CliDocumentKeys.LIBRARY] = Path.GetFullPath(library);

        if (context.HasOption(CliOptions.Pattern))
            context.Document[CliDocumentKeys.PATTERNS] = new JArray(context.OptionValues(CliOptions.Pattern));

        if (context.HasOption(CliOptions.AllPatterns))
            context.Document[CliDocumentKeys.ALL_PATTERNS] = CliOptions.AllPatterns.GetValue(context.ParseResult);
    }
}
