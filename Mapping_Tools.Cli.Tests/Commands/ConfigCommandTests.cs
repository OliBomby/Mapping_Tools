using AwesomeAssertions;
using Mapping_Tools.Cli.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;

namespace Mapping_Tools.Cli.Tests.Commands;

[TestClass]
public sealed class ConfigCommandTests
{
    [TestMethod]
    public async Task RunAsync_SetGetAndShow_PersistsDefaultsAcrossInvocationsAndReportsTheirSources()
    {
        // Arrange
        string directory = CreateDirectory();
        string configuration = Path.Combine(directory, "cli.toml");

        try
        {
            // Act
            var saved = await RunAsync(configuration, "config", "set", "editor-reader", "Mtipc");
            var read = await RunAsync(configuration, "config", "get", "editor-reader");
            var shown = await RunAsync(configuration, "config", "show");

            // Assert
            saved.Code.Should().Be(0, saved.Error);
            read.Code.Should().Be(0, read.Error);
            read.Output.Trim().Should().Be("\"mtipc\"");
            shown.Code.Should().Be(0, shown.Error);
            shown.Output.Should().Contain("editor-reader = \"mtipc\" # saved");
            shown.Output.Should().Contain("make-backups = true # built-in");
            new GlobalConfiguration(configuration).Load()["editor-reader"]!.Value<string>().Should().Be("mtipc");
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [TestMethod]
    public async Task RunAsync_ExplicitFileSelection_CreatesTheChosenConfigurationWithoutWritingTheDefault()
    {
        // Arrange
        string directory = CreateDirectory();
        string defaultConfiguration = Path.Combine(directory, "default.toml");
        string selectedConfiguration = Path.Combine(directory, "selected.toml");

        try
        {
            // Act
            var saved = await RunAsync(defaultConfiguration,
                "--config", selectedConfiguration, "config", "set", "editor-reader", "disabled");
            var read = await RunAsync(defaultConfiguration,
                "config", "get", "editor-reader", "--config", selectedConfiguration);

            // Assert
            saved.Code.Should().Be(0, saved.Error);
            read.Code.Should().Be(0, read.Error);
            read.Output.Trim().Should().Be("\"disabled\"");
            File.Exists(defaultConfiguration).Should().BeFalse();
            File.Exists(selectedConfiguration).Should().BeTrue();
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [TestMethod]
    [DataRow("current", "true")]
    [DataRow("selection", "everything")]
    [DataRow("reload-editor", "true")]
    [DataRow("overwrite", "true")]
    [DataRow("live", "disk")]
    [DataRow("editor-reader", "typo")]
    [DataRow("make-backups", "'false'")]
    public async Task RunAsync_InvalidSavedSetting_PreservesExistingConfiguration(string key, string value)
    {
        // Arrange
        string directory = CreateDirectory();
        string configuration = Path.Combine(directory, "cli.toml");
        const string original = "max-backups = 12\n";
        await File.WriteAllTextAsync(configuration, original);

        try
        {
            // Act
            var result = await RunAsync(configuration, "config", "set", key, value);

            // Assert
            result.Code.Should().Be(2);
            result.Error.Should().Contain(key);
            (await File.ReadAllTextAsync(configuration)).Should().Be(original);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [TestMethod]
    public async Task RunAsync_UnsetAndReset_RestoresBuiltInDefaults()
    {
        // Arrange
        string directory = CreateDirectory();
        string configuration = Path.Combine(directory, "cli.toml");
        await File.WriteAllTextAsync(configuration, "max-backups = 12\nmake-backups = false\n");

        try
        {
            // Act
            var unset = await RunAsync(configuration, "config", "unset", "max-backups");
            var restored = await RunAsync(configuration, "config", "get", "max-backups");
            var reset = await RunAsync(configuration, "config", "reset");
            var backups = await RunAsync(configuration, "config", "get", "make-backups");

            // Assert
            unset.Code.Should().Be(0, unset.Error);
            restored.Output.Trim().Should().Be("1000");
            reset.Code.Should().Be(0, reset.Error);
            backups.Output.Trim().Should().Be("true");
            new GlobalConfiguration(configuration).Load().Properties().Should().BeEmpty();
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [TestMethod]
    public async Task RunAsync_InvalidConfiguration_BlocksToolsButAllowsHelpPathBypassAndReset()
    {
        // Arrange
        string directory = CreateDirectory();
        string configuration = Path.Combine(directory, "cli.toml");
        string project = Path.Combine(directory, "project.json");
        await File.WriteAllTextAsync(configuration, "reload-editor = true\n");

        try
        {
            // Act
            var rejected = await RunAsync(configuration, "map-cleaner", "--save-project", project, "--no-run");
            bool writtenBeforeBypass = File.Exists(project);
            var help = await RunAsync(configuration, "--config", configuration, "--help");
            var path = await RunAsync(configuration, "config", "path");
            var bypass = await RunAsync(configuration, "map-cleaner", "--no-config", "--save-project", project, "--no-run");
            var reset = await RunAsync(configuration, "config", "reset");

            // Assert
            rejected.Code.Should().Be(1);
            rejected.Error.Should().Contain("reload-editor").And.Contain(configuration);
            writtenBeforeBypass.Should().BeFalse();
            help.Code.Should().Be(0, help.Error);
            path.Output.Trim().Should().Be(configuration);
            bypass.Code.Should().Be(0, bypass.Error);
            File.Exists(project).Should().BeTrue();
            reset.Code.Should().Be(0, reset.Error);
            new GlobalConfiguration(configuration).Load().Properties().Should().BeEmpty();
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [TestMethod]
    public async Task RunAsync_SavedBackupPreference_IsUsedByToolsAndCanBeOverriddenWithoutChangingTheFile()
    {
        // Arrange
        string directory = CreateDirectory();
        string configuration = Path.Combine(directory, "cli.toml");
        string target = Path.Combine(directory, "map.osu");
        string backups = Path.Combine(directory, "backups");
        File.Copy(Path.Combine(AppContext.BaseDirectory, "Fixtures", "standard-autofail-2b.osu"), target);
        await RunAsync(configuration, "config", "set", "make-backups", "false");
        await RunAsync(configuration, "config", "set", "backup-path", backups);
        await RunAsync(configuration, "config", "set", "editor-reader", "disabled");
        string saved = await File.ReadAllTextAsync(configuration);

        try
        {
            // Act
            var inherited = await RunAsync(configuration, "map-cleaner", target);
            string[] backupsBeforeOverride = Directory.GetFiles(backups, "*", SearchOption.AllDirectories);
            var overridden = await RunAsync(configuration, "map-cleaner", target, "--make-backups");

            // Assert
            inherited.Code.Should().Be(0, inherited.Error);
            backupsBeforeOverride.Should().BeEmpty();
            overridden.Code.Should().Be(0, overridden.Error);
            Directory.EnumerateFiles(backups).Should().NotBeEmpty();
            (await File.ReadAllTextAsync(configuration)).Should().Be(saved);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [TestMethod]
    public async Task RunAsync_MissingExplicitConfiguration_FailsBeforeSavingAToolProject()
    {
        // Arrange
        string directory = CreateDirectory();
        string configuration = Path.Combine(directory, "missing.toml");
        string project = Path.Combine(directory, "project.json");

        try
        {
            // Act
            var result = await RunAsync(configuration,
                "map-cleaner", "--config", configuration, "--save-project", project, "--no-run");

            // Assert
            result.Code.Should().Be(1);
            File.Exists(project).Should().BeFalse();
            File.Exists(configuration).Should().BeFalse();
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    private static string CreateDirectory()
    {
        string directory = Path.Combine(Path.GetTempPath(), "mapping-tools-config-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);

        return directory;
    }

    private static async Task<(int Code, string Output, string Error)> RunAsync(string configurationPath, params string[] args)
    {
        StringWriter output = new();
        StringWriter error = new();
        CliRunner runner = new(CliRunnerTests.CreateConsole(output), error, new StringReader(""));

        if (!args.Contains("--config") && !args.Contains("--no-config"))
            args = ["--config", configurationPath, .. args];

        int code = await runner.RunAsync(args);

        return (code, output.ToString(), error.ToString());
    }
}
