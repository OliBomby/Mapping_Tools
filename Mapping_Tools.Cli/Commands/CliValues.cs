namespace Mapping_Tools.Cli.Commands;

/// <summary>Canonical values accepted by CLI-specific modes and prompts.</summary>
internal static class CliValues
{
    internal const string ALL_SELECTION_ALIAS = "all";
    internal const string AUTO_FAIL_NO_FIX = "none";
    internal const string AUTO_FAIL_FIRST_FIX = "first";

    internal const char CONFIRM = 'y';
    internal const char DECLINE = 'n';
    internal const char CANCEL = 'c';
}
