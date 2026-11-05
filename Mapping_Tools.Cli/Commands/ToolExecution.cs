using System.CommandLine;
using Mapping_Tools.Application.Localization;
using Mapping_Tools.Cli.Configuration;
using Mapping_Tools.Cli.Localization;
using Mapping_Tools.Core.BeatmapHelper.Enums;
using Newtonsoft.Json.Linq;

namespace Mapping_Tools.Cli.Commands;

/// <summary>Coordinates shared configuration, project loading, option binding, saving, and one tool operation.</summary>
/// <param name="output">Receives project-save and operation messages.</param>
/// <param name="error">Receives tool diagnostics.</param>
/// <param name="input">Supplies input to handlers that explicitly enable interaction.</param>
/// <remarks>
/// Precedence is built-in settings, loaded project, imports, then explicit options. The owning command prepares
/// tool-specific fields before validation and project saving. Project-only invocations still import and prepare
/// settings but never call the operation handler.
/// </remarks>
internal sealed class ToolExecution(TextWriter output, TextWriter error, TextReader input)
{
    /// <summary>Executes the shared lifecycle with a new service scope and mutable document for this invocation.</summary>
    /// <param name="command">The configured command supplying model conversion and lifecycle callbacks.</param>
    /// <param name="parseResult">System.CommandLine's successful parse for the selected command.</param>
    /// <param name="cancellationToken">Cancels file and service operations.</param>
    /// <param name="run">Optional operation handler registered directly on a native subcommand.</param>
    /// <returns>The operation exit code, or zero after successfully saving a project with <c>--no-run</c>.</returns>
    internal async Task<int> RunAsync(
        ToolCommand command,
        ParseResult parseResult,
        CancellationToken cancellationToken,
        Func<ToolContext, object, Task<int>>? run = null)
    {
        JObject defaults = JObject.Parse(command.Write(command.Create()));
        var schema = new OptionSchema(command.Name, command.OptionsType, defaults);
        var special = command.CliOptionDescriptions;

        if (CliOptions.NoRun.GetValue(parseResult) && !CliOptions.SaveProject.WasSpecified(parseResult))
            throw new ArgumentException(CliStrings.NoRunRequiresSave);

        if (CliOptions.MakeBackups.WasSpecified(parseResult)
            && CliOptions.NoBackups.WasSpecified(parseResult))
            throw new ArgumentException(CliStrings.Config_ConflictingBackups);

        bool noConfig = CliOptions.NoConfig.GetValue(parseResult);

        if (noConfig && CliOptions.Config.WasSpecified(parseResult))
            throw new ArgumentException(CliStrings.Config_ConflictingSources);

        JObject configurationDefaults = new();

        if (!noConfig)
        {
            var configuration = new GlobalConfiguration(
                CliOptions.Config.GetValue(parseResult) ?? GlobalConfiguration.DefaultPath);
            configurationDefaults = configuration.Load(required: CliOptions.Config.WasSpecified(parseResult));
        }

        await using ToolContext context = new(parseResult, configurationDefaults, output, error, input, cancellationToken);
        JObject document = defaults;

        if (context.GetOption(CliOptions.Project) is { } projectPath)
        {
            string text = await context.Projects.LoadDocumentAsync(projectPath, cancellationToken);
            var original = JObject.Parse(text);
            document = JObject.Parse(command.Write(command.Read(text)));

            foreach (var property in original.Properties().Where(property =>
                         property.Name.StartsWith(CliDocumentKeys.PREFIX, StringComparison.Ordinal)
                         || property.Name is CliDocumentKeys.IMPORT_MODE_SETTING or CliDocumentKeys.TIME_CODE_SETTING))
                document[property.Name] = property.Value.DeepClone();

            context.LoadedSelection = original[CliDocumentKeys.IMPORT_MODE_SETTING] is not null
                || original[CliDocumentKeys.SELECTION] is not null;
        }

        context.Document = document;

        if (context.GetOption(CliOptions.SaveProject) is { } savePath)
            context.CheckOutput(savePath);

        if (command.Import is not null)
            await command.Import(context);

        schema.Apply(document, context.ExplicitOptions, special.Keys.ToHashSet(StringComparer.Ordinal));
        context.LoadedSelection |= context.ExplicitOptions.Any(option =>
            schema.Options.TryGetValue(option.Key, out var entry)
            && entry.Type == typeof(HitObjectSelectionMode));

        if (command.Prepare is not null)
            await command.Prepare(context, schema);
        else
            await context.PrepareTargetsAsync(schema);

        context.Project = command.Read(document.ToString());

        if (!CliOptions.NoRun.GetValue(parseResult))
        {
            if (schema.Options.ContainsKey(CliOptions.Selection.Name))
                context.Selection();

            foreach (var entry in schema.Options.Values.Distinct().Where(entry => entry.Type == typeof(HitObjectSelectionMode)))
                if (!context.LoadedSelection && !context.ExplicitOptions.Any(option =>
                        schema.Options.TryGetValue(option.Key, out var supplied) && supplied.Path == entry.Path))
                    throw new ArgumentException(CliStrings.SelectionRequired);

            if (parseResult.CommandResult.Command.Arguments.OfType<Argument<string[]>>().Any())
                foreach (string path in await context.PathsAsync(false))
                    if (!context.Files.FileExists(path))
                        throw new FileNotFoundException(ApplicationText.Format(CliStrings.TargetMissing, path), path);
        }

        if (context.GetOption(CliOptions.SaveProject) is { } destination)
        {
            await context.Projects.SaveDocumentAsync(
                destination,
                document.ToString(),
                CliOptions.Overwrite.GetValue(parseResult),
                cancellationToken);
            output.WriteLine(ApplicationText.Format(CliStrings.SavedProject, destination));
        }

        if (CliOptions.NoRun.GetValue(parseResult))
            return 0;

        return await (run ?? command.Run)(context, context.Project);
    }
}
