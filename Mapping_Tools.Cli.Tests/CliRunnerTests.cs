using System.Text.RegularExpressions;
using AwesomeAssertions;
using Mapping_Tools.Application.Tools.TumourGenerator.Models;
using Mapping_Tools.Cli.Commands;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using Spectre.Console;

namespace Mapping_Tools.Cli.Tests;

[TestClass]
public sealed class CliRunnerTests
{
    [TestMethod]
    [DataRow("--no-use-combo-colours")]
    [DataRow("--use-combo-colours=false")]
    public async Task RunAsync_BooleanAliasesAndEndOfOptions_PreservesExplicitValuesAndLiteralTargets(string option)
    {
        // Arrange
        string destination = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".json");

        try
        {
            // Act
            var result = await RunAsync("metadata-manager", option, "--title=-title", "--save-project", destination,
                "--no-run", "--", "--literal.osu");

            // Assert
            result.Code.Should().Be(0, result.Error);
            var document = JObject.Parse(await File.ReadAllTextAsync(destination));
            document["UseComboColours"]!.Value<bool>().Should().BeFalse();
            document["Title"]!.Value<string>().Should().Be("-title");
            document["CliTargets"]!.Values<string>().Should().Equal(Path.GetFullPath("--literal.osu"));
        }
        finally
        {
            File.Delete(destination);
        }
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("sliderator")]
    [DataRow("pattern-gallery collection import")]
    [DataRow("update check")]
    [DataRow("config set")]
    public async Task RunAsync_HelpSections_SeparatesCategoriesWithBlankLines(string? command)
    {
        // Arrange
        string[] arguments = (command ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries).Append("--help").ToArray();
        string[] headings =
        [
            "Usage:", "Arguments:", "Commands:", "Options:", "Notes:", "Examples:", "Generators:"
        ];

        // Act
        var result = await RunAsync(arguments);

        // Assert
        result.Code.Should().Be(0, result.Error);
        result.Output.Should().Contain("Options:");
        result.Output.Should().NotContain("--live");

        foreach (string heading in headings.Where(heading => result.Output.Contains(heading)))
            result.Output.Should().Contain(Environment.NewLine + Environment.NewLine + heading + Environment.NewLine);
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("sliderator")]
    [DataRow("update check")]
    [DataRow("config set")]
    public async Task RunAsync_ColorEnabled_HighlightsHelpWithoutChangingItsText(string? command)
    {
        // Arrange
        string[] arguments = (command ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries).Append("--help").ToArray();
        StringWriter plainOutput = new();
        StringWriter coloredOutput = new();
        StringWriter error = new();
        StringReader input = new("");
        CliRunner plain = new(CreateConsole(plainOutput), error, input);
        CliRunner colored = new(CreateConsole(coloredOutput, useColor: true), error, input);

        // Act
        int plainCode = await plain.RunAsync(arguments);
        int coloredCode = await colored.RunAsync(arguments);

        // Assert
        plainCode.Should().Be(0, error.ToString());
        coloredCode.Should().Be(0, error.ToString());
        plainOutput.ToString().Should().NotContain("\u001b");
        plainOutput.ToString().Should().NotContain("Tool defaults never come from Desktop autosaves.");
        coloredOutput.ToString().Should().Contain("\u001b[");
        Regex.Replace(coloredOutput.ToString(), "\u001b\\[[0-9;]*m", "").Should().Be(plainOutput.ToString());

        plainOutput.ToString().Should().Contain("Usage:").And.NotContain("#####");
    }

    [TestMethod]
    [DataRow("auto-fail-detector", "")]
    [DataRow("pattern-gallery collection list", "")]
    [DataRow("config path", "")]
    [DataRow("config set", "Relative paths passed to config set")]
    [DataRow("sliderator", "Each selected slider")]
    [DataRow("metadata-manager", "Nested values use TOML")]
    public async Task RunAsync_CommandHelp_OnlyIncludesNotesForThatContext(string command, string expectedNote)
    {
        // Arrange
        string[] arguments = command.Split(' ').Append("--help").ToArray();

        // Act
        var result = await RunAsync(arguments);

        // Assert
        result.Code.Should().Be(0, result.Error);
        result.Output.Should().NotContain("Tool defaults never come from Desktop autosaves.");

        if (expectedNote.Length == 0)
            result.Output.Should().NotContain("Notes:");
        else
            result.Output.Should().Contain(expectedNote);

        result.Output.Should().NotContain("Saved Hitsound Studio layers are reused");
        result.Output.Should().NotContain("--layer '{name = \"Layer 1\"");
        result.Output.Should().NotContain("Preview objects are discarded");
    }

    [TestMethod]
    public async Task RunAsync_AllToolAndSubcommandHelp_ReturnsUsageWithoutExecutingTools()
    {
        // Arrange
        var toolCommands = CreateToolCommands();
        var patternGallery = toolCommands.Single(command => command.Name == "pattern-gallery");
        var commands = toolCommands
            .Select(command => new[] { command.Name, "--help" })
            .Concat(SubcommandPaths(patternGallery).Select(path => path.Append("--help").ToArray()))
            .Append(["--help"])
            .Append(["update", "--help"])
            .Append(["update", "check", "--help"])
            .Append(["config", "--help"])
            .Concat(ConfigCommand.Subcommands.Select(command => new[] { "config", command, "--help" }));

        // Act
        var results = new List<(int Code, string Output, string Error)>();

        foreach (string[] command in commands)
            results.Add(await RunAsync(command));

        // Assert
        results.Should().OnlyContain(result => result.Code == 0 && result.Error == "" && result.Output.Contains("Usage:"));
        results.Select(result => result.Output).Should()
            .NotContain(text => text.Contains("Example.") || text.Contains("Option.Mapping_Tools")
                || text.Contains("List`1") || text.Contains("Dictionary`2"));
    }

    [TestMethod]
    public async Task RunAsync_AllToolsSaveOnly_PersistsDesktopCompatibleProjectsWithoutTargets()
    {
        // Arrange
        string directory = Path.Combine(Path.GetTempPath(), "mapping-tools-cli-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);

        var commands = CreateToolCommands();

        // Act
        var results = new List<(int Code, string Output, string Error)>();

        try
        {
            foreach (var command in commands)
                results.Add(await RunAsync(command.Name, "--save-project", Path.Combine(directory, command.Name + ".json"), "--no-run"));

            // Assert
            results.Should().OnlyContain(
                result => result.Code == 0 && result.Error == "",
                string.Join(Environment.NewLine, results.Select(result => result.Error)));

            foreach (var command in commands)
            {
                string json = File.ReadAllText(Path.Combine(directory, command.Name + ".json"));
                JObject.Parse(json)["$schema"]!.Value<string>().Should().Be("mapping-tools.tool." + command.Name);
                command.Read(json).Should().BeOfType(command.OptionsType);
            }
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [TestMethod]
    public async Task RunAsync_TomlLayersReplaceAndAppend_PreservesRequestedOrderAndOverridesProject()
    {
        // Arrange
        string directory = Path.Combine(Path.GetTempPath(), "mapping-tools-cli-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);

        string original = Path.Combine(directory, "original.json");
        string destination = Path.Combine(directory, "destination.json");
        await RunAsync(
            "tumour-generator",
            "--layer",
            "{name = 'Original', random-seed = 1}",
            "--save-project",
            original,
            "--no-run");

        // Act
        var result = await RunAsync(
            "tumour-generator",
            "--project",
            original,
            "--layer",
            "{name = 'First', random-seed = 42}",
            "--append-layer",
            "{name = 'Second', random-seed = 43}",
            "--save-project",
            destination,
            "--no-run");

        // Assert
        try
        {
            result.Code.Should().Be(0, result.Error);

            var layers = (JArray)JObject.Parse(File.ReadAllText(destination))["TumourLayers"]!;
            layers.Select(layer => layer["Name"]!.Value<string>()).Should().Equal("First", "Second");
            layers.Select(layer => layer["RandomSeed"]!.Value<int>()).Should().Equal(42, 43);

            var command = CreateToolCommands().Single(command => command.Name == "tumour-generator");
            var options = (TumourGeneratorServiceOptions)command.Read(await File.ReadAllTextAsync(destination));
            options.TumourLayers.Should().HaveCount(2).And.OnlyContain(layer => layer.IsActive);
            options.TumourLayers[0].TumourLength.GetValue(0).Should().Be(15);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [TestMethod]
    [DataRow("slider-merger")]
    [DataRow("slider-completionator")]
    [DataRow("sliderator")]
    [DataRow("tumour-generator")]
    public async Task RunAsync_MissingSelection_RejectsBeforeReadingBeatmap(string command)
    {
        // Arrange
        string nonexistent = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".osu");

        // Act
        var result = await RunAsync(command, "--path", nonexistent);

        // Assert
        result.Code.Should().Be(2);
        result.Error.Should().Contain("selection explicitly");
        File.Exists(nonexistent).Should().BeFalse();
    }

    [TestMethod]
    public async Task RunAsync_UnknownNestedTomlMember_ReportsTypoWithoutSavingProject()
    {
        // Arrange
        string destination = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".json");

        // Act
        var result = await RunAsync("tumour-generator", "--layer", "{random-sead = 42}", "--save-project", destination, "--no-run");

        // Assert
        result.Code.Should().Be(2);
        result.Error.Should().Contain("random-sead");
        File.Exists(destination).Should().BeFalse();
    }

    [TestMethod]
    public async Task RunAsync_TomlGraphWithAnchors_PreservesCoordinatesInSavedProject()
    {
        // Arrange
        string destination = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".json");

        // Act
        var result = await RunAsync(
            "sliderator",
            "--graph",
            "{min-x = 0, max-x = 2, anchors = [{pos = {x = 0, y = 0}}, {pos = {x = 2, y = 1}}]}",
            "--save-project",
            destination,
            "--no-run");

        // Assert
        try
        {
            result.Code.Should().Be(0, result.Error);

            var graph = JObject.Parse(File.ReadAllText(destination))["GraphState"]!;
            graph["Anchors"]![1]!["Pos"]!["X"]!.Value<double>().Should().Be(2);
            graph["Anchors"]![1]!["Pos"]!["Y"]!.Value<double>().Should().Be(1);
        }
        finally
        {
            File.Delete(destination);
        }
    }

    [TestMethod]
    public async Task RunAsync_ExplicitRoleTargetOverridesSavedTargets_SavesTheNewTargetOnly()
    {
        // Arrange
        string directory = Path.Combine(Path.GetTempPath(), "mapping-tools-target-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);

        string original = Path.Combine(directory, "original.json");
        string destination = Path.Combine(directory, "destination.json");
        string oldTarget = Path.Combine(directory, "old.osu");
        string newTarget = Path.Combine(directory, "new.osu");
        await RunAsync("hitsound-copier", "--path", oldTarget, "--save-project", original, "--no-run");

        // Act
        var result = await RunAsync(
            "hitsound-copier",
            "--project",
            original,
            "--to",
            newTarget,
            "--save-project",
            destination,
            "--no-run");

        // Assert
        try
        {
            result.Code.Should().Be(0, result.Error);

            var document = JObject.Parse(await File.ReadAllTextAsync(destination));
            document["PathTo"]!.Value<string>().Should().Be(newTarget);
            ((JArray)document["CliTargets"]!).Values<string>().Should().Equal(newTarget);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [TestMethod]
    public async Task RunAsync_SaveProjectCollision_PreservesExistingFile()
    {
        // Arrange
        string destination = Path.GetTempFileName();
        await File.WriteAllTextAsync(destination, "existing content");

        // Act
        var result = await RunAsync("map-cleaner", "--save-project", destination, "--no-run");

        // Assert
        try
        {
            result.Code.Should().Be(1);
            result.Error.Should().Contain("--overwrite");
            (await File.ReadAllTextAsync(destination)).Should().Be("existing content");
        }
        finally
        {
            File.Delete(destination);
        }
    }

    internal static async Task<(int Code, string Output, string Error)> RunAsync(params string[] args)
    {
        StringWriter output = new();
        StringWriter error = new();
        int code = await new CliRunner(CreateConsole(output), error, new StringReader("")).RunAsync(["--no-config", .. args]);

        return (code, output.ToString(), error.ToString());
    }

    internal static IAnsiConsole CreateConsole(TextWriter output, bool useColor = false) =>
        AnsiConsole.Create(new AnsiConsoleSettings
        {
            Out = new AnsiConsoleOutput(output),
            Ansi = useColor ? AnsiSupport.Yes : AnsiSupport.No,
            ColorSystem = ColorSystemSupport.Standard,
            Interactive = InteractionSupport.No,
            Enrichment = new ProfileEnrichment { UseDefaultEnrichers = false },
        });

    internal static IReadOnlyList<ToolCommand> CreateToolCommands()
    {
        var root = new CommandTree(CreateConsole(new StringWriter()), new StringWriter(), new StringReader("")).Create();

        return root.Subcommands.OfType<ToolCommand>().ToArray();
    }

    private static IEnumerable<string[]> SubcommandPaths(System.CommandLine.Command parent, string[]? path = null)
    {
        path ??= [parent.Name];

        foreach (System.CommandLine.Command child in parent.Subcommands)
        {
            string[] childPath = [.. path, child.Name];
            yield return childPath;

            foreach (string[] descendant in SubcommandPaths(child, childPath))
                yield return descendant;
        }
    }
}
