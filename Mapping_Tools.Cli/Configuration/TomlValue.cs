using System.Collections;
using Mapping_Tools.Application.Localization;
using Mapping_Tools.Cli.Localization;
using Newtonsoft.Json.Linq;
using Tomlyn.Model;
using Tomlyn;

namespace Mapping_Tools.Cli.Configuration;

/// <summary>Bridges TOML values and the JSON-compatible tokens used by Application project serialization.</summary>
internal static class TomlValue
{
    private const string wrapped_value_key = "value";
    internal const string ASSIGNMENT_SEPARATOR = " = ";

    /// <summary>Formats one settings value as TOML, using inline tables for objects.</summary>
    /// <param name="value">Scalar, array, or object to display.</param>
    /// <returns>TOML value text without a containing assignment.</returns>
    /// <remarks>TOML has no null value; null settings are displayed as empty strings.</remarks>
    internal static string Format(JToken value)
    {
        string document = TomlSerializer.Serialize(new TomlTable { [wrapped_value_key] = ToToml(value) });

        return document[(document.IndexOf('=') + 1)..].Trim();
    }

    private static object ToToml(JToken value)
    {
        if (value is JObject json)
        {
            TomlTable table = new(inline: true);

            foreach (var property in json.Properties())
                table[property.Name] = ToToml(property.Value);

            return table;
        }

        if (value is JArray array)
        {
            TomlArray items = new();

            foreach (var item in array)
                items.Add(ToToml(item));

            return items;
        }

        return (value as JValue)?.Value ?? "";
    }

    /// <summary>Parses one TOML literal, array, or inline table supplied as an option value.</summary>
    /// <param name="value">Value text without a containing assignment.</param>
    /// <returns>The JSON-compatible representation.</returns>
    /// <exception cref="ArgumentException">The text is not a valid TOML value.</exception>
    internal static JToken Parse(string value)
    {
        try
        {
            return Convert(TomlSerializer.Deserialize<TomlTable>(
                wrapped_value_key + ASSIGNMENT_SEPARATOR + value)![wrapped_value_key]);
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            throw new ArgumentException(ApplicationText.Format(CliStrings.InvalidToml, exception.Message), exception);
        }
    }

    /// <summary>Parses a complete CLI configuration document.</summary>
    /// <param name="text">Complete TOML document text.</param>
    /// <returns>The root table represented as an object.</returns>
    /// <exception cref="ArgumentException">The document is not valid TOML.</exception>
    internal static JObject ParseDocument(string text)
    {
        try
        {
            return (JObject)Convert(TomlSerializer.Deserialize<TomlTable>(text)!);
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            throw new ArgumentException(ApplicationText.Format(CliStrings.InvalidToml, exception.Message), exception);
        }
    }

    private static JToken Convert(object? value)
    {
        if (value is TomlDateTime date)
            return new JValue(date.ToString());

        if (value is IDictionary<string, object> table)
            return new JObject(table.Select(pair => new JProperty(pair.Key, Convert(pair.Value))));

        if (value is IEnumerable items && value is not string)
            return new JArray(items.Cast<object?>().Select(Convert));

        return value is null ? JValue.CreateNull() : JToken.FromObject(value);
    }
}
