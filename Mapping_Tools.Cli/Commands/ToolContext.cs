using System.Globalization;
using System.CommandLine;
using Mapping_Tools.Application.Localization;
using Mapping_Tools.Application.Platform;
using Mapping_Tools.Application.Settings.Contracts;
using Mapping_Tools.Application.Tools.GeometryDashboard.Models;
using Mapping_Tools.Application.Workspace.Contracts;
using Mapping_Tools.Cli.Composition;
using Mapping_Tools.Cli.Configuration;
using Mapping_Tools.Cli.Localization;
using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Infrastructure.Files;
using Mapping_Tools.Infrastructure.Projects;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;

namespace Mapping_Tools.Cli.Commands;

/// <summary>Holds one invocation's merged document, target resolution, I/O, and lazily constructed services.</summary>
/// <param name="parseResult">System.CommandLine's validated parse for this invocation.</param>
/// <param name="configurationDefaults">Saved global preferences used when creating Application services.</param>
/// <param name="output">Caller-owned operation output.</param>
/// <param name="error">Caller-owned diagnostic output.</param>
/// <param name="input">Caller-owned input for explicit interactive modes.</param>
/// <param name="cancellationToken">Cancellation shared by imports, settings preparation, and Application services.</param>
/// <remarks>
/// Dispose the context after the operation to release its service provider. Streams remain caller-owned. Set
/// Geometry Dashboard's typed <see cref="Project"/> before first resolving its services, because composition
/// happens only once.
/// </remarks>
internal sealed class ToolContext(
    ParseResult parseResult,
    JObject configurationDefaults,
    TextWriter output,
    TextWriter error,
    TextReader input,
    CancellationToken cancellationToken) : IAsyncDisposable
{
    private const string projects_directory_name = "Projects";
    private const string pattern_gallery_library_directory_name = "Pattern Gallery Projects";

    private ServiceProvider? services;

    private List<string>? targets;

    /// <summary>Temporary export directories whose internal writes are excluded from published-file messages.</summary>
    internal List<string> StagingDirectories { get; } = [];

    /// <summary>The validated command-line parse.</summary>
    internal ParseResult ParseResult { get; } = parseResult;

    /// <summary>Saved global preferences loaded for this invocation.</summary>
    internal JObject ConfigurationDefaults { get; } = configurationDefaults;

    /// <summary>Parsed option occurrences plus values resolved during command preparation.</summary>
    internal List<KeyValuePair<string, string?>> ExplicitOptions { get; } = OptionSchema.ReadOptions(parseResult);

    /// <summary>Source path resolved from <c>--from-current</c>, when requested.</summary>
    internal string? ResolvedSource { get; set; }

    /// <summary>The caller-owned stream for results and completed-file messages.</summary>
    internal TextWriter Output { get; } = output;

    /// <summary>The caller-owned stream for operation diagnostics.</summary>
    internal TextWriter Error { get; } = error;

    /// <summary>The caller-owned input used by explicitly interactive handlers.</summary>
    internal TextReader Input { get; } = input;

    /// <summary>Cancellation propagated to service and file operations.</summary>
    internal CancellationToken CancellationToken { get; } = cancellationToken;

    /// <summary>Shared Infrastructure filesystem operations for CLI-owned file work.</summary>
    internal PhysicalBeatmapsetFileSystem Files { get; } = new();

    /// <summary>Shared project storage, including raw documents with additional CLI metadata.</summary>
    internal FileSystemProjectStore Projects { get; } = new(new VersionedProjectJsonSerializer());

    /// <summary>The mutable settings document, including CLI metadata, merged before typed model deserialization.</summary>
    internal JObject Document { get; set; } = new();

    /// <summary>Typed Application settings read from the prepared document before the operation runs.</summary>
    internal object? Project { get; set; }

    /// <summary>Whether a project or explicit model option supplied selection information, rather than merely an enum default.</summary>
    internal bool LoadedSelection { get; set; }

    /// <summary>Resolves an Application or Infrastructure service, creating the invocation's CLI settings and adapters on first use.</summary>
    /// <typeparam name="T">Registered service contract.</typeparam>
    /// <returns>The service instance from this invocation's provider.</returns>
    internal T Get<T>() where T : notnull
    {
        if (services is null)
        {
            var settings = RuntimeSettings.Create(ParseResult, ConfigurationDefaults);
            services = CliServices.Create(settings, ReportFile, Project as GeometryDashboardServiceOptions);
            services.GetRequiredService<ISettingsPathService>().ApplyDefaults(settings);
        }

        return services.GetRequiredService<T>();
    }

    /// <summary>Reports a completed file write, excluding temporary staged export files.</summary>
    /// <param name="path">The path written by the operation.</param>
    internal void ReportFile(string path)
    {
        if (StagingDirectories.Any(directory => Path.GetFullPath(path).StartsWith(
            Path.GetFullPath(directory) + Path.DirectorySeparatorChar,
            OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal)))
            return;

        Output.WriteLine(ApplicationText.Format(CliStrings.Processed, path));
    }

    /// <summary>Resolves and caches absolute, de-duplicated beatmap targets in their processing order.</summary>
    /// <param name="required">Whether an empty target list is an argument error.</param>
    /// <returns>The cached target list.</returns>
    /// <remarks>
    /// Explicit path options precede positional paths, followed by an explicitly requested current beatmap. Saved CLI
    /// targets are used only when no targets were supplied. Resolve document target fields before calling this method.
    /// </remarks>
    /// <exception cref="ArgumentException">Targets are required but neither explicit nor saved targets are available.</exception>
    internal async Task<IReadOnlyList<string>> PathsAsync(bool required = true)
    {
        if (targets is null)
        {
            targets = OptionValues(CliOptions.Path);
            targets.AddRange(PositionalPaths);

            if (CliOptions.Current.GetValue(ParseResult))
                targets.Add(await Get<ICurrentBeatmapLocator>().FindCurrentBeatmapAsync(CancellationToken));

            if (targets.Count == 0 && Document[CliDocumentKeys.TARGETS] is JArray saved)
                targets.AddRange(saved.Values<string>().OfType<string>());

            targets = targets.Select(Path.GetFullPath)
                .Distinct(OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal)
                .ToList();
        }

        if (required && targets.Count == 0)
            throw new ArgumentException(CliStrings.TargetsRequired);

        return targets;
    }

    /// <summary>Resolves the target for operations that accept exactly one beatmap.</summary>
    /// <returns>The single absolute target path.</returns>
    /// <exception cref="ArgumentException">No target or more than one distinct target was supplied.</exception>
    internal async Task<string> OnePathAsync()
    {
        var paths = await PathsAsync();

        if (paths.Count != 1)
            throw new ArgumentException(CliStrings.OneTarget);

        return paths[0];
    }

    /// <summary>Resolves common targets and optionally binds them to a command-owned settings property.</summary>
    /// <param name="schema">Option bindings used to recognize explicit aliases for the destination property.</param>
    /// <param name="targetProperty">Dotted JSON property path in Application settings, or null to record only CLI targets.</param>
    /// <param name="single">Whether an existing target list must contain exactly one path when binding the property.</param>
    /// <returns>A task completing after target metadata and any bound property are updated.</returns>
    /// <remarks>
    /// Call before targets are cached. Explicit property aliases conflict with common path inputs. Collection
    /// properties receive arrays; other multi-target properties use Desktop's pipe-separated format. An empty list is
    /// allowed here for project-only invocations.
    /// </remarks>
    internal async Task PrepareTargetsAsync(OptionSchema schema, string? targetProperty = null, bool single = false)
    {
        if (targetProperty is not null && ExplicitOptions.Any(option =>
                !CommandOptions.Common.ContainsKey(option.Key)
                && schema.Options.TryGetValue(option.Key, out var entry)
                && entry.Path == targetProperty))
        {
            if (HasOption(CliOptions.Path) || CliOptions.Current.GetValue(ParseResult)
                || PositionalPaths.Length > 0)
                throw new ArgumentException(CliStrings.ConflictingTargets);

            var value = Document.SelectToken(targetProperty);
            string[] explicitPaths = value is JArray array
                ? array.Values<string>().OfType<string>().ToArray()
                : (value?.Value<string>() ?? "").Split('|', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            Document[CliDocumentKeys.TARGETS] = new JArray(explicitPaths.Select(Path.GetFullPath));
        }

        var paths = await PathsAsync(false);

        if (paths.Count == 0)
            return;

        Document[CliDocumentKeys.TARGETS] = new JArray(paths);

        if (targetProperty is not null)
        {
            string[] segments = targetProperty.Split('.');
            var parent = Document;

            foreach (string segment in segments[..^1])
            {
                if (parent[segment] is not JObject child)
                    parent[segment] = child = new JObject();

                parent = child;
            }

            var targetType = schema.Options.Values.First(entry => entry.Path == targetProperty).Type;
            JToken value = single ? new JValue(await OnePathAsync())
                : OptionSchema.ElementType(targetType) is not null ? new JArray(paths) : new JValue(string.Join('|', paths));
            parent[segments[^1]] = value;
        }
    }

    /// <summary>Converts an explicitly requested current source beatmap into a source-path option before imports or model binding.</summary>
    /// <returns>A task completing after source-path resolution, or immediately when <c>--from-current</c> was not requested.</returns>
    /// <exception cref="ArgumentException">Both an explicit source path and <c>--from-current</c> were supplied.</exception>
    internal async Task ResolveCurrentSourceAsync()
    {
        if (!CliOptions.FromCurrent.GetValue(ParseResult))
            return;

        if (HasOption(CliOptions.From))
            throw new ArgumentException(CliStrings.ConflictingSource);

        ResolvedSource = await Get<ICurrentBeatmapLocator>().FindCurrentBeatmapAsync(CancellationToken);
        ExplicitOptions.Add(new KeyValuePair<string, string?>(CliOptions.From.Name, ResolvedSource));
    }

    /// <summary>Replaces imported top-level settings while retaining the destination document's schema and version metadata.</summary>
    /// <param name="imported">Serialized imported settings; properties beginning with a dollar sign are excluded.</param>
    /// <remarks>Values are deep-copied. Arrays replace existing arrays; explicit command-line options are applied afterward.</remarks>
    internal void MergeImportedSettings(JObject imported)
    {
        foreach (var property in imported.Properties().Where(property => !property.Name.StartsWith('$')))
            Document[property.Name] = property.Value.DeepClone();
    }

    /// <summary>Resolves an explicit CLI or saved-project object selection without accepting an implicit model default.</summary>
    /// <returns>The validated selection mode.</returns>
    /// <exception cref="ArgumentException">Selection was omitted or the supplied mode is not supported.</exception>
    internal HitObjectSelectionMode Selection()
    {
        string? value = GetOption(CliOptions.Selection)
            ?? Document[CliDocumentKeys.IMPORT_MODE_SETTING]?.Value<string>()
            ?? Document[CliDocumentKeys.SELECTION]?.Value<string>();

        if (!HasOption(CliOptions.Selection) && !LoadedSelection)
            throw new ArgumentException(CliStrings.SelectionRequired);

        if (value == CliValues.ALL_SELECTION_ALIAS)
            value = nameof(HitObjectSelectionMode.Everything);

        return Enum.TryParse<HitObjectSelectionMode>(value, true, out var result) && Enum.IsDefined(result)
            ? result
            : throw new ArgumentException(ApplicationText.Format(CliStrings.InvalidEnum,
                CliOptions.Selection.Name,
                string.Join(", ", Enum.GetNames<HitObjectSelectionMode>().Select(OptionSchema.Kebab))));
    }

    /// <summary>Explicit time-code text, falling back to the standard or CLI project selection metadata.</summary>
    internal string? TimeCode => GetOption(CliOptions.TimeCode)
        ?? Document[CliDocumentKeys.TIME_CODE_SETTING]?.Value<string>()
        ?? Document[CliDocumentKeys.TIME_CODE]?.Value<string>();

    /// <summary>The explicit or saved Pattern Gallery library path, falling back to Desktop's shared default library.</summary>
    internal string Library => GetOption(CliOptions.Library)
        ?? Document[CliDocumentKeys.LIBRARY]?.Value<string>()
        ?? Path.Combine(Get<IApplicationDirectories>().ApplicationData,
            projects_directory_name,
            pattern_gallery_library_directory_name);

    /// <summary>Writes a human-readable operation result using invariant value formatting.</summary>
    /// <param name="result">
    /// A scalar result or an object whose public primitive, enum, string, date, and duration properties form the
    /// summary.
    /// </param>
    /// <remarks>Complex nested objects and collections are omitted; this is display output rather than a serialization format.</remarks>
    internal void Report(object result)
    {
        if (result is string or int)
        {
            Output.WriteLine(ApplicationText.Format(CliStrings.Done, result));

            return;
        }

        string[] values = result.GetType().GetProperties()
            .Where(property => property.GetIndexParameters().Length == 0)
            .Where(property => property.PropertyType.IsPrimitive
                               || property.PropertyType.IsEnum
                               || property.PropertyType == typeof(string)
                               || property.PropertyType == typeof(TimeSpan)
                               || property.PropertyType == typeof(DateTime))
            .Select(property => OptionSchema.Kebab(property.Name) + "="
                + Convert.ToString(property.GetValue(result), CultureInfo.InvariantCulture))
            .ToArray();

        if (values.Length > 0)
            Output.WriteLine(ApplicationText.Format(CliStrings.Done, string.Join(", ", values)));
    }

    /// <summary>Rejects an existing output file unless the invocation explicitly allows overwriting.</summary>
    /// <param name="path">Proposed export or project-file destination.</param>
    /// <remarks>This is a preflight check. The final writer must also enforce overwrite policy when opening the destination.</remarks>
    /// <exception cref="IOException">The destination already exists and overwrite permission was not supplied.</exception>
    internal void CheckOutput(string path)
    {
        if (Files.FileExists(path) && !CliOptions.Overwrite.GetValue(ParseResult))
            throw new IOException(ApplicationText.Format(CliStrings.OutputExists, path));
    }

    /// <summary>Checks whether an option was explicitly entered.</summary>
    /// <param name="option">Option whose supplied state is checked.</param>
    /// <returns>True when the user supplied the option.</returns>
    internal bool HasOption<T>(CliOption<T> option) => option.WasSpecified(ParseResult);

    /// <summary>Gets a single string option or the source path resolved from <c>--from-current</c>.</summary>
    /// <param name="option">Option whose value is read.</param>
    /// <returns>The parsed value, resolved source path, or null when absent.</returns>
    /// <exception cref="ArgumentException">A non-list option was supplied more than once.</exception>
    internal string? GetOption(CliOption<string> option)
    {
        if (option == CliOptions.From && !option.WasSpecified(ParseResult))
            return ResolvedSource;

        if (option.GetResult(ParseResult) is not { Implicit: false } result)
            return null;

        if (result.IdentifierTokenCount > 1)
            throw new ArgumentException(ApplicationText.Format(CliStrings.RepeatedOption, option.Name));

        return option.GetValue(ParseResult);
    }

    /// <summary>Gets all values for a string-list option in input order.</summary>
    /// <param name="option">List option whose values are read.</param>
    /// <returns>The parsed values, or an empty list when the option was omitted.</returns>
    internal List<string> OptionValues(CliOption<string[]> option) =>
        option.GetValue(ParseResult)?.ToList() ?? [];

    /// <summary>Gets a required string option.</summary>
    /// <param name="option">Required string option.</param>
    /// <returns>The supplied value.</returns>
    /// <exception cref="ArgumentException">The option is absent or repeated.</exception>
    internal string RequireOption(CliOption<string> option) =>
        GetOption(option) ?? throw new ArgumentException(ApplicationText.Format(CliStrings.RequiredOption, option.Name));

    private string[] PositionalPaths
    {
        get
        {
            var argument = ParseResult.CommandResult.Command.Arguments
                .OfType<Argument<string[]>>()
                .SingleOrDefault();

            return argument is null ? [] : ParseResult.GetValue(argument) ?? [];
        }
    }


    /// <inheritdoc />
    /// <remarks>Disposes only lazily created services, including their HTTP clients and adapters; caller-owned streams remain open.</remarks>
    public async ValueTask DisposeAsync()
    {
        if (services is not null)
            await services.DisposeAsync();
    }
}
