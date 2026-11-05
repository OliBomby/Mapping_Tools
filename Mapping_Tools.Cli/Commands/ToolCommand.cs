using System.CommandLine;
using Mapping_Tools.Application.Projects.Models;
using Mapping_Tools.Application.Tools;
using Mapping_Tools.Cli.Configuration;
using Mapping_Tools.Infrastructure.Projects;
using Newtonsoft.Json.Linq;
using Spectre.Console;

namespace Mapping_Tools.Cli.Commands;

/// <summary>A System.CommandLine command bound to an Application settings model and the shared project execution lifecycle.</summary>
/// <remarks>
/// Tool factories define their options, imports, preparation, and handler locally, then register the returned
/// command directly on the root.
/// </remarks>
internal sealed class ToolCommand : Command
{
    private static readonly VersionedProjectJsonSerializer serializer = new();

    private readonly ToolExecution execution;

    /// <summary>The Application model reflected for generated option names and values.</summary>
    internal Type OptionsType { get; }

    /// <summary>The model bindings used to construct help and command-line options.</summary>
    internal OptionSchema Schema { get; }

    /// <summary>Creates fresh built-in settings before loading a project or applying overrides.</summary>
    internal required Func<object> Create { get; init; }

    /// <summary>Deserializes a versioned project into the command's Application settings type.</summary>
    internal required Func<string, object> Read { get; init; }

    /// <summary>Serializes Application settings with this tool's project schema and converters.</summary>
    internal required Func<object, string> Write { get; init; }

    /// <summary>Executes prepared settings after validation and any requested project save; skipped by <c>--no-run</c>.</summary>
    internal required Func<ToolContext, object, Task<int>> Run { get; init; }

    /// <summary>
    /// Optionally loads external settings into <see cref="ToolContext.Document"/> after the project is loaded, before
    /// explicit options override them.
    /// </summary>
    internal Func<ToolContext, Task>? Import { get; init; }

    /// <summary>Optionally finalizes the merged document before typed settings are read, selection is validated, or the project is saved.</summary>
    /// <remarks>
    /// Runs even with <c>--no-run</c>. Supplying this callback replaces default target preparation; commands should
    /// call <see cref="ToolContext.PrepareTargetsAsync"/> when they use file targets.
    /// </remarks>
    internal Func<ToolContext, OptionSchema, Task>? Prepare { get; init; }

    /// <summary>Host-owned option names and help text, excluded from direct binding to the Application model.</summary>
    internal required IReadOnlyDictionary<string, string> CliOptionDescriptions { get; init; }

    private ToolCommand(string name, string description, Type optionsType, JObject defaults, ToolExecution execution)
        : base(name, description)
    {
        OptionsType = optionsType;
        Schema = new OptionSchema(name, optionsType, defaults);
        this.execution = execution;
    }

    /// <summary>Builds a ready-to-register command with generated model options and shared invocation handling.</summary>
    /// <typeparam name="T">Application settings type serialized by the tool's project schema.</typeparam>
    /// <param name="console">Renders help and supplies the output writer for operation results.</param>
    /// <param name="error">Receives diagnostics; its lifetime belongs to the caller.</param>
    /// <param name="input">Supplies standard input for explicitly interactive operations; its lifetime belongs to the caller.</param>
    /// <param name="definition">Supplies the stable tool ID, project-schema identity, and default description.</param>
    /// <param name="create">
    /// Creates a fresh settings model; also called while constructing option help, so it must not perform tool
    /// operations.
    /// </param>
    /// <param name="run">Executes the prepared model and returns its exit code.</param>
    /// <param name="import">Optional import step, after project loading and before explicit option binding.</param>
    /// <param name="options">Additional CLI-only option names and help text handled outside the settings model.</param>
    /// <param name="description">Optional CLI description replacing the Application tool description.</param>
    /// <param name="configure">Optional command/subcommand registration. Replaces the default beatmap argument and action registration.</param>
    /// <param name="prepare">Optional post-binding preparation, including command-specific target fields and project metadata.</param>
    /// <returns>The configured command to add directly to the root command.</returns>
    internal static ToolCommand For<T>(
        IAnsiConsole console,
        TextWriter error,
        TextReader input,
        ToolDefinition definition,
        Func<T> create,
        Func<ToolContext, T, Task<int>> run,
        Func<ToolContext, Task>? import = null,
        IReadOnlyDictionary<string, string>? options = null,
        string? description = null,
        Action<ToolCommand, IAnsiConsole>? configure = null,
        Func<ToolContext, OptionSchema, Task>? prepare = null) where T : class
    {
        var schema = ToolConfigSchema.ForTool(definition.Id);
        var commandOptions = new Dictionary<string, string>(CommandOptions.Common, StringComparer.Ordinal);

        if (options is not null)
            foreach (var option in options)
                commandOptions.Add(option.Key, option.Value);

        var defaults = JObject.Parse(serializer.Serialize(schema, create()));
        var execution = new ToolExecution(console.Profile.Out.Writer, error, input);
        ToolCommand command = new(definition.Id, description ?? definition.Description, typeof(T), defaults, execution)
        {
            CliOptionDescriptions = commandOptions,
            Create = () => create(),
            Read = json => serializer.Deserialize<T>(schema, json),
            Write = project => serializer.Serialize(schema, (T)project),
            Run = (context, project) => run(context, (T)project),
            Import = import,
            Prepare = prepare,
        };

        if (configure is not null)
        {
            CommandOptions.Add(command, CommandOptions.Common.Where(pair =>
                pair.Key != CliOptions.Config.Name
                && pair.Key != CliOptions.NoConfig.Name), recursive: true);
            configure(command, console);
        }
        else
        {
            CommandOptions.Add(command, commandOptions.Where(pair =>
                pair.Key != CliOptions.Config.Name
                && pair.Key != CliOptions.NoConfig.Name));
            CommandOptions.AddToolOptions(command, command.Schema, commandOptions.Keys.ToHashSet(StringComparer.Ordinal));
            CommandOptions.AddTargets(command);
            command.SetAction((result, token) => command.ExecuteAsync(result, token));
        }

        return command;
    }

    /// <summary>Runs this command's shared lifecycle for the selected command or one of its subcommands.</summary>
    /// <param name="result">The successful library parse, preserving ordered option occurrences.</param>
    /// <param name="token">Cancels imports, preparation, saving, and the tool operation.</param>
    /// <returns>The operation's exit code, or zero for a successful project-only invocation.</returns>
    /// <remarks>Exceptions propagate to the runner for consistent diagnostics and exit-code conversion.</remarks>
    internal Task<int> ExecuteAsync(ParseResult result, CancellationToken token) =>
        execution.RunAsync(this, result, token);

    /// <summary>Runs this command's shared lifecycle before invoking the selected operation handler.</summary>
    /// <typeparam name="T">The Application settings model used by this tool.</typeparam>
    /// <param name="result">The successful library parse, preserving ordered option occurrences.</param>
    /// <param name="token">Cancels imports, preparation, saving, and the tool operation.</param>
    /// <param name="run">Operation-specific handler registered on a native subcommand.</param>
    /// <returns>The operation's exit code, or zero for a successful project-only invocation.</returns>
    /// <remarks>Exceptions propagate to the runner for consistent diagnostics and exit-code conversion.</remarks>
    internal Task<int> ExecuteAsync<T>(
        ParseResult result,
        CancellationToken token,
        Func<ToolContext, T, Task<int>> run) where T : class =>
        execution.RunAsync(this, result, token, (context, project) => run(context, (T)project));
}
