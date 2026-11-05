using AwesomeAssertions;
using Mapping_Tools.Application.Tools.GeometryDashboard.Models;
using Mapping_Tools.Application.Tools.RhythmGuide;
using Mapping_Tools.Application.Tools.TumourGenerator.Models;
using Mapping_Tools.Core.Tools.Sliderator.Models;
using Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObjectGenerators.GeneratorSettingses;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Cli.Tests.Configuration;

[TestClass]
public sealed class OptionSchemaTests
{
    [TestMethod]
    [DataRow(-10.5)]
    [DataRow(0.0)]
    [DataRow(125.0)]
    public async Task RunAsync_NumericLayerGraphs_SavesFlatGraphsAndPreservesLayerDefaults(double value)
    {
        // Arrange
        string destination = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".json");
        string number = value.ToString(System.Globalization.CultureInfo.InvariantCulture);

        // Act
        var result = await CliRunnerTests.RunAsync("tumour-generator",
            "--layer", "{tumour-template-enum = \"circle\", tumour-length = " + number + ", tumour-scale = 64}",
            "--save-project", destination, "--no-run");

        // Assert
        try
        {
            result.Code.Should().Be(0, result.Error);
            var command = CliRunnerTests.CreateToolCommands().Single(command => command.Name == "tumour-generator");
            var project = (TumourGeneratorServiceOptions)command.Read(await File.ReadAllTextAsync(destination));
            var layer = project.TumourLayers.Single();
            layer.IsActive.Should().BeTrue();

            foreach (double position in new[] { 0.0, 0.5, 1.0 })
            {
                layer.TumourLength.GetValue(position).Should().Be(value);
                layer.TumourScale.GetValue(position).Should().Be(64);
            }

            layer.TumourLength.MinY.Should().BeLessThanOrEqualTo(value);
            layer.TumourLength.MaxY.Should().BeGreaterThanOrEqualTo(value);
        }
        finally
        {
            File.Delete(destination);
        }
    }

    [TestMethod]
    public async Task RunAsync_NumericTopLevelGraph_SavesFlatSlideratorGraph()
    {
        // Arrange
        string destination = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".json");

        // Act
        var result = await CliRunnerTests.RunAsync("sliderator", "--graph", "12.5",
            "--save-project", destination, "--no-run");

        // Assert
        try
        {
            result.Code.Should().Be(0, result.Error);
            var command = CliRunnerTests.CreateToolCommands().Single(command => command.Name == "sliderator");
            var project = (SlideratorEngineOptions)command.Read(await File.ReadAllTextAsync(destination));
            project.GraphState.GetValue(0).Should().Be(12.5);
            project.GraphState.GetValue(0.5).Should().Be(12.5);
            project.GraphState.GetValue(1).Should().Be(12.5);
        }
        finally
        {
            File.Delete(destination);
        }
    }

    [TestMethod]
    public async Task RunAsync_AdvancedGeometrySettings_UsesTypedGeneratorAndUpdatesActivePreferencesWithoutRunningOverlay()
    {
        // Arrange
        string destination = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".json");

        // Act
        var result = await CliRunnerTests.RunAsync(
            "geometry-dashboard",
            "--generator-settings",
            "{'scale-rotate' = {'$kind' = 'scale-rotate', is-active = true, scalar = 2, angle = 90}}",
            "--inception-level",
            "6",
            "--save-project",
            destination,
            "--no-run");

        // Assert
        try
        {
            result.Code.Should().Be(0, result.Error);

            var command = CliRunnerTests.CreateToolCommands().Single(command => command.Name == "geometry-dashboard");
            var project = (GeometryDashboardServiceOptions)command.Read(await File.ReadAllTextAsync(destination));
            project.CurrentPreferences.InceptionLevel.Should().Be(6);
            project.CurrentPreferences.GeneratorSettings.Values.Should().ContainSingle()
                .Which.Should().BeOfType<ScaleRotateGeneratorSettings>()
                .Which.Scalar.Should().Be(2);
        }
        finally
        {
            File.Delete(destination);
        }
    }

    [TestMethod]
    public async Task RunAsync_RationalAndIrrationalBeatDivisors_PreservesTypedValuesInProject()
    {
        // Arrange
        string destination = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".json");

        // Act
        var result = await CliRunnerTests.RunAsync(
            "rhythm-guide",
            "--beat-divisors",
            "[{numerator = 1, denominator = 7}, {value = 0.123}]",
            "--save-project",
            destination,
            "--no-run");

        // Assert
        try
        {
            result.Code.Should().Be(0, result.Error);

            var command = CliRunnerTests.CreateToolCommands().Single(command => command.Name == "rhythm-guide");
            var project = (RhythmGuideServiceOptions)command.Read(await File.ReadAllTextAsync(destination));
            project.GuideGeneratorArgs.BeatDivisors.Select(divisor => divisor.GetValue()).Should().Equal(1d / 7, 0.123);
        }
        finally
        {
            File.Delete(destination);
        }
    }
}
