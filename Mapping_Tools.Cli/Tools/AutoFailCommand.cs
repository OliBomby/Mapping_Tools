using Mapping_Tools.Application.Localization;
using Mapping_Tools.Application.Tools.AutoFail;
using Mapping_Tools.Cli.Commands;
using Mapping_Tools.Cli.Localization;
using Spectre.Console;

namespace Mapping_Tools.Cli.Tools;

/// <summary>Analyzes auto-fail and optionally applies the first fix or reviews proposed solutions.</summary>
internal static class AutoFailCommand
{
    /// <summary>Registers analysis, optional fix/review modes, and opt-in failure-on-detection behavior.</summary>
    /// <param name="console">Renders help and supplies the output writer for operation results.</param>
    /// <param name="error">Receives diagnostics; its lifetime belongs to the caller.</param>
    /// <param name="input">Supplies standard input for explicitly interactive operations; its lifetime belongs to the caller.</param>
    /// <returns>A ready-to-register command with inherited root help.</returns>
    internal static ToolCommand Create(IAnsiConsole console, TextWriter error, TextReader input) =>
        ToolCommand.For(
            console, error, input,
            AutoFailDetectorToolDefinition.Definition,
            () => new AutoFailServiceOptions(""),
            RunAsync,
            options: new Dictionary<string, string>
            {
                [CliOptions.Fix.Name] = CliStrings.FixHelp,
                [CliOptions.Interactive.Name] = CliStrings.InteractiveHelp,
                [CliOptions.FailOnAutoFail.Name] = CliStrings.FailOnAutoFailHelp,
            },
            prepare: (context, schema) => context.PrepareTargetsAsync(
                schema, nameof(AutoFailServiceOptions.Path), single: true));

    /// <summary>Analyzes the target and performs only the explicitly selected fixing or review behavior.</summary>
    /// <param name="context">Prepared invocation, including any relative subcommand and CLI-only choices.</param>
    /// <param name="options">Typed Application settings after project loading, imports, and overrides.</param>
    /// <returns>Zero for successful analysis or fixing, or 3 when initial detection and fail-on-auto-fail were both enabled.</returns>
    internal static async Task<int> RunAsync(ToolContext context, AutoFailServiceOptions options)
    {
        string fix = context.GetOption(CliOptions.Fix) ?? CliValues.AUTO_FAIL_NO_FIX;

        if (fix is not (CliValues.AUTO_FAIL_NO_FIX or CliValues.AUTO_FAIL_FIRST_FIX))
            throw new ArgumentException(ApplicationText.Format(CliStrings.InvalidEnum,
                CliOptions.Fix.Name,
                $"{CliValues.AUTO_FAIL_NO_FIX}, {CliValues.AUTO_FAIL_FIRST_FIX}"));

        bool interactive = CliOptions.Interactive.GetValue(context.ParseResult);

        if (interactive && fix != CliValues.AUTO_FAIL_NO_FIX)
            throw new ArgumentException(CliStrings.NoInteractiveFix);

        var service = context.Get<IAutoFailService>();
        var run = await service.AnalyzeAsync(options, context.CancellationToken);
        bool remaining = run.Analysis.HasAutoFail;
        context.Output.WriteLine(remaining
            ? ApplicationText.Format(
                CliStrings.AutoFailDetected,
                options.Path,
                string.Join(", ", run.Analysis.UnloadingObjects),
                string.Join(", ", run.Analysis.PotentialUnloadingObjects))
            : ApplicationText.Format(CliStrings.AutoFailClear, options.Path));

        if (remaining && (fix == CliValues.AUTO_FAIL_FIRST_FIX || interactive))
        {
            foreach (var plan in service.GetFixPlans(run, context.CancellationToken))
            {
                context.Output.WriteLine(plan.Guide);

                if (interactive)
                {
                    context.Output.Write(CliStrings.AutoFailChoice);

                    string? choice;

                    do
                    {
                        choice = await context.Input.ReadLineAsync(context.CancellationToken);

                        if (choice is null
                            || choice.Equals(CliValues.CONFIRM.ToString(), StringComparison.OrdinalIgnoreCase)
                            || choice.Equals(CliValues.DECLINE.ToString(), StringComparison.OrdinalIgnoreCase)
                            || choice.Equals(CliValues.CANCEL.ToString(), StringComparison.OrdinalIgnoreCase))
                            break;

                        context.Output.Write(CliStrings.AutoFailChoice);
                    } while (true);

                    if (choice is null || choice.Equals(CliValues.CANCEL.ToString(), StringComparison.OrdinalIgnoreCase))
                        break;

                    if (!choice.Equals(CliValues.CONFIRM.ToString(), StringComparison.OrdinalIgnoreCase))
                        continue;
                }

                await service.ApplyFixAsync(run, plan,
                    quickRun: CliOptions.ReloadEditor.GetValue(context.ParseResult),
                    cancellationToken: context.CancellationToken);
                context.Output.WriteLine(ApplicationText.Format(CliStrings.AutoFailFixed, options.Path));
                break;
            }
        }

        return remaining && CliOptions.FailOnAutoFail.GetValue(context.ParseResult) ? 3 : 0;
    }
}
