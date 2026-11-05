using System.CommandLine;
using System.CommandLine.Parsing;
using Mapping_Tools.Application.Localization;
using Mapping_Tools.Cli.Configuration;
using Mapping_Tools.Cli.Localization;
using Newtonsoft.Json.Linq;
using Spectre.Console;

namespace Mapping_Tools.Cli.Commands;

/// <summary>Defines CLI-default management commands and persists only allowlisted global preferences.</summary>
internal static class ConfigCommand
{
    internal const string COMMAND = "config";
    internal const string SHOW = "show";
    internal const string GET = "get";
    internal const string SET = "set";
    internal const string UNSET = "unset";
    internal const string RESET = "reset";
    internal const string PATH = "path";

    private const string key_argument_name = "key";
    private const string value_argument_name = "value";

    /// <summary>Stable config subcommand names used for registration and contextual help.</summary>
    internal static IReadOnlyList<string> Subcommands { get; } = [SHOW, GET, SET, UNSET, RESET, PATH];

    /// <summary>Creates config and its show, get, set, unset, reset, and path subcommands.</summary>
    /// <param name="console">Supplies command output; help is inherited from the root.</param>
    /// <returns>The configured config command.</returns>
    internal static Command Create(IAnsiConsole console)
    {
        var output = console.Profile.Out.Writer;
        Command config = new(COMMAND, CliStrings.Config_Description);
        config.SetAction((result, token) => RunParsedAsync(result, output, token));

        foreach (string name in Subcommands)
        {
            Command command = new(name, name switch
            {
                SHOW => CliStrings.Config_Command_show,
                GET => CliStrings.Config_Command_get,
                SET => CliStrings.Config_Command_set,
                UNSET => CliStrings.Config_Command_unset,
                RESET => CliStrings.Config_Command_reset,
                PATH => CliStrings.Config_Command_path,
                _ => throw new ArgumentOutOfRangeException(nameof(name)),
            });
            config.Subcommands.Add(command);

            Argument<string>? keyArgument = null;
            Argument<string>? valueArgument = null;

            if (name is GET or SET or UNSET)
            {
                keyArgument = new Argument<string>(key_argument_name)
                {
                    Description = CliStrings.Config_Settings + " " + string.Join(", ", GlobalConfiguration.Options.Keys),
                };
                command.Arguments.Add(keyArgument);
            }

            if (name == SET)
            {
                valueArgument = new Argument<string>(value_argument_name);
                command.Arguments.Add(valueArgument);
            }

            command.SetAction((result, token) =>
                RunParsedAsync(result, output, token, name, keyArgument, valueArgument));
        }

        return config;
    }

    private static Task<int> RunParsedAsync(
        ParseResult result,
        TextWriter output,
        CancellationToken token,
        string? subcommand = null,
        Argument<string>? keyArgument = null,
        Argument<string>? valueArgument = null)
    {
        var configuration = new GlobalConfiguration(
            CliOptions.Config.GetValue(result) ?? GlobalConfiguration.DefaultPath);

        return RunAsync(result, configuration, output, token, subcommand, keyArgument, valueArgument);
    }

    /// <summary>Executes a validated configuration operation; no subcommand defaults to show.</summary>
    /// <param name="parseResult">System.CommandLine's successful parse.</param>
    /// <param name="configuration">Selected CLI configuration store.</param>
    /// <param name="output">Receives values, source annotations, and save confirmations.</param>
    /// <param name="cancellationToken">Cancels configuration file writes.</param>
    /// <param name="subcommand">The selected config operation, or null when the root config command defaults to show.</param>
    /// <param name="keyArgument">The registered setting-key argument, when the operation takes one.</param>
    /// <param name="valueArgument">The registered setting-value argument for set.</param>
    /// <returns>Zero when the requested configuration operation succeeds.</returns>
    internal static async Task<int> RunAsync(
        ParseResult parseResult,
        GlobalConfiguration configuration,
        TextWriter output,
        CancellationToken cancellationToken,
        string? subcommand = null,
        Argument<string>? keyArgument = null,
        Argument<string>? valueArgument = null)
    {
        for (var command = parseResult.CommandResult; command is not null; command = command.Parent as CommandResult)
            foreach (var option in command.Children.OfType<OptionResult>().Where(option => !option.Implicit))
                if (option.Option.Name != CliOptionSyntax.LONG_OPTION_PREFIX + CliOptions.Config.Name)
                    throw new ArgumentException(ApplicationText.Format(CliStrings.UnknownOption,
                        option.Option.Name.TrimStart('-')));

        string operation = subcommand ?? SHOW;
        string? key = keyArgument is null ? null : parseResult.GetValue(keyArgument);
        string? valueText = valueArgument is null ? null : parseResult.GetValue(valueArgument);

        if (operation == PATH)
        {
            output.WriteLine(configuration.FilePath);

            return 0;
        }

        if (operation == RESET)
        {
            await configuration.SaveAsync(new JObject(), cancellationToken);
            output.WriteLine(ApplicationText.Format(CliStrings.Config_Saved, configuration.FilePath));

            return 0;
        }

        JObject saved = configuration.Load(required:
            CliOptions.Config.WasSpecified(parseResult) && (operation is SHOW or GET));
        JObject effective = GlobalConfiguration.Defaults();
        effective.Merge(saved);

        switch (operation)
        {
            case SHOW:
                output.WriteLine(ApplicationText.Format(CliStrings.Config_File, configuration.FilePath));

                foreach (var property in effective.Properties())
                {
                    string source = saved.ContainsKey(property.Name) ? CliStrings.Config_SourceSaved : CliStrings.Config_SourceDefault;
                    output.WriteLine($"{property.Name} = {TomlValue.Format(property.Value)} # {source}");
                }

                output.WriteLine();
                output.WriteLine(CliStrings.Config_DefaultsHelp);
                break;

            case GET:
                if (key is null || !effective.TryGetValue(key, out var settingValue))
                    throw new ArgumentException(ApplicationText.Format(CliStrings.Config_UnknownSetting, key ?? ""));

                output.WriteLine(TomlValue.Format(settingValue));
                break;

            case SET:
                if (key is null || valueText is null)
                    throw new ArgumentException(CliStrings.SubcommandHelp);

                saved[key] = GlobalConfiguration.ParseValue(key, valueText);
                await configuration.SaveAsync(saved, cancellationToken);
                output.WriteLine(ApplicationText.Format(CliStrings.Config_Saved, configuration.FilePath));
                break;

            case UNSET:
                if (key is null || !GlobalConfiguration.Options.ContainsKey(key))
                    throw new ArgumentException(ApplicationText.Format(CliStrings.Config_UnknownSetting, key ?? ""));

                saved.Remove(key);
                await configuration.SaveAsync(saved, cancellationToken);
                output.WriteLine(ApplicationText.Format(CliStrings.Config_Saved, configuration.FilePath));
                break;
        }

        return 0;
    }
}
