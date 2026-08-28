using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
using Mapping_Tools.Application.BeatmapEditing;
using Mapping_Tools.Application.BeatmapEditing.Contracts;
using Mapping_Tools.Application.BeatmapEditing.Models;
using Mapping_Tools.Application.Platform;
using Mapping_Tools.Application.Projects.Contracts;
using Mapping_Tools.Application.Settings.Models;
using Mapping_Tools.Application.Tests.TestDoubles;
using Mapping_Tools.Application.Tools.AutoFail;
using Mapping_Tools.Application.Tools.ComboColourStudio;
using Mapping_Tools.Application.Tools.HitsoundCopier;
using Mapping_Tools.Application.Tools.HitsoundPreviewHelper;
using Mapping_Tools.Application.Tools.HitsoundStudio;
using Mapping_Tools.Application.Tools.HitsoundStudio.Models;
using Mapping_Tools.Application.Tools.MapCleaner;
using Mapping_Tools.Application.Tools.MapsetMerger;
using Mapping_Tools.Application.Tools.MapsetMerger.Models;
using Mapping_Tools.Application.Tools.MetadataManager;
using Mapping_Tools.Application.Tools.PatternGallery;
using Mapping_Tools.Application.Tools.PatternGallery.Models;
using Mapping_Tools.Application.Tools.PropertyTransformer;
using Mapping_Tools.Application.Tools.RhythmGuide;
using Mapping_Tools.Application.Tools.Sliderator;
using Mapping_Tools.Application.Tools.Sliderator.Models;
using Mapping_Tools.Application.Tools.SliderCompletionator;
using Mapping_Tools.Application.Tools.SliderMerger;
using Mapping_Tools.Application.Tools.SliderPicturator;
using Mapping_Tools.Application.Tools.TimingCopier;
using Mapping_Tools.Application.Tools.TimingHelper;
using Mapping_Tools.Application.Tools.TumourGenerator;
using Mapping_Tools.Application.Tools.TumourGenerator.Models;
using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.HitsoundStuff;
using Mapping_Tools.Core.Tools.AutoFail.Models;
using Mapping_Tools.Core.Tools.ComboColourStudio.Models;
using Mapping_Tools.Core.Tools.HitsoundCopier.Models;
using Mapping_Tools.Core.Tools.HitsoundStudio;
using Mapping_Tools.Core.Tools.PatternGallery.Models;
using Mapping_Tools.Core.Tools.Sliderator;
using Mapping_Tools.Infrastructure.Audio;
using Mapping_Tools.Infrastructure.Files;
using Mapping_Tools.Infrastructure.Projects;
using Mapping_Tools.Infrastructure.Tools.PatternGallery;
using Mapping_Tools.Infrastructure.Tools.SliderPicturator;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;

namespace Mapping_Tools.Application.Tests.Execution;

[TestClass]
public sealed class TransformationFixtureTests
{
    private static readonly IProjectSerializer projectJson = new LegacyProjectJsonSerializer();

    [DataTestMethod]
    [DataRow("auto-fail-detector", "Auto-fail Detector")]
    [DataRow("combo-colour-studio", "Combo Colour Studio")]
    [DataRow("hitsound-copier", "Hitsound Copier")]
    [DataRow("hitsound-preview", "Hitsound Preview Helper")]
    [DataRow("hitsound-studio", "Hitsound Studio")]
    [DataRow("map-cleaner", "Map Cleaner")]
    [DataRow("mapset-merger", "Mapset Merger")]
    [DataRow("metadata-manager", "Metadata Manager")]
    [DataRow("pattern-gallery", "Pattern Gallery")]
    [DataRow("property-transformer", "Property Transformer")]
    [DataRow("rhythm-guide", "Rhythm Guide")]
    [DataRow("slider-completionator", "Slider Completionator")]
    [DataRow("slider-merger", "Slider Merger")]
    [DataRow("slider-picturator", "Slider Picturator")]
    [DataRow("sliderator", "Sliderator")]
    [DataRow("timing-copier", "Timing Copier")]
    [DataRow("timing-helper", "Timing Helper")]
    [DataRow("tumour-generator", "Tumour Generator 2")]
    public async Task ExecuteFixture_ProducesEquivalentToolOutput(string fixtureName, string feature)
    {
        // Arrange
        using FixtureWorkspace workspace = new(Path.Combine(AppContext.BaseDirectory, "Fixtures"));
        string fixtureRoot = Path.Combine(workspace.Root, "Transformations");
        string recordPath = Path.Combine(fixtureRoot, $"{fixtureName}.json");
        string optionsPath = Path.Combine(fixtureRoot, $"{fixtureName}.options.json");
        using var record = JsonDocument.Parse(File.ReadAllText(recordPath));
        using var options = JsonDocument.Parse(File.ReadAllText(optionsPath));
        var recordRoot = record.RootElement;
        var optionsRoot = options.RootElement;
        string expectedOutputPath = ResolveFixturePath(fixtureRoot, StringProperty(recordRoot, "expectedOutput"));
        string seedInput = ResolveFixturePath(fixtureRoot, StringProperty(recordRoot, "seedInput"));
        string? secondaryInput = OptionalStringProperty(recordRoot, "secondaryInput") is { } secondary
            ? ResolveFixturePath(fixtureRoot, secondary)
            : null;
        optionsRoot.ValueKind.Should().Be(JsonValueKind.Object);
        File.ReadAllBytes(seedInput).Should().NotBeEmpty();
        if (secondaryInput is not null) File.ReadAllBytes(secondaryInput).Should().NotBeEmpty();
        File.ReadAllText(Path.Combine(fixtureRoot, $"{fixtureName}-report.md")).Should().NotBeNullOrWhiteSpace();
        File.ReadAllText(expectedOutputPath).Should().NotBeNullOrWhiteSpace();
        FileBackedEditingGateway gateway = new();

        // Act
        var actual = await ExecuteFixtureAsync(
            fixtureName, fixtureRoot, seedInput, optionsRoot, gateway, CancellationToken.None);

        // Assert
        recordRoot.GetProperty("feature").GetString().Should().Be(feature);
        recordRoot.GetProperty("status").GetString().Should().Be("accepted");
        actual.WasExecuted.Should().BeTrue();
        if (fixtureName == "auto-fail-detector")
        {
            JsonNode.DeepEquals(
                    JsonNode.Parse(File.ReadAllText(expectedOutputPath)),
                    JsonNode.Parse(actual.JsonOutput!))
                .Should().BeTrue();
        }
        else if (fixtureName == "mapset-merger")
        {
            AssertMapsetOutputEquivalent(fixtureRoot, expectedOutputPath, actual.OutputDirectory!);
        }
        else
        {
            actual.OutputPaths.Should().ContainSingle();
            AssertTextOutputEquivalent(expectedOutputPath, actual.OutputPaths.Single());
        }
    }

    private static async Task<FixtureExecutionResult> ExecuteFixtureAsync(
        string fixtureName,
        string fixtureRoot,
        string seedInput,
        JsonElement options,
        FileBackedEditingGateway gateway,
        CancellationToken cancellationToken)
    {
        string target = OptionalPath(options, "Target", fixtureRoot)
                        ?? OptionalPath(options, "BaseBeatmap", fixtureRoot) ?? OptionalPath(options, "ExportPath", fixtureRoot) ?? seedInput;
        switch (fixtureName)
        {
            case "auto-fail-detector":
            {
                AutoFailServiceOptions autoFailOptions = new(
                    target,
                    NumberProperty(options, "ApproachRateOverride"),
                    NumberProperty(options, "OverallDifficultyOverride"),
                    IntProperty(options, "PhysicsUpdateLeniency"));
                AutoFailService service = new(gateway);
                var positive = await service.AnalyzeAsync(autoFailOptions, cancellationToken);
                var negative = await service.AnalyzeAsync(
                    autoFailOptions with { Path = RequiredPath(options, "NegativeControl", fixtureRoot) },
                    cancellationToken);
                return new FixtureExecutionResult(
                    JsonOutput: JsonSerializer.Serialize(new
                    {
                        autoFailDetected = positive.Analysis.HasAutoFail,
                        unloadingObjects = positive.Analysis.UnloadingObjects.Count,
                        potentialUnloadingObjects = positive.Analysis.PotentialUnloadingObjects.Count,
                        message = AutoFailMessage(positive.Analysis),
                        negativeControlMessage = AutoFailMessage(negative.Analysis),
                    }));
            }
            case "combo-colour-studio":
                await new ComboColourStudioService(gateway).ApplyAsync(
                    [target],
                    ReadTransformationProject<ComboColourServiceOptions>(fixtureRoot, fixtureName),
                    cancellationToken: cancellationToken);
                return new FixtureExecutionResult([target]);
            case "hitsound-copier":
                await new HitsoundCopierService(
                        gateway,
                        new EmptyHitsoundSampleService(),
                        new ApplicationSettings { AutoReload = false })
                    .CopyAsync(
                        ReadTransformationProject<HitsoundCopierServiceOptions>(fixtureRoot, fixtureName),
                        cancellationToken: cancellationToken);
                return new FixtureExecutionResult([target]);
            case "hitsound-preview":
                await new HitsoundPreviewHelperService(gateway).ApplyAsync(
                    [target],
                    ReadTransformationProject<HitsoundPreviewHelperServiceOptions>(fixtureRoot, fixtureName),
                    cancellationToken: cancellationToken);
                return new FixtureExecutionResult([target]);
            case "hitsound-studio":
            {
                var project = ReadTransformationProject<HitsoundStudioServiceOptions>(
                    fixtureRoot,
                    fixtureName);
                project.ExportFolder = Path.Combine(Path.GetDirectoryName(target)!, "hitsound-studio-export");
                var result = await CreateHitsoundStudioService(gateway)
                    .ExportAsync(project, cancellationToken: cancellationToken);
                return new FixtureExecutionResult([result.MapPath!]);
            }
            case "map-cleaner":
            {
                var project = ReadTransformationProject<MapCleanerServiceOptions>(fixtureRoot, fixtureName);
                await new MapCleanerService(
                        gateway, new PhysicalBeatmapsetFileSystem(), new EmptyMapCleanerSampleService())
                    .CleanAsync([target], project.MapCleanerArgs, cancellationToken: cancellationToken);
                return new FixtureExecutionResult([target]);
            }
            case "mapset-merger":
            {
                var project = ReadTransformationProject<MapsetMergerServiceOptions>(
                    fixtureRoot,
                    fixtureName);
                StageMapsetMergerSources(options, fixtureRoot, project);
                await new MapsetMergerService(
                        gateway, new PhysicalBeatmapsetFileSystem())
                    .MergeAsync(
                        project,
                        cancellationToken: cancellationToken);
                return new FixtureExecutionResult(OutputDirectory: project.ExportPath);
            }
            case "metadata-manager":
            {
                var result = await new MetadataManagerService(
                        gateway, new TestBeatmapBackupService())
                    .ExportAsync(
                        ReadTransformationProject<MetadataManagerServiceOptions>(fixtureRoot, fixtureName),
                        cancellationToken: cancellationToken);
                return new FixtureExecutionResult(result.ProcessedPaths);
            }
            case "pattern-gallery":
            {
                var project = ReadTransformationProject<PatternGalleryServiceOptions>(
                    fixtureRoot,
                    fixtureName);
                var paths = ReadPatternGalleryPaths(options, fixtureRoot);
                var pattern = project.Patterns.Single(item =>
                    item.Name.Equals(StringProperty(options, "Pattern"), StringComparison.Ordinal));
                await new PatternGalleryService(gateway, new PatternGalleryFileService())
                    .ExportAsync(target, [pattern], project, paths, false, cancellationToken: cancellationToken);
                return new FixtureExecutionResult([target]);
            }
            case "property-transformer":
                await new PropertyTransformerService(gateway).TransformAsync(
                    [target],
                    ReadTransformationProject<PropertyTransformerServiceOptions>(fixtureRoot, fixtureName),
                    cancellationToken: cancellationToken);
                return new FixtureExecutionResult([target]);
            case "rhythm-guide":
            {
                var rhythmOptions = ReadTransformationProject<RhythmGuideServiceOptions>(
                    fixtureRoot,
                    fixtureName).GuideGeneratorArgs;
                await new RhythmGuideService(
                        gateway, new TestBeatmapBackupService(),
                        new PhysicalBeatmapsetFileSystem(), new PhysicalBeatmapsetFileSystem())
                    .GenerateAsync(rhythmOptions, cancellationToken);
                return new FixtureExecutionResult([rhythmOptions.ExportPath]);
            }
            case "slider-completionator":
                await new SliderCompletionatorService(gateway).CompleteAsync(
                    [target],
                    ReadTransformationProject<SliderCompletionatorServiceOptions>(fixtureRoot, fixtureName),
                    cancellationToken: cancellationToken);
                return new FixtureExecutionResult([target]);
            case "slider-merger":
                await new SliderMergerService(gateway).MergeAsync(
                    [target],
                    ReadTransformationProject<SliderMergerServiceOptions>(fixtureRoot, fixtureName),
                    cancellationToken: cancellationToken);
                return new FixtureExecutionResult([target]);
            case "slider-picturator":
                await new SliderPicturatorService(gateway, new SkiaSharpImageFileService())
                    .PicturateAsync(
                        target,
                        ReadTransformationProject<SliderPicturatorServiceOptions>(fixtureRoot, fixtureName),
                        cancellationToken: cancellationToken);
                return new FixtureExecutionResult([target]);
            case "sliderator":
            {
                var project = ReadTransformationProject<SlideratorServiceOptions>(fixtureRoot, fixtureName);
                var sourceSlider = ReadLegacySlider(fixtureRoot, fixtureName);
                ApplySlideratorTransientState(project, sourceSlider);
                await new SlideratorService(gateway).RunAsync(
                    target, project, sourceSlider, false, cancellationToken: cancellationToken);
                return new FixtureExecutionResult([target]);
            }
            case "timing-copier":
                await new TimingCopierService(gateway).CopyAsync(
                    ReadTransformationProject<TimingCopierServiceOptions>(fixtureRoot, fixtureName),
                    cancellationToken: cancellationToken);
                return new FixtureExecutionResult([target]);
            case "timing-helper":
                await new TimingHelperService(gateway).AdjustAsync(
                    [target],
                    ReadTransformationProject<TimingHelperServiceOptions>(fixtureRoot, fixtureName),
                    cancellationToken: cancellationToken);
                return new FixtureExecutionResult([target]);
            case "tumour-generator":
            {
                var project = ReadTransformationProject<TumourGeneratorServiceOptions>(
                    fixtureRoot,
                    fixtureName);
                project.TumourLayers = project.TumourLayers
                    .Take(IntProperty(options, "LayerCount"))
                    .ToList();
                await new TumourGeneratorService(gateway).RunAsync(
                    [target],
                    project,
                    false, cancellationToken: cancellationToken);
                return new FixtureExecutionResult([target]);
            }
            default:
                throw new ArgumentOutOfRangeException(nameof(fixtureName), fixtureName, "Unknown transformation fixture.");
        }
    }

    private static string AutoFailMessage(AutoFailAnalysis analysis)
    {
        return analysis.HasAutoFail
            ? $"{analysis.UnloadingObjects.Count} unloading objects detected and {analysis.PotentialUnloadingObjects.Count} potential unloading objects detected!"
            : "No auto-fail detected.";
    }

    private static HitsoundStudioService CreateHitsoundStudioService(FileBackedEditingGateway gateway)
    {
        NaudioAudioDecoder decoder = new();
        NaudioAudioGenerator generator = new(decoder, new NaudioSoundFontRenderer(), new NaudioAudioEffectService());
        return new HitsoundStudioService(
            gateway, new EmptyMapCleanerSampleService(), generator,
            new NaudioAudioExporter(), new NaudioAudioClipMixer(), new NaudioMidiService(),
            new PhysicalBeatmapsetFileSystem(), new NoopFileRevealService(), new HitsoundStudioEngine());
    }

    private static T ReadTransformationProject<T>(string fixtureRoot, string fixtureName)
    {
        string projectPath = Path.Combine(fixtureRoot, "Projects", $"{fixtureName}.json");
        string fixturesRoot = Path.GetDirectoryName(fixtureRoot)!;
        string json = File.ReadAllText(projectPath).Replace(
            "{fixtureRoot}",
            fixturesRoot.Replace('\\', '/'),
            StringComparison.Ordinal);
        return ReadProjectJson<T>(json);
    }

    private static HitObject ReadLegacySlider(string fixtureRoot, string fixtureName)
    {
        string projectPath = Path.Combine(fixtureRoot, "Projects", $"{fixtureName}.json");
        var document = JObject.Parse(File.ReadAllText(projectPath));
        string sliderJson = document["LoadedHitObjects"]?.SingleOrDefault()?.ToString()
                            ?? throw new InvalidDataException("The legacy Sliderator fixture contained no source slider.");
        return projectJson.Deserialize<HitObject>(sliderJson);
    }

    private static void ApplySlideratorTransientState(SlideratorServiceOptions project, HitObject sourceSlider)
    {
        // These values are JsonIgnore in the legacy project format. The desktop
        // view model derives them when it installs the loaded source slider.
        double temporalLength = sourceSlider.TemporalLength;
        double beatsPerMinute = sourceSlider.UnInheritedTimingPoint?.GetBpm() ?? 180;
        project.BeatsPerMinute = beatsPerMinute > 0 ? beatsPerMinute : 180;
        project.GraphBeats = project.BeatsPerMinute * temporalLength / 60000;
        project.PixelLength = sourceSlider.PixelLength;
        project.NewVelocity = SlideratorEngine.GetMaximumVelocity(project);
    }

    private static void StageMapsetMergerSources(
        JsonElement options,
        string fixtureRoot,
        MapsetMergerServiceOptions project)
    {
        string sourceRoot = ResolveFixturePath(fixtureRoot, "../Mapsets/multi-difficulty");
        foreach (var mapsetOptions in options.GetProperty("Mapsets").EnumerateArray())
        {
            string name = StringProperty(mapsetOptions, "Name");
            var mapset = project.Mapsets.Single(item => item.Name == name);
            Directory.CreateDirectory(mapset.Path);
            string beatmapPath = RequiredPath(
                mapsetOptions.GetProperty("Beatmaps").EnumerateArray().Single(),
                fixtureRoot);
            File.Copy(
                beatmapPath,
                Path.Combine(mapset.Path, Path.GetFileName(beatmapPath)),
                true);
            foreach (string asset in options.GetProperty("Assets").EnumerateArray().Select(item => item.GetString()!))
            {
                string sourceAsset = Path.Combine(sourceRoot, asset);
                if (File.Exists(sourceAsset)) File.Copy(sourceAsset, Path.Combine(mapset.Path, asset), true);
            }
        }
    }

    private static PatternGalleryCollectionPaths ReadPatternGalleryPaths(JsonElement options, string fixtureRoot)
    {
        string projectPath = RequiredPath(options, "PatternCollection", fixtureRoot);
        string patternFile = RequiredPath(options, "PatternFile", fixtureRoot);
        string collectionRoot = Path.GetDirectoryName(projectPath)!;
        return new PatternGalleryCollectionPaths(
            Path.GetDirectoryName(collectionRoot)!,
            collectionRoot,
            Path.GetDirectoryName(patternFile)!,
            projectPath);
    }

    private static T ReadProjectJson<T>(string json)
    {
        return projectJson.Deserialize<T>(json);
    }

    private static void AssertMapsetOutputEquivalent(string fixtureRoot, string expectedManifestPath, string actualDirectory)
    {
        using var manifest = JsonDocument.Parse(File.ReadAllText(expectedManifestPath));
        foreach (var item in manifest.RootElement.GetProperty("beatmaps").EnumerateArray())
        {
            string expectedPath = ResolveFixturePath(fixtureRoot, StringProperty(item, "path"));
            string actualPath = Path.Combine(actualDirectory, Path.GetFileName(expectedPath));
            AssertTextOutputEquivalent(expectedPath, actualPath);
        }

        foreach (var item in manifest.RootElement.GetProperty("exportedAssets").EnumerateArray())
        {
            string relativePath = StringProperty(item, "path");
            string actualPath = Path.Combine(actualDirectory, relativePath.Replace('/', Path.DirectorySeparatorChar));
            File.Exists(actualPath).Should().BeTrue($"Expected merged asset does not exist: {relativePath}");
            FileHash(actualPath).Should().Be(StringProperty(item, "sha256"));
        }
    }

    private static void AssertTextOutputEquivalent(string expectedPath, string actualPath)
    {
        File.Exists(actualPath).Should().BeTrue($"Tool did not write output: {actualPath}");
        string actual = File.ReadAllText(actualPath);
        string expected = File.ReadAllText(expectedPath);
        actual.Should().Be(expected);
    }

    private static string ResolveFixturePath(string fixtureRoot, string relativePath)
    {
        string path = Path.GetFullPath(Path.Combine(fixtureRoot, relativePath.Replace('/', Path.DirectorySeparatorChar)));
        (File.Exists(path) || Directory.Exists(path)).Should().BeTrue($"Fixture path does not exist: {relativePath}");
        return path;
    }

    private static string? OptionalPath(JsonElement element, string property, string fixtureRoot)
    {
        return OptionalStringProperty(element, property) is { } value ? ResolveFixturePath(fixtureRoot, value) : null;
    }

    private static string RequiredPath(JsonElement element, string property, string fixtureRoot)
    {
        return ResolveFixturePath(fixtureRoot, StringProperty(element, property));
    }

    private static string RequiredPath(JsonElement value, string fixtureRoot)
    {
        return ResolveFixturePath(fixtureRoot, value.GetString()!);
    }

    private static string StringProperty(JsonElement element, string property)
    {
        return element.GetProperty(property).GetString() ?? throw new InvalidDataException($"Fixture property {property} is null.");
    }

    private static string? OptionalStringProperty(JsonElement element, string property)
    {
        return element.TryGetProperty(property, out var value) && value.ValueKind != JsonValueKind.Null
            ? value.GetString()
            : null;
    }

    private static double NumberProperty(JsonElement element, string property)
    {
        return element.GetProperty(property).GetDouble();
    }

    private static int IntProperty(JsonElement element, string property)
    {
        return element.GetProperty(property).GetInt32();
    }

    private static bool BoolProperty(JsonElement element, string property)
    {
        return element.GetProperty(property).GetBoolean();
    }

    private static string FileHash(string path)
    {
        using var sha256 = SHA256.Create();
        return Convert.ToHexString(sha256.ComputeHash(File.ReadAllBytes(path)));
    }

    private sealed record FixtureExecutionResult(
        IReadOnlyList<string>? OutputPaths = null,
        string? JsonOutput = null,
        string? OutputDirectory = null)
    {
        public bool WasExecuted => OutputPaths is not null || JsonOutput is not null || OutputDirectory is not null;
    }

    private sealed class FixtureWorkspace : IDisposable
    {
        public FixtureWorkspace(string sourceRoot)
        {
            Root = Path.Combine(Path.GetTempPath(), "mapping-tools-transformations-" + Guid.NewGuid().ToString("N"));
            foreach (string sourcePath in Directory.EnumerateFiles(sourceRoot, "*", SearchOption.AllDirectories))
            {
                string destinationPath = Path.Combine(Root, Path.GetRelativePath(sourceRoot, sourcePath));
                Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
                File.Copy(sourcePath, destinationPath);
            }
        }

        public string Root { get; }

        public void Dispose()
        {
            if (Directory.Exists(Root)) Directory.Delete(Root, true);
        }
    }

    private sealed class FileBackedEditingGateway : IBeatmapEditingGateway
    {
        private readonly PhysicalBeatmapsetFileSystem files = new();

        public Task<BeatmapEditingSession> OpenBeatmapAsync(
            string path,
            LiveBeatmapPreference livePreference = LiveBeatmapPreference.PreferLive,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(new BeatmapEditingSession(
                new BeatmapEditor(path, files), BeatmapEditingSource.Disk, []));
        }

        public Task<StoryboardEditor> OpenStoryboardAsync(string path, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(new StoryboardEditor(path, files));
        }

        public Task SaveAsync(Editor value, bool reloadEditor = false, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            value.SaveFile();
            return Task.CompletedTask;
        }

        public Task SaveAsync(BeatmapEditingSession session, bool reloadEditor = false, CancellationToken cancellationToken = default)
        {
            return SaveAsync(session.Editor, reloadEditor, cancellationToken);
        }
    }

    private sealed class EmptyHitsoundSampleService : IHitsoundSampleService
    {
        public Task<IReadOnlyDictionary<string, string>> AnalyzeAsync(string directory, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyDictionary<string, string>>(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));
        }

        public HitsoundSampleAssignment? TryCreateAssignment(
            string directory, IReadOnlyList<string> sourceFilenames,
            IReadOnlyDictionary<string, string> firstSamples, string role,
            SampleSet sampleSet, int startIndex, SampleSchema existingSchema)
        {
            return null;
        }

        public Task ExportAsync(SampleSchema schema, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class EmptyMapCleanerSampleService : IMapCleanerSampleService
    {
        public Task<IReadOnlyDictionary<string, string>> AnalyzeAsync(
            string directory, bool detectDuplicates, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyDictionary<string, string>>(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));
        }

        public Task<int> MoveUnusedToRecoveryAsync(
            string directory, string currentBeatmapPath, Beatmap currentBeatmap,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(0);
        }
    }

    private sealed class NoopFileRevealService : IFileRevealService
    {
        public Task<bool> RevealAsync(string path, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(true);
        }
    }
}
