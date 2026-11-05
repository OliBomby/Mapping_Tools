using System.CommandLine.Help;
using System.CommandLine;
using System.Reflection;
using Mapping_Tools.Application.Localization;
using Mapping_Tools.Cli.Commands;
using Mapping_Tools.Cli.Localization;
using Spectre.Console;

namespace Mapping_Tools.Cli;

/// <summary>Parses and invokes one command line, translating failures into the CLI exit-code contract.</summary>
/// <param name="console">Renders help and supplies the output writer for operation results.</param>
/// <param name="error">Receives diagnostics; its lifetime belongs to the caller.</param>
/// <param name="input">Supplies standard input for explicitly interactive operations; its lifetime belongs to the caller.</param>
/// <remarks>
/// The caller owns the console and streams. Syntax and argument errors return 2, operational failures return 1, and
/// cancellation returns 130. Successful handlers may return their own exit code, including Auto Fail Detector's
/// optional 3.
/// </remarks>
internal sealed class CliRunner(
    IAnsiConsole console,
    TextWriter error,
    TextReader input)
{
    private readonly TextWriter output = console.Profile.Out.Writer;

    /// <summary>Invokes help, configuration, update, or a mapping tool through System.CommandLine.</summary>
    /// <param name="arguments">Command-line tokens, including the command name and its options.</param>
    /// <param name="cancellationToken">Cancels cooperative operations in addition to the library's Ctrl+C handling.</param>
    /// <returns>The handler exit code, or the CLI error code when parsing or execution fails.</returns>
    internal async Task<int> RunAsync(string[] arguments, CancellationToken cancellationToken = default)
    {
        try
        {
            var root = new CommandTree(console, error, input).Create();
            var result = root.Parse(arguments, new ParserConfiguration { ResponseFileTokenReplacer = null });

            if (result.Errors.Count > 0 && result.Action is not (HelpAction or HelpWriter))
            {
                foreach (var parseError in result.Errors)
                    await error.WriteLineAsync(parseError.Message);

                return 2;
            }

            return await result.InvokeAsync(new InvocationConfiguration
            {
                Output = output,
                Error = error,
                EnableDefaultExceptionHandler = false,
            }, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            await error.WriteLineAsync(CliStrings.Cancelled);

            return 130;
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            if (exception is TargetInvocationException { InnerException: { } inner })
                exception = inner;

            await error.WriteLineAsync(ApplicationText.Format(CliStrings.Error, exception.Message));

            return exception is ArgumentException ? 2 : 1;
        }
    }
}
