using System.CommandLine;
using Mapping_Tools.Application.Localization;
using Mapping_Tools.Cli.Localization;
using Mapping_Tools.Infrastructure.Updates;
using Spectre.Console;
using Velopack.Locators;

namespace Mapping_Tools.Cli.Commands;

/// <summary>Defines explicitly requested update checks and installation without replaying a previous tool command.</summary>
internal static class UpdateCommand
{
    /// <summary>Creates update and its check-only subcommand.</summary>
    /// <param name="console">Supplies status output; help is inherited from the root.</param>
    /// <returns>The configured update command.</returns>
    internal static Command Create(IAnsiConsole console)
    {
        var output = console.Profile.Out.Writer;
        Command update = new("update", CliStrings.UpdateDescription);
        Command check = new("check", CliStrings.UpdateCheckHelp);
        update.Subcommands.Add(check);
        update.SetAction((_, token) => RunAsync(false, output, token));
        check.SetAction((_, token) => RunAsync(true, output, token));

        return update;
    }

    /// <summary>Checks the installed release channel and optionally prepares and launches its updater.</summary>
    /// <param name="checkOnly">Whether to report availability without downloading or installing an update.</param>
    /// <param name="output">Receives availability and update status.</param>
    /// <param name="cancellationToken">Cancels release lookup and update preparation.</param>
    /// <returns>Zero when the requested operation succeeds.</returns>
    /// <remarks>
    /// Installation requires an installed release package. Updating a shared installation updates its Desktop and CLI
    /// files together.
    /// </remarks>
    internal static async Task<int> RunAsync(bool checkOnly, TextWriter output, CancellationToken cancellationToken)
    {
        using HttpClient httpClient = new();
        using var updates = new VelopackUpdateGateway(httpClient, silent: true);
        var check = await updates.CheckForUpdatesAsync(cancellationToken);

        if (!check.CanUpdate)
        {
            output.WriteLine(CliStrings.UpToDate);

            return 0;
        }

        output.WriteLine(ApplicationText.Format(CliStrings.UpdateAvailable, check.LatestVersion));

        if (checkOnly)
            return 0;

        if (VelopackLocator.Current.CurrentlyInstalledVersion is null)
            throw new InvalidOperationException(CliStrings.UpdateNotInstalled);

        await updates.PrepareUpdateAsync(check.LatestVersion!, new Progress<double>(), cancellationToken);
        updates.LaunchUpdater(check.LatestVersion!, false);
        output.WriteLine(ApplicationText.Format(CliStrings.UpdateStarted, check.LatestVersion));

        return 0;
    }
}
