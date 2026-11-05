using System.CommandLine;
using System.CommandLine.Help;
using System.Text.RegularExpressions;
using AwesomeAssertions;
using Mapping_Tools.Application.Tools.TumourGenerator.Models;
using Mapping_Tools.Cli.Commands;
using Mapping_Tools.Cli.Configuration;
using Mapping_Tools.Cli.Localization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;

namespace Mapping_Tools.Cli.Tests.Commands;

[TestClass]
public sealed class CommandTreeTests
{
    [TestMethod]
    [DataRow("pattern-gallery collection list")]
    [DataRow("pattern-gallery collection create")]
    [DataRow("pattern-gallery collection rename")]
    [DataRow("pattern-gallery collection delete")]
    [DataRow("pattern-gallery collection import")]
    [DataRow("pattern-gallery collection export")]
    [DataRow("pattern-gallery collection merge")]
    [DataRow("pattern-gallery collection restore")]
    [DataRow("pattern-gallery list")]
    [DataRow("pattern-gallery show")]
    [DataRow("pattern-gallery import")]
    [DataRow("pattern-gallery rename")]
    [DataRow("pattern-gallery delete")]
    [DataRow("pattern-gallery export")]
    public async Task Create_SubcommandHelp_ContainsOnlyRelevantExamplesThatParse(string command)
    {
        // Arrange
        var root = new CommandTree(CliRunnerTests.CreateConsole(new StringWriter()), new StringWriter(), new StringReader("")).Create();

        // Act
        var result = await CliRunnerTests.RunAsync([.. command.Split(' '), "--help"]);
        string[] examples = result.Output.Split(Environment.NewLine)
            .Select(line => line.Trim())
            .Where(line => line.StartsWith("mapping-tools ", StringComparison.Ordinal))
            .ToArray();

        // Assert
        result.Code.Should().Be(0, result.Error);
        examples.Should().ContainSingle();
        examples[0].Should().StartWith("mapping-tools " + command);
        string? example = CliStrings.ResourceManager.GetString("Example." + command.Replace(' ', '.'), CliStrings.Culture);
        example.Should().NotBeNullOrWhiteSpace();
        root.Parse(example.Trim()["mapping-tools ".Length..]).Errors.Should().BeEmpty();
    }

    [TestMethod]
    public async Task Create_StructuredOptionHelp_ExplainsFieldsEnumChoicesAndNumericGraphs()
    {
        // Arrange
        string[] arguments = ["tumour-generator", "--help"];

        // Act
        var result = await CliRunnerTests.RunAsync(arguments);

        // Assert
        result.Code.Should().Be(0, result.Error);
        string normalizedOutput = Regex.Replace(result.Output, @"\s+", " ");
        normalizedOutput.Should().Contain("Fields:")
            .And.Contain("tumour-template-enum")
            .And.Contain("triangle | square | circle | parabola")
            .And.Contain("number for a constant graph")
            .And.Contain("Only square uses tumour-parameter")
            .And.Contain("length 0 inserts a full loop")
            .And.Contain("Start-to-start spacing")
            .And.Contain("not a rigid rotation")
            .And.Contain("default, not a limit")
            .And.NotContain("List`1")
            .And.NotContain("GraphState");
    }

    [TestMethod]
    public void Create_TumourLayerHelp_ShowsModelDefaultsBesideEachField()
    {
        // Arrange
        var root = new CommandTree(CliRunnerTests.CreateConsole(new StringWriter()), new StringWriter(), new StringReader("")).Create();
        var command = (ToolCommand)root.Subcommands.Single(command => command.Name == "tumour-generator");
        var defaults = ((TumourGeneratorServiceOptions)command.Create()).TumourLayers.Single();

        // Act
        string description = command.Options.Single(option => option.Name == "--tumour-layers").Description!;
        string[] fields = description.Split(Environment.NewLine);

        // Assert
        description.Should().Contain("Built-in item count: 1");
        fields.Single(line => line.StartsWith("tumour-length ")).Should()
            .EndWith("(built-in default: " + TomlValue.Format(new JValue(defaults.TumourLength.Anchors[0].Pos.Y)) + ")");
        fields.Single(line => line.StartsWith("tumour-scale ")).Should()
            .EndWith("(built-in default: " + TomlValue.Format(new JValue(defaults.TumourScale.Anchors[0].Pos.Y)) + ")");
        fields.Single(line => line.StartsWith("tumour-end ")).Should()
            .EndWith("(built-in default: " + TomlValue.Format(new JValue(defaults.TumourEnd)) + ")");
        fields.Single(line => line.StartsWith("recalculate ")).Should().EndWith("(built-in default: true)");
        fields.Single(line => line.StartsWith("use-absolute-range ")).Should().EndWith("(built-in default: true)");
        fields[1].Should().Contain("built-in default: \"triangle\"");
    }

    [TestMethod]
    public void Create_EmptyLayerListHelp_ShowsItemConstructorDefaultsAndZeroItemCount()
    {
        // Arrange
        var root = new CommandTree(CliRunnerTests.CreateConsole(new StringWriter()), new StringWriter(), new StringReader("")).Create();
        var command = root.Subcommands.Single(command => command.Name == "hitsound-studio");

        // Act
        string description = command.Options.Single(option => option.Name == "--hitsound-layers").Description!;

        // Assert
        description.Should().Contain("Built-in item count: 0")
            .And.Contain("sample-set (none | normal | soft | drum) (built-in default: \"normal\")")
            .And.Contain("sample-args (TOML inline table) (built-in default: {path = \"\", volume = 1.0")
            .And.NotContain("uses-sound-font")
            .And.NotContain("SampleGeneratingArgs");
    }

    [TestMethod]
    public async Task Create_GalleryExportHelp_ExplainsNamedDefaultsAndEditorDependency()
    {
        // Arrange
        string[] arguments = ["pattern-gallery", "export", "--help"];

        // Act
        var result = await CliRunnerTests.RunAsync(arguments);

        // Assert
        result.Code.Should().Be(0, result.Error);
        string normalizedOutput = Regex.Replace(result.Output, @"\s+", " ");
        normalizedOutput.Should().Contain("built-in default: \"current\"")
            .And.Contain("requires editor access")
            .And.Contain("built-in default: \"partitioned-overwrite\"")
            .And.Contain("leaving gaps between sections intact");
    }

    [TestMethod]
    public void Create_NestedCommandHelp_InheritsTheRootHelpOption()
    {
        // Arrange
        StringWriter output = new();
        StringWriter error = new();
        StringReader input = new("");
        var root = new CommandTree(CliRunnerTests.CreateConsole(output), error, input).Create();
        var help = root.Options.OfType<HelpOption>().Single();
        Queue<Command> pending = new(root.Subcommands);
        List<Command> descendants = [];

        while (pending.TryDequeue(out var command))
        {
            descendants.Add(command);

            foreach (var child in command.Subcommands)
                pending.Enqueue(child);
        }

        // Act
        var result = root.Parse(["pattern-gallery", "collection", "import", "--help"]);

        // Assert
        help.Recursive.Should().BeTrue();
        descendants.Should().OnlyContain(command => !command.Options.OfType<HelpOption>().Any());
        result.Action.Should().BeSameAs(help.Action);
    }
}
