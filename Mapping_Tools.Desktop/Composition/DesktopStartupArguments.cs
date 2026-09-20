namespace Mapping_Tools.Desktop.Composition;

internal static class DesktopStartupArguments
{
    private const string local_update_file_option = "--update-file";

    internal static string? GetLocalUpdatePackagePath(IReadOnlyList<string> arguments)
    {
        ArgumentNullException.ThrowIfNull(arguments);

        string? packagePath = null;
        for (int index = 0; index < arguments.Count; index++)
        {
            string argument = arguments[index];
            string? value = null;
            if (string.Equals(argument, local_update_file_option, StringComparison.OrdinalIgnoreCase))
            {
                if (++index >= arguments.Count || string.IsNullOrWhiteSpace(arguments[index]))
                    throw new ArgumentException(
                        $"The {local_update_file_option} option requires a file path.",
                        nameof(arguments));

                value = arguments[index];
            }
            else if (argument.StartsWith(
                         local_update_file_option + "=",
                         StringComparison.OrdinalIgnoreCase))
            {
                value = argument[(local_update_file_option.Length + 1)..];
                if (string.IsNullOrWhiteSpace(value))
                    throw new ArgumentException(
                        $"The {local_update_file_option} option requires a file path.",
                        nameof(arguments));
            }

            if (value is null) continue;
            if (packagePath is not null)
                throw new ArgumentException(
                    $"The {local_update_file_option} option may only be specified once.",
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
