using System.Collections;
using System.CommandLine;
using System.Globalization;
using System.Reflection;
using Mapping_Tools.Application.Localization;
using Mapping_Tools.Cli.Configuration;
using Mapping_Tools.Cli.Localization;
using Mapping_Tools.Core.Graph;
using Mapping_Tools.Core.MathUtil;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Mapping_Tools.Cli.Commands;

/// <summary>Constructs shared host switches and reflected model options for System.CommandLine.</summary>
internal static class CommandOptions
{
    /// <summary>Shared host option names and localized descriptions; these are not direct settings-model assignments.</summary>
    internal static readonly Dictionary<string, string> Common = new(StringComparer.Ordinal)
    {
        [CliOptions.Path.Name] = CliStrings.PathHelp,
        [CliOptions.Current.Name] = CliStrings.CurrentHelp,
        [CliOptions.Project.Name] = CliStrings.ProjectOptionHelp,
        [CliOptions.SaveProject.Name] = CliStrings.SaveProjectHelp,
        [CliOptions.NoRun.Name] = CliStrings.NoRunHelp,
        [CliOptions.Overwrite.Name] = CliStrings.OverwriteHelp,
        [CliOptions.NoBackups.Name] = CliStrings.NoBackupsHelp,
        [CliOptions.BackupPath.Name] = CliStrings.BackupPathHelp,
        [CliOptions.OsuPath.Name] = CliStrings.OsuPathHelp,
        [CliOptions.SongsPath.Name] = CliStrings.SongsPathHelp,
        [CliOptions.OsuConfig.Name] = CliStrings.OsuConfigHelp,
        [CliOptions.ReloadEditor.Name] = CliStrings.ReloadEditorHelp,
        [CliOptions.EditorReader.Name] = CliStrings.ReaderHelp,
        [CliOptions.CurrentBeatmapFetching.Name] = CliStrings.LocatorHelp,
        [CliOptions.EditorReloadMode.Name] = CliStrings.ReloadModeHelp,
        [CliOptions.MaxBackups.Name] = CliStrings.MaxBackupsHelp,
        [CliOptions.MakeBackups.Name] = CliStrings.Config_MakeBackupsHelp,
        [CliOptions.AutoDetectLazerExternalEdit.Name] = CliStrings.Config_LazerHelp,
        [CliOptions.Config.Name] = CliStrings.Config_OptionHelp,
        [CliOptions.NoConfig.Name] = CliStrings.Config_NoConfigHelp,
    };

    private static readonly HashSet<string> boolean =
    [
        CliOptions.Current.Name,
        CliOptions.FromCurrent.Name,
        CliOptions.NoRun.Name,
        CliOptions.Overwrite.Name,
        CliOptions.NoBackups.Name,
        CliOptions.ReloadEditor.Name,
        CliOptions.Reload.Name,
        CliOptions.Interactive.Name,
        CliOptions.FailOnAutoFail.Name,
        CliOptions.Merge.Name,
        CliOptions.AllPatterns.Name,
        CliOptions.UseSelectedSlider.Name,
        CliOptions.CleanOutput.Name,
        CliOptions.MakeBackups.Name,
        CliOptions.AutoDetectLazerExternalEdit.Name,
        CliOptions.NoConfig.Name
    ];

    private static readonly HashSet<string> multipleValues =
    [
        CliOptions.Path.Name,
        CliOptions.Import.Name,
        CliOptions.Pattern.Name,
    ];

    /// <summary>Identifies parameterless host flags when creating native option types.</summary>
    /// <param name="name">Host option name without leading dashes.</param>
    /// <returns>Whether the host option is boolean.</returns>
    internal static bool IsBoolean(string name) => boolean.Contains(name);

    /// <summary>Adds the optional positional beatmap list used by ordinary tool commands.</summary>
    /// <param name="command">Command receiving the positional argument.</param>
    internal static void AddTargets(Command command) =>
        command.Arguments.Add(new Argument<string[]>("beatmap paths") { Arity = ArgumentArity.ZeroOrMore });

    /// <summary>Creates host-owned native options from localized descriptions.</summary>
    /// <param name="command">Command receiving the options.</param>
    /// <param name="options">Option names and their help text.</param>
    /// <param name="recursive">Whether descendants inherit these options, as with root configuration switches.</param>
    internal static void Add(Command command, IEnumerable<KeyValuePair<string, string>> options, bool recursive = false)
    {
        foreach (var (name, description) in options)
        {
            Option option = IsBoolean(name)
                ? new Option<bool>(CliOptionSyntax.LONG_OPTION_PREFIX + name)
                : multipleValues.Contains(name)
                    ? new Option<string[]>(CliOptionSyntax.LONG_OPTION_PREFIX + name) { Arity = ArgumentArity.OneOrMore }
                    : new Option<string>(CliOptionSyntax.LONG_OPTION_PREFIX + name);
            option.Description = description;
            option.Recursive = recursive;
            option.AllowMultipleArgumentsPerToken = false;
            command.Options.Add(option);
        }
    }

    /// <summary>Creates model options, aliases, boolean negations, list append switches, and default-value help.</summary>
    /// <param name="command">Command receiving the options.</param>
    /// <param name="schema">Reflected model entries and aliases.</param>
    /// <param name="special">Host-owned names excluded from direct model option registration.</param>
    internal static void AddToolOptions(Command command, OptionSchema schema, ISet<string> special)
    {
        foreach (var entry in schema.Options.Values.Distinct())
        {
            string[] names = schema.Options.Where(pair => pair.Value == entry && !special.Contains(pair.Key))
                .Select(pair => CliOptionSyntax.LONG_OPTION_PREFIX + pair.Key).ToArray();

            if (names.Length == 0)
                continue;

            string typeHelp = ApplicationText.Format(CliStrings.ValueType, DescribeType(entry.Type));
            string? localized = CliStrings.ResourceManager.GetString(entry.HelpKey, CliStrings.Culture);
            string description = localized ?? typeHelp;

            if (entry.Default is JObject or JArray)
            {
                if (localized is not null)
                    description += " " + typeHelp;

                if (entry.Default is JArray items)
                    description += " " + ApplicationText.Format(CliStrings.DefaultListSize, items.Count);

                string fields = DescribeFields(entry);

                if (fields.Length > 0)
                    description += Environment.NewLine + ApplicationText.Format(CliStrings.ValueFields, fields);
            }

            bool collection = OptionSchema.ElementType(entry.Type) is not null;
            Option option = entry.Type == typeof(bool)
                ? new Option<bool>(names[0], names[1..])
                : collection
                    ? new Option<string[]>(names[0], names[1..]) { Arity = ArgumentArity.OneOrMore }
                    : new Option<string>(names[0], names[1..]);

            if (entry.Type.IsEnum)
                description += " " + ApplicationText.Format(CliStrings.Choices, string.Join(", ", Enum.GetNames(entry.Type).Select(OptionSchema.Kebab)));

            option.Description = entry.Default is JObject or JArray
                ? description
                : ApplicationText.Format(CliStrings.DefaultValue, description, DescribeDefault(entry.Default, entry.Type));
            option.AllowMultipleArgumentsPerToken = false;
            command.Options.Add(option);

            if (collection)
                command.Options.Add(new Option<string[]>(
                    CliOptionSyntax.LONG_OPTION_PREFIX + CliOptionSyntax.APPEND_PREFIX + names[0][CliOptionSyntax.LONG_OPTION_PREFIX.Length..],
                    names[1..].Select(name => CliOptionSyntax.LONG_OPTION_PREFIX
                        + CliOptionSyntax.APPEND_PREFIX + name[CliOptionSyntax.LONG_OPTION_PREFIX.Length..]).ToArray())
                {
                    Arity = ArgumentArity.OneOrMore,
                    AllowMultipleArgumentsPerToken = false,
                    Description = CliStrings.ListHelp,
                });

            if (entry.Type == typeof(bool))
                command.Options.Add(new Option<bool>(
                    CliOptionSyntax.LONG_OPTION_PREFIX + CliOptionSyntax.NEGATED_PREFIX
                    + names[0][CliOptionSyntax.LONG_OPTION_PREFIX.Length..],
                    names[1..].Select(name => CliOptionSyntax.LONG_OPTION_PREFIX
                        + CliOptionSyntax.NEGATED_PREFIX + name[CliOptionSyntax.LONG_OPTION_PREFIX.Length..]).ToArray())
                {
                    Description = ApplicationText.Format(CliStrings.NegatedOptionHelp, names[0]),
                });
        }
    }

    private static string DescribeType(Type type)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;

        if (type.IsEnum)
            return string.Join(" | ", Enum.GetNames(type).Select(OptionSchema.Kebab));

        if (type == typeof(GraphState))
            return CliStrings.GraphValueType;

        if (OptionSchema.ElementType(type) is { } elementType)
            return ApplicationText.Format(CliStrings.ListValueType, DescribeType(elementType));

        if (type == typeof(string) || type == typeof(DateTime) || type == typeof(DateTimeOffset) || type == typeof(TimeSpan))
            return CliStrings.TextValueType;

        if (type == typeof(bool))
            return CliOptionSyntax.TRUE_VALUE + " | " + CliOptionSyntax.FALSE_VALUE;

        return type.IsPrimitive || type == typeof(decimal) ? CliStrings.NumberValueType : CliStrings.TableValueType;
    }

    private static string DescribeFields(OptionSchema.Entry entry)
    {
        Type type = OptionSchema.ElementType(entry.Type) ?? entry.Type;

        if (type == typeof(string) || type.IsPrimitive || type.IsEnum || typeof(IDictionary).IsAssignableFrom(type))
            return "";

        var defaults = entry.Default as JObject ?? (entry.Default as JArray)?.FirstOrDefault() as JObject;

        if (defaults is null && !type.IsAbstract && type.GetConstructor(Type.EmptyTypes) is not null)
            defaults = JObject.FromObject(Activator.CreateInstance(type)!);

        string helpPrefix = entry.HelpKey[..^"Help".Length];

        return string.Join(Environment.NewLine, ValueMembers(type)
            .Where(member => defaults is null || defaults.ContainsKey(member.Name))
            .Select(member =>
            {
                string name = OptionSchema.Kebab(member.Name);
                string? explanation = CliStrings.ResourceManager.GetString(helpPrefix + "_" + member.Name + "Help", CliStrings.Culture);
                string description = explanation is null
                    ? ApplicationText.Format(CliStrings.ValueField, name, DescribeType(member.Type))
                    : ApplicationText.Format(CliStrings.ValueFieldDescription, name, DescribeType(member.Type), explanation);

                return defaults?.TryGetValue(member.Name, out var value) == true
                    ? ApplicationText.Format(CliStrings.DefaultValue, description, DescribeDefault(value, member.Type))
                    : description;
            }));
    }

    private static IEnumerable<(string Name, Type Type)> ValueMembers(Type type)
    {
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(property => property.GetIndexParameters().Length == 0
                && (property.CanWrite || OptionSchema.ElementType(property.PropertyType) is not null)
                && property.GetCustomAttribute<JsonIgnoreAttribute>() is null)
            .Select(property => (property.Name, Type: property.PropertyType));
        var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance)
            .Where(field => field.GetCustomAttribute<JsonIgnoreAttribute>() is null)
            .Select(field => (field.Name, Type: field.FieldType));

        return properties.Concat(fields);
    }

    private static string DescribeDefault(JToken? value, Type type) => value is null || value.Type == JTokenType.Null
        ? CliStrings.UnsetDefault
        : TomlValue.Format(DefaultValueForHelp(value, type));

    private static JToken DefaultValueForHelp(JToken value, Type type)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;

        if (value.Type == JTokenType.Date && ((JValue)value).Value is IFormattable date)
            return new JValue(date.ToString("O", CultureInfo.InvariantCulture));

        if (value.Type == JTokenType.TimeSpan)
            return new JValue(value.Value<TimeSpan>().ToString("c", CultureInfo.InvariantCulture));

        if (type.IsEnum && value.Type == JTokenType.Integer)
        {
            string? name = Enum.GetName(type, value.ToObject(type)!);

            return name is null ? value.DeepClone() : new JValue(OptionSchema.Kebab(name));
        }

        if (type == typeof(GraphState) && value is JObject graph && graph[nameof(GraphState.Anchors)] is JArray { Count: > 0 } anchors)
        {
            var heights = anchors.Select(anchor => anchor[nameof(GraphAnchor.Pos)]?[nameof(Vector2.Y)]).ToArray();

            if (heights.All(height => height is not null && JToken.DeepEquals(height, heights[0])))
                return heights[0]!.DeepClone();
        }

        if (value is JArray items)
            return new JArray(items.Select(item => DefaultValueForHelp(item, OptionSchema.ElementType(type) ?? typeof(object))));

        if (value is not JObject table)
            return value.DeepClone();

        var members = ValueMembers(type).ToDictionary(member => member.Name, member => member.Type);
        bool dictionary = typeof(IDictionary).IsAssignableFrom(type);
        Type dictionaryValue = dictionary && type.IsGenericType ? type.GetGenericArguments()[1] : typeof(object);
        JObject result = new();

        foreach (var property in table.Properties())
        {
            if (!dictionary && !property.Name.StartsWith('$') && !members.ContainsKey(property.Name))
                continue;

            string name = dictionary || property.Name.StartsWith('$') ? property.Name : OptionSchema.Kebab(property.Name);
            result[name] = DefaultValueForHelp(property.Value, members.GetValueOrDefault(property.Name, dictionaryValue));
        }

        return result;
    }
}
