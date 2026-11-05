using System.Collections;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.Reflection;
using System.Text.Json;
using Mapping_Tools.Application.Localization;
using Mapping_Tools.Application.Tools.HitsoundCopier;
using Mapping_Tools.Application.Tools.HitsoundStudio.Models;
using Mapping_Tools.Application.Tools.MapsetMerger.Models;
using Mapping_Tools.Application.Tools.RhythmGuide;
using Mapping_Tools.Application.Tools.TimingCopier;
using Mapping_Tools.Application.Tools.TumourGenerator.Models;
using Mapping_Tools.Cli.Commands;
using Mapping_Tools.Cli.Localization;
using Mapping_Tools.Core.BeatmapHelper.BeatDivisors;
using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.Graph;
using Mapping_Tools.Core.MathUtil;
using Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObjectGenerators;
using Mapping_Tools.Core.Tools.Sliderator.Models;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace Mapping_Tools.Cli.Configuration;

/// <summary>Maps CLI option names and aliases to serialized Application settings properties.</summary>
/// <remarks>
/// Only properties present in serialized defaults are exposed. Nested names use dotted kebab case, unique short
/// aliases are added, and collection entries retain defaults for help and supported layer merging.
/// </remarks>
internal sealed class OptionSchema
{
    private const string setting_suffix = "Setting";
    private const string generator_kind_property = "$kind";

    private static readonly string normalizedSettingSuffix = setting_suffix.ToLowerInvariant();

    private readonly string helpPrefix;

    /// <summary>One settings property binding; several option-name aliases may refer to the same entry.</summary>
    /// <param name="Name">Canonical CLI name without leading dashes.</param>
    /// <param name="Path">Dotted property path in the serialized settings document.</param>
    /// <param name="Type">CLR property type used to normalize TOML values.</param>
    /// <param name="HelpKey">Resource key for this tool's option description.</param>
    /// <param name="Default">Serialized built-in value used by option help and supported partial-value defaults.</param>
    internal sealed record Entry(string Name, string Path, Type Type, string HelpKey, JToken? Default);

    /// <summary>Canonical names and aliases mapped to their settings entries; values may therefore repeat.</summary>
    internal Dictionary<string, Entry> Options { get; } = new(StringComparer.Ordinal);

    /// <summary>Builds model bindings and aliases from one tool's serialized defaults.</summary>
    /// <param name="tool">Stable tool ID used to prefix localized help keys.</param>
    /// <param name="type">Application settings type to inspect.</param>
    /// <param name="defaults">Serialized default properties that may be exposed on the command line.</param>
    internal OptionSchema(string tool, Type type, JObject defaults)
    {
        helpPrefix = string.Concat(tool.Split('-').Select(word => char.ToUpperInvariant(word[0]) + word[1..]));

        Add(type, defaults, "", []);

        foreach (var group in Options.Values.Distinct().GroupBy(entry => entry.Name.Split('.').Last()).ToArray())
            if (group.Count() == 1)
                Options.TryAdd(group.Key, group.Single());

        Alias(CliOptions.Selection.Name, Kebab(nameof(TumourGeneratorServiceOptions.ImportModeSetting)));
        Alias(CliOptions.GRAPH_ALIAS, Kebab(nameof(SlideratorEngineOptions.GraphState)));
        Alias(CliOptions.LAYER_ALIAS, Kebab(nameof(TumourGeneratorServiceOptions.TumourLayers)));
        Alias(CliOptions.LAYER_ALIAS, Kebab(nameof(HitsoundStudioServiceOptions.HitsoundLayers)));
        Alias(CliOptions.MAPSET_ALIAS, Kebab(nameof(MapsetMergerServiceOptions.Mapsets)));
        Alias(CliOptions.From.Name, Kebab(nameof(HitsoundCopierServiceOptions.PathFrom)));
        Alias(CliOptions.From.Name, Kebab(nameof(TimingCopierServiceOptions.ImportPath)));
        Alias(CliOptions.TO_ALIAS, Kebab(nameof(HitsoundCopierServiceOptions.PathTo)));
        Alias(CliOptions.TO_ALIAS, Kebab(nameof(TimingCopierServiceOptions.ExportPath)));
        Alias(CliOptions.Output.Name, Kebab(nameof(HitsoundStudioServiceOptions.ExportFolder)));
        Alias(CliOptions.Output.Name, Kebab(nameof(MapsetMergerServiceOptions.ExportPath)));
        Alias(CliOptions.Output.Name,
            Kebab(nameof(RhythmGuideServiceOptions.GuideGeneratorArgs)) + "."
            + Kebab(nameof(RhythmGuideServiceOptions.RhythmGuideRunOptions.ExportPath)));

        foreach (string name in Options.Where(pair =>
                     pair.Value.Path == nameof(HitsoundStudioServiceOptions.DeleteAllInExportFirst))
                 .Select(pair => pair.Key).ToArray())
            Options.Remove(name);
    }

    private void Alias(string alias, string name)
    {
        if (Options.TryGetValue(name, out var entry))
            Options.TryAdd(alias, entry);
    }

    private void Add(Type type, JObject defaults, string prefix, HashSet<Type> ancestors)
    {
        if (!ancestors.Add(type))
            return;

        foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!defaults.TryGetValue(property.Name, out var value))
                continue;

            if (!property.CanWrite && value is not JArray && (value is not JObject || property.PropertyType.IsValueType))
                continue;

            string path = prefix + property.Name;
            string originalName = string.Join('.', path.Split('.').Select(Kebab));
            string name = string.Join(
                '.',
                path.Split('.').Select(segment => Kebab(segment.EndsWith(setting_suffix, StringComparison.Ordinal)
                    ? segment[..^setting_suffix.Length]
                    : segment)));
            var entry = new Entry(
                name,
                path,
                property.PropertyType,
                helpPrefix + "_" + path.Replace('.', '_') + "Help",
                value);
            Options.Add(name, entry);

            if (originalName != name)
                Options.Add(originalName, entry);

            if (value is JObject child && !typeof(IDictionary).IsAssignableFrom(property.PropertyType))
                Add(property.PropertyType, child, path + ".", new HashSet<Type>(ancestors));
        }
    }

    /// <summary>Recognizes model boolean options, including their negated names.</summary>
    /// <param name="name">Option name without leading dashes, optionally prefixed with <c>no-</c>.</param>
    /// <returns>True when the resolved entry is boolean.</returns>
    internal bool IsBoolean(string name)
    {
        if (name.StartsWith(CliOptionSyntax.NEGATED_PREFIX, StringComparison.Ordinal))
            name = name[CliOptionSyntax.NEGATED_PREFIX.Length..];

        return Options.TryGetValue(name, out var entry) && entry.Type == typeof(bool);
    }

    /// <summary>Collects entered options in command-line order from System.CommandLine's validated parse.</summary>
    /// <param name="parsed">The successful parse for a tool invocation.</param>
    /// <returns>Option spellings and values in the order the user entered them.</returns>
    internal static List<KeyValuePair<string, string?>> ReadOptions(ParseResult parsed)
    {
        List<KeyValuePair<string, string?>> values = [];
        List<OptionResult> options = [];

        for (var command = parsed.CommandResult; command is not null; command = command.Parent as CommandResult)
            options.AddRange(command.Children.OfType<OptionResult>());

        var tokens = parsed.Tokens;

        for (int index = 0; index < tokens.Count; index++)
        {
            var token = tokens[index];

            if (token.Type == TokenType.Option)
            {
                string name = token.Value.StartsWith(CliOptionSyntax.LONG_OPTION_PREFIX, StringComparison.Ordinal)
                    ? token.Value[CliOptionSyntax.LONG_OPTION_PREFIX.Length..]
                    : token.Value.TrimStart('-');
                var option = options.First(option => option.Option.Name == token.Value || option.Option.Aliases.Contains(token.Value));
                string? value = null;

                if (index + 1 < tokens.Count && tokens[index + 1].Type == TokenType.Argument
                    && (option.Option.ValueType != typeof(bool) || bool.TryParse(tokens[index + 1].Value, out _)))
                    value = tokens[++index].Value;

                values.Add(new KeyValuePair<string, string?>(name, value));
            }
        }

        return values;
    }

    /// <summary>Applies ordered explicit options to an already loaded or imported settings document.</summary>
    /// <param name="project">Mutable destination document.</param>
    /// <param name="options">Occurrences in command-line order, retaining aliases and append prefixes.</param>
    /// <param name="common">Host-owned names handled outside the model and therefore skipped.</param>
    /// <remarks>
    /// The first list assignment replaces loaded values; subsequent occurrences extend it, while append options retain
    /// existing values. Scalar aliases share duplicate detection. Enum, nested-member, and TOML errors become argument
    /// failures.
    /// </remarks>
    /// <exception cref="ArgumentException">An option, member, enum, repetition, or value shape is invalid.</exception>
    internal void Apply(JObject project, IEnumerable<KeyValuePair<string, string?>> options, ISet<string> common)
    {
        HashSet<string> replaced = [];

        foreach (var (originalName, text) in options)
        {
            if (common.Contains(originalName))
                continue;

            string name = originalName;
            bool append = name.StartsWith(CliOptionSyntax.APPEND_PREFIX, StringComparison.Ordinal);

            if (append)
                name = name[CliOptionSyntax.APPEND_PREFIX.Length..];

            bool negate = name.StartsWith(CliOptionSyntax.NEGATED_PREFIX, StringComparison.Ordinal) && IsBoolean(name);

            if (negate)
                name = name[CliOptionSyntax.NEGATED_PREFIX.Length..];

            if (!Options.TryGetValue(name, out var entry))
                throw new ArgumentException(ApplicationText.Format(CliStrings.UnknownOption, originalName));

            Type? elementType = ElementType(entry.Type);
            string raw = text ?? (negate ? CliOptionSyntax.FALSE_VALUE : CliOptionSyntax.TRUE_VALUE);

            if (entry.Type == typeof(HitObjectSelectionMode) && raw == CliValues.ALL_SELECTION_ALIAS)
                raw = nameof(HitObjectSelectionMode.Everything);

            JToken token;

            if (entry.Type == typeof(string))
                token = new JValue(raw);
            else if (entry.Type.IsEnum)
                token = new JValue(Enum.GetNames(entry.Type)
                    .FirstOrDefault(value => NormalizeName(value) == NormalizeName(raw))
                    ?? throw new ArgumentException(ApplicationText.Format(
                        CliStrings.InvalidEnum, originalName, string.Join(", ", Enum.GetNames(entry.Type).Select(Kebab)))));
            else
                token = Normalize(
                    TomlValue.Parse(raw),
                    elementType is not null && !raw.TrimStart().StartsWith('[') ? elementType : entry.Type);

            if (entry.Path == nameof(TumourGeneratorServiceOptions.TumourLayers)
                && entry.Default is JArray { Count: > 0 } layerDefaults)
            {
                JToken withDefaults(JToken layer)
                {
                    if (layer is not JObject values)
                        throw new ArgumentException(ApplicationText.Format(CliStrings.InvalidObject, originalName));

                    var configured = (JObject)layerDefaults[0].DeepClone();
                    configured.Merge(values, new JsonMergeSettings { MergeArrayHandling = MergeArrayHandling.Replace });

                    return configured;
                }

                token = token is JArray layers ? new JArray(layers.Select(withDefaults)) : withDefaults(token);
            }

            string[] segments = entry.Path.Split('.');
            JObject parent = project;

            foreach (string segment in segments[..^1])
            {
                parent[segment] ??= new JObject();
                parent = parent[segment] as JObject ?? throw new ArgumentException(ApplicationText.Format(CliStrings.InvalidObject, originalName));
            }

            string key = segments[^1];

            if (elementType is not null)
            {
                if (!append && replaced.Add(entry.Path))
                    parent[key] = new JArray();

                var array = parent[key] as JArray ?? new JArray();

                if (token is JArray values)
                    foreach (var value in values)
                        array.Add(value.DeepClone());
                else
                    array.Add(token);

                parent[key] = array;
            }
            else
            {
                if (append)
                    throw new ArgumentException(ApplicationText.Format(CliStrings.NotCollection, originalName));

                if (!replaced.Add(entry.Path))
                    throw new ArgumentException(ApplicationText.Format(CliStrings.RepeatedOption, originalName));

                parent[key] = token;
            }
        }
    }

    /// <summary>Finds the element type of arrays and supported IList collections.</summary>
    /// <param name="type">The model property type.</param>
    /// <returns>The collection element type, or null for scalar and unsupported collection types.</returns>
    internal static Type? ElementType(Type type) => type.IsArray
        ? type.GetElementType()
        : type.GetInterfaces()
            .Append(type)
            .FirstOrDefault(candidate => candidate.IsGenericType && candidate.GetGenericTypeDefinition() == typeof(IList<>))
            ?.GetGenericArguments()[0];

    /// <summary>Converts CLI member and enum spellings into the names expected by the typed project serializer.</summary>
    /// <param name="value">Parsed TOML represented as JSON-compatible tokens.</param>
    /// <param name="type">Expected CLR value, collection, or nested object type.</param>
    /// <returns>A normalized token suitable for typed deserialization.</returns>
    /// <remarks>
    /// Dictionary keys remain literal. Beat divisors and generator settings resolve their supported concrete types,
    /// including generator <c>$kind</c> identifiers. Unknown members and ignored model properties are rejected.
    /// </remarks>
    internal static JToken Normalize(JToken value, Type type)
    {
        if (type == typeof(GraphState) && value.Type is JTokenType.Integer or JTokenType.Float)
        {
            double constant = value.Value<double>();
            var graph = GraphState.CreateDefault();

            foreach (var anchor in graph.Anchors)
                anchor.Pos = new Vector2(anchor.Pos.X, constant);

            graph.MinY = Math.Min(0, constant);
            graph.MaxY = Math.Max(1, constant);

            return JObject.FromObject(graph);
        }

        if (value is JArray array && ElementType(type) is { } elementType)
            return new JArray(array.Select(item => Normalize(item, elementType)));

        if (type.IsEnum && value.Type == JTokenType.String)
            return new JValue(Enum.GetNames(type)
                .FirstOrDefault(name => NormalizeName(name) == NormalizeName(value.Value<string>()!))
                ?? throw new ArgumentException(ApplicationText.Format(
                    CliStrings.InvalidEnum, type.Name, string.Join(", ", Enum.GetNames(type).Select(Kebab)))));

        if (value is not JObject source)
            return value;

        if (type == typeof(IBeatDivisor))
            type = source.Properties().Any(property =>
                NormalizeName(property.Name) == NormalizeName(nameof(IrrationalBeatDivisor.Value)))
                ? typeof(IrrationalBeatDivisor)
                : typeof(RationalBeatDivisor);

        if (typeof(GeneratorSettings).IsAssignableFrom(type)
            && source[generator_kind_property]?.Value<string>() is { } kind)
            type = typeof(GeneratorSettings).Assembly.GetTypes()
                .Where(candidate => typeof(GeneratorSettings).IsAssignableFrom(candidate)
                                    && !candidate.IsAbstract
                                    && candidate.GetConstructor(Type.EmptyTypes) is not null)
                .FirstOrDefault(candidate => ((GeneratorSettings)Activator.CreateInstance(candidate)!).Id == kind)
                ?? throw new ArgumentException(ApplicationText.Format(CliStrings.UnknownMember, kind, nameof(GeneratorSettings)));

        JObject result = new();
        var dictionary = type.GetInterfaces()
            .Append(type)
            .FirstOrDefault(candidate => candidate.IsGenericType
                                         && candidate.GetGenericTypeDefinition() == typeof(IDictionary<,>));

        foreach (var property in source.Properties())
        {
            if (dictionary is not null)
            {
                result[property.Name] = Normalize(property.Value, dictionary.GetGenericArguments()[1]);
                continue;
            }

            if (property.Name == generator_kind_property)
            {
                result[property.Name] = property.Value.DeepClone();
                continue;
            }

            var target = type.GetProperties()
                .FirstOrDefault(candidate => candidate.GetIndexParameters().Length == 0
                                             && NormalizeName(candidate.Name) == NormalizeName(property.Name));
            var field = type.GetFields(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(candidate => NormalizeName(candidate.Name) == NormalizeName(property.Name));

            if (target is not null && target.GetCustomAttribute<JsonIgnoreAttribute>() is null)
                result[target.Name] = Normalize(property.Value, target.PropertyType);
            else if (field is not null)
                result[field.Name] = Normalize(property.Value, field.FieldType);
            else
                throw new ArgumentException(ApplicationText.Format(CliStrings.UnknownMember, property.Name, type.Name));
        }

        return result;
    }

    /// <summary>Produces the stable kebab-case spelling used by CLI option names and enum choices.</summary>
    /// <param name="value">CLR-style identifier to convert.</param>
    /// <returns>Its lowercase kebab-case spelling.</returns>
    internal static string Kebab(string value) => JsonNamingPolicy.KebabCaseLower.ConvertName(value);

    private static string NormalizeName(string value)
    {
        string normalized = value.Replace("-", "").Replace("_", "").ToLowerInvariant();

        return normalized.EndsWith(normalizedSettingSuffix, StringComparison.Ordinal)
            ? normalized[..^normalizedSettingSuffix.Length]
            : normalized;
    }
}
