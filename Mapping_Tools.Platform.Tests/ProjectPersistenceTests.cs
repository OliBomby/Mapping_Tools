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
    public void LegacyCoreTypeMetadataResolvesToMigratedAssembly()
    {
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

        HitObject hitObject = serializer.Deserialize<HitObject>(hitObjectJson);

        Assert.AreEqual(331598, hitObject.Time);
        Assert.AreEqual(484, hitObject.Pos.X);
        Assert.AreEqual(8, hitObject.Pos.Y);
    }

    [TestMethod]
    public void MigratedCoreTypesKeepLegacyAssemblyNameWhenSaved()
    {
        LegacyProjectJsonSerializer serializer = new();

        string json = serializer.Serialize(new TimingPoint
        {
            Offset = 1250,
            MpB = 500
        });

        StringAssert.Contains(
            json,
            "\"$type\": \"Mapping_Tools.Classes.BeatmapHelper.TimingPoint, Mapping Tools\"");
    }

    [TestMethod]
    public void VectorRepresentationMatchesLegacyProjects()
    {
        LegacyProjectJsonSerializer serializer = new();
        VectorDocument original = new()
        {
            Position = new Vector2(12.5, -7.25)
        };

        string json = serializer.Serialize(original);
        VectorDocument roundTrip = serializer.Deserialize<VectorDocument>(json);

        StringAssert.Contains(json, "\"X\": 12.5");
        StringAssert.Contains(json, "\"Y\": -7.25");
        Assert.AreEqual(original.Position, roundTrip.Position);
    }

    [TestMethod]
    public void UnknownPropertiesAreIgnoredForForwardCompatibleRoundTrips()
    {
        LegacyProjectJsonSerializer serializer = new();

        SimpleDocument project = serializer.Deserialize<SimpleDocument>(
            """{"Name":"known","FutureOption":{"Enabled":true}}""");

        Assert.AreEqual("known", project.Name);
    }

    [TestMethod]
    public void MalformedAndNullDocumentsAreRejected()
    {
        LegacyProjectJsonSerializer serializer = new();

        Assert.ThrowsException<JsonSerializationException>(
            () => serializer.Deserialize<SimpleDocument>("{"));
        Assert.ThrowsException<InvalidDataException>(
            () => serializer.Deserialize<SimpleDocument>("null"));
    }

    [TestMethod]
    public async Task StoreRoundTripOverwritesAtomicallyAndLeavesNoTemporaryFile()
    {
        using TestDirectory test = new();
        FileSystemProjectStore store = new(new LegacyProjectJsonSerializer());
        string path = Path.Combine(test.Root, "nested", "project.json");
        SimpleDocument original = new() { Name = "persisted" };

        await store.SaveAsync(path, original);
        await store.SaveAsync(path, new SimpleDocument { Name = "replaced" });
        SimpleDocument loaded = await store.LoadAsync<SimpleDocument>(path);

        Assert.AreEqual("replaced", loaded.Name);
        Assert.AreEqual(0, Directory.GetFiles(
            Path.GetDirectoryName(path)!,
            "*.tmp").Length);
    }

    [TestMethod]
    public async Task SerializationFailureDoesNotReplaceExistingProject()
    {
        using TestDirectory test = new();
        string path = Path.Combine(test.Root, "project.json");
        await File.WriteAllTextAsync(path, "previous");
        FileSystemProjectStore store = new(new ThrowingSerializer());

        await Assert.ThrowsExceptionAsync<InvalidOperationException>(
            () => store.SaveAsync(path, new SimpleDocument()));

        Assert.AreEqual("previous", await File.ReadAllTextAsync(path));
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
