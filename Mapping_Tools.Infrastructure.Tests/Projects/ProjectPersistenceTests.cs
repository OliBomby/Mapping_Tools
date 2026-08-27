using System.Text.Json;
using Mapping_Tools.Application.Projects.Contracts;
using Mapping_Tools.Application.Tools.HitsoundCopier;
using Mapping_Tools.Application.Tools.HitsoundPreviewHelper;
using Mapping_Tools.Application.Tools.MapCleaner;
using Mapping_Tools.Application.Tools.MetadataManager;
using Mapping_Tools.Application.Tools.PropertyTransformer;
using Mapping_Tools.Application.Tools.RhythmGuide;
using Mapping_Tools.Application.Tools.Sliderator.Models;
using Mapping_Tools.Application.Tools.SliderCompletionator;
using Mapping_Tools.Application.Tools.SliderMerger;
using Mapping_Tools.Application.Tools.SliderPicturator;
using Mapping_Tools.Application.Tools.TimingCopier;
using Mapping_Tools.Application.Tools.TimingHelper;
using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.BeatmapHelper.BeatDivisors;
using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.HitsoundStuff;
using Mapping_Tools.Core.MathUtil;
using Mapping_Tools.Core.Tools.HitsoundCopier.Models;
using Mapping_Tools.Core.Tools.RhythmGuide.Models;
using Mapping_Tools.Core.Tools.TimingCopier.Models;
using Mapping_Tools.Infrastructure.Projects;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace Mapping_Tools.Infrastructure.Tests.Projects;

[TestClass]
public sealed class ProjectPersistenceTests
{
    [TestMethod]
    public void DeserializeAndSerialize_LegacySliderPicturatorProject_PreservesColorsAndTypeAlias()
    {
        // Arrange
        string fixture = Path.Combine(AppContext.BaseDirectory, "Fixtures", "Projects", "sliderpicturatorproject.json");
        LegacyProjectJsonSerializer serializer = new();

        // Act
        var project = serializer.Deserialize<SliderPicturatorServiceOptions>(File.ReadAllText(fixture));
        string json = serializer.Serialize(project);

        // Assert
        project.ViewportSize.Should().Be(32768);
        project.CurrentTrackColor.Should().Be(RgbaColour.FromRgb(0, 128, 255));
        json.Should().Contain("\"$type\": \"Mapping_Tools.Viewmodels.SliderPicturatorVm, Mapping Tools\"");
    }

    [TestMethod]
    public void DeserializeAndSerialize_LegacySlideratorProject_PreservesGraphAndDropsTransientState()
    {
        // Arrange
        string fixture = Path.Combine(
            AppContext.BaseDirectory,
            "Fixtures",
            "Projects",
            "slideratorproject.json");
        LegacyProjectJsonSerializer serializer = new();

        // Act
        var project = serializer.Deserialize<SlideratorServiceOptions>(File.ReadAllText(fixture));
        string json = serializer.Serialize(project);

        // Assert
        project.GlobalSv.Should().BeApproximately(2.1, 0.0001);
        project.BeatSnapDivisor.Should().Be(8);
        project.GraphState.Anchors.Should().HaveCount(3);
        project.GraphState.MaxX.Should().Be(16);
        json.Should().Contain(
            "\"$type\": \"Mapping_Tools.Viewmodels.SlideratorVm, Mapping Tools\"");
        json.Should().Contain(
            "\"$type\": \"Mapping_Tools.Components.Graph.GraphState, Mapping Tools\"");
        json.Should().NotContain("LoadedHitObjects");
        json.Should().NotContain("VisibleHitObjectIndex");
        json.Should().NotContain("DoEditorRead");
    }

    [TestMethod]
    public void Deserialize_WithLegacyCoreTypeMetadata_ResolvesMigratedAssembly()
    {
        // Arrange
        string fixture = Path.Combine(
            AppContext.BaseDirectory,
            "Fixtures",
            "Projects",
            "tumourgeneratorproject.json");
        using var document = JsonDocument.Parse(File.ReadAllText(fixture));
        string hitObjectJson = document.RootElement
            .GetProperty("PreviewHitObject")
            .GetRawText();
        LegacyProjectJsonSerializer serializer = new();

        // Act
        var hitObject = serializer.Deserialize<HitObject>(hitObjectJson);

        // Assert
        hitObject.Time.Should().Be(331598);
        hitObject.Pos.X.Should().Be(484);
        hitObject.Pos.Y.Should().Be(8);
    }

    [TestMethod]
    public void Serialize_WithMigratedCoreTypes_UsesLegacyAssemblyName()
    {
        // Arrange
        LegacyProjectJsonSerializer serializer = new();

        // Act
        string json = serializer.Serialize(new TimingPoint
        {
            Offset = 1250,
            MpB = 500,
        });

        // Assert
        json.Should().Contain("\"$type\": \"Mapping_Tools.Classes.BeatmapHelper.TimingPoint, Mapping Tools\"");
    }

    [TestMethod]
    public void SerializeAndDeserialize_MigratedCoreTypes_PreservesLegacyAliases()
    {
        // Arrange
        LegacyProjectJsonSerializer serializer = new();
        object[] values =
        {
            new TimingPoint { Offset = 1250, MpB = 500 },
            new RationalBeatDivisor(1, 4),
            new Sample(),
            new ComboColour(RgbaColour.FromArgb(0x7F, 0x12, 0x34, 0x56)),
            new HitObject("256,192,1000,1,2,0:0:0:0:"),
            new HitsoundZone(),
        };

        // Act
        Dictionary<object, (string Json, object RoundTrip)> results = values.ToDictionary(
            value => value,
            value =>
            {
                string json = serializer.Serialize(value);
                object roundTrip = serializer.Deserialize<object>(json);
                return (json, roundTrip);
            });

        // Assert
        foreach ((object value, (string json, object roundTrip)) in results)
        {
            string legacyTypeName = value.GetType().FullName!
                .Replace("Mapping_Tools.Core.", "Mapping_Tools.Classes.", StringComparison.Ordinal);

            json.Should().Contain($"\"$type\": \"{legacyTypeName}, Mapping Tools\"");
            roundTrip.Should().BeOfType(value.GetType());
        }
    }

    [TestMethod]
    public void DeserializeAndSerialize_LegacyRhythmGuideProject_PreservesTypeAliasesAndValues()
    {
        // Arrange
        string fixture = Path.Combine(
            AppContext.BaseDirectory,
            "Fixtures",
            "Projects",
            "rhythmguideproject.json");
        LegacyProjectJsonSerializer serializer = new();

        // Act
        var project = serializer.Deserialize<RhythmGuideServiceOptions>(
            File.ReadAllText(fixture));
        string json = serializer.Serialize(project);

        // Assert
        project.GuideGeneratorArgs.OutputGameMode.Should().Be(
            GameMode.Mania);
        project.GuideGeneratorArgs.OutputName.Should().Be("Hitsound Layers");
        project.GuideGeneratorArgs.SelectionMode.Should().Be(
            RhythmGuideSelectionMode.HitsoundEvents);
        project.GuideGeneratorArgs.BeatDivisors.Should().HaveCount(2);
        json.Should().Contain(
            "\"$type\": \"Mapping_Tools.Viewmodels.RhythmGuideVm, Mapping Tools\"");
        json.Should().Contain(
            "\"$type\": \"Mapping_Tools.Classes.Tools.RhythmGuide+RhythmGuideGeneratorArgs, Mapping Tools\"");
    }

    [TestMethod]
    public void DeserializeAndSerialize_LegacyHitsoundPreviewProject_PreservesZonesAndTypeAlias()
    {
        // Arrange
        LegacyProjectJsonSerializer serializer = new();
        const string json =
            "{\"$type\":\"Mapping_Tools.Viewmodels.HitsoundPreviewHelperVm, Mapping Tools\",\"Items\":[{\"$type\":\"Mapping_Tools.Core.Classes.HitsoundStuff.HitsoundZone, Mapping Tools\",\"Name\":\"kick\",\"Filename\":\"kick.wav\",\"XPos\":64.0,\"YPos\":96.0,\"Hitsound\":3,\"SampleSet\":3,\"AdditionsSet\":0,\"CustomIndex\":3}]}";

        // Act
        var project = serializer
            .Deserialize<HitsoundPreviewHelperServiceOptions>(json);
        string roundTrip = serializer.Serialize(project);

        // Assert
        project.Items.Should().ContainSingle();
        project.Items[0].Name.Should().Be("kick");
        project.Items[0].Filename.Should().Be("kick.wav");
        project.Items[0].Hitsound.Should().Be(Hitsound.Clap);
        project.Items[0].CustomIndex.Should().Be(3);
        roundTrip.Should().Contain(
            "\"$type\": \"Mapping_Tools.Viewmodels.HitsoundPreviewHelperVm, Mapping Tools\"");
    }

    [TestMethod]
    public void Deserialize_WithWaveZeroHitsoundPreviewFixture_RestoresAllZones()
    {
        // Arrange
        string fixture = Path.Combine(
            AppContext.BaseDirectory,
            "Fixtures",
            "Projects",
            "hspreviewproject.json");
        LegacyProjectJsonSerializer serializer = new();

        // Act
        var project = serializer.Deserialize<HitsoundPreviewHelperServiceOptions>(File.ReadAllText(fixture));

        // Assert
        project.Items.Should().HaveCount(18);
        project.Items.Select(item => item.Name).Should().Contain("SHEEEESSHHH");
        project.Items.Select(item => item.Filename).Should().Contain("electronic02");
    }

    [TestMethod]
    public void DeserializeAndSerialize_LegacyHitsoundCopierProject_PreservesOptionsAndTypeAlias()
    {
        // Arrange
        LegacyProjectJsonSerializer serializer = new();
        const string json =
            "{\"$type\":\"Mapping_Tools.Viewmodels.HitsoundCopierVm, Mapping Tools\",\"PathFrom\":\"source.osu\",\"PathTo\":\"target.osu\",\"CopyMode\":1,\"TemporalLeniency\":12,\"CopyStoryboardedSamples\":true}";

        // Act
        var project = serializer.Deserialize<HitsoundCopierServiceOptions>(json);
        string roundTrip = serializer.Serialize(project);

        // Assert
        project.PathFrom.Should().Be("source.osu");
        project.PathTo.Should().Be("target.osu");
        project.CopyMode.Should().Be(HitsoundCopierCopyMode.OverwriteOnlyDefined);
        project.TemporalLeniency.Should().Be(12);
        project.CopyStoryboardedSamples.Should().BeTrue();
        roundTrip.Should().Contain("\"CopyMode\": 1");
        roundTrip.Should().Contain(
            "\"$type\": \"Mapping_Tools.Viewmodels.HitsoundCopierVm, Mapping Tools\"");
    }

    [TestMethod]
    public void Deserialize_WithWaveZeroHitsoundCopierFixture_RestoresLegacyOptions()
    {
        // Arrange
        string fixture = Path.Combine(
            AppContext.BaseDirectory,
            "Fixtures",
            "Projects",
            "hitsoundcopierproject.json");
        LegacyProjectJsonSerializer serializer = new();

        // Act
        var project = serializer.Deserialize<HitsoundCopierServiceOptions>(File.ReadAllText(fixture));

        // Assert
        project.PathFrom.Should().Contain("Hitsounds");
        project.PathTo.Should().Contain("Rabbit Hole Collab");
        project.TemporalLeniency.Should().Be(5.5);
        project.CopyStoryboardedSamples.Should().BeTrue();
        project.BeatDivisors.Should().HaveCount(8);
    }

    [TestMethod]
    public void DeserializeAndSerialize_LegacyMapCleanerProject_PreservesTypeAliasesAndValues()
    {
        // Arrange
        string fixture = Path.Combine(AppContext.BaseDirectory, "Fixtures", "Projects", "mapcleanerproject.json");
        LegacyProjectJsonSerializer serializer = new();

        // Act
        var project = serializer.Deserialize<MapCleanerServiceOptions>(File.ReadAllText(fixture));
        string json = serializer.Serialize(project);

        // Assert
        project.MapCleanerArgs.ResnapObjects.Should().BeTrue();
        project.MapCleanerArgs.BeatDivisors.Should().HaveCount(2);
        json.Should().Contain("\"$type\": \"Mapping_Tools.Viewmodels.MapCleanerVm, Mapping Tools\"");
        json.Should().Contain("\"$type\": \"Mapping_Tools.Classes.Tools.MapCleanerStuff.MapCleanerArgs, Mapping Tools\"");
    }

    [TestMethod]
    public void DeserializeAndSerialize_LegacyMetadataManagerProject_PreservesTypeAliasAndValues()
    {
        // Arrange
        string fixture = Path.Combine(
            AppContext.BaseDirectory,
            "Fixtures",
            "Projects",
            "metadataproject.json");
        LegacyProjectJsonSerializer serializer = new();

        // Act
        var project = serializer.Deserialize<MetadataManagerServiceOptions>(
            File.ReadAllText(fixture));
        string json = serializer.Serialize(project);

        // Assert
        project.RomanisedArtist.Should().Be("Kou! & KASOKUKI:Collective");
        project.PreviewTime.Should().Be(315111);
        project.ComboColours.Should().HaveCount(4);
        json.Should().Contain(
            "\"$type\": \"Mapping_Tools.Viewmodels.MetadataManagerVm, Mapping Tools\"");
    }

    [TestMethod]
    public void DeserializeAndSerialize_LegacyPropertyTransformerProject_PreservesTypeAliasAndValues()
    {
        // Arrange
        string fixture = Path.Combine(
            AppContext.BaseDirectory,
            "Fixtures",
            "Projects",
            "propertytransformerproject.json");
        LegacyProjectJsonSerializer serializer = new();

        // Act
        var project = serializer.Deserialize<PropertyTransformerServiceOptions>(
            File.ReadAllText(fixture));
        string json = serializer.Serialize(project);

        // Assert
        project.TimingpointBpmMultiplier.Should().Be(0.5);
        project.MatchFilter.Length.Should().Be(1);
        project.MatchFilter[0].Should().Be(0);
        json.Should().Contain(
            "\"$type\": \"Mapping_Tools.Viewmodels.PropertyTransformerVm, Mapping Tools\"");
    }

    [TestMethod]
    public void DeserializeAndSerialize_LegacyTimingCopierProject_PreservesTypeAliasAndValues()
    {
        // Arrange
        string fixture = Path.Combine(
            AppContext.BaseDirectory,
            "Fixtures",
            "Projects",
            "timingcopierproject.json");
        LegacyProjectJsonSerializer serializer = new();

        // Act
        var project = serializer.Deserialize<TimingCopierServiceOptions>(
            File.ReadAllText(fixture));
        string json = serializer.Serialize(project);

        // Assert
        project.ResnapMode.Should().Be(TimingCopierResnapMode.Resnap);
        project.BeatDivisors.Should().HaveCount(2);
        json.Should().Contain("\"ResnapMode\": \"Just resnap\"");
        json.Should().Contain(
            "\"$type\": \"Mapping_Tools.Viewmodels.TimingCopierVm, Mapping Tools\"");
    }

    [TestMethod]
    public void DeserializeAndSerialize_LegacyTimingHelperProject_PreservesTypeAliasAndValues()
    {
        // Arrange
        string fixture = Path.Combine(
            AppContext.BaseDirectory,
            "Fixtures",
            "Projects",
            "timinghelperproject.json");
        LegacyProjectJsonSerializer serializer = new();

        // Act
        var project = serializer.Deserialize<TimingHelperServiceOptions>(
            File.ReadAllText(fixture));
        string json = serializer.Serialize(project);

        // Assert
        project.Leniency.Should().Be(10);
        project.BeatsBetween.Should().Be(1);
        project.BeatDivisors.Should().ContainSingle();
        json.Should().Contain(
            "\"$type\": \"Mapping_Tools.Viewmodels.TimingHelperVm, Mapping Tools\"");
    }

    [TestMethod]
    public void DeserializeAndSerialize_LegacySliderCompletionatorProject_PreservesTypeAliasAndValues()
    {
        // Arrange
        string fixture = Path.Combine(
            AppContext.BaseDirectory,
            "Fixtures",
            "Projects",
            "slidercompletionatorproject.json");
        LegacyProjectJsonSerializer serializer = new();

        // Act
        var project = serializer.Deserialize<SliderCompletionatorServiceOptions>(
            File.ReadAllText(fixture));
        string json = serializer.Serialize(project);

        // Assert
        project.UseEndTime.Should().BeTrue();
        project.UseCurrentEditorTime.Should().BeTrue();
        project.Length.Should().Be(1);
        json.Should().Contain(
            "\"$type\": \"Mapping_Tools.Viewmodels.SliderCompletionatorVm, Mapping Tools\"");
    }

    [TestMethod]
    public void DeserializeAndSerialize_LegacySliderMergerProject_PreservesTypeAliasAndValues()
    {
        // Arrange
        string fixture = Path.Combine(
            AppContext.BaseDirectory,
            "Fixtures",
            "Projects",
            "slidermergerproject.json");
        LegacyProjectJsonSerializer serializer = new();

        // Act
        var project = serializer.Deserialize<SliderMergerServiceOptions>(
            File.ReadAllText(fixture));
        string json = serializer.Serialize(project);

        // Assert
        project.ImportModeSetting.Should().Be(HitObjectSelectionMode.Selected);
        project.Leniency.Should().Be(999999);
        project.MergeOnSliderEnd.Should().BeTrue();
        json.Should().Contain(
            "\"$type\": \"Mapping_Tools.Viewmodels.SliderMergerVm, Mapping Tools\"");
    }

    [TestMethod]
    public void Deserialize_WithTransitionalCoreTypeMetadata_ResolvesMigratedAssembly()
    {
        // Arrange
        LegacyProjectJsonSerializer serializer = new();
        const string json =
            """{"$type":"Mapping_Tools.Core.Classes.BeatmapHelper.TimingPoint, Mapping Tools","Offset":1250.0,"MpB":500.0}""";

        // Act
        var timingPoint = serializer.Deserialize<TimingPoint>(json);

        // Assert
        timingPoint.Offset.Should().Be(1250);
        timingPoint.MpB.Should().Be(500);
    }

    [TestMethod]
    public void SerializeAndDeserialize_VectorDocument_MatchesLegacyRepresentation()
    {
        // Arrange
        LegacyProjectJsonSerializer serializer = new();
        VectorDocument original = new()
        {
            Position = new Vector2(12.5, -7.25),
        };

        // Act
        string json = serializer.Serialize(original);
        var roundTrip = serializer.Deserialize<VectorDocument>(json);

        // Assert
        json.Should().Contain("\"X\": 12.5");
        json.Should().Contain("\"Y\": -7.25");
        roundTrip.Position.Should().Be(original.Position);
    }

    [TestMethod]
    public void Deserialize_WithUnknownProperties_IgnoresThemForRoundTrip()
    {
        // Arrange
        LegacyProjectJsonSerializer serializer = new();

        // Act
        var project = serializer.Deserialize<SimpleDocument>(
            """{"Name":"known","FutureOption":{"Enabled":true}}""");

        // Assert
        project.Name.Should().Be("known");
    }

    [TestMethod]
    public void Deserialize_WithMalformedOrNullDocuments_Throws()
    {
        // Arrange
        LegacyProjectJsonSerializer serializer = new();

        // Act
        Action act1 = () => serializer.Deserialize<SimpleDocument>("{");

        // Assert
        act1.Should().Throw<JsonSerializationException>();
        Action act2 = () => serializer.Deserialize<SimpleDocument>("null");

        act2.Should().Throw<InvalidDataException>();
    }

    [TestMethod]
    public void Deserialize_WithCorruptProjectFixture_ThrowsJsonSerializationException()
    {
        // Arrange
        string fixture = Path.Combine(
            AppContext.BaseDirectory,
            "Fixtures",
            "Projects",
            "corrupt.json");
        LegacyProjectJsonSerializer serializer = new();

        // Act
        Action act = () => serializer.Deserialize<MapCleanerServiceOptions>(File.ReadAllText(fixture));

        // Assert
        act.Should().Throw<JsonSerializationException>();
    }

    [TestMethod]
    public async Task SaveAsync_RoundTrip_OverwritesAtomicallyWithoutTemporaryFiles()
    {
        // Arrange
        using TestDirectory test = new();
        FileSystemProjectStore store = new(new LegacyProjectJsonSerializer());
        string path = Path.Combine(test.Root, "nested", "project.json");
        SimpleDocument original = new() { Name = "persisted" };

        // Act
        await store.SaveAsync(path, original);
        await store.SaveAsync(path, new SimpleDocument { Name = "replaced" });
        var loaded = await store.LoadAsync<SimpleDocument>(path);

        // Assert
        loaded.Name.Should().Be("replaced");
        Directory.GetFiles(
            Path.GetDirectoryName(path)!,
            "*.tmp").Length.Should().Be(0);
    }

    [TestMethod]
    public async Task SaveAsync_WhenSerializationFails_PreservesExistingProject()
    {
        // Arrange
        using TestDirectory test = new();
        string path = Path.Combine(test.Root, "project.json");
        await File.WriteAllTextAsync(path, "previous");
        FileSystemProjectStore store = new(new ThrowingSerializer());

        // Act
        var act3 = () => store.SaveAsync(path, new SimpleDocument());

        // Assert
        await act3.Should().ThrowAsync<InvalidOperationException>();

        (await File.ReadAllTextAsync(path)).Should().Be("previous");
    }

    public sealed class VectorDocument
    {
        public Vector2 Position { get; set; }
    }

    public sealed class SimpleDocument
    {
        public string Name { get; set; } = "";
    }

    private sealed class ThrowingSerializer : IProjectSerializer
    {
        public string Serialize<TProject>(TProject project)
        {
            throw new InvalidOperationException("Fixture serialization failure.");
        }

        public TProject Deserialize<TProject>(string json)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class TestDirectory : IDisposable
    {
        public TestDirectory()
        {
            Root = Path.Combine(
                Path.GetTempPath(),
                "MappingToolsProjectTests",
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Root);
        }

        public string Root { get; }

        public void Dispose()
        {
            if (Directory.Exists(Root)) Directory.Delete(Root, true);
        }
    }
}
