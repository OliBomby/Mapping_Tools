namespace Mapping_Tools.Cli.Commands;

/// <summary>Shared CLI and TOML tokens used when defining and binding options.</summary>
internal static class CliOptionSyntax
{
    internal const string LONG_OPTION_PREFIX = "--";
    internal const string NEGATED_PREFIX = "no-";
    internal const string APPEND_PREFIX = "append-";
    internal const string TRUE_VALUE = "true";
    internal const string FALSE_VALUE = "false";
}
