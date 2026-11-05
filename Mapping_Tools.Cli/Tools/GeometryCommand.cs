using Mapping_Tools.Application.Localization;
using Mapping_Tools.Application.Tools.GeometryDashboard;
using Mapping_Tools.Application.Tools.GeometryDashboard.Contracts;
using Mapping_Tools.Application.Tools.GeometryDashboard.Models;
using Mapping_Tools.Cli.Commands;
using Mapping_Tools.Cli.Configuration;
using Mapping_Tools.Cli.Localization;
using Newtonsoft.Json.Linq;
using Spectre.Console;

namespace Mapping_Tools.Cli.Tools;

/// <summary>Configures Geometry Dashboard from command-line preferences and runs its overlay until cancellation.</summary>
internal static class GeometryCommand
{
    /// <summary>Registers dashboard settings and explicit current-map tracking without resolving a fixed target file.</summary>
    /// <param name="console">Renders help and supplies the output writer for operation results.</param>
    /// <param name="error">Receives diagnostics; its lifetime belongs to the caller.</param>
    /// <param name="input">Supplies standard input for explicitly interactive operations; its lifetime belongs to the caller.</param>
    /// <returns>A ready-to-register command with inherited root help.</returns>
    internal static ToolCommand Create(IAnsiConsole console, TextWriter error, TextReader input) =>
        ToolCommand.For(
            console, error, input,
            GeometryDashboardToolDefinition.Definition,
            () => new GeometryDashboardServiceOptions(),
            RunAsync,
            prepare: PrepareAsync);

    /// <summary>Starts the configured Windows overlay and stops it when cancellation is requested.</summary>
    /// <param name="context">Prepared invocation, including any relative subcommand and CLI-only choices.</param>
    /// <param name="project">Typed Application settings after project loading, imports, and overrides.</param>
    /// <returns>Zero after the dashboard and overlay have been stopped.</returns>
    internal static async Task<int> RunAsync(ToolContext context, GeometryDashboardServiceOptions project)
    {
        if (!CliOptions.Current.GetValue(context.ParseResult)
            && context.Document[CliDocumentKeys.TRACK_CURRENT]?.Value<bool>() != true)
            throw new ArgumentException(ApplicationText.Format(CliStrings.RequiredOption, CliOptions.Current.Name));

        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException(CliStrings.GeometryUnsupported);

        var overlay = context.Get<IGeometryDashboardOverlayService>();

        if (!overlay.IsSupported)
            throw new PlatformNotSupportedException(CliStrings.GeometryUnsupported);

        var dashboard = context.Get<IGeometryDashboardService>();
        dashboard.ApplyPreferences();
        dashboard.Start();
        context.Output.WriteLine(CliStrings.GeometryRunning);

        try
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, context.CancellationToken);
        }
        catch (OperationCanceledException) when (context.CancellationToken.IsCancellationRequested)
        {
        }
        finally
        {
            dashboard.Stop();
            overlay.Hide();
        }

        return 0;
    }

    /// <summary>Records current-editor tracking in project metadata instead of binding the dashboard to a fixed beatmap file.</summary>
    /// <param name="context">Invocation receiving the tracking metadata.</param>
    /// <param name="_">Unused model schema; the dashboard follows the editor.</param>
    /// <returns>A completed task after updating the in-memory document.</returns>
    private static Task PrepareAsync(ToolContext context, OptionSchema _)
    {
        var document = context.Document;
        document[CliDocumentKeys.TRACK_CURRENT] = CliOptions.Current.GetValue(context.ParseResult)
            || document[CliDocumentKeys.TRACK_CURRENT]?.Value<bool>() == true;

        return Task.CompletedTask;
    }
}
