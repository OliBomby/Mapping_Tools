using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.Settings.Models;
using Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObject;
using Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObjectGenerators.Generators;
using Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObjectGenerators.GeneratorSettingses;
using Mapping_Tools.Core.Tools.GeometryDashboard.Serialization;
using Mapping_Tools.Infrastructure.Projects;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Infrastructure.Tests.Tools.GeometryDashboard.Projects;

[TestClass]
public sealed class GeometryDashboardProjectPersistenceTests
{
    [TestMethod]
    public void DeserializeAndSerialize_LegacyGeometryDashboardProject_PreservesSettingsAndTypeNames()
    {
        // Arrange
        string fixture = Path.Combine(AppContext.BaseDirectory, "Fixtures", "Projects", "geometrydashboardproject.json");
        LegacyProjectJsonSerializer serializer = new();

        // Act
        var project = serializer.Deserialize<GeometryDashboardEngineOptions>(File.ReadAllText(fixture));
        string json = serializer.Serialize(project);

        // Assert
        project.CurrentPreferences.VisiblePlayfieldBoundary.Should().BeTrue();
        project.CurrentPreferences.UpdateMode.Should().Be(UpdateMode.HotkeyDown);
        project.CurrentPreferences.GeneratorSettings.Should().ContainKey(typeof(AnchorPointGenerator));
        project.CurrentPreferences.GeneratorSettings[typeof(SymmetryGenerator)]
            .Should().BeOfType<SymmetryGeneratorSettings>();
        ((SymmetryGeneratorSettings)project.CurrentPreferences.GeneratorSettings[typeof(SymmetryGenerator)])
            .AxisInputPredicate.Predicates.Should().ContainSingle(predicate => predicate.NeedSelected);
        project.CurrentPreferences.RelevantObjectPreferences["Virtual point preferences"].Color
            .Should().Be(RgbaColour.FromArgb(255, 0, 255, 255));
        project.SaveSlots.Should().ContainSingle(slot => slot.Name == "Save 1");
        json.Should().Contain("Mapping_Tools.Classes.Tools.GeometryDashboard.Serialization.GeometryDashboardEngineOptions, Mapping Tools");
        json.Should().Contain("\"$type\": \"Mapping_Tools.Classes.SystemTools.Hotkey, Mapping Tools\"");
        json.Should().Contain("Mapping_Tools.Classes.Tools.GeometryDashboard.DataStructure.RelevantObjectGenerators.Generators.AnchorPointGenerator, Mapping Tools");
        json.Should().Contain("Mapping_Tools.Classes.Tools.GeometryDashboard.DataStructure.RelevantObjectGenerators.GeneratorSettingses.SymmetryGeneratorSettings, Mapping Tools");
        json.Should().Contain("\"Color\": \"#FF00FFFF\"");
    }

    [TestMethod]
    public void GeometryDashboardProject_SaveAndLoadSlot_UsesIndependentPreferencesSnapshot()
    {
        // Arrange
        GeometryDashboardEngineOptions project = new();
        GeometryDashboardSaveSlot slot = new() { Name = "Test" };
        project.SaveSlots.Add(slot);
        project.CurrentPreferences.AcceptableDifference = 70.1;

        // Act
        project.SaveToSlot(slot);
        project.CurrentPreferences.AcceptableDifference = 2;
        project.CurrentPreferences.SnapHotkey = new HotkeySettings(1, 0);
        project.LoadFromSlot(slot);

        // Assert
        project.CurrentPreferences.AcceptableDifference.Should().Be(70.1);
        project.CurrentPreferences.SnapHotkey!.Key.Should().Be(56);
    }

    [TestMethod]
    public void Deserialize_LegacyGeometryDashboardProjectWithAssemblyVersionMetadata_UsesTheSameCompatibilityAliases()
    {
        // Arrange
        string fixture = Path.Combine(AppContext.BaseDirectory, "Fixtures", "Projects", "geometrydashboardproject.json");
        string versionedJson = File.ReadAllText(fixture).Replace(
            ", Mapping Tools\"",
            ", Mapping Tools, Version=99.0.0.0, Culture=neutral, PublicKeyToken=null\"",
            StringComparison.Ordinal);
        LegacyProjectJsonSerializer serializer = new();

        // Act
        var project = serializer.Deserialize<GeometryDashboardEngineOptions>(versionedJson);

        // Assert
        project.CurrentPreferences.RelevantObjectPreferences.Should().ContainKey(RelevantPoint.PreferencesNameStatic);
        project.CurrentPreferences.GeneratorSettings.Should().ContainKey(typeof(AnchorPointGenerator));
    }

    [TestMethod]
    public void Deserialize_IntermediateCoreGeometryDashboardProject_UsesCurrentNamespaceFallbacks()
    {
        // Arrange
        string fixture = Path.Combine(AppContext.BaseDirectory, "Fixtures", "Projects", "geometrydashboardproject.json");
        string intermediateJson = File.ReadAllText(fixture)
            .Replace("Mapping_Tools.Classes", "Mapping_Tools.Core.Classes", StringComparison.Ordinal)
            .Replace("Mapping Tools", "Mapping_Tools.Core", StringComparison.Ordinal);
        LegacyProjectJsonSerializer serializer = new();

        // Act
        var project = serializer.Deserialize<GeometryDashboardEngineOptions>(intermediateJson);

        // Assert
        project.CurrentPreferences.GeneratorSettings.Should().ContainKey(typeof(SymmetryGenerator));
        project.SaveSlots.Should().Contain(slot => slot.Name == "Save 1");
    }

    [TestMethod]
    public void Deserialize_WithWaveZeroGeometryDashboardFixture_PreservesPreferences()
    {
        // Arrange
        string fixture = Path.Combine(
            AppContext.BaseDirectory,
            "Fixtures",
            "Projects",
            "geometrydashboardlegacyproject.json");
        LegacyProjectJsonSerializer serializer = new();

        // Act
        var project = serializer.Deserialize<GeometryDashboardEngineOptions>(File.ReadAllText(fixture));

        // Assert
        project.CurrentPreferences.InceptionLevel.Should().Be(5);
        project.CurrentPreferences.RelevantObjectPreferences.Should().HaveCount(3);
        project.CurrentPreferences.RelevantObjectPreferences.Should().ContainKey("Virtual point preferences");
    }
}
