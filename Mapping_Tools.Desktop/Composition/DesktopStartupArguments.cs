using Mapping_Tools.Application.Localization;
using Mapping_Tools.Desktop.Localization;

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
                        ApplicationText.Format(DesktopStrings.DesktopStartup_UpdateFileOptionRequiresPath, local_update_file_option),
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
                        ApplicationText.Format(DesktopStrings.DesktopStartup_UpdateFileOptionRequiresPath, local_update_file_option),
                        nameof(arguments));
            }

            if (value is null) continue;
            if (packagePath is not null)
                throw new ArgumentException(
                    ApplicationText.Format(DesktopStrings.DesktopStartup_UpdateFileOptionOnlyOnce, local_update_file_option),
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
