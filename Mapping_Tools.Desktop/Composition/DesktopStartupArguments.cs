namespace Mapping_Tools.Desktop.Composition;

internal static class DesktopStartupArguments
{
    private const string localUpdateFileOption = "--update-file";

    internal static string? GetLocalUpdatePackagePath(IReadOnlyList<string> arguments)
    {
        ArgumentNullException.ThrowIfNull(arguments);

        string? packagePath = null;
        for (var index = 0; index < arguments.Count; index++)
        {
            string argument = arguments[index];
            string? value = null;
            if (string.Equals(argument, localUpdateFileOption, StringComparison.OrdinalIgnoreCase))
            {
                if (++index >= arguments.Count || string.IsNullOrWhiteSpace(arguments[index]))
                    throw new ArgumentException(
                        $"The {localUpdateFileOption} option requires a file path.",
                        nameof(arguments));

                value = arguments[index];
            }
            else if (argument.StartsWith(
                         localUpdateFileOption + "=",
                         StringComparison.OrdinalIgnoreCase))
            {
                value = argument[(localUpdateFileOption.Length + 1)..];
                if (string.IsNullOrWhiteSpace(value))
                    throw new ArgumentException(
                        $"The {localUpdateFileOption} option requires a file path.",
                        nameof(arguments));
            }

            if (value is null) continue;
            if (packagePath is not null)
                throw new ArgumentException(
                    $"The {localUpdateFileOption} option may only be specified once.",
                    nameof(arguments));

            packagePath = Path.GetFullPath(value);
        }

        if (packagePath is null) return null;
        if (!File.Exists(packagePath))
            throw new FileNotFoundException(
                "The local update package does not exist.",
                packagePath);

        return packagePath;
    }
}
