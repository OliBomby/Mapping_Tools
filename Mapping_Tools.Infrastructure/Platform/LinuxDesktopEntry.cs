namespace Mapping_Tools.Infrastructure.Platform;

internal static class LinuxDesktopEntry
{
    private const string generated_marker = "X-MappingTools-Generated=true";

    internal static void EnsureInstalled(IEnumerable<string> dataDirectories, string executable, string? assembly)
    {
        string[] directories = dataDirectories.ToArray();
        string userEntry = Path.Combine(directories[0], "applications", "MappingTools.desktop");
        foreach (string directory in directories)
        {
            string entry = Path.Combine(directory, "applications", "MappingTools.desktop");
            if (!File.Exists(entry)) continue;

            // Preserve launchers installed or customized by the user or a package manager.
            if (entry != userEntry || !File.ReadAllLines(entry).Contains(generated_marker)) return;
            break;
        }

        string content = CreateContents(executable, assembly);
        if (File.Exists(userEntry) && File.ReadAllText(userEntry) == content) return;

        Directory.CreateDirectory(Path.GetDirectoryName(userEntry)!);
        File.WriteAllText(userEntry, content);
    }

    internal static string CreateContents(string executable, string? assembly)
    {
        string command = QuoteArgument(executable);
        if (assembly is not null) command += " " + QuoteArgument(assembly);

        return $"""
            [Desktop Entry]
            Type=Application
            Name=Mapping Tools
            Exec={command}
            Terminal=false
            Categories=Utility;
            StartupWMClass=MappingTools
            {generated_marker}

            """;
    }

    private static string QuoteArgument(string argument)
    {
        // Exec arguments are quoted first, then escaped as desktop-entry strings.
        string quoted = argument.Replace("\\", "\\\\").Replace("\"", "\\\"")
            .Replace("`", "\\`").Replace("$", "\\$");
        return "\"" + quoted.Replace("\\", "\\\\").Replace("%", "%%")
            .Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t") + "\"";
    }
}
