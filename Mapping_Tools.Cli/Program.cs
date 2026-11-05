using System.Globalization;
using Mapping_Tools.Cli;
using Spectre.Console;
using Velopack;

CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.GetCultureInfo("en");
VelopackApp.Build().SetAutoApplyOnStartup(false).Run();

var console = AnsiConsole.Create(new AnsiConsoleSettings
{
    ColorSystem = ColorSystemSupport.Standard,
    Interactive = InteractionSupport.No,
});

return await new CliRunner(console, Console.Error, Console.In).RunAsync(args);
