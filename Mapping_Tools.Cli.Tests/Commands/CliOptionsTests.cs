using System.CommandLine;
using AwesomeAssertions;
using Mapping_Tools.Cli.Commands;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Cli.Tests.Commands;

[TestClass]
public sealed class CliOptionsTests
{
    [TestMethod]
    public void GetValue_OmittedAndUnregisteredOptions_ReturnsDefaultsWithoutThrowing()
    {
        // Arrange
        RootCommand root = new();
        root.Options.Add(new Option<bool>("--no-run"));
        var result = root.Parse([]);

        // Act
        bool noRun = CliOptions.NoRun.GetValue(result);
        string? selection = CliOptions.Selection.GetValue(result);
        bool specified = CliOptions.NoRun.WasSpecified(result);

        // Assert
        noRun.Should().BeFalse();
        selection.Should().BeNull();
        specified.Should().BeFalse();
    }

    [TestMethod]
    public void GetValue_SelectionAlias_ReadsCanonicalOptionValue()
    {
        // Arrange
        RootCommand root = new();
        root.Options.Add(new Option<string>("--import-mode", "--selection"));
        var result = root.Parse(["--selection", "selected"]);

        // Act
        string? selection = CliOptions.Selection.GetValue(result);

        // Assert
        selection.Should().Be("selected");
        CliOptions.Selection.WasSpecified(result).Should().BeTrue();
    }

    [TestMethod]
    public void GetValue_NestedCommandWithInheritedOption_ReadsExplicitValue()
    {
        // Arrange
        RootCommand root = new();
        root.Options.Add(new Option<string>("--config") { Recursive = true });
        Command collection = new("collection");
        collection.Subcommands.Add(new Command("create"));
        root.Subcommands.Add(collection);
        var result = root.Parse(["--config", "custom.toml", "collection", "create"]);

        // Act
        string? configuration = CliOptions.Config.GetValue(result);

        // Assert
        configuration.Should().Be("custom.toml");
        CliOptions.Config.WasSpecified(result).Should().BeTrue();
    }

    [TestMethod]
    public void WasSpecified_ExplicitFalseFlag_ReturnsTrue()
    {
        // Arrange
        RootCommand root = new();
        root.Options.Add(new Option<bool>("--no-run"));
        var result = root.Parse(["--no-run=false"]);

        // Act
        bool specified = CliOptions.NoRun.WasSpecified(result);

        // Assert
        specified.Should().BeTrue();
        CliOptions.NoRun.GetValue(result).Should().BeFalse();
    }
}
