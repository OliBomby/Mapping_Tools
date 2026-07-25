using System.Text.Json;
using Mapping_Tools.ApplicationServices.Projects;
using Mapping_Tools.Classes.BeatmapHelper;
using Mapping_Tools.Classes.MathUtil;
using Mapping_Tools.Infrastructure.Projects;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace Mapping_Tools.Platform.Tests;

[TestClass]
public sealed class ProjectPersistenceTests
{
    [TestMethod]
    public void Deserialize_WithLegacyCoreTypeMetadata_ResolvesMigratedAssembly()
    {
        // Arrange
        string fixture = Path.Combine(
            AppContext.BaseDirectory,
            "Fixtures",
            "Projects",
            "tumourgeneratorproject.json");
        using JsonDocument document = JsonDocument.Parse(File.ReadAllText(fixture));
        string hitObjectJson = document.RootElement
            .GetProperty("PreviewHitObject")
            .GetRawText();
        LegacyProjectJsonSerializer serializer = new();

        // Act
        HitObject hitObject = serializer.Deserialize<HitObject>(hitObjectJson);

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
            MpB = 500
        });

        // Assert
        json.Should().Contain("\"$type\": \"Mapping_Tools.Classes.BeatmapHelper.TimingPoint, Mapping Tools\"");
    }

    [TestMethod]
    public void SerializeAndDeserialize_VectorDocument_MatchesLegacyRepresentation()
    {
        // Arrange
        LegacyProjectJsonSerializer serializer = new();
        VectorDocument original = new()
        {
            Position = new Vector2(12.5, -7.25)
        };

        // Act
        string json = serializer.Serialize(original);
        VectorDocument roundTrip = serializer.Deserialize<VectorDocument>(json);

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
        SimpleDocument project = serializer.Deserialize<SimpleDocument>(
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
        SimpleDocument loaded = await store.LoadAsync<SimpleDocument>(path);

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
        Func<Task> act3 = () => store.SaveAsync(path, new SimpleDocument());

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
            if (Directory.Exists(Root))
            {
                Directory.Delete(Root, recursive: true);
            }
        }
    }
}
